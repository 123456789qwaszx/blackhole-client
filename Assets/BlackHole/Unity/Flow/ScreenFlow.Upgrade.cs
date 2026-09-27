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
                    GoToNodeTree(root);
                    RefreshUpgrade();
                },
                afterClosed: Unbind);
        }

        // 업그레이드 화면을 호스트로 트리 보기 페이지를 연다. 업그레이드 화면이 닫히면 페이지도 닫히고 연결이 풀린다.
        private void GoToNodeTree(UpgradeScreen host)
        {
            _ui.SwitchPage<NodeTreeView>(
                host,
                _nodeTreePresentation,
                afterPresented: page =>
                {
                    BindView(page, ApplyBindings);
                    page.Build(_nodes, _tree.Graph.Links);
                },
                afterClosed: Unbind);
        }

        private void ApplyBindings(UpgradeScreen root)
        {
            AddBinding(root,
                r => r.StartBattleClicked += HandleUpgradeStartBattleClicked,
                r => r.StartBattleClicked -= HandleUpgradeStartBattleClicked);
        }

        private void ApplyBindings(NodeTreeView page)
        {
            AddBinding(page,
                p => p.NodeClicked += HandleUpgradeNodeClicked,
                p => p.NodeClicked -= HandleUpgradeNodeClicked);
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

            _ui.GetUI<NodeTreeView>().Show(states);
        }
    }
}
