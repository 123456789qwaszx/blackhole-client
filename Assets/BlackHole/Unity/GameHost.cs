using System;
using BlackHole.Core;

namespace BlackHole.Unity
{
    // 조립된 한 판의 실행 수명. Unity의 Awake에서 만들고 Start/Update/OnDestroy에서 호출한다.
    internal sealed class GameHost : IDisposable
    {
        private readonly UIManager _ui;
        private readonly BattleSystem _battle;
        private readonly BattleOrchestrator _orchestrator;
        private readonly AimInput _aim;
        private readonly ScreenFlow _screens;
        private readonly EnemyLooks _enemyLooks;
        private readonly EnemyView _enemyView;
        private readonly SkillView _skillView;
        private readonly DeathEffectView _deathEffectView;
        private readonly ControlConsole _console;
        private readonly BattleLifecycleConsole _lifecycleConsole;
        private readonly EnemyCommandConsole _commandConsole;
        private readonly UpgradeConsole _upgradeConsole;
        private readonly SkillConsole _skillConsole;

        public GameHost(UIManager ui, BattleSystem battle, BattleOrchestrator orchestrator,
            AimInput aim, ScreenFlow screens, EnemyLooks enemyLooks, EnemyView enemyView,
            SkillView skillView, DeathEffectView deathEffectView, ControlConsole console,
            BattleLifecycleConsole lifecycleConsole, EnemyCommandConsole commandConsole,
            UpgradeConsole upgradeConsole, SkillConsole skillConsole)
        {
            _ui = ui;
            _battle = battle;
            _orchestrator = orchestrator;
            _aim = aim;
            _screens = screens;
            _enemyLooks = enemyLooks;
            _enemyView = enemyView;
            _skillView = skillView;
            _deathEffectView = deathEffectView;
            _console = console;
            _lifecycleConsole = lifecycleConsole;
            _commandConsole = commandConsole;
            _upgradeConsole = upgradeConsole;
            _skillConsole = skillConsole;
        }

        public void Start() => _screens.GoToUpgrade();

        public void Tick(float deltaTime)
        {
            _aim.Tick();
            _skillConsole?.Tick();
            _battle.Tick(deltaTime);
            RefreshBattleHud();
            _console?.Tick();
            _lifecycleConsole?.Tick();
            _commandConsole?.Tick();
            _upgradeConsole?.Tick();
        }

        private void RefreshBattleHud()
        {
            if (!(_ui.CurrentRoot is BattleScreen screen))
                return;

            GameSession session = _battle.Session;
            if (session == null)
                screen.ShowIdle();
            else
                screen.Show(session.Remaining, session.World.EarnedGold, session.Phase == SessionPhase.Paused);
        }

        public void Dispose()
        {
            _skillConsole?.Dispose();
            _upgradeConsole?.Dispose();
            _commandConsole?.Dispose();
            _lifecycleConsole?.Dispose();
            _console?.Dispose();
            _screens.Dispose();
            _deathEffectView.Dispose();
            _skillView.Dispose();
            _enemyView.Dispose();
            _enemyLooks.Dispose();
        }
    }
}
