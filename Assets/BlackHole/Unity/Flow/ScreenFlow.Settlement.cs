using System.Collections.Generic;
using BlackHole.Core;

namespace BlackHole.Unity
{
    internal sealed partial class ScreenFlow
    {
        public void GoToSettlement(float seconds, int reachedLevel, bool milestone, int totalKills,
            IReadOnlyList<EnemyKillCount> kills, long earnedGold, long settledGold, long totalGold)
        {
            _ui.SwitchRoot<SettlementScreen>(
                _settlementPresentation,
                afterPresented: root =>
                {
                    BindView(root, ApplyBindings);
                    root.ShowResult(seconds, reachedLevel, milestone);
                    root.ShowKills(totalKills, kills);
                    root.ShowGold(earnedGold, settledGold, milestone, totalGold);
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
