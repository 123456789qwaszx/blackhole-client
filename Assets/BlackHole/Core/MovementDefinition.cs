namespace BlackHole.Core
{
    // 살아 있는 대상의 이동 방식 정의. 종류별 하위 타입이 자기 수치 규칙을 생성자에서 보장한다.
    // 정의는 실행 규칙을 품지 않는다 — MovementRuleFactory가 해석한다.
    // 새 이동 방식: 하위 정의 + 실행 규칙(MovementRules) + factory 분기 + ContentLoader의 종류 이름.
    public abstract class MovementDefinition
    {
        // 하위 정의는 Core 안에서만 만든다. factory가 모르는 종류가 밖에서 생기지 않게 한다.
        private protected MovementDefinition() { }
    }

    // 블랙홀 주위를 돌며 일정 속도로 안쪽으로 다가온다.
    public sealed class OrbitMovementDefinition : MovementDefinition
    {
        // 라디안/초. 부호가 회전 방향이다.
        public float AngularSpeed { get; }
        public float InwardSpeed { get; }

        public OrbitMovementDefinition(float angularSpeed, float inwardSpeed)
        {
            AngularSpeed = DefinitionGuard.Finite(angularSpeed, nameof(angularSpeed));
            InwardSpeed = DefinitionGuard.Positive(inwardSpeed, nameof(inwardSpeed));
        }
    }

    // 각도를 유지한 채 중심을 향해 곧게 다가오며 점점 빨라진다.
    // 속도 = InitialSpeed + Acceleration × 출현 후 시간. Orbit의 수치 변형으로는 표현할 수 없다.
    public sealed class DiveMovementDefinition : MovementDefinition
    {
        public float InitialSpeed { get; }
        public float Acceleration { get; }

        public DiveMovementDefinition(float initialSpeed, float acceleration)
        {
            InitialSpeed = DefinitionGuard.Positive(initialSpeed, nameof(initialSpeed));
            Acceleration = DefinitionGuard.NonNegative(acceleration, nameof(acceleration));
        }
    }
}
