using System;
using System.Collections.Generic;
using BlackHole.Core;

namespace BlackHole.Core.Tests
{
    // Player별 진행 상태(Gold·산 노드). 전투는 진행 상태를 묶고 풀 뿐이며, 진행 상태는 전투 사이에 이어진다. 판이 번 Gold는 결산이 한 번 더한다.
    internal static class ProgressContracts
    {
        public static IEnumerable<Contract> Cases()
        {
            yield return new Contract("Progress.CannotEnterTwoBattlesAtOnce", CannotEnterTwoBattlesAtOnce);
            yield return new Contract("Progress.NextBattleKeepsProgress", NextBattleKeepsProgress);
            yield return new Contract("Progress.FailedAssemblyLeavesProgressFree", FailedAssemblyLeavesProgressFree);
            yield return new Contract("Progress.GoldCannotBeTakenByEarning", GoldCannotBeTakenByEarning);
            yield return new Contract("Progress.CheatsBypassPurchaseButGoldStaysNonNegative", CheatsBypassPurchaseButGoldStaysNonNegative);
            yield return new Contract("Progress.CheatsAreRefusedDuringBattle", CheatsAreRefusedDuringBattle);
            yield return new Contract("Progress.BattleGoldIsSettledOnceAfterTheEnd", BattleGoldIsSettledOnceAfterTheEnd);
            yield return new Contract("Progress.FailedSettlementIsNotRecorded", FailedSettlementIsNotRecorded);
        }

        // 결산은 Gold를 더한 뒤에야 마친 것으로 기록한다. 더하기가 실패하면(Gold 넘침) 결산하지 않은 상태로 남고 진행 상태도 그대로다.
        // 원인을 치운 뒤 다시 하면 번 Gold를 한 번만 더한다.
        private static void FailedSettlementIsNotRecorded()
        {
            ContentData data = TestContent.Arena(3, 3, TestContent.Supply(TestContent.EnemyId, 1));
            data.Enemies.Add(TestContent.Enemy(TestContent.EnemyId, health: 1, gold: 7));
            var state = new PlayerState(TestContent.First);
            state.EarnGold(long.MaxValue - 3);
            GameSession game = TestContent.Begun(SessionAssembler.CreateBattle(TestContent.Load(data), state));

            game.World.DealDamage(game.World.Enemies[0], new Damage(1, TestContent.First));
            game.RequestEnd();

            Expect.Throws<OverflowException>(() => game.Settle());
            Expect.True(!game.IsSettled, "실패한 결산을 마친 것으로 기록하면 안 된다.");
            Expect.Equal(long.MaxValue - 3, state.Gold);

            ProgressCheats.TakeGold(state, 10);
            game.Settle();
            game.Settle();
            Expect.True(game.IsSettled, "다시 한 결산은 마쳐야 한다.");
            Expect.Equal(long.MaxValue - 6, state.Gold);
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

            GameSession battle = SessionAssembler.CreateBattle(content, state);
            Expect.Throws<InvalidOperationException>(() => ProgressCheats.TakeGold(state, 10));
            Expect.Throws<InvalidOperationException>(() => ProgressCheats.UnlockAllNodes(state, tree));
            Expect.Throws<InvalidOperationException>(() => ProgressCheats.LockAllNodes(state));
            Expect.Equal(50L, state.Gold);
            Expect.Equal(0, state.OwnedNodes.Count);

            battle.RequestEnd();
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

        // 판이 번 Gold는 전투 중에는 진행 상태에 들어가지 않는다. 판이 끝난 뒤 결산 때 한 번만 들어간다.
        private static void BattleGoldIsSettledOnceAfterTheEnd()
        {
            ContentData data = TestContent.Arena(3, 3, TestContent.Supply(TestContent.EnemyId, 1));
            data.Enemies.Add(TestContent.Enemy(TestContent.EnemyId, health: 1, gold: 7));
            var state = new PlayerState(TestContent.First);
            state.EarnGold(20);
            GameSession game = TestContent.Begun(SessionAssembler.CreateBattle(TestContent.Load(data), state));

            game.World.DealDamage(game.World.Enemies[0], new Damage(1, TestContent.First));
            Expect.Equal(20L, state.Gold);
            Expect.Throws<InvalidOperationException>(() => game.Settle());

            game.RequestEnd();
            Expect.True(!game.IsSettled, "끝났다고 결산된 것은 아니다.");
            game.Settle();
            game.Settle();
            Expect.True(game.IsSettled, "결산을 마쳐야 한다.");
            Expect.Equal(27L, state.Gold);
        }

        // 진행 중인 전투에 들어간 진행 상태는 다른 전투에 들어갈 수 없다. 전투가 끝나면 풀린다.
        private static void CannotEnterTwoBattlesAtOnce()
        {
            GameContent content = TestContent.Load(TestContent.Data());
            var state = new PlayerState(TestContent.First);

            GameSession battle = SessionAssembler.CreateBattle(content, state);
            Expect.True(state.InBattle, "전투에 들어가 있어야 한다.");
            Expect.Throws<InvalidOperationException>(() => SessionAssembler.CreateBattle(content, state));

            battle.RequestEnd();
            Expect.True(!state.InBattle, "전투가 끝나면 풀려야 한다.");
            SessionAssembler.CreateBattle(content, state);
        }

        // 다음 전투는 같은 진행 상태를 이어받는다. 시간이 끝나 끝난 전투도 진행 상태를 풀어 준다.
        private static void NextBattleKeepsProgress()
        {
            GameContent content = TestContent.Load(TestContent.Data(timeLimit: 1));
            var state = new PlayerState(TestContent.First);
            state.EarnGold(20);

            GameSession first = TestContent.Begun(SessionAssembler.CreateBattle(content, state));
            first.Advance(2);
            Expect.True(!state.InBattle, "시간이 끝나도 풀려야 한다.");

            SessionAssembler.CreateBattle(content, state);
            Expect.Equal(20, state.Gold);
        }

        // 조립이 실패하면(산 노드가 질량 단계 표 밖을 가리키는 등) 진행 상태는 전투에 묶이지 않고, 그대로 다음 조립에 쓸 수 있다.
        private static void FailedAssemblyLeavesProgressFree()
        {
            ContentData data = TestContent.Data();
            data.Enemies.Add(TestContent.Enemy(TestContent.EnemyId));
            GameContent content = TestContent.Load(data);
            var state = new PlayerState(TestContent.First);
            NodeTree tooHeavy = TestContent.Owned(state,
                new Upgrade(EnemyUpgradeStats.MassLevel(TestContent.EnemyId), UpgradeOperation.Add, 1));

            Expect.Throws<ArgumentOutOfRangeException>(() => SessionAssembler.CreateBattle(content, state, 1, tooHeavy));
            Expect.True(!state.InBattle, "실패한 조립이 진행 상태를 묶으면 안 된다.");

            SessionAssembler.CreateBattle(content, state);
            Expect.True(state.InBattle, "다음 조립은 진행 상태를 전투에 들여야 한다.");
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
