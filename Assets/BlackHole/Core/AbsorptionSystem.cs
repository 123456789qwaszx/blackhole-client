namespace BlackHole.Core
{
    // 흡수 확정과 보상 지급의 순서를 한 흐름에 둔다. 사망 콜백과 뷰 Destroy에서 따로 보상을 주지 않는다.
    //
    // 1. 판정: 이번 단계 시작의 흡수 반경으로 판정한다(같은 단계의 흡수로 반경이 커져도 다시 보지 않는다).
    // 2. 확정: Defeated → Absorbed 전이에 성공한 대상만 흡수한다. 이미 흡수된 대상은 다시 전이하지 않는다.
    // 3. 보상: 확정된 대상마다 질량은 블랙홀에, 재화는 지갑에 반영한다.
    // 4. 제거: 확정된 대상을 같은 단계에 목록에서 제거한다.
    internal sealed class AbsorptionSystem
    {
        public void Resolve(TargetWorld world, BlackHoleState blackHole, WalletState wallet)
        {
            float radius = blackHole.AbsorptionRadius;
            foreach (TargetState target in world.Targets)
            {
                if (target.TryAbsorb(radius))
                    GrantReward(target.Definition.Reward, blackHole, wallet);
            }
            world.RemoveAbsorbed();
        }

        private static void GrantReward(RewardDefinition reward, BlackHoleState blackHole, WalletState wallet)
        {
            blackHole.Absorb(reward.Mass);
            wallet.Deposit(reward.Credits);
        }
    }
}
