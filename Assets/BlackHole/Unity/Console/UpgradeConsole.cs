using System;
using System.Collections.Generic;
using System.Text;
using BlackHole.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static BlackHole.Unity.ConsoleParts;
using Object = UnityEngine.Object;

namespace BlackHole.Unity
{
    // 업그레이드 콘솔(개발용, 오른쪽 아래). 업그레이드 화면(F02)이 생기기 전까지 전투 사이에 노드를 사는 자리다.
    // 노드마다 한 줄: 상태(산 것 / 살 수 있음 / 선행 노드 없음 / Gold 부족 / 전투 중), ID, 가격, Grant.
    // 살 수 있는 줄을 누르면 산다(UpgradePurchase). 산 노드는 다음 전투의 판 구성이 된다(Loadout).
    // 개발용 버튼으로 Gold를 더할 수 있다. 구매자는 지금 실제 구성인 로컬 Player 1명(진행 상태의 첫 번째)이다.
    //
    // ` 키로 다른 콘솔 창과 함께 숨고 보인다. GameHost가 에디터와 개발 빌드에서만 만든다.
    internal sealed class UpgradeConsole : IDisposable
    {
        private const float RowWidth = 620;

        private readonly GameContent _content;
        private readonly PlayerState _buyer;
        private readonly GameObject _canvas;
        private readonly TMP_Text _goldText;
        private readonly List<NodeRow> _rows = new List<NodeRow>();
        private readonly StringBuilder _builder = new StringBuilder();

        private long _shownGold = -1;
        private int _shownOwned = -1;
        private bool _shownInBattle;

        public UpgradeConsole(Transform parent, GameContent content, BattleOrchestrator orchestrator)
        {
            _content = content;
            _buyer = orchestrator.Progress[0];

            RectTransform canvas = CreateCanvas(parent, "Upgrade Console");
            _canvas = canvas.gameObject;

            RectTransform panel = Panel(Stack(canvas, new Vector2(1, 0)), "Panel", PanelColor);
            Text(panel, "Title", "UPGRADES  ( ` )", 22);
            _goldText = Text(panel, "Gold", string.Empty, 22);

            RectTransform devButtons = Child(panel, "DevGold");
            HorizontalLayout(devButtons, 8);
            DevGoldButton(devButtons, 1000);
            DevGoldButton(devButtons, 100000);

            Text(panel, "Note", "Owned nodes apply from the next battle. Cannot buy during a battle.", 18);

            foreach (UpgradeNodeDefinition node in content.Upgrades)
                _rows.Add(CreateRow(panel, node));

            Refresh();
        }

        public void Tick()
        {
            if (TogglePressed())
                _canvas.SetActive(!_canvas.activeSelf);

            Refresh();
        }

        public void Dispose() => Object.Destroy(_canvas);

        // Gold·산 노드·전투 중 여부가 바뀐 프레임에만 다시 쓴다.
        private void Refresh()
        {
            if (_buyer.Gold == _shownGold && _buyer.Upgrades.Count == _shownOwned && _buyer.InBattle == _shownInBattle)
                return;

            _shownGold = _buyer.Gold;
            _shownOwned = _buyer.Upgrades.Count;
            _shownInBattle = _buyer.InBattle;
            _goldText.text = $"{_buyer.Id}  Gold {_buyer.Gold}  ({_buyer.Upgrades.Count}/{_content.Upgrades.Count} nodes)";

            foreach (NodeRow row in _rows)
            {
                PurchaseResult result = UpgradePurchase.Check(_buyer, row.Node);
                SetInteractable(row.Button, result == PurchaseResult.Purchased);
                row.Label.text = Describe(row.Node, result);
            }
        }

        private string Describe(UpgradeNodeDefinition node, PurchaseResult result)
        {
            _builder.Clear();

            switch (result)
            {
                case PurchaseResult.AlreadyOwned: _builder.Append("<color=#7CFC7C>[owned]</color>"); break;
                case PurchaseResult.Purchased: _builder.Append("<color=#FFD24D>[buy]</color>"); break;
                case PurchaseResult.MissingPrerequisite: _builder.Append("<color=#808080>[locked]</color>"); break;
                case PurchaseResult.NotEnoughGold: _builder.Append("<color=#FF6B6B>[gold]</color>"); break;
                case PurchaseResult.InBattle: _builder.Append("<color=#808080>[battle]</color>"); break;
                default: _builder.Append('[').Append(result).Append(']'); break;
            }

            _builder.Append("<pos=5em>").Append(node.Id);
            _builder.Append("<pos=14em>").Append(node.Price);

            foreach (EnemyGrant grant in node.Grants)
            {
                _builder.Append("<pos=18em>").Append(grant.Enemy.Id).Append(' ').Append(grant.Stat).Append(' ');

                switch (grant.Operation)
                {
                    case GrantOperation.Add: _builder.Append('+'); break;
                    case GrantOperation.Multiply: _builder.Append('x'); break;
                    default: _builder.Append('='); break;
                }

                _builder.Append(Number(grant.Value));
            }

            return _builder.ToString();
        }

        private NodeRow CreateRow(RectTransform parent, UpgradeNodeDefinition node)
        {
            Button button = ButtonOf(parent, node.Id, string.Empty, RowWidth,
                () => UpgradePurchase.TryPurchase(_buyer, _content, node.Id));
            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            label.fontSize = 18;
            label.alignment = TextAlignmentOptions.Left;
            label.margin = new Vector4(10, 0, 10, 0);
            return new NodeRow(node, button, label);
        }

        private void DevGoldButton(RectTransform parent, long amount) =>
            ButtonOf(parent, $"Gold{amount}", $"+{amount} Gold (dev)", 240, () => _buyer.EarnGold(amount));

        private sealed class NodeRow
        {
            public readonly UpgradeNodeDefinition Node;
            public readonly Button Button;
            public readonly TMP_Text Label;

            public NodeRow(UpgradeNodeDefinition node, Button button, TMP_Text label)
            {
                Node = node;
                Button = button;
                Label = label;
            }
        }
    }
}
