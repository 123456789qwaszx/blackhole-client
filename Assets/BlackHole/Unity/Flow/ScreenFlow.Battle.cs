using BlackHole.Core;

namespace BlackHole.Unity
{
    internal sealed partial class ScreenFlow
    {
        private BattleScreen _battleScreen;
        public bool IsBattleOpen => _battleScreen != null;

        public void GoToBattle()
        {
            _ui.SwitchRoot<BattleScreen>(
                _battlePresentation,
                afterPresented: root =>
                {
                    BindView(root, ApplyBindings);
                    root.ShowIdle();
                },
                afterClosed: Unbind);
        }

        private void ApplyBindings(BattleScreen root)
        {
            _battleScreen = root;
            AddCleanup(root, () => _battleScreen = null);

            AddBinding(root,
                r => r.PauseClicked += HandleBattlePauseClicked,
                r => r.PauseClicked -= HandleBattlePauseClicked);

            AddBinding(root,
                r => r.EndClicked += HandleBattleEndClicked,
                r => r.EndClicked -= HandleBattleEndClicked);
        }

        private void HandleBattlePauseClicked() => _battle.TogglePause();
        private void HandleBattleEndClicked() => _orchestrator.RequestEnd(SessionEndReason.TimeExpired);

        public void ShowBattle(float remaining, long earnedGold, bool paused) =>
            _battleScreen?.Show(remaining, earnedGold, paused);

        public void ShowBattleIdle() => _battleScreen?.ShowIdle();
    }
}
