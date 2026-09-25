using System.Collections.Generic;

namespace BlackHole.Core
{
    public enum SessionPhase { Running, Paused, Ended }

    public enum SessionEndReason { TimeExpired, Stopped }

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

    // 한 판의 상태(진행/정지/종료), 경과 시간, 결과, 요청 허용 여부를 가진다.
    // 판 안의 대상과 한 단계의 처리 순서는 World가, 종료 판정은 TimeLimitRule이 가진다.
    // 재시작은 같은 객체의 부분 초기화가 아니라 새 조립이다(SessionAssembler).
    public sealed class GameSession
    {
        private readonly IReadOnlyList<PlayerState> _players;

        public World World { get; }
        public TimeLimitRule TimeLimit { get; }
        public SessionPhase Phase { get; private set; }
        public float Elapsed { get; private set; }
        public float Remaining => TimeLimit.Remaining(Elapsed);
        // 판이 끝나기 전에는 null이다.
        public SessionResult Result { get; private set; }

        internal GameSession(World world, TimeLimitRule timeLimit, IReadOnlyList<PlayerState> players)
        {
            World = world;
            TimeLimit = timeLimit;
            _players = players;
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

        public void TogglePause()
        {
            if (Phase == SessionPhase.Ended)
                return;

            Phase = Phase == SessionPhase.Running ? SessionPhase.Paused : SessionPhase.Running;
        }

        public void Stop() => End(SessionEndReason.Stopped);

        // 전투가 끝나면 PlayerState를 전투에서 풀어 준다. 그때부터 구매할 수 있다.
        // 남아 있는 적은 처치가 아니다 — 사망 기록 없이 판과 함께 버려진다(GAME_RULES 9절).
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
    }
}
