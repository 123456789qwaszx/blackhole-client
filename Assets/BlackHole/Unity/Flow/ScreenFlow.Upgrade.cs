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
                    _upgradePresenter.Present(root);
                },
                afterClosed: Unbind);
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
            _upgradePresenter.PresentCurrent();
        }

        private void HandleUpgradeStartBattleClicked() => _orchestrator.RequestStart();

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
