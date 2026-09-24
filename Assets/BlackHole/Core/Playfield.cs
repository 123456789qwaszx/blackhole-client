using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 한 번의 진행 순서만 조립한다. 피해/보상/강화 공식은 각 소유자가 갖는다.
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

        public Playfield(SpawnSchedule spawn, SkillLoadout loadout, GrowthState growth)
        {
            _world = new TargetWorld();
            _spawn = spawn ?? throw new ArgumentNullException(nameof(spawn));
            _loadout = loadout ?? throw new ArgumentNullException(nameof(loadout));
            Growth = growth ?? throw new ArgumentNullException(nameof(growth));
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
