using System;

namespace BlackHole.Core
{
    // 흡수 보상. 블랙홀 질량 증가와 재화 지급은 서로 독립인 수치다.
    // 대상 정의(TargetDefinition.Reward)가 들고, 흡수 확정 때 AbsorptionSystem이 지급한다.
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
}
