using BlackHole.Core;

namespace BlackHole.Unity
{
    // 전투 화면 ↔ 전투 Session.
    // 화면 → Session: 일시정지, 전투 끝내기.  Session → 화면: 남은 시간, 일시정지 여부.
    // 끝난 전투(시간 종료 또는 끝내기)는 다음 Tick에 업그레이드 화면으로 간다. 전환하는 자리는 TickBattle 하나다.
    internal sealed partial class ScreenFlow
    {
        // 열려 있는 전투 화면. 닫히면 null이다.
        private BattleScreen _battleScreen;

        // 진행 상태로 새 전투를 조립하고 전투 화면을 연다. 조립이 진행 상태를 전투에 묶는다(구매 불가).
        private void GoToBattle()
        {
            _battle = SessionAssembler.CreateBattle(_content, _progress);

            _ui.SwitchRoot<BattleScreen>(
                _battlePresentation,
                afterPresented: screen => BindView(screen, BindBattle),
                afterClosed: Unbind);
        }

        private void BindBattle(BattleScreen screen)
        {
            _battleScreen = screen;
            AddCleanup(screen, () => _battleScreen = null);

            AddBinding(screen,
                s => s.PauseClicked += TogglePause,
                s => s.PauseClicked -= TogglePause);

            AddBinding(screen,
                s => s.EndClicked += StopBattle,
                s => s.EndClicked -= StopBattle);

            ShowBattle();
        }

        private void TogglePause()
        {
            _battle.TogglePause();
            ShowBattle();
        }

        // 결과를 확정하고 진행 상태를 풀어 준다. 화면 전환은 다음 Tick이 한다.
        private void StopBattle() => _battle.Stop();

        private void TickBattle(float delta)
        {
            if (_battleScreen == null)
                return;

            _battle.Advance(delta);

            if (_battle.Phase == SessionPhase.Ended)
            {
                GoToUpgrade();
                return;
            }

            ShowBattle();
        }

        private void ShowBattle() =>
            _battleScreen.Show(_battle.Remaining, _battle.Phase == SessionPhase.Paused);
    }
}
