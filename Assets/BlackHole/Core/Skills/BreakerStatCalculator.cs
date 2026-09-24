using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // Breaker 실행 수치에 대한 보정 하나. 지금 보정의 출처는 구매한 업그레이드다.
    public interface IBreakerStatModifier
    {
        BreakerStats Apply(BreakerSkillDefinition definition, BreakerStats current);
    }

    // 실행 수치 = 기본 수치 + 보정(순서대로). 기본 정의는 바뀌지 않는다.
    public static class BreakerStatCalculator
    {
        public static BreakerStats Compute(BreakerSkillDefinition definition,
            IReadOnlyList<IBreakerStatModifier> modifiers)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            BreakerStats stats = definition.BaseStats;
            if (modifiers == null) return stats;
            foreach (IBreakerStatModifier modifier in modifiers)
                stats = modifier.Apply(definition, stats);
            return stats;
        }
    }
}
