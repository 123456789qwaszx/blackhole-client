using System;

namespace BlackHole.Core
{
    // 업그레이드 효과의 종류. 종류마다 받는 시스템과 적용 시점이 하나씩 정해져 있다(UpgradeModifiers).
    // 콘텐츠 데이터는 이 이름을 문자열로 쓴다. 새 종류는 여기, 대상 규칙, 적용 자리에 하나씩 더한다.
    public enum UpgradeEffectKind
    {
        // 대상 Breaker의 피해량 + 값. 전투 조립 때.
        SkillDamageAdd,
        // 대상 Breaker의 반경 + 값. 전투 조립 때. 화면의 원도 같은 값을 읽는다.
        SkillRadiusAdd,
        // 대상 Breaker의 주기 × 값. 전투 조립 때.
        SkillIntervalMultiply,
        // 대상 적의 HP × 값. 출현 때.
        EnemyHealthMultiply,
        // 대상 적의 Gold 보상 × 값. 출현 때.
        GoldMultiply,
        // 대상 적의 HQ EXP 보상 × 값. 출현 때.
        HqExpMultiply,
        // 성장 공급에서 대상 적의 수 + 값(정수). 공급 요청 때.
        GrowthSupplyAdd
    }

    // 업그레이드 노드의 보정 하나.
    // Skill 효과는 대상 Skill이 반드시 있다(Skill은 각자 자기 특성을 가진 개별 존재다).
    // 현재 Skill 계열 효과는 구현된 보정이 Breaker뿐이므로 Breaker만 대상으로 허용한다.
    // 다른 종류를 받으면 구매해도 아무 일이 없는 노드가 되므로 정의 오류로 막는다. 레이저 대상 효과의 정책은 CA-004에서 정한다.
    // 공급 효과는 대상 적이 반드시 있다. 적 수치·보상 효과의 대상 적이 없으면 모든 종류에 적용한다.
    public sealed class UpgradeEffect
    {
        public UpgradeEffectKind Kind { get; }
        public float Value { get; }
        public PassiveSkillDefinition Skill { get; }
        public EnemyDefinition Enemy { get; }

        public UpgradeEffect(
            UpgradeEffectKind kind,
            float value,
            PassiveSkillDefinition skill = null,
            EnemyDefinition enemy = null)
        {
            if (!Enum.IsDefined(typeof(UpgradeEffectKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind));

            if (TargetsSkill(kind) != (skill != null))
                throw new ArgumentException(
                    TargetsSkill(kind) ? "대상 Skill이 필요하다." : "Skill 대상을 받지 않는 종류다.",
                    nameof(skill));

            if (TargetsSkill(kind) && enemy != null)
                throw new ArgumentException("Skill 효과는 적 대상을 받지 않는다.", nameof(enemy));

            if (TargetsSkill(kind) && !(skill is BreakerSkillDefinition))
                throw new ArgumentException(
                    $"{kind}는 Breaker 종류의 Skill만 대상으로 한다. '{skill.Id}'는 {skill.GetType().Name}이다.",
                    nameof(skill));

            if (RequiresEnemy(kind) && enemy == null)
                throw new ArgumentException("대상 적이 필요하다.", nameof(enemy));

            DefinitionGuard.Positive(value, nameof(value));

            if (kind == UpgradeEffectKind.GrowthSupplyAdd && value != Math.Floor(value))
                throw new ArgumentOutOfRangeException(nameof(value), "양의 정수가 필요하다.");

            Kind = kind;
            Value = value;
            Skill = skill;
            Enemy = enemy;
        }

        // 대상 규칙의 유일한 자리. 로더도 이 규칙으로 데이터의 대상 ID를 해석한다.
        public static bool TargetsSkill(UpgradeEffectKind kind) =>
            kind == UpgradeEffectKind.SkillDamageAdd
            || kind == UpgradeEffectKind.SkillRadiusAdd
            || kind == UpgradeEffectKind.SkillIntervalMultiply;

        public static bool RequiresEnemy(UpgradeEffectKind kind) =>
            kind == UpgradeEffectKind.GrowthSupplyAdd;
    }
}
