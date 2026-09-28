using System.Collections.Generic;
using BlackHole.Core;

namespace BlackHole.Unity
{
    internal sealed partial class ScreenFlow
    {
        public void GoToSettlement(float seconds, int reachedLevel, int stage, int nextStage, bool milestone, int totalKills,
            IReadOnlyList<EnemyKillCount> kills, long earnedGold, long settledGold, long totalGold)
        {
            _ui.SwitchRoot<SettlementScreen>(
                _settlementPresentation,
                afterPresented: root =>
                {
                    BindView(root, ApplyBindings);
                    root.ShowResult(seconds, reachedLevel, stage, nextStage, milestone);
                    root.ShowKills(totalKills, kills);
                    root.ShowGold(earnedGold, settledGold, milestone, totalGold);
                    // 결산을 마친 진행 상태로 계산한다(성장도는 이미 올라 있다).
                    root.ShowProgress(_growth.MilestonesReachedBy(_player.GrowthStage), _growth.Milestones.Count,
                        _player.OwnedNodes.Count, _tree.Nodes.Count);
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
