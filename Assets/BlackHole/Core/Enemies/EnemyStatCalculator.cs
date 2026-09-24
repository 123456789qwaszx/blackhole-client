using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // Enemy 실행 수치에 대한 보정 하나. 보정의 출처(진행도, PlayerState, 획득 능력)와 방식은 미정이다(B4).
    public interface IEnemyStatModifier
    {
        EnemyStats Apply(EnemyDefinition definition, EnemyStats current);
    }

    // 실행 수치 = 기본 수치 + 보정(순서대로). 기본 정의는 바뀌지 않는다.
    // 계산 시점은 [임시]로 출현 때 1회다(EnemySpawner). 살아 있는 Enemy에 즉시 반영할지는 미정이다.
    public static class EnemyStatCalculator
    {
        public static EnemyStats Compute(EnemyDefinition definition, IReadOnlyList<IEnemyStatModifier> modifiers)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            EnemyStats stats = definition.BaseStats;
            if (modifiers == null) return stats;
            foreach (IEnemyStatModifier modifier in modifiers)
                stats = modifier.Apply(definition, stats);
            return stats;
        }
    }
}
