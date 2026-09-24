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

        public bool Equals(EnemyId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is EnemyId other && Equals(other);
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
        // 실행 수치. 출현 때 계산된다([임시]).
        public EnemyStats Stats { get; }
        public float Health { get; private set; }
        public Point2 Position { get; private set; }

        internal Enemy(EnemyId id, EnemyDefinition definition, EnemyStats stats, Point2 position, IEnemyBehavior behavior)
        {
            Id = id;
            Definition = definition;
            Stats = stats;
            Health = stats.MaxHealth;
            Position = position;
            _behavior = behavior ?? throw new ArgumentNullException(nameof(behavior));
        }

        internal void Move(float delta, Point2 hqPosition)
        {
            Position = _behavior.NextPosition(new EnemyBehaviorInput(Position, Stats, hqPosition), delta);
        }
    }
}
