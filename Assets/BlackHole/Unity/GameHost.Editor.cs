namespace BlackHole.Unity
{
    // 개발용 콘솔의 수명. 에디터와 개발 빌드에서만 GameBootstrap이 콘솔을 만들어 붙인다. 붙이지 않으면 아무것도 하지 않는다.
    internal sealed partial class GameHost
    {
        private ControlConsole _console;
        private BattleLifecycleConsole _lifecycleConsole;
        private EnemyCommandConsole _commandConsole;
        private UpgradeConsole _upgradeConsole;
        private SkillConsole _skillConsole;

        internal void AttachConsoles(ControlConsole console, BattleLifecycleConsole lifecycleConsole,
            EnemyCommandConsole commandConsole, UpgradeConsole upgradeConsole, SkillConsole skillConsole)
        {
            _console = console;
            _lifecycleConsole = lifecycleConsole;
            _commandConsole = commandConsole;
            _upgradeConsole = upgradeConsole;
            _skillConsole = skillConsole;
        }

        // 스킬 콘솔에서 고른 켜짐은 새 판의 첫 Step 전에 맞춰야 하므로 전투 Step보다 먼저 갱신한다.
        private void TickConsolesBeforeBattle()
        {
            _skillConsole?.Tick();
        }

        // 나머지 콘솔은 전투 Step 뒤의 상태를 보여 준다.
        private void TickConsolesAfterBattle()
        {
            _console?.Tick();
            _lifecycleConsole?.Tick();
            _commandConsole?.Tick();
            _upgradeConsole?.Tick();
        }

        private void DisposeConsoles()
        {
            _skillConsole?.Dispose();
            _upgradeConsole?.Dispose();
            _commandConsole?.Dispose();
            _lifecycleConsole?.Dispose();
            _console?.Dispose();
        }
    }
}
