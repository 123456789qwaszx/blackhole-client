using BlackHole.Core;

namespace BlackHole.Unity
{
    // 전투 화면 ↔ 전투 시스템·오케스트레이터.
    // 화면 → 일시정지(전투 시스템), 전투 끝내기(오케스트레이터에 종료 요청 — 사유는 지금 시간 종료로 통일).
    // 전투 시스템 → 화면: 남은 시간, 일시정지 여부. 진행 중인 판이 없으면 비어 있는 표시다.
    // 전투 화면은 판을 시작하거나 정리하지 않는다. 판이 정리돼도 화면(UI)은 남는다.
    internal sealed partial class ScreenFlow
    {
        // 열려 있는 전투 화면. 닫히면 null이다.
        private BattleScreen _battleScreen;

        public void OpenBattleScreen()
        {
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
                s => s.EndClicked += RequestEnd,
                s => s.EndClicked -= RequestEnd);

            ShowBattle();
        }

        private void TogglePause() => _battle.TogglePause();

        private void RequestEnd() => _orchestrator.RequestEnd(SessionEndReason.TimeExpired);

        private void ShowBattle()
        {
            if (_battleScreen == null)
                return;

            GameSession session = _battle.Session;

            if (session == null)
                _battleScreen.ShowIdle();
            else
                _battleScreen.Show(session.Remaining, session.Phase == SessionPhase.Paused);
        }
    }
}
