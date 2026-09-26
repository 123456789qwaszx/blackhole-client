using System.Collections.Generic;
using BlackHole.Core;

namespace BlackHole.Unity
{
    // 업그레이드 화면 ↔ 노드 트리·진행 상태.
    // 화면 → 노드 구매(노드 ID), 전투 시작(오케스트레이터에 요청). 노드 트리·진행 상태 → 화면: 노드마다 칸·가격·상태, 선 목록, Gold, 산 노드 수.
    // 진행 상태는 개발용 콘솔도 바꾼다(Gold, 전체 해금·잠금). 그래서 매 프레임 Gold와 산 노드 수를 보고, 바뀐 프레임에만 다시 칠한다.
    internal sealed partial class ScreenFlow
    {
        // 열려 있는 업그레이드 화면. 닫히면 null이다.
        private UpgradeScreen _upgradeScreen;
        private long _shownGold;
        private int _shownOwned;

        public void OpenUpgradeScreen()
        {
            _ui.SwitchRoot<UpgradeScreen>(
                _upgradePresentation,
                afterPresented: screen => BindView(screen, BindUpgrade),
                afterClosed: Unbind);
        }

        private void BindUpgrade(UpgradeScreen screen)
        {
            _upgradeScreen = screen;
            AddCleanup(screen, () => _upgradeScreen = null);

            AddBinding(screen,
                s => s.NodeClicked += Purchase,
                s => s.NodeClicked -= Purchase);

            AddBinding(screen,
                s => s.StartBattleClicked += RequestStart,
                s => s.StartBattleClicked -= RequestStart);

            screen.BuildTree(NodeItems(), _tree.Graph.Links);
            ShowUpgrade();
        }

        private void Purchase(string nodeId)
        {
            NodePurchase.TryPurchase(_player, _tree, nodeId);
            ShowUpgrade();
        }

        // 시작만 요청한다. 판이 돌기 시작하면 화면 흐름이 전투 화면으로 바꾼다(ScreenFlow.FollowBattle).
        private void RequestStart() => _orchestrator.RequestStart();

        private void TickUpgrade()
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
            _upgradeScreen.ShowGold(_player.Gold);
            _upgradeScreen.ShowProgress(_player.OwnedNodes.Count, _tree.Nodes.Count);
            _upgradeScreen.ShowNodes(id => NodePurchase.StateOf(_player, _tree, id));
        }

        // 노드마다 칸과 가격. 칸은 저작 데이터에서 읽는다(로더가 같은 데이터로 트리를 만들었으니 모든 노드에 칸이 있다).
        private List<NodeTreeView.NodeItem> NodeItems()
        {
            var items = new List<NodeTreeView.NodeItem>(_tree.Nodes.Count);

            foreach (NodeDefinition node in _tree.Nodes)
            {
                (int x, int y) = _cells.TryGetValue(node.Id, out (int X, int Y) cell) ? cell : (0, 0);
                items.Add(new NodeTreeView.NodeItem(node.Id, x, y, node.Price));
            }

            return items;
        }
    }
}
