using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 스킬 사용자 한 명의 보유 스킬과 쿨다운. 판마다 새로 조립한다.
    // 정의는 ContentCatalog가 검증했다(ID 유일, 종류별 수치 조합).
    internal sealed class SkillLoadout
    {
        private readonly Dictionary<string, SkillState> _byId =
            new Dictionary<string, SkillState>(StringComparer.Ordinal);
        public IReadOnlyList<SkillState> Skills { get; }

        public SkillLoadout(IReadOnlyList<SkillDefinition> definitions)
        {
            var states = new List<SkillState>(definitions.Count);
            foreach (SkillDefinition definition in definitions)
            {
                var state = new SkillState(definition, SkillEffectFactory.Create(definition));
                _byId.Add(definition.Id, state);
                states.Add(state);
            }
            Skills = states.AsReadOnly();
        }

        public void Advance(float delta)
        {
            foreach (SkillState skill in Skills) skill.Advance(delta);
        }

        public CastResult TryCast(string id, Point2 aim, float multiplier,
            TargetWorld world, CombatResolver combat)
        {
            if (id == null || !_byId.TryGetValue(id, out SkillState skill))
                return CastResult.UnknownSkill;
            if (skill.RemainingCooldown > 0) return CastResult.CoolingDown;

            int hits = skill.Effect.Execute(aim, multiplier, world.Targets, combat);
            // 빈 조준은 쿨다운을 소비하지 않는 것이 이번 실험의 정책이다.
            if (hits == 0) return CastResult.NoTarget;
            skill.BeginCooldown();
            return CastResult.Cast;
        }
    }
}
