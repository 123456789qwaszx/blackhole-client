using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 두 실제 실행 규칙이 공유하는 확장점. 보상과 Session 수명은 알지 못한다.
    public interface ISkillEffect
    {
        int Execute(Point2 aim, float damageMultiplier,
            IReadOnlyList<TargetState> targets, CombatResolver combat);
    }

    public sealed class SkillDefinition
    {
        public string Id { get; }
        public float Cooldown { get; }
        public ISkillEffect Effect { get; }

        public SkillDefinition(string id, float cooldown, ISkillEffect effect)
        {
            Id = DefinitionGuard.Id(id);
            Cooldown = DefinitionGuard.Positive(cooldown, nameof(cooldown));
            Effect = effect ?? throw new ArgumentNullException(nameof(effect));
        }
    }

    public sealed class SkillState
    {
        public SkillDefinition Definition { get; }
        public float RemainingCooldown { get; private set; }

        internal SkillState(SkillDefinition definition) { Definition = definition; }
        internal void Advance(float delta) => RemainingCooldown = Math.Max(0, RemainingCooldown - delta);
        internal void BeginCooldown() => RemainingCooldown = Definition.Cooldown;
    }

    public enum CastResult { Cast, NoTarget, CoolingDown, UnknownSkill, SessionInactive }
}
