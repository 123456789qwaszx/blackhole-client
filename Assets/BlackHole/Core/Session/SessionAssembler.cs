using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 검증된 콘텐츠와 진행 상태로 한 전투를 새로 조립하는 유일한 진입점.
    // 정의는 공유하고, 전투의 실행 상태(시간, 상태, 결과, 판 안의 적)는 전투마다 새로 만든다.
    //
    // 진행 상태는 방장의 것 하나다: 방장이 노드를 사고(Gold·산 노드), 그 결과가 판 전체에 반영된다.
    // 스탯과 처치 버프는 판 안의 모든 참가자가 함께 받는다. 지금 판 안의 참가자는 방장 한 명이다.
    //
    // 조립한 판은 준비 단계(Preparing)다: 진행 상태를 전투에 묶고, 이 판의 판 구성과 적 수치를 확정하고,
    // 판 안의 참가자(조준점·스킬)를 만든다. 적은 아직 없다. 전투 시작 공급은 GameSession.Begin이 내보낸다.
    // 진행 상태(Gold)는 전투 사이에 이어진다. 새 진행을 시작할지는 호출하는 쪽이 새 PlayerState로 정한다.
    //
    // 업그레이드: 노드 트리를 받으면 방장의 산 노드로 업그레이드 표(UpgradeTable)를 한 번 만들어 판에 둔다(GameSession.Upgrades).
    // 표는 판이 끝날 때까지 같다(전투 중에는 살 수 없다). 노드 트리가 없으면 빈 표다. 이 판의 값은 모두 여기서 한 번 계산한다:
    // - 참가자의 Breaker 수치(피해·주기·반지름·치명타 확률)는 이 표에서 계산한다(BreakerDefinition.Upgraded).
    // - 적 종류의 판 구성(질량 단계·황금 비율·황금 배율·더할 공급 수)은 이 표에서 계산한다(EnemyComposition.From).
    // - 적 수치(Gold 포함)와 색·황금 비율은 판 구성으로 적 수치 표(EnemyStatTable)에 옮겨 적는다.
    //
    // stage는 진행도(적의 강도 단계, 1 ~ 콘텐츠의 단계 수)다. HQ 성장 단계와 다르다.
    // 그 단계의 적 풀이 이 판의 풀 여과 장치가 된다 — 어떤 종류가 나오는가(이정표·종류 해금, BATTLE_COMPOSITION_PLAN 3절).
    // seed는 이 전투의 난수(BattleRandom)를 정한다. 같은 콘텐츠·단계·산 노드·seed·진행 시간이면 같은 결과가 나온다.
    public static class SessionAssembler
    {
        public const int FirstStage = 1;
        public const int DefaultSeed = 0;

        public static GameSession CreateBattle(GameContent content, PlayerState progress) =>
            CreateBattle(content, progress, FirstStage, DefaultSeed);

        public static GameSession CreateBattle(GameContent content, PlayerState progress, int stage, int seed, NodeTree nodes = null)
        {
            UpgradeTable table = UpgradesOf(progress, nodes);

            // 판 안의 참가자: 콘텐츠의 스킬을 모두 받는다. Breaker 수치는 방장의 표로 계산한다(레이저를 보정하는 노드는 아직 없다).
            var battlePlayers = new List<BattlePlayer>
            {
                new BattlePlayer(
                    progress.Id,
                    content.Breaker?.Upgraded(table),
                    content.Laser,
                    seed),
            };

            // 적의 수치(Gold 포함)와 색·황금 비율은 여기서 — 전투 Session이 시작되기 전에 — 정해지고 이 판 동안 바뀌지 않는다.
            var stats = new EnemyStatTable(
                content.Enemies,
                CompositionsOf(content, table));

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
                progress,
                table,
                StartSupplyOf(content, stats));

            // 모든 검사를 통과한 뒤에 전투에 들인다. 조립이 실패하면 진행 상태는 묶이지 않는다.
            progress.EnterBattle();

            return session;
        }

        // 지금 산 노드로 조립하면 받을 적 종류의 판 구성. 조립과 같은 계산이며 판을 만들지 않는다. 콘솔이 다음 판을 미리 보여 줄 때 쓴다.
        public static IReadOnlyDictionary<EnemyDefinition, EnemyComposition> PreviewCompositions(
            GameContent content, PlayerState progress, NodeTree nodes)
        {
            return CompositionsOf(content, UpgradesOf(progress, nodes));
        }

        // 방장의 산 노드로 만든 업그레이드 표. 노드 트리가 없으면 빈 표다.
        private static UpgradeTable UpgradesOf(PlayerState progress, NodeTree nodes) =>
            nodes == null
                ? new UpgradeTable(Array.Empty<Upgrade>())
                : NodePurchase.UpgradesFor(progress, nodes);

        // 적 종류마다의 판 구성. 빈 표면 모두 기본값이다.
        private static Dictionary<EnemyDefinition, EnemyComposition> CompositionsOf(GameContent content, UpgradeTable upgrades)
        {
            var compositions = new Dictionary<EnemyDefinition, EnemyComposition>();

            foreach (EnemyDefinition kind in content.Enemies)
                compositions.Add(kind, EnemyComposition.From(kind, upgrades));

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
    }
}
