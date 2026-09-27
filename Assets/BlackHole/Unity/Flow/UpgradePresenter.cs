using System;
using System.Collections.Generic;
using BlackHole.Core;

namespace BlackHole.Unity
{
    // 진행 상태를 화면에 필요한 값으로 바꾼다. 구매와 개발용 콘솔이 변경 시점에 갱신을 요청한다.
    internal sealed class UpgradePresenter
    {
        private readonly UIManager _ui;
        private readonly PlayerState _player;
        private readonly NodeTree _tree;

        public UpgradePresenter(UIManager ui, PlayerState player, NodeTree tree)
        {
            _ui = ui;
            _player = player;
            _tree = tree;
        }

        public void PresentCurrent()
        {
            if (_ui.CurrentRoot is UpgradeScreen screen)
                Present(screen);
        }

        public void Present(UpgradeScreen screen)
        {
            screen.ShowGold(_player.Gold);
            screen.ShowProgress(_player.OwnedNodes.Count, _tree.Nodes.Count);

            var states = new Dictionary<string, NodeState>(_tree.Nodes.Count, StringComparer.Ordinal);
            foreach (NodeDefinition node in _tree.Nodes)
                states.Add(node.Id, NodePurchase.StateOf(_player, _tree, node.Id));

            screen.ShowNodes(states);
        }
    }
}
