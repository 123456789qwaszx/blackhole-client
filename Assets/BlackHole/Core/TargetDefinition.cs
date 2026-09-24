using System;

namespace BlackHole.Core
{
    // 대상 한 종류의 공유 정의. 개별 HP·위치·생명주기는 TargetState가 소유한다.
    public sealed class TargetDefinition
    {
        public string Id { get; }
        public float MaxHealth { get; }
        public MovementDefinition Movement { get; }
        // 흡수가 확정될 때 한 번 받는다. 사망만으로는 받지 않는다.
        public RewardDefinition Reward { get; }

        public TargetDefinition(string id, float maxHealth, MovementDefinition movement, RewardDefinition reward)
        {
            Id = DefinitionGuard.Id(id, nameof(id));
            MaxHealth = DefinitionGuard.Positive(maxHealth, nameof(maxHealth));
            Movement = movement ?? throw new ArgumentNullException(nameof(movement), "이동 정의가 필요하다.");
            Reward = reward ?? throw new ArgumentNullException(nameof(reward), "보상 정의가 필요하다.");
        }
    }

    // 흡수 보상. 블랙홀 질량 증가와 재화 지급은 서로 독립인 수치다.
    public sealed class RewardDefinition
    {
        public int Mass { get; }
        public int Credits { get; }

        public RewardDefinition(int mass, int credits)
        {
            if (mass < 0) throw new ArgumentOutOfRangeException(nameof(mass), "0 이상의 정수가 필요하다.");
            if (credits < 0) throw new ArgumentOutOfRangeException(nameof(credits), "0 이상의 정수가 필요하다.");
            Mass = mass;
            Credits = credits;
        }
    }

    // 모든 대상에 공통인 생명주기 규칙.
    public sealed class TargetRulesDefinition
    {
        // 살아 있는 대상은 흡수 반경 + 이 거리보다 블랙홀에 가까이 가지 않는다.
        public float AliveMargin { get; }
        // 사망한 대상이 중심으로 떨어지는 속도.
        public float FallSpeed { get; }

        public TargetRulesDefinition(float aliveMargin, float fallSpeed)
        {
            AliveMargin = DefinitionGuard.NonNegative(aliveMargin, nameof(aliveMargin));
            FallSpeed = DefinitionGuard.Positive(fallSpeed, nameof(fallSpeed));
        }
    }
}
