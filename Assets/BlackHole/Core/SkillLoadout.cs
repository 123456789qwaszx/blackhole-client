using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 스킬 사용자 한 명의 보유 스킬과 쿨다운. 판마다 새로 조립한다.
    // 정의는 ContentCatalog가 검증했다(ID 유일, 선택·효과가 실행 규칙으로 해석됨).
    internal sealed class SkillLoadout
    {
        private readonly Dictionary<string, SkillState> _byId =
            new Dictionary<string, SkillState>(StringComparer.Ordinal);
        // 한 번의 시전에서 고른 대상. 시전마다 비우고 다시 쓴다.
        private readonly List<TargetState> _selected = new List<TargetState>();

        public IReadOnlyList<SkillState> Skills { get; }

        public SkillLoadout(IReadOnlyList<SkillDefinition> definitions)
        {
            var states = new List<SkillState>(definitions.Count);
            foreach (SkillDefinition definition in definitions)
            {
                SkillState state = SkillRuleFactory.CreateState(definition);
                _byId.Add(definition.Id, state);
                states.Add(state);
            }
            Skills = states.AsReadOnly();
        }

        public void Advance(float delta)
        {
            foreach (SkillState skill in Skills) skill.Advance(delta);
        }

        // 시전 순서:
        // 1. 자격: 알 수 없는 스킬, 쿨다운 중이면 아무것도 바꾸지 않고 거절한다.
        // 2. 선택: 효과를 적용하기 전에 대상 집합을 확정한다. 이후 효과가 위치·상태를 바꿔도 다시 고르지 않는다.
        // 3. 적용: 고른 순서대로, 대상마다 정의된 효과 순서대로 적용한다.
        //    피해로 사망한 대상에도 같은 시전의 뒤 효과(당김)는 적용된다.
        // 4. 확정: 효과가 하나라도 적용됐을 때만 쿨다운을 시작한다(빈 조준은 쿨다운을 소비하지 않는다).
        public CastResult TryCast(string id, in CastContext context, TargetWorld world, CombatResolver combat,
            out CastReport report)
        {
            report = default;
            if (id == null || !_byId.TryGetValue(id, out SkillState skill))
                return CastResult.UnknownSkill;
            if (skill.RemainingCooldown > 0) return CastResult.CoolingDown;

            _selected.Clear();
            skill.Selector.Select(context.Aim, world.Targets, _selected);

            int affected = 0;
            foreach (TargetState target in _selected)
                if (ApplyEffects(skill, target, context, combat)) affected++;
            _selected.Clear();

            if (affected == 0) return CastResult.NoTarget;
            skill.BeginCooldown();
            report = new CastReport(context.Aim, skill.Selector.AreaRadius, affected);
            return CastResult.Cast;
        }

        private static bool ApplyEffects(SkillState skill, TargetState target, in CastContext context,
            CombatResolver combat)
        {
            bool applied = false;
            foreach (ISkillEffect effect in skill.Effects)
                applied |= effect.Apply(target, context, combat);
            return applied;
        }
    }
}
