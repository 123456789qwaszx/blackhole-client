using System;
using System.Collections;
using NUnit.Framework;

namespace BlackHole.Core.Tests
{
    // Unity EditMode에서 계약 목록을 실행한다. tests/CoreSmoke도 같은 목록을 실행한다.
    public sealed class ContractTests
    {
        public static IEnumerable Cases()
        {
            foreach (Contract contract in Contracts.All())
                yield return new TestCaseData(contract.Run).SetName(contract.Name);
        }

        [TestCaseSource(nameof(Cases))]
        public void Holds(Action run) => run();
    }
}
