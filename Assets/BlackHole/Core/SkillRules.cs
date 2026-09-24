using System.Collections.Generic;

namespace BlackHole.Core
{
    // 효과 적용에 필요한 사용자 쪽 값. 효과 규칙의 인자가 늘지 않도록 여기에 모은다.
    internal readonly struct CastContext
    {
        public readonly Point2 Aim;
        public readonly float DamageMultiplier;

        public CastContext(Point2 aim, float damageMultiplier)
        {
            Aim = aim;
            DamageMultiplier = damageMultiplier;
        }
    }

    // ── 대상 선택 규칙 ─────────────────────────────────────────────────────
    // 선택은 상태를 바꾸지 않는다. 고른 대상을 into에 목록 순서대로 더한다.

    internal interface ITargetSelector
    {
        float AreaRadius { get; }
        void Select(Point2 aim, IReadOnlyList<TargetState> targets, List<TargetState> into);
    }

    internal sealed class NearestInRadiusSelector : ITargetSelector
    {
        private readonly NearestInRadiusDefinition _definition;

        public NearestInRadiusSelector(NearestInRadiusDefinition definition)
        {
            _definition = definition;
        }

        public float AreaRadius => _definition.Radius;

        // 거리가 같으면 목록 앞쪽 대상을 고른다.
        public void Select(Point2 aim, IReadOnlyList<TargetState> targets, List<TargetState> into)
        {
            TargetState nearest = null;
            float best = _definition.Radius * _definition.Radius;
            foreach (TargetState target in targets)
            {
                if (target.Phase != TargetPhase.Alive) continue;
                float distance = target.Position.DistanceSquared(aim);
                if (distance > best || (nearest != null && distance == best)) continue;
                nearest = target;
                best = distance;
            }
            if (nearest != null) into.Add(nearest);
        }
    }

    internal sealed class AllInRadiusSelector : ITargetSelector
    {
        private readonly AllInRadiusDefinition _definition;

        public AllInRadiusSelector(AllInRadiusDefinition definition)
        {
            _definition = definition;
        }

        public float AreaRadius => _definition.Radius;

        public void Select(Point2 aim, IReadOnlyList<TargetState> targets, List<TargetState> into)
        {
            float radiusSquared = _definition.Radius * _definition.Radius;
            foreach (TargetState target in targets)
            {
                if (target.Phase == TargetPhase.Alive && target.Position.DistanceSquared(aim) <= radiusSquared)
                    into.Add(target);
            }
        }
    }

    // ── 효과 규칙 ──────────────────────────────────────────────────────────
    // 효과는 대상 상태를 직접 쓰지 않고 CombatResolver에 요청한다. 적용됐으면 true.

    internal interface ISkillEffect
    {
        bool Apply(TargetState target, in CastContext context, CombatResolver combat);
    }

    internal sealed class DamageEffect : ISkillEffect
    {
        private readonly DamageEffectDefinition _definition;

        public DamageEffect(DamageEffectDefinition definition)
        {
            _definition = definition;
        }

        public bool Apply(TargetState target, in CastContext context, CombatResolver combat) =>
            combat.Damage(target.Id, _definition.Amount * context.DamageMultiplier);
    }

    internal sealed class PullEffect : ISkillEffect
    {
        private readonly PullEffectDefinition _definition;

        public PullEffect(PullEffectDefinition definition)
        {
            _definition = definition;
        }

        public bool Apply(TargetState target, in CastContext context, CombatResolver combat) =>
            combat.Pull(target.Id, _definition.Distance);
    }
}
