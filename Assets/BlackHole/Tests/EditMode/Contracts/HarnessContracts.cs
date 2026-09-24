using System;
using System.Collections.Generic;

namespace BlackHole.Core.Tests
{
    // 실행기 자체의 확인: 판정 도우미가 실패해야 할 때 실제로 실패하는가.
    internal static class HarnessContracts
    {
        public static IEnumerable<Contract> Cases()
        {
            yield return new Contract("Harness.FailedChecksFail", FailedChecksFail);
        }

        private static void FailedChecksFail()
        {
            Expect.Throws<InvalidOperationException>(() => Expect.True(false, "의도한 실패"));
            Expect.Throws<InvalidOperationException>(() => Expect.Equal(1, 2));
            Expect.Throws<InvalidOperationException>(() => Expect.Near(0, 1));
            Expect.Throws<InvalidOperationException>(() => Expect.Throws<ArgumentException>(() => { }));
        }
    }
}
