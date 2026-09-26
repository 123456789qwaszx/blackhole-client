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
            yield return new Contract("Progress.BattleGoldIsSettledOnceAfterTheEnd", BattleGoldIsSettledOnceAfterTheEnd);
        }

        // 판이 번 Gold는 전투 중에는 진행 상태에 들어가지 않는다. 판이 끝난 뒤 결산 때 한 번만 들어간다.
        private static void BattleGoldIsSettledOnceAfterTheEnd()
        {
            ContentData data = TestContent.Arena(3, 3, TestContent.Supply(TestContent.EnemyId, 1));
            data.Enemies.Add(TestContent.Enemy(TestContent.EnemyId, health: 1, gold: 7));
            TestContent.Allow(data, TestContent.EnemyId);
            var state = new PlayerState(TestContent.First);
            state.EarnGold(20);
            GameSession game = TestContent.Begun(SessionAssembler.CreateBattle(TestContent.Load(data), new[] { state }));

            game.World.DealDamage(game.World.Enemies[0], new Damage(1, TestContent.First));
            Expect.Equal(20L, state.Gold);
            Expect.Throws<InvalidOperationException>(() => game.Settle());

            game.RequestEnd(SessionEndReason.TimeExpired);
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
