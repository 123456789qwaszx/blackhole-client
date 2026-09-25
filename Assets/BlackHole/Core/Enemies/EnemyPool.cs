using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 적 풀의 항목 하나: 나올 수 있는 적 종류와, 그 종류가 판에 동시에 살아 있을 수 있는 최대 수(출현 제한).
    public readonly struct EnemyPoolEntry
    {
        public EnemyDefinition Enemy { get; }
        public int MaxAlive { get; }

        public EnemyPoolEntry(EnemyDefinition enemy, int maxAlive)
        {
            if (maxAlive < 1)
                throw new ArgumentOutOfRangeException(nameof(maxAlive), "1 이상의 정수가 필요하다.");

            Enemy = enemy ?? throw new ArgumentNullException(nameof(enemy));
            MaxAlive = maxAlive;
        }
    }

    // 단계에서 나올 수 있는 적 종류의 묶음. 여러 단계가 같은 풀을 쓸 수 있다.
    // 공급이 요청한 적은 이 풀을 거쳐서만 나온다(PoolFilter): 풀에 없는 종류와, 최대 수에 닿은 종류는 나오지 않는다.
    public sealed class EnemyPoolDefinition
    {
        public string Id { get; }
        // 풀의 항목(저작 순서). 한 종류는 한 번만 든다.
        public IReadOnlyList<EnemyPoolEntry> Entries { get; }

        public EnemyPoolDefinition(string id, IReadOnlyList<EnemyPoolEntry> entries)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID가 비어 있다.", nameof(id));

            if (entries == null || entries.Count == 0)
                throw new ArgumentException("적 종류가 하나 이상 필요하다.", nameof(entries));

            var copy = new EnemyPoolEntry[entries.Count];
            var seen = new HashSet<EnemyDefinition>();

            for (int i = 0; i < copy.Length; i++)
            {
                EnemyPoolEntry entry = entries[i];

                if (entry.Enemy == null)
                    throw new ArgumentException($"Entries[{i}]의 적 종류가 비어 있다.", nameof(entries));

                if (!seen.Add(entry.Enemy))
                    throw new ArgumentException($"적 종류 '{entry.Enemy.Id}'가 두 번 들어 있다.", nameof(entries));

                copy[i] = entry;
            }

            Id = id;
            Entries = Array.AsReadOnly(copy);
        }
    }

    // 진행도(적의 강도 단계) 하나. HQ 성장 단계와 다르다.
    // 이 단계에서 쓰는 적 풀을 가진다. 체력·크기 계수는 단계 표에 더해질 때 여기에 붙는다.
    public sealed class StageDefinition
    {
        // 1부터 시작하는 단계 번호.
        public int Number { get; }
        public EnemyPoolDefinition Pool { get; }

        public StageDefinition(int number, EnemyPoolDefinition pool)
        {
            if (number < 1)
                throw new ArgumentOutOfRangeException(nameof(number), "단계 번호는 1부터다.");

            Number = number;
            Pool = pool ?? throw new ArgumentNullException(nameof(pool), "적 풀이 필요하다.");
        }
    }
}
