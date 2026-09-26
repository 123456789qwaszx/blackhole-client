using System;

namespace BlackHole.Core
{
    // 사망 효과의 정의. 적 종류에 붙는 특성이다 — 종류가 아니다(전기 소행성은 연쇄 번개를 가진 적 종류다).
    // 효과를 가진 적은 사망 효과의 피해를 받지 않는다(GAME_RULES 10절). 그래서 효과가 효과를 부르지 않는다.
    // 새 효과: 하위 정의 + DeathEffects.Resolve 분기 + ContentLoader의 종류 이름(EnemyBehaviorDefinition과 같은 자리).
    public abstract class DeathEffectDefinition
    {
        private protected DeathEffectDefinition() { }
    }

    // 연쇄 번개: 죽은 자리에서 가장 가까운 적으로 번개가 옮겨 가며 피해를 준다(원작의 전기 천체, REFERENCE_ANALYSIS R3).
    // 한 번 옮겨 가는 거리는 Radius 이하, 옮겨 가는 횟수는 MaxTargets 이하, 한 번 맞힌 적은 다시 맞히지 않는다.
    // 세부(가장 가까운 순, 거리가 같으면 목록 순서)는 [임시]다(SYSTEM_CATALOG S06).
    public sealed class ChainLightningDefinition : DeathEffectDefinition
    {
        // 무한 연쇄를 막는 상한은 MaxTargets 자체다. 이 값은 저작 실수(지나치게 큰 수)만 막는다.
        public const int MaxTargetsLimit = 64;

        public float Damage { get; }
        public float Radius { get; }
        public int MaxTargets { get; }

        public ChainLightningDefinition(float damage, float radius, int maxTargets)
        {
            Damage = DefinitionGuard.Positive(damage, nameof(damage));
            Radius = DefinitionGuard.Positive(radius, nameof(radius));

            if (maxTargets < 1 || maxTargets > MaxTargetsLimit)
                throw new ArgumentOutOfRangeException(nameof(maxTargets), $"1부터 {MaxTargetsLimit}까지의 정수가 필요하다.");

            MaxTargets = maxTargets;
        }
    }

    // 폭발: 죽은 자리를 중심으로 Radius 안의 적 전부에게 한 번 피해를 준다(원작 초신성으로 보임, 피해 공식 미확인 [임시]).
    public sealed class ExplosionDefinition : DeathEffectDefinition
    {
        public float Damage { get; }
        public float Radius { get; }

        public ExplosionDefinition(float damage, float radius)
        {
            Damage = DefinitionGuard.Positive(damage, nameof(damage));
            Radius = DefinitionGuard.Positive(radius, nameof(radius));
        }
    }
}
