using System;
using System.Collections.Generic;
using BlackHole.Core;

namespace BlackHole.Unity
{
    internal sealed partial class ScreenFlow
    {
        public void GoToUpgrade()
        {
            _ui.SwitchRoot<UpgradeScreen>(
                _upgradePresentation,
                afterPresented: root =>
                {
                    BindView(root, ApplyBindings);
                    _treeView.Build(_nodes, _tree.Graph.Links);
                    RefreshUpgrade();
                },
                afterClosed: Unbind);
        }

        private void ApplyBindings(UpgradeScreen root)
        {
            // 트리 보기는 업그레이드 화면 프리팹 안에 있다. 연결은 그 화면의 수명과 함께 풀린다.
            AddBinding(root,
                _ => _treeView.NodeClicked += HandleUpgradeNodeClicked,
                _ => _treeView.NodeClicked -= HandleUpgradeNodeClicked);

            AddBinding(root,
                r => r.StartBattleClicked += HandleUpgradeStartBattleClicked,
                r => r.StartBattleClicked -= HandleUpgradeStartBattleClicked);
        }

        private void HandleUpgradeNodeClicked(string id)
        {
            NodePurchase.TryPurchase(_player, _tree, id);
            RefreshUpgrade();
        }

        private void HandleUpgradeStartBattleClicked()
        {
            RequestStart();
        }

        // 진행 상태를 화면 값(Gold, 산 노드 수, 노드마다의 상태)으로 바꿔 넘긴다.
        private void RefreshUpgrade()
        {
            if (!(_ui.CurrentRoot is UpgradeScreen root))
                return;

            root.ShowGold(_player.Gold);
            root.ShowProgress(_player.OwnedNodes.Count, _tree.Nodes.Count);

            var states = new Dictionary<string, NodeState>(_tree.Nodes.Count, StringComparer.Ordinal);
            foreach (NodeDefinition node in _tree.Nodes)
                states.Add(node.Id, NodePurchase.StateOf(_player, _tree, node.Id));

            _treeView.Show(states);
        }
    }
}
