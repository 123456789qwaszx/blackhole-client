using System;
using System.Collections.Generic;
using BlackHole.Core;

namespace BlackHole.Unity
{
    internal sealed partial class ScreenFlow
    {
        private UpgradeScreen _upgradeScreen;
        public bool IsUpgradeOpen => _upgradeScreen != null;

        public void OpenUpgradeScreen(IReadOnlyList<NodeTreeView.NodeItem> nodes,
            IReadOnlyList<(string A, string B)> links)
        {
            _ui.SwitchRoot<UpgradeScreen>(
                _upgradePresentation,
                afterPresented: screen =>
                {
                    BindView(screen, BindUpgrade);
                    screen.BuildTree(nodes, links);
                },
                afterClosed: Unbind);
        }

        private void BindUpgrade(UpgradeScreen screen)
        {
            _upgradeScreen = screen;
            AddCleanup(screen, () => _upgradeScreen = null);
            AddBinding(screen, s => s.NodeClicked += OnNodeClicked, s => s.NodeClicked -= OnNodeClicked);
            AddBinding(screen, s => s.StartBattleClicked += OnStartClicked, s => s.StartBattleClicked -= OnStartClicked);
        }

        private void OnNodeClicked(string id) => NodeClicked?.Invoke(id);
        private void OnStartClicked() => StartBattleClicked?.Invoke();

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
