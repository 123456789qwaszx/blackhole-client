using System;
using System.Collections.Generic;
using BlackHole.Core;

namespace BlackHole.Unity
{
    // 화면 연결과 전환. 버튼은 시스템에 직접 요청하고, 성공한 전투 수명 사건에서 다음 화면을 연다.
    internal sealed partial class ScreenFlow : IDisposable
    {
        private readonly UIManager _ui;
        private readonly UIPresentationSpec _battlePresentation;
        private readonly UIPresentationSpec _upgradePresentation;
        private readonly UIPresentationSpec _settlementPresentation;
        private readonly BattleSystem _battle;
        private readonly BattleOrchestrator _orchestrator;
        private readonly NodeTree _tree;
        private readonly PlayerState _player;
        private readonly UpgradePresenter _upgradePresenter;
        private readonly Dictionary<UIBase, List<Action>> _cleanupByScreen = new Dictionary<UIBase, List<Action>>();

        public ScreenFlow(UIManager ui, UIPresentationSpec battlePresentation,
            UIPresentationSpec upgradePresentation, UIPresentationSpec settlementPresentation,
            BattleSystem battle, BattleOrchestrator orchestrator, NodeTree tree,
            NodeTreeData layout, PlayerState player, UpgradePresenter upgradePresenter)
        {
            _ui = ui;
            _battlePresentation = battlePresentation;
            _upgradePresentation = upgradePresentation;
            _settlementPresentation = settlementPresentation;
            _battle = battle;
            _orchestrator = orchestrator;
            _tree = tree;
            _player = player;
            _upgradePresenter = upgradePresenter;
            _nodes = BuildNodeItems(tree, layout);
            _orchestrator.BattleStarted += HandleBattleStarted;
            _orchestrator.BattleCompleted += HandleBattleCompleted;
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
            _orchestrator.BattleStarted -= HandleBattleStarted;
            _orchestrator.BattleCompleted -= HandleBattleCompleted;

            foreach (List<Action> cleanups in _cleanupByScreen.Values)
                RunCleanups(cleanups);

            _cleanupByScreen.Clear();
        }
    }
}
