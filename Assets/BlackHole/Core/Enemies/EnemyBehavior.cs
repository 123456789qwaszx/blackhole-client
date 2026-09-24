using System;

namespace BlackHole.Core
{
    // 행동이 한 단계에서 읽는 값. 위치의 원본은 Enemy이고, 행동은 다음 위치만 정한다.
    public readonly struct EnemyBehaviorInput
    {
        public Point2 Position { get; }
        public EnemyStats Stats { get; }
        public Point2 HqPosition { get; }

        public EnemyBehaviorInput(
            Point2 position, 
            EnemyStats stats, 
            Point2 hqPosition)
        {
            Position = position;
            Stats = stats;
            HqPosition = hqPosition;
        }
    }

    // Enemy 행동의 경계(B3). Enemy 본체는 이 인터페이스만 안다.
    // 상태 기계나 행동 전환은 만들지 않는다. 실제 기획이 생길 때 도입한다.
    public interface IEnemyBehavior
    {
        Point2 NextPosition(in EnemyBehaviorInput input, float delta);
    }

    // 행동 정의 → 행동 구현. 판 조립 때 쓰인다.
    // 게임은 EnemyBehaviors.Standard를 쓴다(D3: 테스트만 다른 해석기를 꽂는다).
    public delegate IEnemyBehavior EnemyBehaviorResolver(
        EnemyBehaviorDefinition definition);

    public static class EnemyBehaviors
    {
        // 행동 정의를 구현으로 해석하는 유일한 자리. 지금은 Orbit 하나다.
        public static IEnemyBehavior Standard(
            EnemyBehaviorDefinition definition)
        {
            switch (definition)
            {
                case OrbitHqBehaviorDefinition orbit:
                    return new OrbitHqBehavior(orbit);
                default:
                    throw new ArgumentException(
                        $"실행 규칙이 연결되지 않은 행동 종류 '{definition?.GetType().Name}'.", nameof(definition));
            }
        }
    }

    // HQ 주위를 돈다. HQ로부터의 거리는 유지하고 이동 속도만큼 원 둘레를 따라 움직인다.
    // "거리 유지"는 가이드의 "HQ 주위를 빙글빙글 돈다"를 가장 단순하게 읽은 [임시] 해석이다.
    internal sealed class OrbitHqBehavior : IEnemyBehavior
    {
        private readonly OrbitHqBehaviorDefinition _definition;

        public OrbitHqBehavior(OrbitHqBehaviorDefinition definition)
        {
            _definition = definition;
        }

        public Point2 NextPosition(in EnemyBehaviorInput input, float delta)
        {
            float dx = input.Position.X - input.HqPosition.X;
            float dy = input.Position.Y - input.HqPosition.Y;
            
            float radius = (float)Math.Sqrt(dx * dx + dy * dy);
            
            if (radius == 0) 
                return input.Position;

            float angle = (float)Math.Atan2(dy, dx);
            float turn = 
                input.Stats.MoveSpeed 
                / radius 
                * delta 
                * (_definition.Clockwise 
                    ? -1 
                    : 1);
            
            return new Point2(
                input.HqPosition.X + radius * (float)Math.Cos(angle + turn),
                input.HqPosition.Y + radius * (float)Math.Sin(angle + turn));
        }
    }
}
