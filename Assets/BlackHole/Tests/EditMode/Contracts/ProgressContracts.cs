using System;
using System.Collections.Generic;
using BlackHole.Core;

namespace BlackHole.Core.Tests
{
    // Player별 진행 상태(Gold). 전투는 진행 상태를 묶고 풀 뿐이며, 진행 상태는 전투 사이에 이어진다.
    internal static class ProgressContracts
    {
        public static IEnumerable<Contract> Cases()
        {
            yield return new Contract("Progress.CannotEnterTwoBattlesAtOnce", CannotEnterTwoBattlesAtOnce);
            yield return new Contract("Progress.NextBattleKeepsProgress", NextBattleKeepsProgress);
            yield return new Contract("Progress.PlayerStatesAreIndependent", PlayerStatesAreIndependent);
            yield return new Contract("Progress.GoldCannotBeTakenByEarning", GoldCannotBeTakenByEarning);
            yield return new Contract("Progress.CheatsBypassPurchaseButGoldStaysNonNegative", CheatsBypassPurchaseButGoldStaysNonNegative);
            yield return new Contract("Progress.CheatsAreRefusedDuringBattle", CheatsAreRefusedDuringBattle);
        }

        // 진행 상태는 전투 밖에서만 바뀐다. 개발용 치트도 전투 중에는 거부하고, 전투가 끝나면 다시 된다.
        private static void CheatsAreRefusedDuringBattle()
        {
            GameContent content = TestContent.Load(TestContent.Data());
            var data = new NodeTreeData();
            data.Nodes.Add(new NodeData { Id = "s", Price = 10, Start = true });
            NodeTree tree = NodeTreeLoader.Load(data).Tree;
            var state = new PlayerState(TestContent.First);
            state.EarnGold(50);

            GameSession battle = SessionAssembler.CreateBattle(content, new[] { state });
            Expect.Throws<InvalidOperationException>(() => ProgressCheats.TakeGold(state, 10));
            Expect.Throws<InvalidOperationException>(() => ProgressCheats.UnlockAllNodes(state, tree));
            Expect.Throws<InvalidOperationException>(() => ProgressCheats.LockAllNodes(state));
            Expect.Equal(50L, state.Gold);
            Expect.Equal(0, state.OwnedNodes.Count);

            battle.RequestEnd(SessionEndReason.TimeExpired);
            Expect.Equal(10L, ProgressCheats.TakeGold(state, 10));
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

        // 진행 중인 전투에 들어간 진행 상태는 다른 전투에 들어갈 수 없다. 전투가 끝나면 풀린다.
        private static void CannotEnterTwoBattlesAtOnce()
        {
            GameContent content = TestContent.Load(TestContent.Data());
            var state = new PlayerState(TestContent.First);

            GameSession battle = SessionAssembler.CreateBattle(content, new[] { state });
            Expect.True(state.InBattle, "전투에 들어가 있어야 한다.");
            Expect.Throws<InvalidOperationException>(() => SessionAssembler.CreateBattle(content, new[] { state }));

            battle.RequestEnd(SessionEndReason.TimeExpired);
            Expect.True(!state.InBattle, "전투가 끝나면 풀려야 한다.");
            SessionAssembler.CreateBattle(content, new[] { state });
        }

        // 다음 전투는 같은 진행 상태를 이어받는다. 시간이 끝나 끝난 전투도 진행 상태를 풀어 준다.
        private static void NextBattleKeepsProgress()
        {
            GameContent content = TestContent.Load(TestContent.Data(timeLimit: 1));
            var state = new PlayerState(TestContent.First);
            state.EarnGold(20);

            GameSession first = TestContent.Begun(SessionAssembler.CreateBattle(content, new[] { state }));
            first.Advance(2);
            Expect.Equal(SessionEndReason.TimeExpired, first.Result.Reason);
            Expect.True(!state.InBattle, "시간이 끝나도 풀려야 한다.");

            SessionAssembler.CreateBattle(content, new[] { state });
            Expect.Equal(20, state.Gold);
        }

        // 두 Player의 진행 상태는 서로 영향을 주지 않는다. 조립이 실패하면 어느 쪽도 전투에 묶이지 않는다.
        private static void PlayerStatesAreIndependent()
        {
            GameContent content = TestContent.Load(TestContent.Data());
            var first = new PlayerState(TestContent.First);
            var second = new PlayerState(TestContent.Second);
            first.EarnGold(20);
            Expect.Equal(0, second.Gold);

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
    }
}
