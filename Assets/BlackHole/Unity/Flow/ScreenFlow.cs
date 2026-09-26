using System;
using System.Collections.Generic;

namespace BlackHole.Unity
{
    // 화면과, 화면이 전투 시스템과 만나는 경계.
    // 화면(View)은 버튼 사건을 알리고 표시 값을 받을 뿐이다.
    //
    // 지금 화면은 전투 화면 하나이고, 남은 시간만 보여 준다. 타이틀·설정·업그레이드 화면은 지웠다.
    // 전투의 시작·일시정지·정리는 화면이 아니라 전투 시작·종료 콘솔(개발용)이 오케스트레이터·전투 시스템에 요청한다.
    // 나중의 화면 전환(GoToBattle, GoToUpgrade)은 오케스트레이터의 시작·종료를 부르고 화면을 바꾸는 식으로 붙는다.
    // 화면마다 partial 파일 하나가 전환과 사건 연결을 가진다. 연결은 화면이 닫힐 때 모두 푼다.
    internal sealed partial class ScreenFlow : IDisposable
    {
        private readonly UIManager _ui;
        private readonly BattleSystem _battle;
        private readonly UIPresentationSpec _battlePresentation;

        public ScreenFlow(UIManager ui, BattleSystem battle, UIPresentationSpec battlePresentation)
        {
            _ui = ui;
            _battle = battle;
            _battlePresentation = battlePresentation;
        }

        // 한 프레임. 열린 화면의 표시 값을 맞춘다.
        public void Tick() => ShowBattle();

        #region 연결

        private readonly Dictionary<UIBase, List<Action>> _cleanupByScreen = new Dictionary<UIBase, List<Action>>();

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

        #endregion
    }
}
