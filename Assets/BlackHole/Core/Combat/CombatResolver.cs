namespace BlackHole.Core
{
    // 스킬 효과는 대상 상태를 직접 쓰지 않고 이 경계로 요청한다. 피해와 당김은 서로 다른 요청이다.
    // 전이 가능 여부(살아 있는가, 흡수됐는가)는 대상 상태의 주인인 TargetState가 판정한다.
    internal sealed class CombatResolver
    {
        private readonly TargetWorld _world;

        public CombatResolver(TargetWorld world) { _world = world; }

        // 살아 있는 대상에게만 적용된다. 적용됐으면 true.
        public bool Damage(int targetId, float amount)
        {
            DefinitionGuard.Positive(amount, nameof(amount));
            TargetState target = _world.Find(targetId);
            return target != null && target.ApplyDamage(amount);
        }

        // 흡수되지 않은 대상에게 적용된다(사망한 대상 포함). 적용됐으면 true.
        public bool Pull(int targetId, float distance)
        {
            DefinitionGuard.Delta(distance);
            TargetState target = _world.Find(targetId);
            return target != null && target.Pull(distance);
        }
    }
}
