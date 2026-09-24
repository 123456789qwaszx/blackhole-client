using System;

namespace BlackHole.Core
{
    // 블랙홀 중심 기준의 극좌표. 이 게임의 규칙(흡수·하한·당김·낙하)은 모두 중심까지의 거리로 판정한다.
    internal readonly struct PolarPoint
    {
        public readonly float Radius;
        public readonly float Angle;

        public PolarPoint(float radius, float angle)
        {
            Radius = radius;
            Angle = angle;
        }
    }

    // 살아 있는 대상의 이동 규칙. 위치의 원본은 TargetState가 갖고, 규칙은 다음 위치만 계산한다.
    // 블랙홀 하한과 사망 후 낙하는 모든 대상에 공통이므로 TargetState가 적용한다.
    internal interface IMovementRule
    {
        // age: 출현 후 이번 단계 시작까지의 시간.
        PolarPoint Next(PolarPoint current, float age, float delta);
    }

    internal sealed class OrbitMovement : IMovementRule
    {
        private const float FullTurn = (float)Math.PI * 2;
        private readonly OrbitMovementDefinition _definition;

        public OrbitMovement(OrbitMovementDefinition definition)
        {
            _definition = definition;
        }

        public PolarPoint Next(PolarPoint current, float age, float delta) => new PolarPoint(
            current.Radius - _definition.InwardSpeed * delta,
            (current.Angle + _definition.AngularSpeed * delta) % FullTurn);
    }
}
