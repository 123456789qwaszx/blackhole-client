using System;

namespace BlackHole.Core
{
    // 스킬 선택·효과 정의를 실행 규칙으로 해석하는 유일한 자리.
    // ContentInvariants가 모든 스킬이 해석되는지 확인하므로, 판 안에서는 Create가 실패하지 않는다.
    internal static class SkillRuleFactory
    {
        public static bool TryCreateSelector(TargetSelectionDefinition definition, out ITargetSelector selector)
        {
            switch (definition)
            {
                case NearestInRadiusDefinition nearest:
                    selector = new NearestInRadiusSelector(nearest);
                    return true;
                case AllInRadiusDefinition all:
                    selector = new AllInRadiusSelector(all);
                    return true;
                default:
                    selector = null;
                    return false;
            }
        }

        public static bool TryCreateEffect(SkillEffectDefinition definition, out ISkillEffect effect)
        {
            switch (definition)
            {
                case DamageEffectDefinition damage:
                    effect = new DamageEffect(damage);
                    return true;
                case PullEffectDefinition pull:
                    effect = new PullEffect(pull);
                    return true;
                default:
                    effect = null;
                    return false;
            }
        }

        public static SkillState CreateState(SkillDefinition definition)
        {
            if (!TryCreateSelector(definition.Selection, out ITargetSelector selector))
                throw Unsupported(definition.Selection);

            var effects = new ISkillEffect[definition.Effects.Count];
            for (int i = 0; i < effects.Length; i++)
                if (!TryCreateEffect(definition.Effects[i], out effects[i]))
                    throw Unsupported(definition.Effects[i]);

            return new SkillState(definition, selector, Array.AsReadOnly(effects));
        }

        public static string Describe(object definition) =>
            $"실행 규칙이 연결되지 않은 종류 '{definition.GetType().Name}'.";

        private static ArgumentException Unsupported(object definition) =>
            new ArgumentException(Describe(definition), nameof(definition));
    }
}
