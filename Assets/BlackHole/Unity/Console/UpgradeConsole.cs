using System;
using System.Collections.Generic;
using System.Globalization;
using BlackHole.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static BlackHole.Unity.ConsoleParts;
using Object = UnityEngine.Object;

namespace BlackHole.Unity
{
    // 업그레이드 콘솔(개발용, 오른쪽 아래). 구매 규칙을 거치지 않고 진행 상태를 바꾼다(ProgressCheats).
    // - Gold 더하기·빼기: 100, 1K(1,000), 10K(1만), 1M(100만), 100M(1억), 10B(100억), 1T(1조). 빼기는 0에서 멈춘다.
    // - Unlock all: 모든 노드를 산 것으로 한다(Gold를 쓰지 않는다). Lock all: 산 노드를 모두 지운다(Gold는 돌려주지 않는다).
    // 화면은 진행 상태가 바뀐 것을 보고 스스로 다시 칠한다(ScreenFlow).
    // 진행 상태는 전투 밖에서만 바뀌므로, 전투 중에는 모든 버튼이 꺼진다(치트도 전투 중에는 거부한다).
    //
    // ` 키로 숨고 보인다. GameHost가 에디터와 개발 빌드에서만 만든다.
    internal sealed class UpgradeConsole : IDisposable
    {
        private const float GoldButtonWidth = 104;
        private const float NodeButtonWidth = 360;

        private static readonly (string Label, long Amount)[] GoldSteps =
        {
            ("100", 100L),
            ("1K", 1_000L),
            ("10K", 10_000L),
            ("1M", 1_000_000L),
            ("100M", 100_000_000L),
            ("10B", 10_000_000_000L),
            ("1T", 1_000_000_000_000L),
        };

        private readonly PlayerState _player;
        private readonly NodeTree _tree;
        private readonly GameObject _canvas;
        private readonly TMP_Text _goldText;
        private readonly List<Button> _buttons = new List<Button>();

        private long _shownGold = -1;
        private bool? _shownInBattle;

        public UpgradeConsole(Transform parent, PlayerState player, NodeTree tree)
        {
            _player = player;
            _tree = tree;

            RectTransform canvas = CreateCanvas(parent, "Upgrade Console");
            _canvas = canvas.gameObject;

            RectTransform panel = Panel(Stack(canvas, new Vector2(1, 0)), "Panel", PanelColor);
            Text(panel, "Title", "UPGRADE CONSOLE  ( ` )", 22);
            _goldText = Text(panel, "Gold", string.Empty, 22);

            RectTransform earn = Child(panel, "Earn");
            HorizontalLayout(earn, 6);
            RectTransform take = Child(panel, "Take");
            HorizontalLayout(take, 6);

            foreach ((string label, long amount) in GoldSteps)
            {
                _buttons.Add(ButtonOf(earn, "Earn" + label, "+" + label, GoldButtonWidth, () => _player.EarnGold(amount)));
                _buttons.Add(ButtonOf(take, "Take" + label, "-" + label, GoldButtonWidth, () => ProgressCheats.TakeGold(_player, amount)));
            }

            RectTransform nodes = Child(panel, "Nodes");
            HorizontalLayout(nodes, 8);
            _buttons.Add(ButtonOf(nodes, "UnlockAll", "Unlock all", NodeButtonWidth, () => ProgressCheats.UnlockAllNodes(_player, _tree)));
            _buttons.Add(ButtonOf(nodes, "LockAll", "Lock all", NodeButtonWidth, () => ProgressCheats.LockAllNodes(_player)));

            Refresh();
        }

        public void Tick()
        {
            if (TogglePressed())
                _canvas.SetActive(!_canvas.activeSelf);

            Refresh();
        }

        public void Dispose() => Object.Destroy(_canvas);

        private void Refresh()
        {
            if (_player.InBattle != _shownInBattle)
            {
                _shownInBattle = _player.InBattle;

                foreach (Button button in _buttons)
                    SetInteractable(button, !_player.InBattle);
            }

            if (_player.Gold == _shownGold)
                return;

            _shownGold = _player.Gold;
            _goldText.text = "Gold " + _player.Gold.ToString("N0", CultureInfo.InvariantCulture);
        }
    }
}
