using System;
using System.Collections.Generic;
using BlackHole.Core;

namespace BlackHole.Core.Tests
{
    // Player별 진행 상태(Gold, 산 노드).
    internal static class ProgressContracts
    {
        public static IEnumerable<Contract> Cases()
        {
            yield return new Contract("Progress.PlayerStatesAreIndependent", PlayerStatesAreIndependent);
            yield return new Contract("Progress.GoldCannotBeTakenByEarning", GoldCannotBeTakenByEarning);
            yield return new Contract("Progress.CheatsBypassPurchaseButGoldStaysNonNegative", CheatsBypassPurchaseButGoldStaysNonNegative);
        }

        // 개발용 치트: Gold 빼기는 0에서 멈춘다. 전체 해금은 Gold를 쓰지 않고 숨은 노드까지 산 것으로 한다.
        // 전체 잠금은 Gold를 돌려주지 않고, 그 뒤에는 구매 규칙이 처음부터 다시 적용된다.
        private static void CheatsBypassPurchaseButGoldStaysNonNegative()
        {
            var data = new NodeTreeData();
            data.Nodes.Add(new NodeData { Id = "s", Price = 10, Start = true, Links = new List<string> { "far" } });
            data.Nodes.Add(new NodeData { Id = "far", Price = 1000 });
            NodeTree tree = NodeTreeLoader.Load(data).Tree;
            var state = new PlayerState(new PlayerId(1));
            state.EarnGold(50);

            Expect.Equal(30L, ProgressCheats.TakeGold(state, 30));
            Expect.Equal(20L, ProgressCheats.TakeGold(state, 1_000_000_000_000));
            Expect.Equal(0L, state.Gold);
            Expect.Throws<ArgumentOutOfRangeException>(() => ProgressCheats.TakeGold(state, -1));

            ProgressCheats.UnlockAllNodes(state, tree);
            Expect.True(state.Owns("s") && state.Owns("far"), "숨은 노드까지 산 것이어야 한다.");
            Expect.Equal(0L, state.Gold);
            ProgressCheats.UnlockAllNodes(state, tree);
            Expect.Equal(2, state.OwnedNodes.Count);

            state.EarnGold(10);
            ProgressCheats.LockAllNodes(state);
            Expect.Equal(0, state.OwnedNodes.Count);
            Expect.Equal(10L, state.Gold);
            Expect.Equal(NodeState.Purchasable, NodePurchase.StateOf(state, tree, "s"));
            Expect.Equal(NodeState.Hidden, NodePurchase.StateOf(state, tree, "far"));
        }

        // 두 Player의 진행 상태는 서로 영향을 주지 않는다.
        private static void PlayerStatesAreIndependent()
        {
            var first = new PlayerState(new PlayerId(1));
            var second = new PlayerState(new PlayerId(2));
            first.EarnGold(20);

            Expect.Equal(20L, first.Gold);
            Expect.Equal(0L, second.Gold);
        }

        // Gold를 더하는 입구로 Gold를 뺄 수 없다.
        private static void GoldCannotBeTakenByEarning()
        {
            var state = new PlayerState(new PlayerId(1));
            state.EarnGold(5);
            Expect.Throws<ArgumentOutOfRangeException>(() => state.EarnGold(-1));
            Expect.Equal(5L, state.Gold);
        }
    }
}
