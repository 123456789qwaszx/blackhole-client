using System;

namespace BlackHole.Core
{
    // 규칙에서 사용하는 평면 좌표. Unity Vector2 변환은 호스트의 책임이다.
    public readonly struct Point2
    {
        public float X { get; }
        public float Y { get; }

        public Point2(float x, float y)
        {
            if (float.IsNaN(x) || float.IsInfinity(x) ||
                float.IsNaN(y) || float.IsInfinity(y))
                throw new ArgumentOutOfRangeException(nameof(x), "좌표는 유한해야 한다.");

            X = x;
            Y = y;
        }

        public float DistanceSquared(Point2 other)
        {
            float dx = X - other.X;
            float dy = Y - other.Y;
            return dx * dx + dy * dy;
        }

        public static Point2 FromPolar(float radius, float angle) =>
            new Point2(radius * (float)Math.Cos(angle), radius * (float)Math.Sin(angle));
    }
}
