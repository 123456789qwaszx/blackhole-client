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
