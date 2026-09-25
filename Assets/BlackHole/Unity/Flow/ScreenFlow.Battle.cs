using System;
using BlackHole.Core;

namespace BlackHole.Unity
{
    // 전투 화면 ↔ 전투 Session.
    // 화면 → Session: 일시정지, 전투 끝내기.  Session → 화면: 남은 시간, 일시정지 여부, 판 안의 적(EnemyView).
    // 끝난 전투(시간 종료 또는 끝내기)는 다음 Tick에 업그레이드 화면으로 간다. 전환하는 자리는 TickBattle 하나다.
    internal sealed partial class ScreenFlow
    {
        // 열려 있는 전투 화면. 닫히면 null이다.
        private BattleScreen _battleScreen;

        // 진행 상태로 새 전투를 조립하고 전투 화면을 연다. 조립이 진행 상태를 전투에 묶는다(구매 불가).
        // 전투마다 seed를 새로 정한다. 같은 전투를 재현할 방법(seed 기록·지정)은 아직 없다.
        private void GoToBattle()
        {
            _battle = SessionAssembler.CreateBattle(_content, _progress, Environment.TickCount);

            _ui.SwitchRoot<BattleScreen>(
                _battlePresentation,
                afterPresented: screen => BindView(screen, BindBattle),
                afterClosed: Unbind);
        }

        private void BindBattle(BattleScreen screen)
        {
            _battleScreen = screen;

            // 전투 화면을 떠나면 남은 적의 모습도 치운다. 판 정리는 처치가 아니므로 연출이 없다.
            AddCleanup(screen, () =>
            {
                _battleScreen = null;
                _enemyView.Reset();
            });

            AddBinding(screen,
                s => s.PauseClicked += TogglePause,
                s => s.PauseClicked -= TogglePause);

            AddBinding(screen,
                s => s.EndClicked += StopBattle,
                s => s.EndClicked -= StopBattle);

            _enemyView.Reset();
            ShowBattle();
        }

        private void TogglePause()
        {
            _battle.TogglePause();
            ShowBattle();
        }

        // 결과를 확정하고 진행 상태를 풀어 준다. 화면 전환은 다음 Tick이 한다.
        private void StopBattle() => _battle.Stop();

        private void TickBattle(float delta)
        {
            if (_battleScreen == null)
                return;

            _battle.Advance(delta);

            if (_battle.Phase == SessionPhase.Ended)
            {
                GoToUpgrade();
                return;
            }

            ShowBattle();
        }

        private void ShowBattle()
        {
            _battleScreen.Show(_battle.Remaining, _battle.Phase == SessionPhase.Paused);
            _enemyView.Synchronize(_battle.World);
        }
    }
}
