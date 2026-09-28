using UnityEngine;

namespace BlackHole.Unity
{
    // 개발용 콘솔 조립. 에디터와 개발 빌드에서만 만들고, 조립한 GameHost에 붙인다.
    public sealed partial class GameBootstrap
    {
        private void BootstrapDevelopmentConsoles()
        {
            if (!Debug.isDebugBuild)
                return;

            var console = new ControlConsole(transform, _orchestrator, _battle, _content, _nodeTree, _enemyLooks);
            var lifecycleConsole = new BattleLifecycleConsole(transform, _orchestrator, _battle, _nodeTree,
                _screens.HandleLifecycleStartBattleClicked,
                _screens.HandleLifecyclePauseClicked, _screens.HandleLifecycleEndBattleClicked);
            var commandConsole = new EnemyCommandConsole(transform, _battle, _content.Enemies);
            var upgradeConsole = new UpgradeConsole(transform, _viewer, _nodeTree, _content.Growth, _screens.HandleUpgradeConsoleProgressChanged);
            var skillConsole = new SkillConsole(transform, _content, _battle, _viewer.Id);

            _host.AttachConsoles(console, lifecycleConsole, commandConsole, upgradeConsole, skillConsole);
        }
    }
}
