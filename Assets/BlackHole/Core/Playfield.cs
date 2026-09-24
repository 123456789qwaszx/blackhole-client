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

        public IReadOnlyList<TargetState> Targets => _world.Targets;
        public IReadOnlyList<SkillState> Skills => _loadout.Skills;
        public GrowthState Growth { get; }

        internal Playfield(TargetWorld world, SpawnSchedule spawn, SkillLoadout loadout, GrowthState growth)
        {
            _world = world;
            _spawn = spawn;
            _loadout = loadout;
            Growth = growth;
            _combat = new CombatResolver(_world);
            _spawn.Advance(0, _world);
        }

        internal void Advance(float delta)
        {
            _loadout.Advance(delta);
            _world.Move(delta, Growth.AbsorptionRadius);
            _absorption.Resolve(_world, Growth);
            _spawn.Advance(delta, _world);
        }

        internal CastResult TryCast(string id, Point2 aim) =>
            _loadout.TryCast(id, aim, Growth.DamageMultiplier, _world, _combat);
    }
}
