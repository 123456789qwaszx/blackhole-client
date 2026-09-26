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

    // 검증된 노드 트리: 노드, 선, 구매 규칙. NodeTreeLoader만 만든다.
    //
    // 드러남 = 시작 노드이거나, 산 노드와 선으로 이어져 있다. 이웃 하나만 사도 드러난다(원작: 사야 그 너머가 드러난다).
    // 선은 방향이 없고 순환을 허용한다(원작 트리의 격자 묶음은 이웃끼리 모두 이어져 있다).
    // 살 수 있음 = 드러남 + 아직 안 삼 + 전투 밖 + Gold ≥ 가격. 사면 Gold를 빼고 진행 상태에 노드 ID를 남긴다.
    // 실패한 구매는 Gold와 산 노드를 전혀 바꾸지 않는다. 화면·콘솔은 노드 ID로 요청한다.
    //
    // 로더가 보장하는 것: 노드 ID가 유일하다, 선은 트리 안의 다른 노드를 가리킨다,
    // 노드가 있으면 시작 노드가 하나 이상이고 모든 노드가 시작 노드에서 선을 따라 닿는다.
    public sealed class NodeTree
    {
        private readonly Dictionary<string, NodeDefinition> _byId = new Dictionary<string, NodeDefinition>(StringComparer.Ordinal);
        private readonly Dictionary<NodeDefinition, IReadOnlyList<NodeDefinition>> _neighbors;

        public IReadOnlyList<NodeDefinition> Nodes { get; }

        internal NodeTree(List<NodeDefinition> nodes, Dictionary<NodeDefinition, IReadOnlyList<NodeDefinition>> neighbors)
        {
            Nodes = nodes.AsReadOnly();
            _neighbors = neighbors;

            foreach (NodeDefinition node in nodes)
                _byId.Add(node.Id, node);
        }

        public bool TryGet(string id, out NodeDefinition node)
        {
            node = null;
            return id != null && _byId.TryGetValue(id, out node);
        }

        // 지금 사면 어떻게 되는지. 상태를 바꾸지 않는다.
        public PurchaseResult Check(PlayerState state, string nodeId)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            return TryGet(nodeId, out NodeDefinition node) ? Check(state, node) : PurchaseResult.UnknownNode;
        }

        public PurchaseResult TryPurchase(PlayerState state, string nodeId)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (!TryGet(nodeId, out NodeDefinition node))
                return PurchaseResult.UnknownNode;

            PurchaseResult result = Check(state, node);

            if (result == PurchaseResult.Purchased)
                state.Buy(node);

            return result;
        }

        public NodeState StateOf(PlayerState state, string nodeId)
        {
            switch (Check(state, nodeId))
            {
                case PurchaseResult.UnknownNode: throw new ArgumentException($"트리에 없는 노드다: '{nodeId}'.", nameof(nodeId));
                case PurchaseResult.AlreadyOwned: return NodeState.Owned;
                case PurchaseResult.Hidden: return NodeState.Hidden;
                case PurchaseResult.Purchased: return NodeState.Purchasable;
                default: return NodeState.Revealed;
            }
        }

        // 산 노드들의 업그레이드를 모은 표. 두 시스템이 만나는 자리는 여기 하나다.
        // 진행 상태에 있지만 트리에 없는 ID(삭제된 노드)는 건너뛴다. 삭제된 노드의 진행 처리는 미정이다(F04).
        public UpgradeTable UpgradesFor(PlayerState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            var upgrades = new List<Upgrade>();

            foreach (NodeDefinition node in Nodes)
            {
                if (state.Owns(node.Id))
                    upgrades.AddRange(node.Upgrades);
            }

            return new UpgradeTable(upgrades);
        }

        // 판정 순서: 산 것 → 숨김 → 전투 중 → Gold. 그래서 전투 중에도 산 노드와 숨은 노드는 그 상태로 보인다.
        private PurchaseResult Check(PlayerState state, NodeDefinition node)
        {
            if (state.Owns(node.Id))
                return PurchaseResult.AlreadyOwned;

            if (!IsRevealed(state, node))
                return PurchaseResult.Hidden;

            if (state.InBattle)
                return PurchaseResult.InBattle;

            if (state.Gold < node.Price)
                return PurchaseResult.NotEnoughGold;

            return PurchaseResult.Purchased;
        }

        private bool IsRevealed(PlayerState state, NodeDefinition node)
        {
            if (node.IsStart)
                return true;

            foreach (NodeDefinition neighbor in _neighbors[node])
            {
                if (state.Owns(neighbor.Id))
                    return true;
            }

            return false;
        }
    }
}
