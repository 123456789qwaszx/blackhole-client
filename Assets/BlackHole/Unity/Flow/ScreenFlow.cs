using System;
using System.Collections.Generic;

namespace BlackHole.Unity
{
    // 화면 프리팹과 UIManager를 연결한다. 게임 상태, 화면 전환 판단, 게임 명령은 GameFlow가 가진다.
    internal sealed partial class ScreenFlow : IDisposable
    {
        private readonly UIManager _ui;
        private readonly UIPresentationSpec _battlePresentation;
        private readonly UIPresentationSpec _upgradePresentation;
        private readonly UIPresentationSpec _settlementPresentation;
        private readonly Dictionary<UIBase, List<Action>> _cleanupByScreen = new Dictionary<UIBase, List<Action>>();

        public event Action PauseClicked;
        public event Action EndClicked;
        public event Action<string> NodeClicked;
        public event Action StartBattleClicked;
        public event Action ContinueClicked;

        public ScreenFlow(UIManager ui, UIPresentationSpec battlePresentation,
            UIPresentationSpec upgradePresentation, UIPresentationSpec settlementPresentation)
        {
            _ui = ui;
            _battlePresentation = battlePresentation;
            _upgradePresentation = upgradePresentation;
            _settlementPresentation = settlementPresentation;
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
