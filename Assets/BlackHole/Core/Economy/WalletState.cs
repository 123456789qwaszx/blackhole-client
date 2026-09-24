using System;

namespace BlackHole.Core
{
    // 한 판의 재화 잔액. 지급은 흡수 보상(AbsorptionSystem), 소비는 강화 구매(UpgradePurchase)로만 일어난다.
    // 이번 실험에서 재화는 한 판 수명이다. 영구 재화 정책은 아직 없다.
    public sealed class WalletState
    {
        public int Credits { get; private set; }

        internal void Deposit(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "지급액은 0 이상이어야 한다.");
            Credits += amount;
        }

        internal bool CanAfford(int cost) => cost >= 0 && Credits >= cost;

        // 호출 전에 CanAfford로 판정한다. 판정 없이 부족한 금액을 빼면 오류다.
        internal void Withdraw(int cost)
        {
            if (!CanAfford(cost)) throw new InvalidOperationException($"잔액 {Credits}로 {cost}를 낼 수 없다.");
            Credits -= cost;
        }
    }
}
