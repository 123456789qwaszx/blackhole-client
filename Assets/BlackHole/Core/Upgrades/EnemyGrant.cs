using System;

namespace BlackHole.Core
{
    // Grant를 적용하는 방식. 여러 노드가 같은 수치를 보정하면 산 순서와 관계없이
    // 정하기(가장 큰 값) → 더하기(합) → 곱하기(곱) 순서로 합친다 [임시, SKILL_TREE_PLAN 8.2 합성 규칙].
    public enum GrantOperation
    {
        Add,
        Multiply,
        Set,
    }

    // 노드가 주는 것 중 적 종류의 판 구성 보정(SKILL_TREE_PLAN 4.3 "천체 수치·보상 보정"): 적 종류 + 수치 + 연산 + 값.
    // 노드 트리는 이것을 해석하지 않는다. 산 노드의 Grant를 모아 판 구성을 만드는 일은 Loadout이 한다.
    public readonly struct EnemyGrant
    {
        public EnemyDefinition Enemy { get; }
        public EnemyUpgradeStat Stat { get; }
        public GrantOperation Operation { get; }
        public float Value { get; }

        public EnemyGrant(EnemyDefinition enemy, EnemyUpgradeStat stat, GrantOperation operation, float value)
        {
            Enemy = enemy ?? throw new ArgumentNullException(nameof(enemy), "적 종류가 필요하다.");

            if (!Enum.IsDefined(typeof(EnemyUpgradeStat), stat))
                throw new ArgumentOutOfRangeException(nameof(stat));

            if (!Enum.IsDefined(typeof(GrantOperation), operation))
                throw new ArgumentOutOfRangeException(nameof(operation));

            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), "0 이상의 유한한 값이 필요하다.");

            // 질량 증가는 산 수만큼 단계가 오른다. 한 노드가 단계를 정하거나 곱하지 않는다.
            if (stat == EnemyUpgradeStat.MassLevel && (operation != GrantOperation.Add || value != Math.Floor(value)))
                throw new ArgumentException("질량 단계는 정수를 더하기만 한다.", nameof(operation));

            if (stat != EnemyUpgradeStat.MassLevel && !enemy.CanBeGolden)
                throw new ArgumentException($"'{enemy.Id}'는 황금이 되지 않아 황금 수치를 보정할 수 없다.", nameof(stat));

            Stat = stat;
            Operation = operation;
            Value = value;
        }
    }
}
