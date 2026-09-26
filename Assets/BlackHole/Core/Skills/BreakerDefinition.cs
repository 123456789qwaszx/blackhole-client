namespace BlackHole.Core
{
    // Breaker의 공유 정의: 기본 수치. 조준점을 중심으로 한 원 안의 적 전부를 주기마다 친다(GAME_RULES 6절).
    // 콘텐츠에 Breaker가 없으면 판에 Breaker가 없다. 실행 상태는 참가자마다의 BreakerSkill이 가진다.
    public sealed class BreakerDefinition
    {
        public float Damage { get; }
        // 공격 주기(초).
        public float Interval { get; }
        // 공격 원의 반지름. 화면의 범위 표시도 이 값이다.
        public float Radius { get; }

        public BreakerDefinition(float damage, float interval, float radius)
        {
            Damage = DefinitionGuard.Positive(damage, nameof(damage));
            Interval = DefinitionGuard.Positive(interval, nameof(interval));
            Radius = DefinitionGuard.Positive(radius, nameof(radius));
        }
    }
}
