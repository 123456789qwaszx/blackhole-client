using System;

namespace BlackHole.Core
{
    // 스킬 종류를 실행 규칙으로 해석하는 유일한 자리.
    // 새 실행 규칙: SkillKind 값 + 여기 두 분기 + 실행 클래스. Session·Loadout·UI는 바뀌지 않는다.
    // 같은 수치의 변형은 정의 추가만으로 만든다.
    internal static class SkillEffectFactory
    {
        // 종류별 수치 조합 규칙. ContentInvariants가 진단으로 모으고, Create가 생성 보장에 쓴다.
        public static string Verify(SkillDefinition definition)
        {
            switch (definition.Kind)
            {
                case SkillKind.FocusedStrike:
                    return definition.PullDistance == 0 ? null : "집중 공격은 당김 거리를 쓰지 않는다. 0으로 둘 것.";
                case SkillKind.GravityPulse:
                    return definition.PullDistance > 0 ? null : "중력파는 양수 당김 거리가 필요하다.";
                default:
                    return $"실행 규칙이 연결되지 않은 스킬 종류 '{definition.Kind}'.";
            }
        }

        public static ISkillEffect Create(SkillDefinition definition)
        {
            string error = Verify(definition);
            if (error != null) throw new ArgumentException(error, nameof(definition));

            switch (definition.Kind)
            {
                case SkillKind.FocusedStrike:
                    return new FocusedStrike(definition.Damage, definition.Radius);
                case SkillKind.GravityPulse:
                    return new GravityPulse(definition.Damage, definition.Radius, definition.PullDistance);
                default:
                    throw new ArgumentOutOfRangeException(nameof(definition));
            }
        }
    }
}
