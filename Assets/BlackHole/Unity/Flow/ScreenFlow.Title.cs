using BlackHole.Core;

namespace BlackHole.Unity
{
    internal sealed partial class ScreenFlow
    {
        private void GoToTitle()
        {
            _ui.SwitchRoot<TitleScreen>(
                _titlePresentation,
                afterPresented: screen => BindView(screen, BindTitle),
                afterClosed: Unbind);
        }

        private void BindTitle(TitleScreen screen)
        {
            AddBinding(screen,
                s => s.StartClicked += StartNewRun,
                s => s.StartClicked -= StartNewRun);

            AddBinding(screen,
                s => s.SettingsClicked += GoToSettings,
                s => s.SettingsClicked -= GoToSettings);

            AddBinding(screen,
                s => s.QuitClicked += Quit,
                s => s.QuitClicked -= Quit);
        }

        // 새 진행: 참가자마다 빈 진행 상태로 첫 전투를 연다. 이전 진행은 버린다.
        private void StartNewRun()
        {
            _progress = new PlayerState[_participants.Count];

            for (int i = 0; i < _progress.Length; i++)
            {
                _progress[i] = new PlayerState(_participants[i]);
            }

            GoToBattle();
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            UnityEngine.Application.Quit();
#endif
        }
    }
}
