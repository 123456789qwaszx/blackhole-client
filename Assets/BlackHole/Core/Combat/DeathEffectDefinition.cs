using System;

namespace BlackHole.Core
{
    // 적 종류가 지정하는 사망 효과의 정의. 적 본체는 이것이 어떤 효과인지 모른다(GAME_RULES 11절).
    // 새 효과: 하위 정의 + DeathEffects.Resolve 분기 + ContentLoader의 종류 이름.
    public abstract class DeathEffectDefinition
    {
        private protected DeathEffectDefinition() { }
    }

    // 전기 연쇄 번개. 죽은 자리에서 가까운 적으로 튀며 피해를 준다.
    public sealed class ChainLightningDefinition : DeathEffectDefinition
    {
        // 번개 한 번의 피해.
        public float Damage { get; }
        // 한 번 튈 수 있는 거리.
        public float Range { get; }
        // 번개가 맞히는 최대 적 수.
        public int Chains { get; }

        public ChainLightningDefinition(float damage, float range, int chains)
        {
            if (chains < 1)
                throw new ArgumentOutOfRangeException(nameof(chains), "1 이상의 정수가 필요하다.");

            Damage = DefinitionGuard.Positive(damage, nameof(damage));
            Range = DefinitionGuard.Positive(range, nameof(range));
            Chains = chains;
        }
    }
}
