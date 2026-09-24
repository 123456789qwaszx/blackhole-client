namespace BlackHole.Core
{
    // 스킬을 이루는 두 부분의 정의: 누구를 고르는가(선택)와 고른 대상에게 무엇을 하는가(효과).
    // 기존 선택·효과의 조합은 SkillDefinition 데이터로 만든다.
    // 새 선택이나 새 효과의 의미는 하위 정의 + 실행 규칙(SkillRules) + SkillRuleFactory 분기 +
    // ContentLoader 종류 이름으로 추가한다. Session·Loadout·HUD는 바뀌지 않는다.

    // ── 대상 선택 ──────────────────────────────────────────────────────────

    public abstract class TargetSelectionDefinition
    {
        private protected TargetSelectionDefinition() { }
    }

    // 조준점에서 Radius 안에 있는 살아 있는 대상 중 가장 가까운 하나.
    public sealed class NearestInRadiusDefinition : TargetSelectionDefinition
    {
        public float Radius { get; }

        public NearestInRadiusDefinition(float radius)
        {
            Radius = DefinitionGuard.Positive(radius, nameof(radius));
        }
    }

    // 조준점에서 Radius 안에 있는 살아 있는 대상 전부.
    public sealed class AllInRadiusDefinition : TargetSelectionDefinition
    {
        public float Radius { get; }

        public AllInRadiusDefinition(float radius)
        {
            Radius = DefinitionGuard.Positive(radius, nameof(radius));
        }
    }

    // ── 효과 ───────────────────────────────────────────────────────────────

    public abstract class SkillEffectDefinition
    {
        private protected SkillEffectDefinition() { }
    }

    // 피해. 실제 피해량 = Amount × 사용자의 공격 배율.
    public sealed class DamageEffectDefinition : SkillEffectDefinition
    {
        public float Amount { get; }

        public DamageEffectDefinition(float amount)
        {
            Amount = DefinitionGuard.Positive(amount, nameof(amount));
        }
    }

    // 블랙홀 중심 방향 당김.
    public sealed class PullEffectDefinition : SkillEffectDefinition
    {
        public float Distance { get; }

        public PullEffectDefinition(float distance)
        {
            Distance = DefinitionGuard.Positive(distance, nameof(distance));
        }
    }
}
