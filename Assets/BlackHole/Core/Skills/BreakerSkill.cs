using System;
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
        // 이 Tick이 치명타였는가. 맞은 적 모두가 같은 결과를 받는다. 맞은 적이 없으면 false다.
        public bool IsCritical { get; }

        internal BreakerTick(int number, Point2? center, float radius, int hitCount, bool isCritical)
        {
            Number = number;
            Center = center;
            Radius = radius;
            HitCount = hitCount;
            IsCritical = isCritical;
        }
    }

    // 참가자 한 명의 Breaker 실행 상태. 판마다, 참가자마다 따로 있다.
    //
    // 공격 Tick(World.Step의 2. Passive Attack 자리):
    // 1. 소유 참가자의 지금 조준점을 읽는다. 없으면 그 Tick은 빈 Tick이다([임시], SYSTEM_CATALOG S04).
    // 2. 조준점 원 안의 살아 있는 적을 먼저 모두 모은다. 피해를 주면 죽은 적이 World.Enemies에서 바로 빠지기 때문이다.
    // 3. 맞을 적이 있으면 이 Tick의 치명타를 한 번 정한다. 맞은 적 모두가 같은 결과를 받고, 맞을 적이 없으면 난수를 뽑지 않는다
    //    [샌드박스 선택, 원작 미확인]. 치명타면 피해에 CritMultiplier를 한 번 곱한다.
    // 4. 모은 적마다 World.DealDamage로 피해를 준다. 출처는 소유 참가자다. 사망 1회·사망 기록·처치 수는 적 시스템이 맡는다.
    // 빈 Tick도 주기를 소비한다. 적이 들어올 때까지 Tick을 미뤄 두는 규칙은 없다.
    // 첫 Tick은 판의 첫 Step이다. MVP 기본 공격의 규칙(GAME_RULES 6절)이며 모든 스킬의 공통 규칙이 아니다.
    //
    // 처치 버프(달·혜성의 사망 효과)는 Breaker에만 붙는다(SKILL_SYSTEM_PLAN D2, 원작 R7·R8·R9).
    // - 공격 주기 감소: 켜져 있는 동안 주기가 IntervalMultiplier배로 줄어든다. 돌던 주기의 진행률은 그대로 두고 남은 부분만 빨리 찬다.
    // - 확정 치명타: 켜져 있는 동안 맞을 적이 있는 Tick은 모두 치명타다. 배율은 Breaker의 CritMultiplier다(버프가 배율을 더하지 않는다).
    // 한 Step의 공격은 Step을 시작할 때의 버프로 하고, 버프 시간은 공격 뒤에 준다. 사망 효과(Step 4)가 준 버프는 다음 Step부터다.
    // 같은 종류를 다시 받으면 남은 시간은 긴 쪽, 주기 배율은 강한 쪽(작은 값)을 남기고 곱으로 쌓지 않는다 [샌드박스 선택].
    public sealed class BreakerSkill
    {
        // 진행 시간을 더한 값의 끝자리 오차. 이만큼 모자라도 Tick 시각에 닿은 것으로 본다.
        private const float TimeEpsilon = 1e-5f;

        private readonly BattleRandom _critical;
        private readonly List<Enemy> _targets = new List<Enemy>();
        private readonly List<BreakerTick> _ticks = new List<BreakerTick>();
        // 다음 Tick까지 남은 주기(기본 주기 기준). 공격 주기 감소 중에는 더 빨리 준다.
        private float _untilNextTick;

        public BreakerDefinition Definition { get; }
        // 켜져 있는가. 꺼진 Breaker는 공격하지 않는다. 지금 끄고 켜는 곳은 개발용 스킬 콘솔뿐이다(게임 규칙으로 끄는 일은 없다).
        public bool Enabled { get; private set; } = true;
        // 지금까지 일어난 Tick 수. 빈 Tick도 센다.
        public int TickCount { get; private set; }
        // 마지막 Tick이 피해를 준 적의 수. 빈 Tick이면 0이다.
        public int LastTickHitCount { get; private set; }
        // 마지막 진행 동안의 Tick(일어난 순서). 다음 진행이 시작될 때 비운다.
        public IReadOnlyList<BreakerTick> Ticks { get; }
        // 공격 주기 감소 버프의 남은 시간(초)과 주기 배율. 버프가 없으면 0과 1이다.
        public float HasteRemaining { get; private set; }
        public float HasteMultiplier { get; private set; } = 1;
        // 확정 치명타 버프의 남은 시간(초). 버프가 없으면 0이다.
        public float GuaranteedCriticalRemaining { get; private set; }

        // critical은 이 Breaker의 치명타만 쓰는 난수다. 다른 곳이 난수를 뽑는 횟수가 치명타 순서를 바꾸지 않는다.
        internal BreakerSkill(BreakerDefinition definition, BattleRandom critical)
        {
            Definition = definition;
            _critical = critical;
            Ticks = _ticks.AsReadOnly();
        }

        // 끄면 돌던 주기를 버린다. 다시 켜면 처음부터 돈다: 켠 뒤 첫 Step에 첫 Tick. 버프 시간은 꺼져 있어도 흐른다.
        public void SetEnabled(bool enabled)
        {
            if (Enabled == enabled)
                return;

            Enabled = enabled;
            _untilNextTick = 0;
        }

        internal void BeginAdvance() => _ticks.Clear();

        // 한 Step 동안 주기가 여러 번 차면 그만큼 Tick한다. 같은 Step 안의 Tick은 같은 조준점과 같은 적 위치를 본다.
        internal void Advance(float delta, BattlePlayer owner, World world)
        {
            if (Enabled)
            {
                _untilNextTick -= delta / HasteMultiplier;

                while (_untilNextTick <= TimeEpsilon)
                {
                    Tick(owner, world);
                    _untilNextTick += Definition.Interval;
                }
            }

            AgeBuffs(delta);
        }

        internal void GrantHaste(AttackHasteDefinition haste)
        {
            HasteMultiplier = HasteRemaining > 0 ? Math.Min(HasteMultiplier, haste.IntervalMultiplier) : haste.IntervalMultiplier;
            HasteRemaining = Math.Max(HasteRemaining, haste.Duration);
        }

        internal void GrantGuaranteedCritical(GuaranteedCriticalDefinition critical) =>
            GuaranteedCriticalRemaining = Math.Max(GuaranteedCriticalRemaining, critical.Duration);

        private void AgeBuffs(float delta)
        {
            HasteRemaining = Math.Max(0, HasteRemaining - delta);
            GuaranteedCriticalRemaining = Math.Max(0, GuaranteedCriticalRemaining - delta);

            if (HasteRemaining == 0)
                HasteMultiplier = 1;
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

            bool critical = _targets.Count > 0 && RollCritical();
            var damage = new Damage(critical ? Definition.Damage * Definition.CritMultiplier : Definition.Damage, owner.Id);

            foreach (Enemy target in _targets)
                world.DealDamage(target, damage);

            LastTickHitCount = _targets.Count;
            _ticks.Add(new BreakerTick(TickCount, center, Definition.Radius, _targets.Count, critical));
        }

        // 확정 치명타 중이면 굴리지 않고 치명타다. 확률이 0이나 1이면 굴리지 않는다.
        private bool RollCritical()
        {
            if (GuaranteedCriticalRemaining > 0 || Definition.CritChance >= 1)
                return true;

            return Definition.CritChance > 0 && _critical.NextFloat() < Definition.CritChance;
        }
    }
}
