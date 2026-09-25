using System;

namespace BlackHole.Core
{
    // 적 행동의 경계. 적 본체는 이 인터페이스만 안다. 위치의 원본은 Enemy이고, 행동은 다음 위치만 정한다.
    // 상태 기계나 행동 전환은 만들지 않는다(GAME_RULES 7절). 실제 기획이 생길 때 도입한다.
    internal interface IEnemyBehavior
    {
        Point2 NextPosition(Point2 position, EnemyStats stats, float delta);
    }

    // 행동 정의 → 행동 구현. 출현 때 쓰인다.
    internal static class EnemyBehaviors
    {
        public static IEnemyBehavior Create(EnemyBehaviorDefinition definition)
        {
            switch (definition)
            {
                case OrbitBehaviorDefinition orbit:
                    return new OrbitBehavior(orbit);
                default:
                    throw new ArgumentException(
                        $"실행 규칙이 연결되지 않은 행동 종류 '{definition?.GetType().Name}'.", nameof(definition));
            }
        }
    }

    // HQ(원점) 주위를 돈다. 원점으로부터의 거리는 유지하고, 이동 속도만큼 원 둘레를 따라 움직인다.
    // [임시] "거리 유지"는 "HQ를 중심으로 공전한다"(GAME_RULES 7절)를 가장 단순하게 읽은 해석이다.
    internal sealed class OrbitBehavior : IEnemyBehavior
    {
        private readonly OrbitBehaviorDefinition _definition;

        public OrbitBehavior(OrbitBehaviorDefinition definition)
        {
            _definition = definition;
        }

        public Point2 NextPosition(Point2 position, EnemyStats stats, float delta)
        {
            Point2 center = BattleSpace.Origin;
            float dx = position.X - center.X;
            float dy = position.Y - center.Y;
            float radius = (float)Math.Sqrt(dx * dx + dy * dy);

            if (radius == 0)
                return position;

            float angle = (float)Math.Atan2(dy, dx);
            float turn = stats.MoveSpeed / radius * delta * (_definition.Clockwise ? -1 : 1);

            return new Point2(
                center.X + radius * (float)Math.Cos(angle + turn),
                center.Y + radius * (float)Math.Sin(angle + turn));
        }
    }
}
