using System.Collections.Generic;

namespace BlackHole.Core.Tests
{
    // 휴식 → 판 → 결산 → 휴식 → 다음 판으로 이어지는 한 루프(INTEGRATED_GAMEPLAY_FLOW). 시스템마다의 계약을 한 흐름으로 잇는다:
    // 처치 → 판의 Gold → 결산 → 진행 상태의 Gold → 노드 구매 → 업그레이드 표 → 다음 판의 Breaker 수치와 적 판 구성.
    internal static class LoopContracts
    {
        private const string KindId = "rock";

        public static IEnumerable<Contract> Cases()
        {
            yield return new Contract("Loop.SettledGoldBuysANodeThatChangesTheNextBattle", SettledGoldBuysANodeThatChangesTheNextBattle);
        }

        // 판이 번 Gold는 판 동안 진행 상태에 들지 않고, 판이 끝난 뒤 결산이 한 번 더한다. 판 중에는 노드를 살 수 없다.
        // 결산한 Gold로 산 노드는 다음 판 조립에 들어가 Breaker 피해와 적 종류의 질량 단계(HP·Gold 계수)를 바꾼다. 끝난 판의 수치는 그대로다.
        private static void SettledGoldBuysANodeThatChangesTheNextBattle()
        {
            GameContent content = TestContent.Load(Content());
            content.TryGetEnemy(KindId, out EnemyDefinition kind);
            var data = new NodeTreeData();
            data.Nodes.Add(new NodeData
            {
                Id = "power",
                Price = 10,
                Start = true,
                Upgrades =
                {
                    new UpgradeData { Stat = BreakerUpgradeStats.Damage, Operation = UpgradeOperation.Add, Value = 1 },
                    new UpgradeData { Stat = EnemyUpgradeStats.MassLevel(KindId), Operation = UpgradeOperation.Add, Value = 1 },
                },
            });
            NodeTree tree = NodeTreeLoader.Load(data).Tree;
            var state = new PlayerState(TestContent.First);

            // 첫 판: 원이 판 전체를 덮는 Breaker가 첫 Tick에 HP 1인 적 셋을 모두 처치한다(색 0, Gold 5씩).
            GameSession first = TestContent.Begun(SessionAssembler.CreateBattle(content, state, 0, tree));
            first.SetAimPoint(state.Id, BattleSpace.Origin);
            first.Advance(0.1f);
            Expect.Equal(3, first.World.TotalKills);
            Expect.Equal(15L, first.World.EarnedGold);
            Expect.Equal(0L, state.Gold);
            Expect.Equal(PurchaseResult.InBattle, NodePurchase.TryPurchase(state, tree, "power"));

            first.RequestEnd();
            first.ClearRemainingEnemies();
            BattleRawData raw = first.CreateRawData();
            first.Settle();
            first.Settle();
            Expect.Equal(15L, raw.EarnedGold);
            Expect.Equal(15L, state.Gold);

            // 휴식: 결산한 Gold로 노드를 산다. 끝난 판의 Breaker는 그대로다.
            Expect.Equal(PurchaseResult.Purchased, NodePurchase.TryPurchase(state, tree, "power"));
            Expect.Equal(5L, state.Gold);
            Expect.Near(1, first.World.PlayerOf(state.Id).Breaker.Definition.Damage);

            // 다음 판: 산 노드가 Breaker 피해(1 → 2)와 적의 질량 단계(0 → 1: Gold 5 × 2)를 바꾼다. 색은 블랙홀 Level이 정하므로 그대로다.
            GameSession next = TestContent.Begun(SessionAssembler.CreateBattle(content, state, 0, tree));
            Expect.Near(2, next.World.PlayerOf(state.Id).Breaker.Definition.Damage);
            Expect.Equal(1, next.World.Stats.CompositionOf(kind).MassLevel);

            foreach (Enemy enemy in next.World.Enemies)
            {
                Expect.Equal(0, enemy.Tier);
                Expect.Equal(10L, enemy.Stats.Gold);
            }
        }

        // HQ 둘레 [2, 4] 띠에 셋이 나오는 판. 적은 색 둘(Gold 5)이고 모두 색 0으로 나온다. 질량 단계 1은 Gold 두 배다.
        // Breaker는 피해 1, 주기 1초, 원이 판 전체를 덮는다.
        private static ContentData Content()
        {
            ContentData data = TestContent.Arena(2, 4, TestContent.Supply(KindId, 3));
            EnemyData kind = TestContent.Tiered(KindId, 1, false, TestContent.Tier(1, 0.2f, 5), TestContent.Tier(1, 0.3f, 5));
            kind.StageColors.Add(TestContent.StageColor(1, 1, 0));
            kind.MassLevels.Add(TestContent.MassLevel(1, 1));
            kind.MassLevels.Add(TestContent.MassLevel(1, 2));
            data.Enemies.Add(kind);
            data.Breaker = new BreakerData { Damage = 1, Interval = 1, Radius = 100, CritMultiplier = 1 };
            return data;
        }
    }
}
