using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // Preparing: 조립이 끝났다(진행 상태를 묶고 적 수치를 확정했다). 아직 적이 없고 시간이 흐르지 않는다.
    // Running / Paused: Begin 뒤. Ended: 결과가 확정됐다.
    public enum SessionPhase { Preparing, Running, Paused, Ended }

    // 판이 끝난 사유. 지금은 시간 종료 하나로 통일한다(돌아가기 요청 같은 사유는 그 흐름이 생길 때 더한다).
    public enum SessionEndReason { TimeExpired }

    // 판이 끝날 때 한 번 확정되는 결과. 이후 판 상태가 바뀌어도 변하지 않는 스냅샷이다.
    public sealed class SessionResult
    {
        public SessionEndReason Reason { get; }
        public float PlayedSeconds { get; }

        internal SessionResult(SessionEndReason reason, float playedSeconds)
        {
            Reason = reason;
            PlayedSeconds = playedSeconds;
        }
    }

    // 한 판의 상태(준비/진행/정지/종료), 경과 시간, 결과, 요청 허용 여부를 가진다.
    // 판 안의 대상과 한 단계의 처리 순서는 World가, 종료 판정은 TimeLimitRule이 가진다.
    // 재시작은 같은 객체의 부분 초기화가 아니라 새 조립이다(SessionAssembler).
    //
    // 수명: 조립(Preparing) → Begin(전투 시작 공급, Running) → 끝(Ended: 시간 종료 또는 종료 요청)
    //      → 남은 적 정리(처치 아님) → 원자료 만들기. 이 순서를 누가 언제 부를지는 판 바깥(오케스트레이터)이 정한다.
    public sealed class GameSession
    {
        private readonly IReadOnlyList<PlayerState> _players;
        private readonly IReadOnlyDictionary<PlayerId, UpgradeTable> _upgrades;
        private readonly IReadOnlyList<SupplyRequest> _startSupply;

        public World World { get; }
        public TimeLimitRule TimeLimit { get; }
        // 이 판을 조립한 진행도(적의 강도 단계)와 난수 seed. 같은 콘텐츠·단계·seed면 같은 판이 나온다.
        public int Stage { get; }
        public int Seed { get; }
        public SessionPhase Phase { get; private set; } = SessionPhase.Preparing;
        public float Elapsed { get; private set; }
        public float Remaining => TimeLimit.Remaining(Elapsed);
        // 판이 끝나기 전에는 null이다.
        public SessionResult Result { get; private set; }

        internal GameSession(
            World world,
            TimeLimitRule timeLimit,
            int stage,
            int seed,
            IReadOnlyList<PlayerState> players,
            IReadOnlyDictionary<PlayerId, UpgradeTable> upgrades,
            IReadOnlyList<SupplyRequest> startSupply)
        {
            World = world;
            TimeLimit = timeLimit;
            Stage = stage;
            Seed = seed;
            _players = players;
            _upgrades = upgrades;
            _startSupply = startSupply;
        }

        // 이 판에서 그 참가자가 받는 업그레이드 표. 판 조립 때 그 참가자의 산 노드로 한 번 만들어졌고, 판이 끝날 때까지 같다.
        // 표를 읽어 수치를 정하는 것은 각 시스템의 일이다(아직 읽는 시스템은 없다).
        public UpgradeTable UpgradesOf(PlayerId player)
        {
            if (!_upgrades.TryGetValue(player, out UpgradeTable table))
                throw new ArgumentException($"이 판의 참가자가 아니다: {player}.", nameof(player));

            return table;
        }

        // 그 참가자의 조준점을 바꾼다. 없으면 null. 호스트가 입력(지금은 마우스)을 읽어 프레임마다 넣는다.
        // 판의 단계와 관계없이 받는다. 스킬은 공격할 때의 조준점을 읽는다.
        public void SetAimPoint(PlayerId player, Point2? aimPoint) => World.PlayerOf(player).SetAimPoint(aimPoint);

        // 전투를 시작한다: 전투 시작 공급을 생성 요청으로 넣고 그 자리(0초)에서 공급 처리한 뒤 진행 단계로 들어간다.
        // 준비 단계에서 한 번만 부를 수 있다.
        public void Begin()
        {
            if (Phase != SessionPhase.Preparing)
                throw new InvalidOperationException($"준비 단계에서만 시작할 수 있다. 지금: {Phase}.");

            foreach (SupplyRequest request in _startSupply)
                World.RequestSpawn(request);

            World.ProcessSpawnRequests();
            Phase = SessionPhase.Running;
        }

        // 진행 중일 때만 시간이 흐른다. 제한 시간을 넘겨 진행하지 않는다.
        // 한 단계를 처리한 뒤 종료를 판정한다(GAME_RULES 13·14절). 끝난 판은 더 진행하지 않는다.
        public void Advance(float delta)
        {
            DefinitionGuard.Delta(delta);

            if (Phase != SessionPhase.Running || delta == 0)
                return;

            World.BeginAdvance();

            float step = TimeLimit.LimitStep(Elapsed, delta);
            World.Step(step);
            Elapsed += step;

            if (TimeLimit.TryEnd(Elapsed, out SessionEndReason reason))
                End(reason);
        }

        // 진행 ↔ 정지. 준비 중이거나 끝난 판에서는 아무 일도 없다.
        public void TogglePause()
        {
            if (Phase == SessionPhase.Running)
                Phase = SessionPhase.Paused;
            else if (Phase == SessionPhase.Paused)
                Phase = SessionPhase.Running;
        }

        // 판을 끝내라는 요청. 이미 끝난 판이면 처음 사유를 그대로 둔다.
        public void RequestEnd(SessionEndReason reason)
        {
            if (!Enum.IsDefined(typeof(SessionEndReason), reason))
                throw new ArgumentOutOfRangeException(nameof(reason));

            End(reason);
        }

        // 끝난 판에 남은 적과 처리되지 않은 생성·파괴 요청을 치운다. 처치가 아니다 — 사망 기록도, 처치 수도 없다(GAME_RULES 9절).
        // 치운 적의 수를 돌려준다.
        public int ClearRemainingEnemies()
        {
            RequireEnded();
            return World.ClearRemainingEnemies();
        }

        // 끝난 판의 원자료(조립 조건, 끝난 사유와 시간, 종류별 처치 수)를 만든다.
        public BattleRawData CreateRawData()
        {
            RequireEnded();
            return new BattleRawData(Stage, Seed, Result.Reason, Result.PlayedSeconds, World.Kills());
        }

        // 결과를 확정하고 진행 상태를 전투에서 풀어 준다.
        private void End(SessionEndReason reason)
        {
            if (Phase == SessionPhase.Ended)
                return;

            Result = new SessionResult(reason, Elapsed);
            Phase = SessionPhase.Ended;

            foreach (PlayerState player in _players)
            {
                player.LeaveBattle();
            }
        }

        private void RequireEnded()
        {
            if (Phase != SessionPhase.Ended)
                throw new InvalidOperationException($"끝난 판에서만 할 수 있다. 지금: {Phase}.");
        }
    }
}
