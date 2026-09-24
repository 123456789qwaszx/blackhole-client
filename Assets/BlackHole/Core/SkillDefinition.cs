using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    public enum SkillKind { FocusedStrike, GravityPulse }

    // 스킬 한 종류의 공유 정의: 종류와 수치만 가진다.
    // 종류를 실행 규칙으로 바꾸는 일은 SkillEffectFactory 한 곳이 맡는다.
    // 종류별로 쓰는 수치가 다르다(집중 공격은 당김을 쓰지 않는다). 그 조합 규칙도 factory가 본다.
    public sealed class SkillDefinition
    {
        public string Id { get; }
        public SkillKind Kind { get; }
        public float Cooldown { get; }
        public float Damage { get; }
        // 집중 공격: 조준 허용 반경. 중력파: 효과 범위.
        public float Radius { get; }
        public float PullDistance { get; }

        public SkillDefinition(string id, SkillKind kind, float cooldown,
            float damage, float radius, float pullDistance)
        {
            Id = DefinitionGuard.Id(id, nameof(id));
            if (!Enum.IsDefined(typeof(SkillKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind), $"정의되지 않은 스킬 종류 값 {(int)kind}.");
            Kind = kind;
            Cooldown = DefinitionGuard.Positive(cooldown, nameof(cooldown));
            Damage = DefinitionGuard.Positive(damage, nameof(damage));
            Radius = DefinitionGuard.Positive(radius, nameof(radius));
            PullDistance = DefinitionGuard.NonNegative(pullDistance, nameof(pullDistance));
        }
    }

    // 실행 규칙의 확장점. 보상과 Session 수명은 알지 못한다.
    // 구현은 SkillEffectFactory만 만든다. 정의는 구현을 품지 않는다.
    internal interface ISkillEffect
    {
        int Execute(Point2 aim, float damageMultiplier,
            IReadOnlyList<TargetState> targets, CombatResolver combat);
    }

    // 스킬 사용자 한 명이 가진 스킬 하나의 실행 상태. 판마다 새로 만든다.
    public sealed class SkillState
    {
        public SkillDefinition Definition { get; }
        public float RemainingCooldown { get; private set; }
        internal ISkillEffect Effect { get; }

        internal SkillState(SkillDefinition definition, ISkillEffect effect)
        {
            Definition = definition;
            Effect = effect;
        }

        internal void Advance(float delta) => RemainingCooldown = Math.Max(0, RemainingCooldown - delta);
        internal void BeginCooldown() => RemainingCooldown = Definition.Cooldown;
    }

    public enum CastResult { Cast, NoTarget, CoolingDown, UnknownSkill, SessionInactive }
}
