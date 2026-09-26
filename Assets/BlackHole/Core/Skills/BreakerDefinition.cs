using System;

namespace BlackHole.Core
{
    // Breaker의 공유 정의: 기본 수치. 조준점을 중심으로 한 원 안의 적 전부를 주기마다 친다(GAME_RULES 6절).
    // 콘텐츠에 Breaker가 없으면 판에 Breaker가 없다. 실행 상태는 참가자마다의 BreakerSkill이 가진다.
    // 치명타는 Breaker의 수치다(원작의 Breaker 강화 축, 노드 수치 breaker.crit-chance). 모든 스킬의 공통 수치가 아니다(SKILL_SYSTEM_PLAN D2).
    public sealed class BreakerDefinition
    {
        public float Damage { get; }
        // 공격 주기(초).
        public float Interval { get; }
        // 공격 원의 반지름. 화면의 범위 표시도 이 값이다.
        public float Radius { get; }
        // 한 Tick이 치명타일 확률(0 ~ 1).
        public float CritChance { get; }
        // 치명타 Tick의 피해 배율(1 이상). 확정 치명타 버프도 이 배율을 쓴다.
        public float CritMultiplier { get; }

        public BreakerDefinition(float damage, float interval, float radius, float critChance, float critMultiplier)
        {
            Damage = DefinitionGuard.Positive(damage, nameof(damage));
            Interval = DefinitionGuard.Positive(interval, nameof(interval));
            Radius = DefinitionGuard.Positive(radius, nameof(radius));

            if (float.IsNaN(critChance) || critChance < 0 || critChance > 1)
                throw new ArgumentOutOfRangeException(nameof(critChance), "0부터 1까지의 값이 필요하다.");

            if (float.IsNaN(critMultiplier) || float.IsInfinity(critMultiplier) || critMultiplier < 1)
                throw new ArgumentOutOfRangeException(nameof(critMultiplier), "1 이상의 유한한 값이 필요하다.");

            CritChance = critChance;
            CritMultiplier = critMultiplier;
        }
    }
}
