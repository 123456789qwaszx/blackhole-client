using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 검증된 콘텐츠와 참가자의 진행 상태로 한 전투를 새로 조립하는 유일한 진입점.
    // 정의는 공유하고, 전투의 실행 상태(시간, 상태, 결과, 판 안의 적)는 전투마다 새로 만든다.
    // 누가 참가하는지는 콘텐츠가 아니라 판 설정이다 — 호스트가 넘긴다(지금은 로컬 1명).
    //
    // 조립한 판은 준비 단계(Preparing)다: 진행 상태를 전투에 묶고, 이 판의 판 구성과 적 수치를 확정하고,
    // 참가자마다 판 안의 참가자(조준점·스킬)를 만든다. 적은 아직 없다. 전투 시작 공급은 GameSession.Begin이 내보낸다.
    // 진행 상태(Gold)는 전투 사이에 이어진다. 새 진행을 시작할지는 호출하는 쪽이 새 PlayerState로 정한다.
    //
    // 업그레이드: 노드 트리를 받으면 참가자마다 산 노드로 업그레이드 표(UpgradeTable)를 한 번 만들어 판에 둔다(GameSession.UpgradesOf).
    // 표는 판이 끝날 때까지 같다(전투 중에는 살 수 없다). 노드 트리가 없으면 빈 표다. 이 판의 값은 모두 여기서 한 번 계산한다:
    // - 참가자의 Breaker 수치(피해·주기·반지름·치명타 확률)는 그 참가자의 표에서 계산한다(BreakerDefinition.Upgraded).
    // - 적 종류의 판 구성(질량 단계·황금 비율·황금 배율·더할 공급 수)은 표에서 계산한다(EnemyComposition.From).
    //   적 종류는 모든 참가자가 함께 쓰므로 참가자가 1명일 때 그 표를 쓴다. 둘 이상이면 보정 없이 기본값이다
    //   (여러 Player의 구매를 공유 대상에 합치는 정책은 미정, F06 — 결산의 보상 귀속과 같은 전제).
    // - 적 수치(Gold 포함)와 색·황금 비율은 판 구성으로 적 수치 표(EnemyStatTable)에 옮겨 적는다.
    //
    // stage는 진행도(적의 강도 단계, 1 ~ 콘텐츠의 단계 수)다. HQ 성장 단계와 다르다.
    // 그 단계의 적 풀이 이 판의 풀 여과 장치가 된다 — 어떤 종류가 나오는가(이정표·종류 해금, BATTLE_COMPOSITION_PLAN 3절).
    // seed는 이 전투의 난수(BattleRandom)를 정한다. 같은 콘텐츠·단계·산 노드·seed·진행 시간이면 같은 결과가 나온다.
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
                UpgradeTable table = nodes == null ? new UpgradeTable(Array.Empty<Upgrade>()) : NodePurchase.UpgradesFor(state, nodes);
                upgrades.Add(state.Id, table);
                // 판 안의 참가자: 콘텐츠의 스킬을 모두 받는다. Breaker 수치는 그 참가자의 표로 계산한다(레이저를 보정하는 노드는 아직 없다).
                battlePlayers.Add(new BattlePlayer(state.Id, content.Breaker?.Upgraded(table), content.Laser, seed));
            }

            // 적의 수치(Gold 포함)와 색·황금 비율은 여기서 — 전투 Session이 시작되기 전에 — 정해지고 이 판 동안 바뀌지 않는다.
            var stats = new EnemyStatTable(content.Enemies, CompositionsOf(content, players.Count == 1 ? upgrades[players[0].Id] : null));
            var world = new World(
                seed,
                content.GetStage(stage).Pool,
                stats,
                content.EnemyPlacement,
                content.MaxAliveEnemies,
                battlePlayers);
            var session = new GameSession(
                world,
                new TimeLimitRule(content.TimeLimit),
                stage,
                seed,
                players.AsReadOnly(),
                upgrades,
                StartSupplyOf(content, stats));

            // 모든 검사를 통과한 뒤에 전투에 들인다. 조립이 실패하면 PlayerState는 묶이지 않는다.
            foreach (PlayerState state in players)
            {
                state.EnterBattle();
            }

            return session;
        }

        // 지금 산 노드로 조립하면 받을 적 종류의 판 구성. 조립과 같은 계산이며 판을 만들지 않는다. 콘솔이 다음 판을 미리 보여 줄 때 쓴다.
        public static IReadOnlyDictionary<EnemyDefinition, EnemyComposition> PreviewCompositions(
            GameContent content, IReadOnlyList<PlayerState> states, NodeTree nodes)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            UpgradeTable shared = states != null && states.Count == 1
                ? (nodes == null ? new UpgradeTable(Array.Empty<Upgrade>()) : NodePurchase.UpgradesFor(states[0], nodes))
                : null;
            return CompositionsOf(content, shared);
        }

        // 적 종류마다의 판 구성. 표가 없으면(참가자가 둘 이상) 모두 기본값이다.
        private static Dictionary<EnemyDefinition, EnemyComposition> CompositionsOf(GameContent content, UpgradeTable upgrades)
        {
            var compositions = new Dictionary<EnemyDefinition, EnemyComposition>();

            foreach (EnemyDefinition kind in content.Enemies)
                compositions.Add(kind, upgrades == null ? EnemyComposition.Base(kind) : EnemyComposition.From(kind, upgrades));

            return compositions;
        }

        // 이 판의 전투 시작 공급: 콘텐츠의 공급에 판 구성의 더할 공급 수(산 공급 수 노드)를 더한다.
        // 종류가 콘텐츠 공급에 있으면 그 종류의 첫 요청에 더하고, 없으면 콘텐츠 종류 순서로 요청을 뒤에 붙인다.
        private static IReadOnlyList<SupplyRequest> StartSupplyOf(GameContent content, EnemyStatTable stats)
        {
            var supply = new List<SupplyRequest>(content.StartSupply);
            var bonused = new HashSet<EnemyDefinition>();

            for (int i = 0; i < supply.Count; i++)
            {
                EnemyDefinition kind = supply[i].Enemy;
                int bonus = stats.CompositionOf(kind).StartSupplyBonus;

                if (bonus > 0 && bonused.Add(kind))
                    supply[i] = new SupplyRequest(kind, supply[i].Count + bonus);
            }

            foreach (EnemyDefinition kind in stats.Kinds)
            {
                int bonus = stats.CompositionOf(kind).StartSupplyBonus;

                if (bonus > 0 && bonused.Add(kind))
                    supply.Add(new SupplyRequest(kind, bonus));
            }

            return supply.AsReadOnly();
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
