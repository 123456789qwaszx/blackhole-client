using System;

namespace BlackHole.Core
{
    public enum PurchaseResult
    {
        Purchased,
        InBattle,
        AlreadyOwned,
        MissingPrerequisite,
        NotEnoughGold,
        // 요청한 노드 ID가 콘텐츠에 없다.
        UnknownNode
    }

    // 전투 밖 구매 규칙: 선행 노드와 Gold 가격(GAME_RULES 8절).
    // 실패하면 Gold와 구매 상태를 전혀 바꾸지 않는다.
    // UI는 노드 ID로 요청한다. 정의를 찾고 조건을 보고 기록하는 일은 여기서 한다. 효과는 다음 전투 조립 때 반영된다.
    public static class UpgradePurchase
    {
        public static PurchaseResult Check(PlayerState state, GameContent content, string nodeId)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (content == null)
                throw new ArgumentNullException(nameof(content));

            return content.TryGetUpgrade(nodeId, out UpgradeNodeDefinition node)
                ? Check(state, node)
                : PurchaseResult.UnknownNode;
        }

        public static PurchaseResult TryPurchase(PlayerState state, GameContent content, string nodeId)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (content == null)
                throw new ArgumentNullException(nameof(content));

            return content.TryGetUpgrade(nodeId, out UpgradeNodeDefinition node)
                ? TryPurchase(state, node)
                : PurchaseResult.UnknownNode;
        }

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
