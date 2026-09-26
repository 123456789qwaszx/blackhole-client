using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    public enum PurchaseResult
    {
        Purchased,
        // 요청한 노드 ID가 트리에 없다.
        UnknownNode,
        AlreadyOwned,
        // 아직 드러나지 않았다: 시작 노드가 아니고 산 이웃도 없다.
        Hidden,
        InBattle,
        NotEnoughGold,
    }

    // 화면이 노드를 그릴 때 쓰는 상태. 구매 판정(PurchaseResult)을 네 가지로 줄인 것이다.
    public enum NodeState
    {
        // 시작 노드가 아니고 산 이웃도 없다. 실루엣으로라도 보여 줄지는 화면이 정한다.
        Hidden,
        // 보이지만 지금은 살 수 없다(Gold 부족, 전투 중).
        Revealed,
        Purchasable,
        Owned,
    }

    // 노드 구매: 가격·Gold·산 노드. "무엇을 사는가"의 규칙이다. 도달 가능 여부는 그래프(NodeGraph)에 묻는다.
    //
    // 살 수 있음 = 드러남(그래프) + 아직 안 삼 + 전투 밖 + Gold ≥ 가격. 사면 Gold를 빼고 진행 상태에 노드 ID를 남긴다.
    // 실패한 구매는 Gold와 산 노드를 전혀 바꾸지 않는다. 화면·콘솔은 노드 ID로 요청한다. 환불은 없다(원작에 없다).
    public static class NodePurchase
    {
        // 지금 사면 어떻게 되는지. 상태를 바꾸지 않는다.
        public static PurchaseResult Check(PlayerState state, NodeTree tree, string nodeId)
        {
            Verify(state, tree);
            return tree.TryGet(nodeId, out NodeDefinition node) ? Check(state, tree, node) : PurchaseResult.UnknownNode;
        }

        public static PurchaseResult TryPurchase(PlayerState state, NodeTree tree, string nodeId)
        {
            Verify(state, tree);

            if (!tree.TryGet(nodeId, out NodeDefinition node))
                return PurchaseResult.UnknownNode;

            PurchaseResult result = Check(state, tree, node);

            if (result == PurchaseResult.Purchased)
                state.Buy(node);

            return result;
        }

        public static NodeState StateOf(PlayerState state, NodeTree tree, string nodeId)
        {
            switch (Check(state, tree, nodeId))
            {
                case PurchaseResult.UnknownNode: throw new ArgumentException($"트리에 없는 노드다: '{nodeId}'.", nameof(nodeId));
                case PurchaseResult.AlreadyOwned: return NodeState.Owned;
                case PurchaseResult.Hidden: return NodeState.Hidden;
                case PurchaseResult.Purchased: return NodeState.Purchasable;
                default: return NodeState.Revealed;
            }
        }

        // 산 노드들의 업그레이드를 모은 표. 노드 트리와 업그레이드가 만나는 자리는 여기 하나다.
        // 진행 상태에 있지만 트리에 없는 ID(삭제된 노드)는 건너뛴다. 삭제된 노드의 진행 처리는 미정이다(F04).
        public static UpgradeTable UpgradesFor(PlayerState state, NodeTree tree)
        {
            Verify(state, tree);
            var upgrades = new List<Upgrade>();

            foreach (NodeDefinition node in tree.Nodes)
            {
                if (state.Owns(node.Id))
                    upgrades.AddRange(node.Upgrades);
            }

            return new UpgradeTable(upgrades);
        }

        // 판정 순서: 산 것 → 숨김 → 전투 중 → Gold. 그래서 전투 중에도 산 노드와 숨은 노드는 그 상태로 보인다.
        private static PurchaseResult Check(PlayerState state, NodeTree tree, NodeDefinition node)
        {
            if (state.Owns(node.Id))
                return PurchaseResult.AlreadyOwned;

            if (!tree.Graph.IsRevealed(node.Id, state.Owns))
                return PurchaseResult.Hidden;

            if (state.InBattle)
                return PurchaseResult.InBattle;

            if (state.Gold < node.Price)
                return PurchaseResult.NotEnoughGold;

            return PurchaseResult.Purchased;
        }

        private static void Verify(PlayerState state, NodeTree tree)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (tree == null)
                throw new ArgumentNullException(nameof(tree));
        }
    }
}
