using BlackHole.Core;

namespace BlackHole.Unity
{
    // 결산 화면 ↔ 오케스트레이터의 완료 알림·진행 상태.
    // 오케스트레이터 → 흐름: 판의 종료 순서가 성공하면(정리·결산 뒤) 그 판의 원자료를 결산 대기로 둔다.
    // 흐름 → 화면: 원자료의 끝난 사유·진행 시간·처치 수·번 Gold와, 결산을 마친 진행 상태의 Gold.
    // 화면 → 흐름: 계속하기 — 결산 대기를 비운다. 판이 없으므로 다음 프레임에 업그레이드 화면이 열린다(ScreenFlow.FollowBattle).
    // 결산(Gold 더하기)은 여기서 하지 않는다. 화면이 열릴 때 이미 끝나 있다.
    internal sealed partial class ScreenFlow
    {
        // 열려 있는 결산 화면. 닫히면 null이다.
        private SettlementScreen _settlementScreen;
        // 결산 화면에 보일, 결산을 마친 판의 원자료. 계속하기 전까지 남는다.
        private BattleRawData _pendingSettlement;

        private void AwaitSettlement(BattleRawData raw) => _pendingSettlement = raw;

        private void OpenSettlementScreen()
        {
            _ui.SwitchRoot<SettlementScreen>(
                _settlementPresentation,
                afterPresented: screen => BindView(screen, BindSettlement),
                afterClosed: Unbind);
        }

        private void BindSettlement(SettlementScreen screen)
        {
            _settlementScreen = screen;
            AddCleanup(screen, () => _settlementScreen = null);

            AddBinding(screen,
                s => s.ContinueClicked += Continue,
                s => s.ContinueClicked -= Continue);

            screen.ShowResult(_pendingSettlement.EndReason, _pendingSettlement.PlayedSeconds);
            screen.ShowKills(_pendingSettlement.TotalKills, _pendingSettlement.Kills);
            screen.ShowGold(_pendingSettlement.EarnedGold, _player.Gold);
        }

        private void Continue() => _pendingSettlement = null;
    }
}
