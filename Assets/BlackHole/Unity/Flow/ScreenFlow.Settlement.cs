using System.Collections.Generic;
using BlackHole.Core;

namespace BlackHole.Unity
{
    internal sealed partial class ScreenFlow
    {
        public void GoToSettlement(SessionEndReason reason, float seconds, int totalKills,
            IReadOnlyList<EnemyKillCount> kills, long earnedGold, long totalGold)
        {
            _ui.SwitchRoot<SettlementScreen>(
                _settlementPresentation,
                afterPresented: root =>
                {
                    BindView(root, ApplyBindings);
                    root.ShowResult(reason, seconds);
                    root.ShowKills(totalKills, kills);
                    root.ShowGold(earnedGold, totalGold);
                },
                afterClosed: Unbind);
        }

        private void ApplyBindings(SettlementScreen root)
        {
            AddBinding(root,
                r => r.ContinueClicked += HandleSettlementContinueClicked,
                r => r.ContinueClicked -= HandleSettlementContinueClicked);
        }

        private void HandleSettlementContinueClicked() => GoToUpgrade();
    }
}
