using System.Collections.Generic;
using BlackHole.Core;

namespace BlackHole.Unity
{
    internal sealed partial class ScreenFlow
    {
        private SettlementScreen _settlementScreen;
        public bool IsSettlementOpen => _settlementScreen != null;

        public void OpenSettlementScreen(SessionEndReason reason, float seconds, int totalKills,
            IReadOnlyList<EnemyKillCount> kills, long earnedGold, long totalGold)
        {
            _ui.SwitchRoot<SettlementScreen>(
                _settlementPresentation,
                afterPresented: screen =>
                {
                    BindView(screen, BindSettlement);
                    screen.ShowResult(reason, seconds);
                    screen.ShowKills(totalKills, kills);
                    screen.ShowGold(earnedGold, totalGold);
                },
                afterClosed: Unbind);
        }

        private void BindSettlement(SettlementScreen screen)
        {
            _settlementScreen = screen;
            AddCleanup(screen, () => _settlementScreen = null);
            AddBinding(screen, s => s.ContinueClicked += OnContinueClicked, s => s.ContinueClicked -= OnContinueClicked);
        }

        private void OnContinueClicked() => ContinueClicked?.Invoke();
    }
}
