using System;
using System.Collections.Generic;
using BlackHole.Core;

namespace BlackHole.Unity
{
    // 화면 연결과 전환. 버튼과 시간 종료에서 전투 수명을 요청하고, 성공 결과로 다음 화면을 연다.
    internal sealed partial class ScreenFlow : IDisposable
    {
        private readonly UIManager _ui;
        private readonly UIPresentationSpec _battlePresentation;
        private readonly UIPresentationSpec _upgradePresentation;
        private readonly UIPresentationSpec _settlementPresentation;
        private readonly UIPresentationSpec _nodeTreePresentation;
        private readonly BattleSystem _battle;
        private readonly BattleOrchestrator _orchestrator;
        private readonly NodeTree _tree;
        // 업그레이드 화면에 그릴 노드(칸·가격). 조립 때 저작 데이터의 격자 칸으로 만들어 받는다.
        private readonly IReadOnlyList<NodeTreeView.NodeItem> _nodes;
        private readonly PlayerState _player;
        private readonly Dictionary<UIBase, List<Action>> _cleanupByScreen = new Dictionary<UIBase, List<Action>>();

        public ScreenFlow(UIManager ui, UIPresentationSpec battlePresentation,
            UIPresentationSpec upgradePresentation, UIPresentationSpec settlementPresentation,
            UIPresentationSpec nodeTreePresentation,
            BattleSystem battle, BattleOrchestrator orchestrator, NodeTree tree,
            IReadOnlyList<NodeTreeView.NodeItem> nodes, PlayerState player)
        {
            _ui = ui;
            _battlePresentation = battlePresentation;
            _upgradePresentation = upgradePresentation;
            _settlementPresentation = settlementPresentation;
            _nodeTreePresentation = nodeTreePresentation;
            _battle = battle;
            _orchestrator = orchestrator;
            _tree = tree;
            _nodes = nodes;
            _player = player;
        }

        private void BindView<T>(T screen, Action<T> apply) where T : UIBase
        {
            Unbind(screen);
            apply(screen);
        }

        private void AddBinding<T>(T screen, Action<T> attach, Action<T> detach) where T : UIBase
        {
            attach(screen);
            AddCleanup(screen, () => detach(screen));
        }

        private void AddCleanup(UIBase screen, Action cleanup)
        {
            if (!_cleanupByScreen.TryGetValue(screen, out List<Action> cleanups))
            {
                cleanups = new List<Action>();
                _cleanupByScreen[screen] = cleanups;
            }

            cleanups.Add(cleanup);
        }

        private void Unbind(UIBase screen)
        {
            if (!_cleanupByScreen.TryGetValue(screen, out List<Action> cleanups))
                return;

            _cleanupByScreen.Remove(screen);
            RunCleanups(cleanups);
        }

        private static void RunCleanups(List<Action> cleanups)
        {
            for (int i = cleanups.Count - 1; i >= 0; i--)
                cleanups[i]?.Invoke();
        }

        public void Dispose()
        {
            foreach (List<Action> cleanups in _cleanupByScreen.Values)
                RunCleanups(cleanups);

            _cleanupByScreen.Clear();
        }
    }
}
