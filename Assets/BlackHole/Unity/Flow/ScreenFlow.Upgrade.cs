using System;
using System.Collections.Generic;
using BlackHole.Core;

namespace BlackHole.Unity
{
    internal sealed partial class ScreenFlow
    {
        private readonly IReadOnlyList<NodeTreeView.NodeItem> _nodes;

        public void GoToUpgrade()
        {
            _ui.SwitchRoot<UpgradeScreen>(
                _upgradePresentation,
                afterPresented: root =>
                {
                    BindView(root, ApplyBindings);
                    root.BuildTree(_nodes, _tree.Graph.Links);
                    ShowUpgrade(root);
                },
                afterClosed: Unbind);
        }

        // 진행 상태가 바뀐 뒤 부른다(구매, 개발용 콘솔). 업그레이드 화면이 열려 있을 때만 다시 그린다.
        public void RefreshUpgrade()
        {
            if (_ui.CurrentRoot is UpgradeScreen root)
                ShowUpgrade(root);
        }

        private void ApplyBindings(UpgradeScreen root)
        {
            AddBinding(root,
                r => r.NodeClicked += HandleUpgradeNodeClicked,
                r => r.NodeClicked -= HandleUpgradeNodeClicked);

            AddBinding(root,
                r => r.StartBattleClicked += HandleUpgradeStartBattleClicked,
                r => r.StartBattleClicked -= HandleUpgradeStartBattleClicked);
        }

        private void HandleUpgradeNodeClicked(string id)
        {
            NodePurchase.TryPurchase(_player, _tree, id);
            RefreshUpgrade();
        }

        private void HandleUpgradeStartBattleClicked() => RequestStart();

        // 진행 상태를 화면 값(Gold, 산 노드 수, 노드마다의 상태)으로 바꿔 넘긴다.
        private void ShowUpgrade(UpgradeScreen root)
        {
            root.ShowGold(_player.Gold);
            root.ShowProgress(_player.OwnedNodes.Count, _tree.Nodes.Count);

            var states = new Dictionary<string, NodeState>(_tree.Nodes.Count, StringComparer.Ordinal);
            foreach (NodeDefinition node in _tree.Nodes)
                states.Add(node.Id, NodePurchase.StateOf(_player, _tree, node.Id));

            root.ShowNodes(states);
        }

        // 격자 칸은 화면 배치용 데이터다. 규칙 트리와 같은 저작 데이터에서 한 번 읽는다.
        private static IReadOnlyList<NodeTreeView.NodeItem> BuildNodeItems(NodeTree tree, NodeTreeData layout)
        {
            var cells = new Dictionary<string, (int X, int Y)>(StringComparer.Ordinal);
            foreach (NodeData node in layout.Nodes)
            {
                if (node?.Id != null && !cells.ContainsKey(node.Id))
                    cells.Add(node.Id, (node.X, node.Y));
            }

            var nodes = new List<NodeTreeView.NodeItem>(tree.Nodes.Count);
            foreach (NodeDefinition node in tree.Nodes)
            {
                (int x, int y) = cells.TryGetValue(node.Id, out (int X, int Y) cell) ? cell : (0, 0);
                nodes.Add(new NodeTreeView.NodeItem(node.Id, x, y, node.Price));
            }

            return nodes;
        }
    }
}
