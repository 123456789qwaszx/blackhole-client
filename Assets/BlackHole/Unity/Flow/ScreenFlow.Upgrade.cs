using System;
using System.Collections.Generic;
using BlackHole.Core;

namespace BlackHole.Unity
{
    internal sealed partial class ScreenFlow
    {
        private UpgradeScreen _upgradeScreen;
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

        private void HandleUpgradeNodeClicked(string id) => NodeClicked?.Invoke(id);
        private void HandleUpgradeStartBattleClicked() => StartBattleClicked?.Invoke();

        public void ShowUpgrade(long gold, int owned, int total, Func<string, NodeState> stateOf)
        {
            if (_upgradeScreen == null)
                return;

            _upgradeScreen.ShowGold(gold);
            _upgradeScreen.ShowProgress(owned, total);
            _upgradeScreen.ShowNodes(stateOf);
        }
    }
}
