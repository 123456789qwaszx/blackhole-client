using System;
using System.Collections.Generic;
using BlackHole.Core;

namespace BlackHole.Core.Tests
{
    // 업그레이드 시스템: 업그레이드 목록을 수치별로 모아 기본값에 적용한다. 수치 이름의 뜻과 업그레이드의 출처는 모른다.
    internal static class UpgradeContracts
    {
        public static IEnumerable<Contract> Cases()
        {
            yield return new Contract("Upgrade.StatWithoutUpgradesKeepsItsBaseValue", StatWithoutUpgradesKeepsItsBaseValue);
            yield return new Contract("Upgrade.AddThenPercentThenMultiply", AddThenPercentThenMultiply);
            yield return new Contract("Upgrade.OrderDoesNotChangeTheValue", OrderDoesNotChangeTheValue);
            yield return new Contract("Upgrade.RejectsUpgradeWithoutStatOrFiniteValue", RejectsUpgradeWithoutStatOrFiniteValue);
        }

        // 업그레이드가 없는 수치는 기본값 그대로다. 다른 수치의 업그레이드는 섞이지 않는다. 이름은 대소문자까지 같아야 같은 수치다.
        private static void StatWithoutUpgradesKeepsItsBaseValue()
        {
            var table = new UpgradeTable(new[] { new Upgrade("damage", UpgradeOperation.Add, 5) });

            Expect.Equal(8f, table.Apply("damage", 3));
            Expect.Equal(3f, table.Apply("radius", 3));
            Expect.Equal(3f, table.Apply("Damage", 3));
            Expect.Equal(3f, new UpgradeTable(Array.Empty<Upgrade>()).Apply("damage", 3));
        }

        // (기본값 + Σ더하기) × (1 + Σ비율) × Π곱하기. 비율끼리는 더하고(25% + 50% = 175%), 곱하기끼리는 곱한다(×10 ×2 = ×20).
        private static void AddThenPercentThenMultiply()
        {
            var table = new UpgradeTable(new[]
            {
                new Upgrade("s", UpgradeOperation.Multiply, 10),
                new Upgrade("s", UpgradeOperation.Percent, 0.25f),
                new Upgrade("s", UpgradeOperation.Add, 2),
                new Upgrade("s", UpgradeOperation.Percent, 0.5f),
                new Upgrade("s", UpgradeOperation.Add, 3),
                new Upgrade("s", UpgradeOperation.Multiply, 2),
            });

            // (10 + 5) × 1.75 × 20
            Expect.Near(525f, table.Apply("s", 10));
        }

        // 같은 업그레이드 묶음이면 어떤 순서로 받아도 끝자리까지 같은 값이다.
        private static void OrderDoesNotChangeTheValue()
        {
            var upgrades = new List<Upgrade>
            {
                // 자릿수가 크게 다른 값은 더하는 순서에 따라 합이 달라진다: (1e20 + 1) - 1e20 = 0, (1e20 - 1e20) + 1 = 1.
                new Upgrade("s", UpgradeOperation.Add, 1e20f),
                new Upgrade("s", UpgradeOperation.Add, -1e20f),
                new Upgrade("s", UpgradeOperation.Add, 1),
            };

            foreach (float value in new[] { 0.1f, 0.2f, 0.3f, 0.7f, 1.3f })
            {
                upgrades.Add(new Upgrade("s", UpgradeOperation.Percent, value));
                upgrades.Add(new Upgrade("s", UpgradeOperation.Multiply, 1 + value));
                upgrades.Add(new Upgrade("t", UpgradeOperation.Add, value));
            }

            var table = new UpgradeTable(upgrades);
            float s = table.Apply("s", 0.9f);
            float t = table.Apply("t", 0.9f);
            var random = new Random(7);

            for (int round = 0; round < 50; round++)
            {
                Shuffle(upgrades, random);
                var shuffled = new UpgradeTable(upgrades);

                Expect.Equal(s, shuffled.Apply("s", 0.9f));
                Expect.Equal(t, shuffled.Apply("t", 0.9f));
            }
        }

        private static void RejectsUpgradeWithoutStatOrFiniteValue()
        {
            Expect.Throws<ArgumentException>(() => new Upgrade(" ", UpgradeOperation.Add, 1));
            Expect.Throws<ArgumentException>(() => new Upgrade("s", UpgradeOperation.Add, float.NaN));
            Expect.Throws<ArgumentException>(() => new Upgrade("s", UpgradeOperation.Multiply, float.PositiveInfinity));
            Expect.Throws<ArgumentException>(() => new UpgradeTable(new[] { default(Upgrade) }));
        }

        private static void Shuffle(List<Upgrade> list, Random random)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
