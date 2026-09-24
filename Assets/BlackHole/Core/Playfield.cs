using System.Collections.Generic;

namespace BlackHole.Core
{
    // 판 안의 시스템과 한 번의 진행 순서를 소유한다. 피해/보상/강화 공식은 각 소유자가 갖는다.
    // 조립은 SessionAssembler가 한다.
    public sealed class Playfield
    {
        private readonly TargetWorld _world;
        private readonly SpawnSchedule _spawn;
        private readonly CombatResolver _combat;
        private readonly AbsorptionSystem _absorption = new AbsorptionSystem();
        private readonly SkillLoadout _loadout;
        private readonly UpgradePurchase _purchase;

        public IReadOnlyList<TargetState> Targets => _world.Targets;
        public IReadOnlyList<SkillState> Skills => _loadout.Skills;
        public BlackHoleState BlackHole { get; }
        public WalletState Wallet { get; }
        public UpgradeState Upgrades { get; }

        // 스킬 사용자의 공격 배율: 기본 1 + 획득 강화 보정. 원본을 저장하지 않고 매번 계산한다.
        public float DamageMultiplier => 1 + Upgrades.Bonus(UpgradeStat.DamageMultiplier);

        internal Playfield(TargetWorld world, SpawnSchedule spawn, SkillLoadout loadout,
            BlackHoleState blackHole, WalletState wallet, UpgradeState upgrades)
        {
            _world = world;
            _spawn = spawn;
            _loadout = loadout;
            BlackHole = blackHole;
            Wallet = wallet;
            Upgrades = upgrades;
            _combat = new CombatResolver(_world);
            _purchase = new UpgradePurchase(upgrades, wallet);
            _spawn.Advance(0, _world);
        }

        // 한 단계: 쿨다운 → 이동 → 흡수·보상·제거 → 출현.
        internal void Advance(float delta)
        {
            _loadout.Advance(delta);
            _world.Move(delta, BlackHole.AbsorptionRadius);
            _absorption.Resolve(_world, BlackHole, Wallet);
            _spawn.Advance(delta, _world);
        }

        internal CastResult TryCast(string id, Point2 aim, out CastReport report) =>
            _loadout.TryCast(id, new CastContext(aim, DamageMultiplier), _world, _combat, out report);

        internal UpgradeResult TryPurchaseUpgrade(string id) => _purchase.TryPurchase(id);
    }
}
