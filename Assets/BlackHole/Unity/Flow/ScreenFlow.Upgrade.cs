using System;
using System.Collections.Generic;
using BlackHole.Core;

namespace BlackHole.Unity
{
    // 업그레이드 화면 ↔ 업그레이드 시스템(진행 상태, 구매 규칙, 노드 정의).
    // 화면 → 업그레이드: 구매(노드 ID), [개발용] Gold 추가.  업그레이드 → 화면: Gold, 노드마다 살 수 있는지.
    // 화면은 끝난 전투의 결과도 받는다. 업그레이드 화면에는 진행 중인 전투가 없다.
    internal sealed partial class ScreenFlow
    {
        // [개발용] 버튼 한 번에 더하는 Gold. 적 처치 보상이 지워진 동안 구매를 확인하는 값이며 게임 규칙이 아니다.
        private const int DevGoldAmount = 100;

        private readonly List<UpgradeNodeItem> _nodeItems = new List<UpgradeNodeItem>();

        private void GoToUpgrade()
        {
            _ui.SwitchRoot<UpgradeScreen>(
                _upgradePresentation,
                afterPresented: screen => BindView(screen, BindUpgrade),
                afterClosed: Unbind);
        }

        private void BindUpgrade(UpgradeScreen screen)
        {
            Action<string> purchase = nodeId =>
            {
                UpgradePurchase.TryPurchase(Viewer, _content, nodeId);
                ShowUpgrade(screen);
            };

            Action devGold = () =>
            {
                Viewer.EarnGold(DevGoldAmount);
                ShowUpgrade(screen);
            };

            AddBinding(screen,
                s => s.PurchaseClicked += purchase,
                s => s.PurchaseClicked -= purchase);

            AddBinding(screen,
                s => s.DevGoldClicked += devGold,
                s => s.DevGoldClicked -= devGold);

            AddBinding(screen,
                s => s.NextBattleClicked += GoToBattle,
                s => s.NextBattleClicked -= GoToBattle);

            AddBinding(screen,
                s => s.TitleClicked += GoToTitle,
                s => s.TitleClicked -= GoToTitle);

            SessionResult result = _battle.Result;
            screen.ShowResult(result.Reason == SessionEndReason.TimeExpired, result.PlayedSeconds);
            ShowUpgrade(screen);
        }

        private void ShowUpgrade(UpgradeScreen screen)
        {
            PlayerState viewer = Viewer;
            _nodeItems.Clear();

            foreach (UpgradeNodeDefinition node in _content.Upgrades)
            {
                PurchaseResult check = UpgradePurchase.Check(viewer, node);
                _nodeItems.Add(new UpgradeNodeItem(node.Id, node.Price, StateOf(check)));
            }

            screen.ShowGold(viewer.Gold);
            screen.ShowNodes(_nodeItems);
        }

        private PlayerState Viewer
        {
            get
            {
                foreach (PlayerState state in _progress)
                {
                    if (state.Id.Equals(_viewer))
                        return state;
                }

                throw new InvalidOperationException($"{_viewer}의 진행 상태가 없다.");
            }
        }

        // 구매 규칙의 판정을 화면의 노드 상태로 옮긴다.
        // 업그레이드 화면에는 진행 중인 전투가 없어 InBattle은 오지 않는다. 화면이 모르는 판정은 잠김으로 보인다.
        private static UpgradeNodeState StateOf(PurchaseResult check)
        {
            switch (check)
            {
                case PurchaseResult.Purchased: return UpgradeNodeState.Available;
                case PurchaseResult.NotEnoughGold: return UpgradeNodeState.NotEnoughGold;
                case PurchaseResult.AlreadyOwned: return UpgradeNodeState.Owned;
                default: return UpgradeNodeState.Locked;
            }
        }
    }
}
