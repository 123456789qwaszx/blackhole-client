using System;
using System.Collections.Generic;
using BlackHole.Core;

namespace BlackHole.Core.Tests
{
    // Player별 진행 상태와 전투 밖 구매. 전투는 진행 상태를 묶고 풀 뿐 Gold·구매를 바꾸지 않는다.
    internal static class UpgradeContracts
    {
        public static IEnumerable<Contract> Cases()
        {
            yield return new Contract("Upgrade.PurchaseNeedsPrerequisiteAndGold", PurchaseNeedsPrerequisiteAndGold);
            yield return new Contract("Upgrade.PurchaseByUnknownIdChangesNothing", PurchaseByUnknownIdChangesNothing);
            yield return new Contract("Upgrade.CannotPurchaseDuringBattle", CannotPurchaseDuringBattle);
            yield return new Contract("Upgrade.NextBattleKeepsProgress", NextBattleKeepsProgress);
            yield return new Contract("Upgrade.PlayerStatesAreIndependent", PlayerStatesAreIndependent);
            yield return new Contract("Upgrade.GoldCannotBeTakenByEarning", GoldCannotBeTakenByEarning);
        }

        // 실패한 구매는 Gold와 구매 상태를 바꾸지 않는다.
        private static void PurchaseNeedsPrerequisiteAndGold()
        {
            GameContent content = TestContent.Load(Market());
            var state = new PlayerState(TestContent.First);
            state.EarnGold(20);

            Expect.Equal(PurchaseResult.MissingPrerequisite, UpgradePurchase.TryPurchase(state, content, "child"));
            Expect.Equal(20, state.Gold);
            Expect.True(!state.Owns("child"), "선행 노드 없이 사면 안 된다.");

            Expect.Equal(PurchaseResult.Purchased, UpgradePurchase.TryPurchase(state, content, "root"));
            Expect.Equal(10, state.Gold);

            Expect.Equal(PurchaseResult.AlreadyOwned, UpgradePurchase.TryPurchase(state, content, "root"));
            Expect.Equal(10, state.Gold);

            Expect.Equal(PurchaseResult.NotEnoughGold, UpgradePurchase.TryPurchase(state, content, "child"));
            Expect.Equal(10, state.Gold);
            Expect.True(!state.Owns("child"), "Gold가 모자라면 사면 안 된다.");
            Expect.Equal(1, state.Upgrades.Count);
        }

        // 화면은 노드 ID로 요청한다. 콘텐츠에 없는 ID는 아무것도 바꾸지 않는다.
        private static void PurchaseByUnknownIdChangesNothing()
        {
            GameContent content = TestContent.Load(Market());
            var state = new PlayerState(TestContent.First);
            state.EarnGold(20);

            Expect.Equal(PurchaseResult.UnknownNode, UpgradePurchase.Check(state, content, "ghost"));
            Expect.Equal(PurchaseResult.UnknownNode, UpgradePurchase.TryPurchase(state, content, "ghost"));
            Expect.Equal(20, state.Gold);
            Expect.Equal(0, state.Upgrades.Count);
        }

        // 진행 중인 전투에 들어간 PlayerState는 살 수 없고, 다른 전투에도 들어갈 수 없다. 전투가 끝나면 풀린다.
        private static void CannotPurchaseDuringBattle()
        {
            GameContent content = TestContent.Load(Market());
            var state = new PlayerState(TestContent.First);
            state.EarnGold(20);

            GameSession battle = SessionAssembler.CreateBattle(content, new[] { state });
            Expect.True(state.InBattle, "전투에 들어가 있어야 한다.");
            Expect.Equal(PurchaseResult.InBattle, UpgradePurchase.TryPurchase(state, content, "root"));
            Expect.Equal(20, state.Gold);
            Expect.Throws<InvalidOperationException>(() => SessionAssembler.CreateBattle(content, new[] { state }));

            battle.Stop();
            Expect.True(!state.InBattle, "전투가 끝나면 풀려야 한다.");
            Expect.Equal(PurchaseResult.Purchased, UpgradePurchase.TryPurchase(state, content, "root"));
        }

        // 다음 전투는 같은 진행 상태를 이어받는다. 시간이 끝나 끝난 전투도 진행 상태를 풀어 준다.
        private static void NextBattleKeepsProgress()
        {
            GameContent content = TestContent.Load(Market(timeLimit: 1));
            var state = new PlayerState(TestContent.First);
            state.EarnGold(20);

            GameSession first = SessionAssembler.CreateBattle(content, new[] { state });
            first.Advance(2);
            Expect.Equal(SessionEndReason.TimeExpired, first.Result.Reason);
            Expect.True(!state.InBattle, "시간이 끝나도 풀려야 한다.");
            UpgradePurchase.TryPurchase(state, content, "root");

            SessionAssembler.CreateBattle(content, new[] { state });
            Expect.Equal(10, state.Gold);
            Expect.True(state.Owns("root"), "구매는 이어져야 한다.");
        }

        // 두 Player의 진행 상태는 서로 영향을 주지 않는다. 조립이 실패하면 어느 쪽도 전투에 묶이지 않는다.
        private static void PlayerStatesAreIndependent()
        {
            GameContent content = TestContent.Load(Market());
            var first = new PlayerState(TestContent.First);
            var second = new PlayerState(TestContent.Second);
            first.EarnGold(20);
            UpgradePurchase.TryPurchase(first, content, "root");
            Expect.Equal(0, second.Gold);
            Expect.Equal(0, second.Upgrades.Count);

            var duplicate = new PlayerState(TestContent.First);
            Expect.Throws<ArgumentException>(() => SessionAssembler.CreateBattle(content, new[] { first, duplicate }));
            Expect.True(!first.InBattle && !duplicate.InBattle, "실패한 조립이 PlayerState를 묶으면 안 된다.");

            SessionAssembler.CreateBattle(content, new[] { first, second });
            Expect.True(first.InBattle && second.InBattle, "두 Player 모두 전투에 들어가야 한다.");
        }

        // Gold를 더하는 입구로 Gold를 뺄 수 없다.
        private static void GoldCannotBeTakenByEarning()
        {
            var state = new PlayerState(TestContent.First);
            state.EarnGold(5);
            Expect.Throws<ArgumentOutOfRangeException>(() => state.EarnGold(-1));
            Expect.Equal(5, state.Gold);
        }

        // 노드: root(10G), child(15G, root 다음).
        private static ContentData Market(float timeLimit = 60)
        {
            ContentData data = TestContent.Data(timeLimit);
            data.Upgrades = new List<UpgradeData>
            {
                TestContent.Upgrade("root", 10, null),
                TestContent.Upgrade("child", 15, "root")
            };
            return data;
        }
    }
}
