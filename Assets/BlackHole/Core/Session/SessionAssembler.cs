using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 검증된 콘텐츠와 참가자의 진행 상태로 한 전투를 새로 조립하는 유일한 진입점.
    // 정의는 공유하고, 전투의 실행 상태(시간, 상태, 결과, 판 안의 적)는 전투마다 새로 만든다.
    // 누가 참가하는지는 콘텐츠가 아니라 판 설정이다 — 호스트가 넘긴다(지금은 로컬 1명).
    //
    // 조립한 판은 준비 단계(Preparing)다: 진행 상태를 전투에 묶고, 이 판의 적 수치를 확정하고, 참가자마다 판 안의 참가자(조준점·스킬)를 만든다. 적은 아직 없다.
    // 전투 시작 공급은 GameSession.Begin이 내보낸다.
    // 진행 상태(Gold)는 전투 사이에 이어진다. 새 진행을 시작할지는 호출하는 쪽이 새 PlayerState로 정한다.
    // 업그레이드: 노드 트리를 받으면 참가자마다 산 노드로 업그레이드 표(UpgradeTable)를 한 번 만들어 판에 둔다(GameSession.UpgradesOf).
    // 표는 판이 끝날 때까지 같다(전투 중에는 살 수 없다). 노드 트리가 없으면 빈 표다.
    // 표를 읽는 시스템(적·스킬 수치)은 아직 잇지 않았다. 판 공유 수치에 누구의 표를 쓸지는 그때 정한다(F06, UPGRADE_LINK_PLAN 6절).
    //
    // stage는 진행도(적의 강도 단계, 1 ~ 콘텐츠의 단계 수)다. HQ 성장 단계와 다르다.
    // 그 단계의 적 풀이 이 판의 풀 여과 장치가 된다. 체력·크기 계수는 단계 표에 붙을 때 여기서 쓴다.
    // seed는 이 전투의 난수(BattleRandom)를 정한다. 같은 콘텐츠·단계·seed·진행 시간이면 같은 결과가 나온다.
    public static class SessionAssembler
    {
        public const int FirstStage = 1;
        public const int DefaultSeed = 0;

        public static GameSession CreateBattle(GameContent content, IReadOnlyList<PlayerState> states) =>
            CreateBattle(content, states, FirstStage, DefaultSeed);

        public static GameSession CreateBattle(GameContent content, IReadOnlyList<PlayerState> states, int stage, int seed, NodeTree nodes = null)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            if (stage < FirstStage || stage > content.StageCount)
                throw new ArgumentOutOfRangeException(
                    nameof(stage), $"단계는 {FirstStage}부터 {content.StageCount}까지다. 받은 값: {stage}.");

            VerifyParticipants(states);

            var players = new List<PlayerState>(states);
            var upgrades = new Dictionary<PlayerId, UpgradeTable>();
            var battlePlayers = new List<BattlePlayer>();

            foreach (PlayerState state in players)
            {
                upgrades.Add(state.Id, nodes == null ? new UpgradeTable(Array.Empty<Upgrade>()) : NodePurchase.UpgradesFor(state, nodes));
                // 판 안의 참가자: 콘텐츠의 스킬을 모두 받는다. 스킬 수치는 아직 업그레이드 표를 읽지 않는다(SKILL_SYSTEM_PLAN 2절).
                battlePlayers.Add(new BattlePlayer(state.Id, content.Breaker, content.Laser, seed));
            }

            // 적의 수치는 여기서 — 전투 Session이 시작되기 전에 — 정해지고 이 판 동안 바뀌지 않는다.
            var world = new World(
                new BattleRandom(seed),
                content.GetStage(stage).Pool,
                new EnemyStatTable(content.Enemies),
                content.EnemyPlacement,
                battlePlayers);
            var session = new GameSession(
                world,
                new TimeLimitRule(content.TimeLimit),
                stage,
                seed,
                players.AsReadOnly(),
                upgrades,
                content.StartSupply);

            // 모든 검사를 통과한 뒤에 전투에 들인다. 조립이 실패하면 PlayerState는 묶이지 않는다.
            foreach (PlayerState state in players)
            {
                state.EnterBattle();
            }

            return session;
        }

        private static void VerifyParticipants(IReadOnlyList<PlayerState> states)
        {
            if (states == null || states.Count == 0)
                throw new ArgumentException(
                    "참가 Player가 한 명 이상 필요하다.", nameof(states));

            var ids = new HashSet<PlayerId>();

            foreach (PlayerState state in states)
            {
                if (state == null)
                    throw new ArgumentException("PlayerState가 비어 있다.", nameof(states));

                if (!ids.Add(state.Id))
                    throw new ArgumentException(
                        $"{state.Id}가 두 번 참가했다.", nameof(states));

                if (state.InBattle)
                    throw new InvalidOperationException($"{state.Id}는 이미 진행 중인 전투에 들어가 있다.");
            }
        }
    }
}
