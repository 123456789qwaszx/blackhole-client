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
                    root.BuildTree(_nodes, _tree.Graph.Links);
                    ShowUpgrade(root);
                },
                afterClosed: Unbind);
        }

        // 화면 밖에서 진행 상태가 바뀐 뒤 부른다(개발용 업그레이드 콘솔).
        // 콘솔은 결산 화면 위에서도 누를 수 있으므로 업그레이드 화면이 열려 있을 때만 다시 그린다. 결산 뒤에는 GoToUpgrade가 새 값으로 그린다.
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
            // 노드 클릭은 열려 있는 업그레이드 화면에서만 온다(바인딩이 그 화면의 수명과 같다).
            ShowUpgrade((UpgradeScreen)_ui.CurrentRoot);
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
    }
}
