namespace BlackHole.Unity
{
    internal sealed partial class ScreenFlow
    {
        private BattleScreen _battleScreen;
        public bool IsBattleOpen => _battleScreen != null;

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
            AddBinding(screen, s => s.PauseClicked += OnPauseClicked, s => s.PauseClicked -= OnPauseClicked);
            AddBinding(screen, s => s.EndClicked += OnEndClicked, s => s.EndClicked -= OnEndClicked);
            screen.ShowIdle();
        }

        private void OnPauseClicked() => PauseClicked?.Invoke();
        private void OnEndClicked() => EndClicked?.Invoke();

        public void ShowBattle(float remaining, long earnedGold, bool paused) =>
            _battleScreen?.Show(remaining, earnedGold, paused);

        public void ShowBattleIdle() => _battleScreen?.ShowIdle();
    }
}
