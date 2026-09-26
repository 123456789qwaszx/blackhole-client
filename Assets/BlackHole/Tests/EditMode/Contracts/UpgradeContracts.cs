using System;
using System.Collections.Generic;
using BlackHole.Core;

namespace BlackHole.Core.Tests
{
    // 전투 밖 구매와, 산 노드가 판 구성이 되는 자리(Loadout). 진행 상태 자체의 계약은 ProgressContracts에 있다.
    internal static class UpgradeContracts
    {
        public static IEnumerable<Contract> Cases()
        {
            yield return new Contract("Upgrade.PurchaseNeedsPrerequisiteAndGold", PurchaseNeedsPrerequisiteAndGold);
            yield return new Contract("Upgrade.PurchaseByUnknownIdChangesNothing", PurchaseByUnknownIdChangesNothing);
            yield return new Contract("Upgrade.CannotPurchaseDuringBattle", CannotPurchaseDuringBattle);
            yield return new Contract("Upgrade.OwnedNodesBecomeTheEnemyComposition", OwnedNodesBecomeTheEnemyComposition);
            yield return new Contract("Upgrade.SupplyNodesAddToTheStartSupply", SupplyNodesAddToTheStartSupply);
        }

        // 산 공급 수 노드는 그 종류의 전투 시작 공급에 더해진다. 콘텐츠 공급에 없는 종류는 공급이 새로 붙는다.
        private static void SupplyNodesAddToTheStartSupply()
        {
            ContentData data = TestContent.Arena(1, 3, TestContent.Supply("rock", 2));
            data.Enemies.Add(TestContent.Enemy("rock"));
            data.Enemies.Add(TestContent.Enemy("pebble"));
            TestContent.Allow(data, "rock");
            TestContent.Allow(data, "pebble");
            data.Upgrades.Add(TestContent.Upgrade("more-rocks", 1, null, TestContent.Grant("rock", "StartSupply", "Add", 3)));
            data.Upgrades.Add(TestContent.Upgrade("pebbles", 1, null, TestContent.Grant("pebble", "StartSupply", "Add", 2)));
            GameContent content = TestContent.Load(data);
            content.TryGetEnemy("rock", out EnemyDefinition rock);
            content.TryGetEnemy("pebble", out EnemyDefinition pebble);
            var state = new PlayerState(TestContent.First);

            GameSession plain = Begin(content, state);
            Expect.Equal(2, plain.World.CountAlive(rock));
            Expect.Equal(0, plain.World.CountAlive(pebble));
            plain.RequestEnd(SessionEndReason.TimeExpired);

            state.EarnGold(2);
            UpgradePurchase.TryPurchase(state, content, "more-rocks");
            UpgradePurchase.TryPurchase(state, content, "pebbles");
            Expect.Equal(3, Loadout.EnemiesFor(content, new[] { state })[rock].StartSupplyBonus);

            GameSession supplied = Begin(content, state);
            Expect.Equal(5, supplied.World.CountAlive(rock));
            Expect.Equal(2, supplied.World.CountAlive(pebble));
        }

        private static GameSession Begin(GameContent content, PlayerState state) =>
            TestContent.Begun(SessionAssembler.CreateBattle(
                content, new[] { state }, SessionAssembler.FirstStage, 0, Loadout.EnemiesFor(content, new[] { state })));

        // 실패한 구매는 Gold와 구매 상태를 바꾸지 않는다.
        private static void PurchaseNeedsPrerequisiteAndGold()
        {
            GameContent content = TestContent.Load(Market());
            var state = new PlayerState(TestContent.First);
            state.EarnGold(20);

            Expect.Equal(PurchaseResult.MissingPrerequisite, UpgradePurchase.TryPurchase(state, content, "child"));
            Expect.Equal(20L, state.Gold);
            Expect.True(!state.Owns("child"), "선행 노드 없이 사면 안 된다.");

            Expect.Equal(PurchaseResult.Purchased, UpgradePurchase.TryPurchase(state, content, "root"));
            Expect.Equal(10L, state.Gold);

            Expect.Equal(PurchaseResult.AlreadyOwned, UpgradePurchase.TryPurchase(state, content, "root"));
            Expect.Equal(10L, state.Gold);

            Expect.Equal(PurchaseResult.NotEnoughGold, UpgradePurchase.TryPurchase(state, content, "child"));
            Expect.Equal(10L, state.Gold);
            Expect.True(!state.Owns("child"), "Gold가 모자라면 사면 안 된다.");
            Expect.Equal(1, state.Upgrades.Count);
        }

        // 화면·콘솔은 노드 ID로 요청한다. 콘텐츠에 없는 ID는 아무것도 바꾸지 않는다.
        private static void PurchaseByUnknownIdChangesNothing()
        {
            GameContent content = TestContent.Load(Market());
            var state = new PlayerState(TestContent.First);
            state.EarnGold(20);

            Expect.Equal(PurchaseResult.UnknownNode, UpgradePurchase.Check(state, content, "ghost"));
            Expect.Equal(PurchaseResult.UnknownNode, UpgradePurchase.TryPurchase(state, content, "ghost"));
            Expect.Equal(20L, state.Gold);
            Expect.Equal(0, state.Upgrades.Count);
        }

