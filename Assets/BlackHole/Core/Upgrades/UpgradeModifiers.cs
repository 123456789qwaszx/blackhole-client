using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 한 전투에 적용할 구매 효과. 효과 종류마다 적용하는 자리가 여기에 하나씩 있다.
    // 효과는 콘텐츠의 노드 순서대로 쌓는다. 산 순서와 무관하게 같은 결과가 나온다.
    // 기본 정의는 바꾸지 않는다. 계산된 값은 각 시스템이 적용 시점에 스냅샷으로 가진다.
    internal sealed class UpgradeModifiers : IBreakerStatModifier, IEnemyStatModifier
    {
        public static readonly UpgradeModifiers None = new UpgradeModifiers(Array.Empty<UpgradeEffect>());

        private readonly IReadOnlyList<UpgradeEffect> _effects;

        private UpgradeModifiers(IReadOnlyList<UpgradeEffect> effects)
        {
            _effects = effects;
        }

        // state가 산 노드들의 효과. 산 노드 ID가 콘텐츠에 없으면 조립 오류다.
        public static UpgradeModifiers For(PlayerState state, GameContent content)
        {
            foreach (string id in state.Upgrades)
            {
                if (!content.TryGetUpgrade(id, out _))
                    throw new InvalidOperationException($"{state.Id}가 산 노드 '{id}'가 콘텐츠에 없다.");
            }

            var effects = new List<UpgradeEffect>();

            foreach (UpgradeNodeDefinition node in content.Upgrades)
            {
                if (state.Owns(node.Id))
                    effects.AddRange(node.Effects);
            }

            return effects.Count == 0 ? None : new UpgradeModifiers(effects);
        }

        // 적·보상·공급처럼 판의 공유 대상에 영향을 주는 효과가 있는가.
        public bool AffectsSharedWorld
        {
            get
            {
                foreach (UpgradeEffect effect in _effects)
                {
                    if (!UpgradeEffect.TargetsSkill(effect.Kind))
                        return true;
                }

                return false;
            }
        }

        // Breaker 실행 수치(전투 조립 때).
        public BreakerStats Apply(BreakerSkillDefinition definition, BreakerStats current)
        {
            foreach (UpgradeEffect effect in _effects)
            {
                if (effect.Skill != definition)
                    continue;

                switch (effect.Kind)
                {
                    case UpgradeEffectKind.SkillDamageAdd:
                        current = new BreakerStats(current.Radius, current.Interval, current.Damage + effect.Value);
                        break;

                    case UpgradeEffectKind.SkillRadiusAdd:
                        current = new BreakerStats(current.Radius + effect.Value, current.Interval, current.Damage);
                        break;

                    case UpgradeEffectKind.SkillIntervalMultiply:
                        current = new BreakerStats(current.Radius, current.Interval * effect.Value, current.Damage);
                        break;
                }
            }

            return current;
        }

        // 관통 레이저 실행 수치(전투 조립 때).
        public PiercingLaserStats Apply(PiercingLaserDefinition definition, PiercingLaserStats current)
        {
            foreach (UpgradeEffect effect in _effects)
            {
                if (effect.Skill != definition)
                    continue;

                switch (effect.Kind)
                {
                    case UpgradeEffectKind.LaserDamageAdd:
                        current = new PiercingLaserStats(current.Interval, current.Damage + effect.Value, current.Width, current.TelegraphDuration);
                        break;

                    case UpgradeEffectKind.LaserIntervalMultiply:
                        current = new PiercingLaserStats(current.Interval * effect.Value, current.Damage, current.Width, current.TelegraphDuration);
                        break;

                    case UpgradeEffectKind.LaserWidthAdd:
                        current = new PiercingLaserStats(current.Interval, current.Damage, current.Width + effect.Value, current.TelegraphDuration);
                        break;
                }
            }

            return current;
        }

        // 산 해금 노드가 주는 Skill(콘텐츠의 노드 순서). 전투 조립 때 시작 구성 뒤에 붙는다.
        // 한 Skill의 해금 노드는 하나이고 시작 구성과 겹치지 않는다(ContentInvariants).
        public IReadOnlyList<PassiveSkillDefinition> UnlockedSkills()
        {
            var skills = new List<PassiveSkillDefinition>();

            foreach (UpgradeEffect effect in _effects)
            {
                if (effect.Kind == UpgradeEffectKind.SkillUnlock)
                    skills.Add(effect.Skill);
            }

            return skills;
        }

        // 적 실행 수치(출현 때).
        public EnemyStats Apply(EnemyDefinition definition, EnemyStats current)
        {
            foreach (UpgradeEffect effect in _effects)
            {
                if (effect.Kind == UpgradeEffectKind.EnemyHealthMultiply && Targets(effect, definition))
                    current = new EnemyStats(current.MaxHealth * effect.Value, current.MoveSpeed, current.Size);
            }

            return current;
        }

        // 적 사망 보상(출현 때). 배율을 모두 곱한 뒤 한 번 올림한다([임시]).
        public EnemyReward ApplyToReward(EnemyDefinition definition, EnemyReward current)
        {
            double gold = current.Gold;
            double hqExp = current.HqExp;

            foreach (UpgradeEffect effect in _effects)
            {
                if (!Targets(effect, definition))
                    continue;

                if (effect.Kind == UpgradeEffectKind.GoldMultiply)
                    gold *= effect.Value;
                else if (effect.Kind == UpgradeEffectKind.HqExpMultiply)
                    hqExp *= effect.Value;
            }

            return new EnemyReward(RoundUp(gold), RoundUp(hqExp));
        }

        // 성장 공급에서 이 적에 더해질 수(공급 요청 때).
        public int GrowthSupplyBonus(EnemyDefinition definition)
        {
            int bonus = 0;

            foreach (UpgradeEffect effect in _effects)
            {
                if (effect.Kind == UpgradeEffectKind.GrowthSupplyAdd && effect.Enemy == definition)
                    bonus = checked(bonus + (int)effect.Value);
            }

            return bonus;
        }

        private static bool Targets(UpgradeEffect effect, EnemyDefinition definition) =>
            effect.Enemy == null || effect.Enemy == definition;

        // 부동소수 오차로 정수가 한 칸 올라가지 않게 소수 넷째 자리에서 먼저 반올림한다.
        private static int RoundUp(double value) =>
            checked((int)Math.Ceiling(Math.Round(value, 4)));
    }
}
