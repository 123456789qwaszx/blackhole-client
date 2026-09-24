using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 소유 Player가 가진 Passive Skill 하나의 실행 상태(B5). 판마다, Player마다 따로 있다.
    // 자기 주기로 자동 실행된다. 버튼이 없다.
    //
    // 공격 틱:
    // 1. 기준점: 정의의 기준점 종류로 찾는다. 지금은 소유 Player의 AimPoint다. 없으면 그 틱은 아무 일도 없다([임시]).
    // 2. 선택: 기준점 원 안의 Enemy 전부.
    // 3. 적용: 고른 Enemy마다 피해를 요청한다. 피해에는 출처(소유 Player)가 담긴다.
    // 범위가 비어 있어도 타이머는 계속 돈다. 빈 틱을 보류했다가 적이 들어오는 순간 공격하는 규칙은 없다.
    public sealed class PassiveSkill
    {
        private readonly Player _owner;
        private readonly List<Enemy> _targets = new List<Enemy>();
        private float _untilNextTick;

        public PassiveSkillDefinition Definition { get; }
        // 실행 수치. 판 조립 때 계산된다(보정의 출처가 미정이라 지금은 보정이 없다).
        public PassiveSkillStats Stats { get; }
        // 지금까지 일어난 틱 수. 맞은 적이 없는 틱도 센다. 화면(틱 표시)과 계약이 읽는다.
        public int TickCount { get; private set; }

        internal PassiveSkill(PassiveSkillDefinition definition, PassiveSkillStats stats, Player owner)
        {
            Definition = definition;
            Stats = stats;
            _owner = owner;
            // 첫 틱은 판 시작(0초)이다.
            _untilNextTick = 0;
        }

        // 지금 발동하면 어디를 기준으로 하는가. 화면은 이것으로 범위 원을 그린다.
        public bool TryGetOrigin(out Point2 origin)
        {
            switch (Definition.Origin)
            {
                case SkillOrigin.OwnerAimPoint:
                    if (_owner.AimPoint.HasValue)
                    {
                        origin = _owner.AimPoint.Value;
                        return true;
                    }
                    break;
            }
            origin = default;
            return false;
        }

        internal void Advance(float delta, World world)
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

            var damage = new Damage(Stats.Damage, _owner.Id);
            
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
