using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // Passive Skill 실행 수치에 대한 보정 하나. Skill 성장은 주로 수치 변화지만, 그 출처(레벨업, 강화 등)는 미정이다.
    public interface IPassiveSkillModifier
    {
        PassiveSkillStats Apply(PassiveSkillDefinition definition, PassiveSkillStats current);
    }

    // 실행 수치 = 기본 수치 + 보정(순서대로). 기본 정의는 바뀌지 않는다.
    public static class PassiveSkillStatCalculator
    {
        public static PassiveSkillStats Compute(PassiveSkillDefinition definition,
            IReadOnlyList<IPassiveSkillModifier> modifiers)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            PassiveSkillStats stats = definition.BaseStats;
            if (modifiers == null) return stats;
            foreach (IPassiveSkillModifier modifier in modifiers)
                stats = modifier.Apply(definition, stats);
            return stats;
        }
    }
}
