using BlackHole.Core;

namespace BlackHole.Unity
{
    // 전투 화면 ↔ 전투 시스템.
    // 전투 시스템 → 화면: 남은 시간. 진행 중인 판이 없으면 비어 있는 표시다.
    // 전투 화면은 판을 시작·정지·정리하지 않는다(지금은 전투 시작·종료 콘솔이 한다). 판이 정리돼도 화면(UI)은 남는다.
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
            ShowBattle();
        }

        private void ShowBattle()
        {
            if (_battleScreen == null)
                return;

            GameSession session = _battle.Session;

            if (session == null)
                _battleScreen.ShowIdle();
            else
                _battleScreen.Show(session.Remaining);
        }
    }
}
