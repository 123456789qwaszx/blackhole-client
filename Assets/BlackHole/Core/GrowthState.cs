using System;

namespace BlackHole.Core
{
    public enum UpgradeResult { Purchased, InsufficientCredits, MaxLevel, SessionInactive }

    public sealed class GrowthDefinition
    {
        public int UpgradeCost { get; }
        public int MaxPowerLevel { get; }
        public float PowerPerLevel { get; }

        public GrowthDefinition(int upgradeCost, int maxPowerLevel, float powerPerLevel)
        {
            if (upgradeCost <= 0) throw new ArgumentOutOfRangeException(nameof(upgradeCost));
            if (maxPowerLevel <= 0) throw new ArgumentOutOfRangeException(nameof(maxPowerLevel));
            if ((long)upgradeCost * maxPowerLevel > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(upgradeCost));
            UpgradeCost = upgradeCost;
            MaxPowerLevel = maxPowerLevel;
            PowerPerLevel = DefinitionGuard.Positive(powerPerLevel, nameof(powerPerLevel));
        }
    }

    // 실험에서는 재화와 강화 모두 한 판 수명이다. 영구 성장 정책은 아직 없다.
    public sealed class GrowthState
    {
        private readonly GrowthDefinition _definition;
        public int Mass { get; private set; }
        public int Credits { get; private set; }
        public int AbsorbedCount { get; private set; }
        public int PowerLevel { get; private set; }
        public bool IsMaxLevel => PowerLevel >= _definition.MaxPowerLevel;
        public int NextUpgradeCost => IsMaxLevel ? 0 : _definition.UpgradeCost * (PowerLevel + 1);
        public float DamageMultiplier => 1 + PowerLevel * _definition.PowerPerLevel;
        public float AbsorptionRadius => 0.65f + Math.Min(Mass, 50) * 0.012f;

        public GrowthState(GrowthDefinition definition)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        internal void ReceiveAbsorption(int reward)
        {
            Mass += reward;
            Credits += reward;
            AbsorbedCount++;
        }

        internal UpgradeResult TryUpgrade()
        {
            if (IsMaxLevel) return UpgradeResult.MaxLevel;
            int cost = NextUpgradeCost;
            if (Credits < cost) return UpgradeResult.InsufficientCredits;
            Credits -= cost;
            PowerLevel++;
            return UpgradeResult.Purchased;
        }
    }
}
