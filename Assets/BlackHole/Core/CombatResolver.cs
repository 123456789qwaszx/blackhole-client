namespace BlackHole.Core
{
    // 스킬은 대상 상태를 직접 쓰지 않고 이 경계로 피해와 끌어당김을 요청한다.
    public sealed class CombatResolver
    {
        private readonly TargetWorld _world;

        internal CombatResolver(TargetWorld world) { _world = world; }

        public bool Hit(int targetId, float damage, float pullDistance = 0)
        {
            DefinitionGuard.Positive(damage, nameof(damage));
            DefinitionGuard.Delta(pullDistance);
            TargetState target = _world.Find(targetId);
            if (target == null || !target.ApplyDamage(damage)) return false;
            target.Pull(pullDistance);
            return true;
        }
    }
}
