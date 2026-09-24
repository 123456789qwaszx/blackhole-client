namespace BlackHole.Core
{
    // 출현 위치의 공유 정의: HQ로부터의 거리와 출현마다 더하는 각도.
    // 무엇을·얼마나 내보낼지는 공급(전투 시작 배치, 성장 공급)이 정한다.
    // 값은 전부 [임시]이며 샘플 콘텐츠가 정한다. HQ가 커져도 출현 거리는 바뀌지 않는다(공간 공식은 미정).
    public sealed class SpawnDefinition
    {
        public float Distance { get; }
        // 출현마다 더하는 각도(라디안). 연속 출현 위치가 겹치지 않게 한다.
        public float AngleStep { get; }

        public SpawnDefinition(float distance, float angleStep)
        {
            Distance = DefinitionGuard.Positive(distance, nameof(distance));
            AngleStep = DefinitionGuard.Finite(angleStep, nameof(angleStep));
        }
    }
}
