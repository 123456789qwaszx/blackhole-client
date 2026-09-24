using System.Collections.Generic;

namespace BlackHole.Core
{
    // 조준점 주변의 가장 가까운 살아 있는 대상 하나를 공격한다.
    public sealed class FocusedStrike : ISkillEffect
    {
        private readonly float _damage;
        private readonly float _aimRadius;

        public FocusedStrike(float damage, float aimRadius)
        {
            _damage = DefinitionGuard.Positive(damage, nameof(damage));
            _aimRadius = DefinitionGuard.Positive(aimRadius, nameof(aimRadius));
        }

        public int Execute(Point2 aim, float damageMultiplier,
            IReadOnlyList<TargetState> targets, CombatResolver combat)
        {
            TargetState nearest = null;
            float best = _aimRadius * _aimRadius;
            foreach (TargetState target in targets)
            {
                if (target.Phase != TargetPhase.Orbiting) continue;
                float distance = target.Position.DistanceSquared(aim);
                if (distance > best || (nearest != null && distance == best)) continue;
                nearest = target;
                best = distance;
            }

            return nearest != null && combat.Hit(nearest.Id, _damage * damageMultiplier) ? 1 : 0;
        }
    }
}
