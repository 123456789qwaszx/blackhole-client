namespace BlackHole.Core
{
    // 처리 순서: 흡수 전이 성공 → 보상/성장 → 목록 제거.
    // 사망 콜백과 뷰 Destroy에서 따로 보상을 주지 않는다.
    public sealed class AbsorptionSystem
    {
        internal void Resolve(TargetWorld world, GrowthState growth)
        {
            float radius = growth.AbsorptionRadius;
            foreach (TargetState target in world.Targets)
            {
                if (target.TryAbsorb(radius))
                    growth.ReceiveAbsorption(target.Definition.Reward);
            }
            world.RemoveAbsorbed();
        }
    }
}
