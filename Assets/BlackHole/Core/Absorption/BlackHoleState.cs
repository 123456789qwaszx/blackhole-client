using System;

namespace BlackHole.Core
{
    // 블랙홀 성장 규칙의 공유 정의. 흡수 반경 = Base + min(질량, MassRadiusCap) × RadiusPerMass + 강화 보정.
    public sealed class BlackHoleDefinition
    {
        public float BaseAbsorptionRadius { get; }
        public float RadiusPerMass { get; }
        // 이 질량을 넘으면 질량으로는 반경이 더 커지지 않는다.
        public int MassRadiusCap { get; }

        public BlackHoleDefinition(float baseAbsorptionRadius, float radiusPerMass, int massRadiusCap)
        {
            BaseAbsorptionRadius = DefinitionGuard.Positive(baseAbsorptionRadius, nameof(baseAbsorptionRadius));
            RadiusPerMass = DefinitionGuard.NonNegative(radiusPerMass, nameof(radiusPerMass));
            if (massRadiusCap < 0)
                throw new ArgumentOutOfRangeException(nameof(massRadiusCap), "0 이상의 정수가 필요하다.");
            MassRadiusCap = massRadiusCap;
        }

        public float RadiusFor(int mass) => BaseAbsorptionRadius + Math.Min(mass, MassRadiusCap) * RadiusPerMass;
    }

    // 한 판의 블랙홀 성장 상태: 질량과 흡수 수. 흡수 확정(AbsorptionSystem)으로만 바뀐다.
    // 흡수 반경은 저장하지 않고 질량과 획득 강화에서 매번 계산한다.
    public sealed class BlackHoleState
    {
        private readonly BlackHoleDefinition _definition;
        private readonly UpgradeState _upgrades;

        public int Mass { get; private set; }
        public int AbsorbedCount { get; private set; }
        public float AbsorptionRadius =>
            _definition.RadiusFor(Mass) + _upgrades.Bonus(UpgradeStat.AbsorptionRadius);

        internal BlackHoleState(BlackHoleDefinition definition, UpgradeState upgrades)
        {
            _definition = definition;
            _upgrades = upgrades;
        }

        internal void Absorb(int mass)
        {
            Mass += mass;
            AbsorbedCount++;
        }
    }
}
