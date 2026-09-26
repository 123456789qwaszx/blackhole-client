using System.Collections.Generic;

namespace BlackHole.Core
{
    // Breaker Tick 하나의 기록. 화면은 이것을 읽어 그릴 뿐 피해를 다시 계산하지 않는다.
    public readonly struct BreakerTick
    {
        // 이 Breaker의 몇 번째 Tick인가(1부터). 같은 Tick을 두 번 그리지 않는 데 쓴다.
        public int Number { get; }
        // 공격 원의 중심(그 Tick의 조준점). 조준점이 없던 빈 Tick이면 null이다.
        public Point2? Center { get; }
        public float Radius { get; }
        // 피해를 준 적의 수.
        public int HitCount { get; }

        internal BreakerTick(int number, Point2? center, float radius, int hitCount)
        {
            Number = number;
            Center = center;
            Radius = radius;
            HitCount = hitCount;
        }
    }

    // 참가자 한 명의 Breaker 실행 상태. 판마다, 참가자마다 따로 있다.
    //
    // 공격 Tick(World.Step의 2. Passive Attack 자리):
    // 1. 소유 참가자의 지금 조준점을 읽는다. 없으면 그 Tick은 빈 Tick이다([임시], SYSTEM_CATALOG S04).
    // 2. 조준점 원 안의 살아 있는 적을 먼저 모두 모은다. 피해를 주면 죽은 적이 World.Enemies에서 바로 빠지기 때문이다.
    // 3. 모은 적마다 World.DealDamage로 피해를 준다. 출처는 소유 참가자다. 사망 1회·사망 기록·처치 수는 적 시스템이 맡는다.
    // 빈 Tick도 주기를 소비한다. 적이 들어올 때까지 Tick을 미뤄 두는 규칙은 없다.
    // 첫 Tick은 판의 첫 Step이다. MVP 기본 공격의 규칙(GAME_RULES 6절)이며 모든 스킬의 공통 규칙이 아니다.
    public sealed class BreakerSkill
    {
        // 진행 시간을 더한 값의 끝자리 오차. 이만큼 모자라도 Tick 시각에 닿은 것으로 본다.
        private const float TimeEpsilon = 1e-5f;

        private readonly List<Enemy> _targets = new List<Enemy>();
        private readonly List<BreakerTick> _ticks = new List<BreakerTick>();
        private float _untilNextTick;

        public BreakerDefinition Definition { get; }
        // 지금까지 일어난 Tick 수. 빈 Tick도 센다.
        public int TickCount { get; private set; }
        // 마지막 Tick이 피해를 준 적의 수. 빈 Tick이면 0이다.
        public int LastTickHitCount { get; private set; }
        // 마지막 진행 동안의 Tick(일어난 순서). 다음 진행이 시작될 때 비운다.
        public IReadOnlyList<BreakerTick> Ticks { get; }

        internal BreakerSkill(BreakerDefinition definition)
        {
            Definition = definition;
            Ticks = _ticks.AsReadOnly();
        }

        internal void BeginAdvance() => _ticks.Clear();

        // 한 Step 동안 주기가 여러 번 차면 그만큼 Tick한다. 같은 Step 안의 Tick은 같은 조준점과 같은 적 위치를 본다.
        internal void Advance(float delta, BattlePlayer owner, World world)
        {
            _untilNextTick -= delta;

            while (_untilNextTick <= TimeEpsilon)
            {
                Tick(owner, world);
                _untilNextTick += Definition.Interval;
            }
        }

        private void Tick(BattlePlayer owner, World world)
        {
            TickCount++;
            _targets.Clear();
            Point2? center = owner.AimPoint;

            if (center.HasValue)
            {
                float radiusSquared = Definition.Radius * Definition.Radius;
                IReadOnlyList<Enemy> enemies = world.Enemies;

                for (int i = 0; i < enemies.Count; i++)
                {
                    if (enemies[i].Position.DistanceSquared(center.Value) <= radiusSquared)
                        _targets.Add(enemies[i]);
                }
            }

            var damage = new Damage(Definition.Damage, owner.Id);

            foreach (Enemy target in _targets)
                world.DealDamage(target, damage);

            LastTickHitCount = _targets.Count;
            _ticks.Add(new BreakerTick(TickCount, center, Definition.Radius, _targets.Count));
        }
    }
}
