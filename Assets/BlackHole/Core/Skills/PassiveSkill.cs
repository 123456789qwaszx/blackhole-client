using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 소유 Player가 가진 Passive Skill 하나의 실행 상태(B5). 판마다, Player마다 따로 있다.
    // 자기 규칙으로 자동 실행된다. 버튼이 없다. 무엇을 하는지는 종류(하위 클래스)가 정한다.
    public abstract class PassiveSkill
    {
        public PassiveSkillDefinition Definition { get; }
        private protected Player Owner { get; }

        private protected PassiveSkill(PassiveSkillDefinition definition, Player owner)
        {
            Definition = definition;
            Owner = owner;
        }

        // World.Step의 Passive Attack 단계에서 부른다.
        internal abstract void Advance(float delta, World world);
    }

    // Skill 정의 → 실행 상태. 종류마다 한 갈래다(EnemyBehaviors.Standard와 같은 자리).
    // 실행 수치는 여기서 한 번 계산한다: 기본 수치 + 소유 Player가 산, 이 Skill을 대상으로 한 보정.
    internal static class PassiveSkills
    {
        public static PassiveSkill Create(PassiveSkillDefinition definition, Player owner, UpgradeModifiers modifiers)
        {
            switch (definition)
            {
                case BreakerSkillDefinition breaker:
                    return new BreakerSkill(breaker, BreakerStatCalculator.Compute(breaker, new IBreakerStatModifier[] { modifiers }), owner);
                case PiercingLaserDefinition laser:
                    return new PiercingLaserSkill(laser, modifiers.Apply(laser, laser.BaseStats), owner);
                default:
                    throw new ArgumentException(
                        $"실행 규칙이 연결되지 않은 Skill 종류 '{definition?.GetType().Name}'.", nameof(definition));
            }
        }
    }

    // Breaker의 실행 상태.
    //
    // 공격 틱:
    // 1. 기준점: 틱마다 소유 Player의 현재 AimPoint를 읽는다. 없으면 그 틱은 아무 일도 없다([임시]).
    // 2. 선택: 기준점 원 안의 Enemy 전부.
    // 3. 적용: 고른 Enemy마다 피해를 요청한다. 피해에는 출처(소유 Player)가 담긴다.
    // 범위가 비어 있어도 타이머는 계속 돈다. 빈 틱을 보류했다가 적이 들어오는 순간 공격하는 규칙은 없다.
    public sealed class BreakerSkill : PassiveSkill
    {
        private readonly List<Enemy> _targets = new List<Enemy>();
        private float _untilNextTick;

        // 실행 수치. 판 조립 때 기본 수치 + 구매 보정으로 계산된다.
        public BreakerStats Stats { get; }
        // 지금까지 일어난 틱 수. 맞은 적이 없는 틱도 센다. 화면(틱 표시)과 계약이 읽는다.
        public int TickCount { get; private set; }
        // 마지막 틱이 피해를 준 Enemy 수. 빈 틱(조준점 없음, 범위가 빔)이면 0이다. 소리가 적중 유무와 세기를 정할 때 읽는다.
        public int LastTickHitCount { get; private set; }

        internal BreakerSkill(BreakerSkillDefinition definition, BreakerStats stats, Player owner)
            : base(definition, owner)
        {
            Stats = stats;
            // 첫 틱은 판 시작(0초)이다. MVP 기본 공격의 규칙이며 모든 Skill의 공통 규칙이 아니다.
            _untilNextTick = 0;
        }

        // 지금 발동하면 어디를 기준으로 하는가. 화면은 이것으로 범위 원을 그린다.
        public bool TryGetOrigin(out Point2 origin)
        {
            if (Owner.AimPoint.HasValue)
            {
                origin = Owner.AimPoint.Value;
                return true;
            }

            origin = default;
            return false;
        }

        internal override void Advance(float delta, World world)
        {
            _untilNextTick -= delta;
            while (_untilNextTick <= 0)
            {
                Tick(world);
                TickCount++;
                _untilNextTick += Stats.Interval;
            }
        }

        private void Tick(World world)
        {
            LastTickHitCount = 0;

            if (!TryGetOrigin(out Point2 origin))
                return;

            _targets.Clear();
            IReadOnlyList<Enemy> enemies = world.Enemies;

            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i].IsAlive
                    && IsInside(enemies[i], origin, Stats.Radius))
                {
                    _targets.Add(enemies[i]);
                }
            }

            LastTickHitCount = _targets.Count;
            var damage = new Damage(Stats.Damage, Owner.Id);

            foreach (Enemy target in _targets)
                world.DealDamage(target, damage);

            _targets.Clear();
        }

        // "원 안의 Enemy" 판정의 유일한 자리. [임시]로 Enemy의 중심이 원 안에 있는지 본다.
        // 크기(Stats.Size)까지 포함할지는 미정이다.
        private static bool IsInside(Enemy enemy, Point2 origin, float radius) =>
            enemy.Position.DistanceSquared(origin) <= radius * radius;
    }
}
