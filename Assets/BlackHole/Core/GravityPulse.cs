using System.Collections.Generic;

namespace BlackHole.Core
{
    // 조준점의 범위 안에 있는 모든 대상에게 피해와 중심 방향 당김을 함께 적용한다.
    public sealed class GravityPulse : ISkillEffect
    {
        public float Radius { get; }
        private readonly float _damage;
        private readonly float _pullDistance;

        public GravityPulse(float damage, float radius, float pullDistance)
        {
            _damage = DefinitionGuard.Positive(damage, nameof(damage));
            Radius = DefinitionGuard.Positive(radius, nameof(radius));
            _pullDistance = DefinitionGuard.Positive(pullDistance, nameof(pullDistance));
        }

        public int Execute(Point2 aim, float damageMultiplier,
            IReadOnlyList<TargetState> targets, CombatResolver combat)
        {
            int hits = 0;
            foreach (TargetState target in targets)
            {
                if (target.Position.DistanceSquared(aim) <= Radius * Radius &&
                    combat.Hit(target.Id, _damage * damageMultiplier, _pullDistance))
                    hits++;
            }
            return hits;
        }
    }
}
