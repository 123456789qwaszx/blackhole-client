using System;
using BlackHole.Core;
using UnityEngine;

namespace BlackHole.Unity
{
    internal sealed partial class ScreenFlow
    {
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
            AddBinding(root,
                r => r.PauseClicked += HandleBattlePauseClicked,
                r => r.PauseClicked -= HandleBattlePauseClicked);

            AddBinding(root,
                r => r.EndClicked += HandleBattleEndClicked,
                r => r.EndClicked -= HandleBattleEndClicked);
        }

        private void HandleBattlePauseClicked() => _battle.TogglePause();
        private void HandleBattleEndClicked() => RequestEnd();

        internal void HandleBattleTimeExpired() => RequestEnd();

        // 화면 버튼, 시간 종료, 개발용 콘솔이 같은 전환 경로를 사용한다.
        private async void RequestStart()
        {
            try
            {
                if (await _orchestrator.StartBattleAsync())
                    GoToBattle();
            }
            catch (Exception error) { Debug.LogException(error); }
        }

        private async void RequestEnd()
        {
            try
            {
                BattleRawData raw = await _orchestrator.EndBattleAsync();
                if (raw != null)
                    GoToSettlement(raw.PlayedSeconds, raw.ReachedLevel, raw.TotalKills,
                        raw.Kills, raw.EarnedGold, _player.Gold);
            }
            catch (Exception error) { Debug.LogException(error); }
        }
    }
}
