using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // Enemy 실행 수치에 대한 보정 하나. 보정의 출처는 구매한 업그레이드다(연결은 M6).
    public interface IEnemyStatModifier
    {
        EnemyStats Apply(EnemyDefinition definition, EnemyStats current);
    }

    // 실행 수치 = 기본 수치 + 보정(순서대로). 기본 정의는 바뀌지 않는다.
    // 계산 시점은 출현 때 1회로 확정이다(EnemySpawner). 살아 있는 Enemy에는 다시 적용하지 않는다.
    public static class EnemyStatCalculator
    {
        public static EnemyStats Compute(
            EnemyDefinition definition,
            IReadOnlyList<IEnemyStatModifier> modifiers)
        {
            if (definition == null) 
                throw new ArgumentNullException(nameof(definition));
            
            EnemyStats stats = definition.BaseStats;
            
            if (modifiers == null)
                return stats;
            
            foreach (IEnemyStatModifier modifier in modifiers)
                stats = modifier.Apply(definition, stats);
            
            return stats;
        }
    }
}
