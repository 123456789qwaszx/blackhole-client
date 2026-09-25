using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 단계에서 나올 수 있는 적 종류의 묶음. 여러 단계가 같은 풀을 쓸 수 있다.
    // 풀에 든 종류가 모두 그대로 나오는 것이 아니다. 무엇이 얼마나 나올지는 풀을 거르는 규칙(적 비율, 출현 제한)이
    // 정한다 — 그 규칙은 아직 없고, 지금 전투의 적은 전투 시작 공급이 정한다.
    public sealed class EnemyPoolDefinition
    {
        public string Id { get; }
        // 풀에 든 적 종류(저작 순서).
        public IReadOnlyList<EnemyDefinition> Enemies { get; }

        public EnemyPoolDefinition(string id, IReadOnlyList<EnemyDefinition> enemies)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID가 비어 있다.", nameof(id));

            if (enemies == null || enemies.Count == 0)
                throw new ArgumentException("적 종류가 하나 이상 필요하다.", nameof(enemies));

            var copy = new EnemyDefinition[enemies.Count];

            for (int i = 0; i < copy.Length; i++)
            {
                EnemyDefinition enemy = enemies[i]
                    ?? throw new ArgumentException($"Enemies[{i}]가 비어 있다.", nameof(enemies));

                if (Array.IndexOf(copy, enemy, 0, i) >= 0)
                    throw new ArgumentException($"적 종류 '{enemy.Id}'가 두 번 들어 있다.", nameof(enemies));

                copy[i] = enemy;
            }

            Id = id;
            Enemies = Array.AsReadOnly(copy);
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
