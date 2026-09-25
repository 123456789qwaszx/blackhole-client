using System;

namespace BlackHole.Core
{
    // 한 판 안에서 적 하나를 가리키는 식별자. 출현 순서대로 발급한다.
    public readonly struct EnemyId : IEquatable<EnemyId>
    {
        public int Value { get; }

        public EnemyId(int value)
        {
            Value = value;
        }

        public bool Equals(EnemyId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is EnemyId other && Equals(other);
        public override int GetHashCode() => Value;
        public override string ToString() => $"Enemy {Value}";
    }

    // 판 안의 적 하나. HP·위치·생존 여부·실행 수치의 원본이다. 화면은 읽기만 한다.
    // 어떤 행동인지는 모른다 — 행동 경계(IEnemyBehavior)에 다음 위치를 묻는다.
    // 자기 사망만 판정한다. 목록에서 빠지는 일과 사망 기록은 EnemyRoster가 한다.
    public sealed class Enemy
    {
        private readonly IEnemyBehavior _behavior;

        public EnemyId Id { get; }
        public EnemyDefinition Definition { get; }
        // 실행 수치. 출현 때 정해지고 바뀌지 않는다.
        public EnemyStats Stats { get; }
        public float Health { get; private set; }
        public bool IsAlive { get; private set; } = true;
        public Point2 Position { get; private set; }
        // 마지막으로 피해를 준 Player. 기록일 뿐이며 보상 귀속 규칙으로 쓰지 않는다(귀속 정책은 미정).
        public PlayerId? LastDamageSource { get; private set; }

        internal Enemy(EnemyId id, EnemyDefinition definition, EnemyStats stats, Point2 position, IEnemyBehavior behavior)
        {
            Id = id;
            Definition = definition;
            Stats = stats;
            Health = stats.MaxHealth;
            Position = position;
            _behavior = behavior ?? throw new ArgumentNullException(nameof(behavior));
        }

        internal void Move(float delta)
        {
            Position = _behavior.NextPosition(Position, Stats, delta);
        }

        // true는 이번 피해로 처음 죽었다는 뜻이다. 이미 죽은 적은 피해를 받지 않는다.
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
