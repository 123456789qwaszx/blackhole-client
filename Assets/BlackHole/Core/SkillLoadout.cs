using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    public sealed class SkillLoadout
    {
        private readonly Dictionary<string, SkillState> _byId =
            new Dictionary<string, SkillState>(StringComparer.Ordinal);
        public IReadOnlyList<SkillState> Skills { get; }

        public SkillLoadout(IReadOnlyList<SkillDefinition> definitions)
        {
            if (definitions == null || definitions.Count == 0)
                throw new ArgumentException("스킬 정의가 필요하다.");
            var states = new List<SkillState>();
            foreach (SkillDefinition definition in definitions)
            {
                if (definition == null || _byId.ContainsKey(definition.Id))
                    throw new ArgumentException("스킬 정의가 null이거나 ID가 중복됐다.");
                var state = new SkillState(definition);
                _byId.Add(definition.Id, state);
                states.Add(state);
            }
            Skills = states.AsReadOnly();
        }

        internal void Advance(float delta)
        {
            foreach (SkillState skill in Skills) skill.Advance(delta);
        }

        internal CastResult TryCast(string id, Point2 aim, float multiplier,
            TargetWorld world, CombatResolver combat)
        {
            if (id == null || !_byId.TryGetValue(id, out SkillState skill))
                return CastResult.UnknownSkill;
            if (skill.RemainingCooldown > 0) return CastResult.CoolingDown;

            int hits = skill.Definition.Effect.Execute(aim, multiplier, world.Targets, combat);
            // 빈 조준은 쿨다운을 소비하지 않는 것이 이번 실험의 정책이다.
            if (hits == 0) return CastResult.NoTarget;
            skill.BeginCooldown();
            return CastResult.Cast;
        }
    }
}
