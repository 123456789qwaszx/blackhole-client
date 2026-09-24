using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 스킬 한 종류의 공유 정의: 쿨다운, 대상 선택, 효과 목록(적용 순서대로).
    // 정의는 실행 규칙을 품지 않는다 — SkillRuleFactory가 해석한다.
    public sealed class SkillDefinition
    {
        public string Id { get; }
        public float Cooldown { get; }
        public TargetSelectionDefinition Selection { get; }
        public IReadOnlyList<SkillEffectDefinition> Effects { get; }

        public SkillDefinition(string id, float cooldown,
            TargetSelectionDefinition selection, IReadOnlyList<SkillEffectDefinition> effects)
        {
            Id = DefinitionGuard.Id(id, nameof(id));
            Cooldown = DefinitionGuard.Positive(cooldown, nameof(cooldown));
            Selection = selection ?? throw new ArgumentNullException(nameof(selection), "대상 선택 정의가 필요하다.");
            if (effects == null || effects.Count == 0)
                throw new ArgumentException("효과가 하나 이상 필요하다.", nameof(effects));

            var copy = new SkillEffectDefinition[effects.Count];
            for (int i = 0; i < copy.Length; i++)
                copy[i] = effects[i] ?? throw new ArgumentNullException($"{nameof(effects)}[{i}]", "효과 정의가 null이다.");
            Effects = Array.AsReadOnly(copy);
        }
    }

    // 스킬 사용자 한 명이 가진 스킬 하나의 실행 상태. 판마다 새로 만든다.
    public sealed class SkillState
    {
        public SkillDefinition Definition { get; }
        public float RemainingCooldown { get; private set; }
        internal ITargetSelector Selector { get; }
        internal IReadOnlyList<ISkillEffect> Effects { get; }

        internal SkillState(SkillDefinition definition, ITargetSelector selector, IReadOnlyList<ISkillEffect> effects)
        {
            Definition = definition;
            Selector = selector;
            Effects = effects;
        }

        internal void Advance(float delta) => RemainingCooldown = Math.Max(0, RemainingCooldown - delta);
        internal void BeginCooldown() => RemainingCooldown = Definition.Cooldown;
    }

    public enum CastResult { Cast, NoTarget, CoolingDown, UnknownSkill, SessionInactive }

    // 발동에 성공했을 때 실제로 적용된 내용. 화면은 이것으로 연출하고 스킬 수치를 다시 적지 않는다.
    public readonly struct CastReport
    {
        public Point2 Aim { get; }
        // 선택에 쓴 범위 반경.
        public float AreaRadius { get; }
        // 효과가 하나 이상 적용된 대상 수.
        public int AffectedCount { get; }

        internal CastReport(Point2 aim, float areaRadius, int affectedCount)
        {
            Aim = aim;
            AreaRadius = areaRadius;
            AffectedCount = affectedCount;
        }
    }
}