        // 진행 중인 전투에 들어간 PlayerState는 살 수 없다. 전투가 끝나면 살 수 있고, 산 노드는 다음 전투에 이어진다.
        private static void CannotPurchaseDuringBattle()
        {
            GameContent content = TestContent.Load(Market());
            var state = new PlayerState(TestContent.First);
            state.EarnGold(20);

            GameSession battle = SessionAssembler.CreateBattle(content, new[] { state });
            Expect.Equal(PurchaseResult.InBattle, UpgradePurchase.TryPurchase(state, content, "root"));
            Expect.Equal(20L, state.Gold);

            battle.RequestEnd(SessionEndReason.TimeExpired);
            Expect.Equal(PurchaseResult.Purchased, UpgradePurchase.TryPurchase(state, content, "root"));

            SessionAssembler.CreateBattle(content, new[] { state });
            Expect.True(state.Owns("root"), "산 노드는 다음 전투에 이어져야 한다.");
        }

        // 산 노드의 Grant가 종류별 판 구성이 된다. 사지 않은 노드는 아무것도 바꾸지 않고, 산 순서와 결과는 무관하다.
        // 질량 단계는 더하고, 황금 비율은 정한 뒤(황금 소행성 추가) 곱하며(자릿수 올리기) 1을 넘지 않는다.
        // 황금 배율은 정하기 가운데 가장 큰 값이다. 참가자가 둘 이상이면 노드 보정 없이 기본값이다.
        private static void OwnedNodesBecomeTheEnemyComposition()
        {
            ContentData data = TestContent.Data();
            EnemyData rockData = TestContent.Tiered("rock", 1, false, TestContent.Tier(10, 0.2f, 1));
            for (int i = 0; i < 3; i++)
                rockData.MassLevels.Add(TestContent.MassLevel(1, 1, 1));
            rockData.GoldenMultiplier = 50;
            data.Enemies.Add(rockData);
            data.Upgrades.Add(TestContent.Upgrade("mass-1", 1, null, TestContent.Grant("rock", "MassLevel", "Add", 1)));
            data.Upgrades.Add(TestContent.Upgrade("mass-2", 1, "mass-1", TestContent.Grant("rock", "MassLevel", "Add", 1)));
            data.Upgrades.Add(TestContent.Upgrade("golden", 1, "mass-1", TestContent.Grant("rock", "GoldenRatio", "Set", 0.01f)));
            data.Upgrades.Add(TestContent.Upgrade("digits-1", 1, "golden", TestContent.Grant("rock", "GoldenRatio", "Multiply", 10)));
            data.Upgrades.Add(TestContent.Upgrade("digits-2", 1, "digits-1", TestContent.Grant("rock", "GoldenRatio", "Multiply", 10)));
            data.Upgrades.Add(TestContent.Upgrade("rich", 1, "golden",
                TestContent.Grant("rock", "GoldenMultiplier", "Set", 4200),
                TestContent.Grant("rock", "GoldenMultiplier", "Set", 100)));
            GameContent content = TestContent.Load(data);
            content.TryGetEnemy("rock", out EnemyDefinition rock);

            var fresh = new PlayerState(TestContent.First);
            EnemyComposition none = Loadout.EnemiesFor(content, new[] { fresh })[rock];
            Expect.Equal(0, none.MassLevel);
            Expect.Near(0, none.GoldenRatio);
            Expect.Near(50, none.GoldenMultiplier);

            var forward = new PlayerState(TestContent.First);
            var backward = new PlayerState(TestContent.Second);
            forward.EarnGold(100);
            backward.EarnGold(100);
            string[] order = { "mass-1", "mass-2", "golden", "digits-1", "rich" };

            foreach (string node in order)
                Expect.Equal(PurchaseResult.Purchased, UpgradePurchase.TryPurchase(forward, content, node));

            // 선행 순서를 지키면서 다르게 산다.
            foreach (string node in new[] { "mass-1", "golden", "rich", "digits-1", "mass-2" })
                Expect.Equal(PurchaseResult.Purchased, UpgradePurchase.TryPurchase(backward, content, node));

            foreach (PlayerState state in new[] { forward, backward })
            {
                EnemyComposition bought = Loadout.EnemiesFor(content, new[] { state })[rock];
                Expect.Equal(2, bought.MassLevel);
                Expect.Near(0.1f, bought.GoldenRatio);
                Expect.Near(4200, bought.GoldenMultiplier);
            }

            UpgradePurchase.TryPurchase(forward, content, "digits-2");
            Expect.Near(1, Loadout.EnemiesFor(content, new[] { forward })[rock].GoldenRatio);

            EnemyComposition shared = Loadout.EnemiesFor(content, new[] { forward, backward })[rock];
            Expect.Equal(0, shared.MassLevel);
            Expect.Near(0, shared.GoldenRatio);

            // 판 조립은 이 판 구성을 그대로 받는다.
            GameSession game = SessionAssembler.CreateBattle(
                content, new[] { forward }, SessionAssembler.FirstStage, 0, Loadout.EnemiesFor(content, new[] { forward }));
            Expect.Equal(2, game.World.Stats.CompositionOf(rock).MassLevel);
            Expect.Equal(4200L, game.World.Stats.Of(rock, 0, golden: true).Gold);
        }

        // 노드: root(10G), child(15G, root 다음).
        private static ContentData Market()
        {
            ContentData data = TestContent.Data();
            data.Upgrades.Add(TestContent.Upgrade("root", 10, null));
            data.Upgrades.Add(TestContent.Upgrade("child", 15, "root"));
            return data;
        }
    }
}
