using System;

namespace BlackHole.Core
{
    // 한 판 안에서 Enemy 하나를 가리키는 식별자. World가 출현 순서대로 발급한다.
    public readonly struct EnemyId : IEquatable<EnemyId>
    {
        public int Value { get; }

        public EnemyId(int value)
        {
            Value = value;
        }

        public bool Equals(EnemyId other) =>
            Value == other.Value;
        public override bool Equals(object obj) =>
            obj is EnemyId other && Equals(other);
        public override int GetHashCode() => Value;
        public override string ToString() => $"Enemy {Value}";
    }

    // 판 안의 Enemy 하나. 위치·HP·실행 수치의 원본이다. 화면은 읽기만 한다.
    // 어떤 행동인지는 모른다 — 행동 경계(IEnemyBehavior)에 다음 위치를 묻는다(B3).
    public sealed class Enemy
    {
        private readonly IEnemyBehavior _behavior;

        public EnemyId Id { get; }
        public EnemyDefinition Definition { get; }
        // 실행 수치. 출현 때 계산하고 고정한다.
        public EnemyStats Stats { get; }
        // 사망 보상. 출현 때 구매 보정을 반영해 확정한다.
        public EnemyReward Reward { get; }
        public float Health { get; private set; }
        public bool IsAlive { get; private set; } = true;
        public Point2 Position { get; private set; }
        // 마지막으로 피해를 준 Player.
        // 기록일 뿐이며 보상 귀속 규칙으로 쓰지 않는다(귀속 정책은 미정).
        public PlayerId? LastDamageSource { get; private set; }

        internal Enemy(
            EnemyId id, 
            EnemyDefinition definition,
            EnemyStats stats, 
            EnemyReward reward,
            Point2 position,
            IEnemyBehavior behavior)
        {
            Id = id;
            Definition = definition;
            Stats = stats;
            Reward = reward;
            Health = stats.MaxHealth;
            Position = position;
            _behavior = behavior ?? throw new ArgumentNullException(nameof(behavior));
        }

        internal void Move(float delta, Point2 hqPosition)
        {
            Position = _behavior.NextPosition(
                new EnemyBehaviorInput(Position, Stats, hqPosition),
                delta);
        }

        // true는 이번 피해에서 최초로 사망했음을 뜻한다.
        internal bool ApplyDamage(Damage damage)
        {
            if (!IsAlive) 
                return false;
            
            Health = Math.Max(0, Health - damage.Amount);
            LastDamageSource = damage.Source;
            
            if (Health > 0)
                return false;
            
            IsAlive = false;
            
            return true;
        }
    }
}
