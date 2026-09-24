using System;

namespace BlackHole.Core
{
    // 대상 한 종류의 공유 정의. 개별 HP·위치·생명주기는 TargetState가 소유한다.
    public sealed class TargetDefinition
    {
        public string Id { get; }
        public float MaxHealth { get; }
        public MovementDefinition Movement { get; }
        public int Reward { get; }

        public TargetDefinition(string id, float maxHealth, MovementDefinition movement, int reward)
        {
            Id = DefinitionGuard.Id(id, nameof(id));
            MaxHealth = DefinitionGuard.Positive(maxHealth, nameof(maxHealth));
            Movement = movement ?? throw new ArgumentNullException(nameof(movement), "이동 정의가 필요하다.");
            Reward = DefinitionGuard.Positive(reward, nameof(reward));
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
