using System.Collections.Generic;
using BlackHole.Core;

namespace BlackHole.Unity
{
    internal sealed partial class ScreenFlow
    {
        private void HandleBattleCompleted(BattleRawData raw) =>
            GoToSettlement(raw.EndReason, raw.PlayedSeconds, raw.TotalKills,
                raw.Kills, raw.EarnedGold, _player.Gold);

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
