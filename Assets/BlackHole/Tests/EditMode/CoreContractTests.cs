using System;
using System.Collections;
using NUnit.Framework;

namespace BlackHole.Core.Tests
{
    public sealed class CoreContractTests
    {
        public static IEnumerable Contracts()
        {
            foreach (var test in CoreContracts.Cases())
                yield return new TestCaseData(test.Value).SetName(test.Key);
        }

        [TestCaseSource(nameof(Contracts))]
        public void Contract(Action run) { run(); }
    }
}
