using System;
using BlackHole.Core;

namespace BlackHole.Unity
{
    // 조립된 한 판의 실행 수명. Unity의 Awake에서 만들고 Start/Update/OnDestroy에서 호출한다.
    // 개발용 콘솔은 GameHost.Editor에 있다.
    internal sealed partial class GameHost : IDisposable
    {
        private readonly UIManager _ui;
        private readonly BattleSystem _battle;
        private readonly AimInput _aim;
        private readonly ScreenFlow _screens;
        private readonly EnemyLooks _enemyLooks;
        private readonly EnemyView _enemyView;
        private readonly SkillView _skillView;
        private readonly DeathEffectView _deathEffectView;
        private readonly HqView _hqView;

        public GameHost(UIManager ui, BattleSystem battle, AimInput aim, ScreenFlow screens,
            EnemyLooks enemyLooks, EnemyView enemyView,
            SkillView skillView, DeathEffectView deathEffectView, HqView hqView)
        {
            _ui = ui;
            _battle = battle;
            _aim = aim;
            _screens = screens;
            _enemyLooks = enemyLooks;
            _enemyView = enemyView;
            _skillView = skillView;
            _deathEffectView = deathEffectView;
            _hqView = hqView;
        }

        public void Start() => _screens.GoToUpgrade();

        public void Tick(float deltaTime)
        {
            _aim.Tick();
            TickConsolesBeforeBattle();
            if (_battle.Tick(deltaTime))
                _screens.HandleBattleTimeExpired();
            RefreshBattleHud();
            TickConsolesAfterBattle();
        }

        private void RefreshBattleHud()
        {
            if (!(_ui.CurrentRoot is BattleScreen screen))
                return;

            GameSession session = _battle.Session;
            if (session == null)
                screen.ShowIdle();
            else
                screen.Show(session.Remaining, session.World.EarnedGold, session.Phase == SessionPhase.Paused,
                    session.World.Hq.Level, session.World.Hq.Progress, session.World.Hq.GoalLevel);
        }

        public void Dispose()
        {
            DisposeConsoles();
            _screens.Dispose();
            _deathEffectView.Dispose();
            _hqView.Dispose();
            _skillView.Dispose();
            _enemyView.Dispose();
            _enemyLooks.Dispose();
        }
    }
}
