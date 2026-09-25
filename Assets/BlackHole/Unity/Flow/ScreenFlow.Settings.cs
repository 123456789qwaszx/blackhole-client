namespace BlackHole.Unity
{
    internal sealed partial class ScreenFlow
    {
        private void GoToSettings()
        {
            _ui.SwitchRoot<SettingsScreen>(
                _settingsPresentation,
                afterPresented: screen => BindView(screen, BindSettings),
                afterClosed: Unbind);
        }

        private void BindSettings(SettingsScreen screen)
        {
            AddBinding(screen,
                s => s.BackClicked += GoToTitle,
                s => s.BackClicked -= GoToTitle);
        }
    }
}
