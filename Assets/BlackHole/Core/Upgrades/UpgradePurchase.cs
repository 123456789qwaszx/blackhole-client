using System;

namespace BlackHole.Core
{
    public enum PurchaseResult
    {
        Purchased,
        InBattle,
        AlreadyOwned,
        MissingPrerequisite,
        NotEnoughGold
    }

    // 전투 밖 구매 규칙: 선행 노드와 Gold 가격(GAME_RULES 8절).
    // 실패하면 Gold와 구매 상태를 전혀 바꾸지 않는다.
    public static class UpgradePurchase
    {
        // 지금 사면 어떻게 되는지. 상태를 바꾸지 않는다. 구매 화면이 버튼 상태를 정할 때 쓴다.
        public static PurchaseResult Check(PlayerState state, UpgradeNodeDefinition node)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (node == null)
                throw new ArgumentNullException(nameof(node));

            if (state.InBattle)
                return PurchaseResult.InBattle;

            if (state.Owns(node.Id))
                return PurchaseResult.AlreadyOwned;

            if (node.Requires != null && !state.Owns(node.Requires))
                return PurchaseResult.MissingPrerequisite;

            if (state.Gold < node.Price)
                return PurchaseResult.NotEnoughGold;

            return PurchaseResult.Purchased;
        }

        public static PurchaseResult TryPurchase(PlayerState state, UpgradeNodeDefinition node)
        {
            PurchaseResult result = Check(state, node);

            if (result == PurchaseResult.Purchased)
                state.Buy(node);

            return result;
        }
    }
}
