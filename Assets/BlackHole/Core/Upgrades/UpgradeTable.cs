using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 업그레이드 목록을 수치별로 한 번 모아 둔 표. 다른 시스템은 자기 수치의 이름과 기본값을 주고 최종 값을 가져간다.
    // 업그레이드가 어디서 왔는지(노드 트리 등), 수치가 무슨 뜻인지는 모른다.
    //
    // 최종 값 = (기본값 + Σ더하기) × (1 + Σ비율) × Π곱하기. 업그레이드가 없는 수치는 기본값 그대로다.
    // 받은 순서는 결과를 바꾸지 않는다. 같은 업그레이드 묶음이면 어떤 순서로 받아도 끝자리까지 같은 값이다.
    // 한계(0 이상, 1 이하, 정수 등)는 가져가는 시스템이 최종 값에 건다.
    public sealed class UpgradeTable
    {
        private readonly Dictionary<string, Sum> _sums = new Dictionary<string, Sum>(StringComparer.Ordinal);

        public UpgradeTable(IEnumerable<Upgrade> upgrades)
        {
            if (upgrades == null)
                throw new ArgumentNullException(nameof(upgrades));

            var sorted = new List<Upgrade>(upgrades);

            foreach (Upgrade upgrade in sorted)
            {
                if (upgrade.Stat == null)
                    throw new ArgumentException("생성자로 만들지 않은 업그레이드(default)가 있다.", nameof(upgrades));
            }

            // 소수의 합은 더하는 순서에 따라 끝자리가 달라진다. 받은 순서가 결과에 닿지 않도록 정렬한 뒤에 모은다.
            sorted.Sort(Compare);

            foreach (Upgrade upgrade in sorted)
            {
                if (!_sums.TryGetValue(upgrade.Stat, out Sum sum))
                    sum = Sum.None;

                _sums[upgrade.Stat] = sum.With(upgrade.Operation, upgrade.Value);
            }
        }

        public float Apply(string stat, float baseValue)
        {
            if (stat == null)
                throw new ArgumentNullException(nameof(stat));

            return _sums.TryGetValue(stat, out Sum sum) ? sum.Apply(baseValue) : baseValue;
        }

        private static int Compare(Upgrade a, Upgrade b)
        {
            int byStat = string.CompareOrdinal(a.Stat, b.Stat);

            if (byStat != 0)
                return byStat;

            int byOperation = a.Operation.CompareTo(b.Operation);
            return byOperation != 0 ? byOperation : a.Value.CompareTo(b.Value);
        }

        // 한 수치에 모인 업그레이드.
        private readonly struct Sum
        {
            public static readonly Sum None = new Sum(0, 0, 1);

            private readonly double _add;
            private readonly double _percent;
            private readonly double _multiply;

            private Sum(double add, double percent, double multiply)
            {
                _add = add;
                _percent = percent;
                _multiply = multiply;
            }

            public Sum With(UpgradeOperation operation, float value)
            {
                switch (operation)
                {
                    case UpgradeOperation.Add: return new Sum(_add + value, _percent, _multiply);
                    case UpgradeOperation.Percent: return new Sum(_add, _percent + value, _multiply);
                    default: return new Sum(_add, _percent, _multiply * value);
                }
            }

            public float Apply(float baseValue) => (float)((baseValue + _add) * (1 + _percent) * _multiply);
        }
    }
}
