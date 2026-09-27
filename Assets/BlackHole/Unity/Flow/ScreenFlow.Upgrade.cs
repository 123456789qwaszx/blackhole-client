using System.Collections.Generic;
using BlackHole.Core;

namespace BlackHole.Unity
{
    internal sealed partial class ScreenFlow
    {
        private UpgradeScreen _upgradeScreen;
        private long _shownGold;
        private int _shownOwned;
        public bool IsUpgradeOpen => _upgradeScreen != null;

        public void GoToUpgrade(IReadOnlyList<NodeTreeView.NodeItem> nodes,
            IReadOnlyList<(string A, string B)> links)
        {
            _ui.SwitchRoot<UpgradeScreen>(
                _upgradePresentation,
                afterPresented: root =>
                {
                    BindView(root, ApplyBindings);
                    root.BuildTree(nodes, links);
                    ShowUpgrade();
                },
                afterClosed: Unbind);
        }

        private void ApplyBindings(UpgradeScreen root)
        {
            _upgradeScreen = root;
            AddCleanup(root, () => _upgradeScreen = null);

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
            ShowUpgrade();
        }

        private void HandleUpgradeStartBattleClicked() => _orchestrator.RequestStart();

        public void RefreshUpgrade()
        {
            if (_upgradeScreen != null && (_player.Gold != _shownGold || _player.OwnedNodes.Count != _shownOwned))
                ShowUpgrade();
        }

        private void ShowUpgrade()
        {
            if (_upgradeScreen == null)
                return;

            _shownGold = _player.Gold;
            _shownOwned = _player.OwnedNodes.Count;
            _upgradeScreen.ShowGold(_shownGold);
            _upgradeScreen.ShowProgress(_shownOwned, _tree.Nodes.Count);
            _upgradeScreen.ShowNodes(id => NodePurchase.StateOf(_player, _tree, id));
        }
    }
}
