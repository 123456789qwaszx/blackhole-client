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
    // 노드 콘솔(개발용, 오른쪽 아래). 트리 화면(F02)이 생기기 전까지 노드를 사 보는 자리다.
    // 숨은 노드는 줄이 없다. 드러난 노드와 산 노드가 한 줄씩 보인다: 상태, ID, 가격, 사면 받는 업그레이드.
    // 살 수 있는 줄을 누르면 산다(NodePurchase.TryPurchase). 사면 그 노드와 선으로 이어진 노드가 드러난다.
    // 개발용 버튼으로 Gold를 더할 수 있다. 구매자는 지금 실제 구성인 로컬 Player 1명이다.
    //
    // ` 키로 다른 콘솔 창과 함께 숨고 보인다. GameHost가 에디터와 개발 빌드에서만 만든다.
    internal sealed class NodeConsole : IDisposable
    {
        private const float RowWidth = 620;

        private readonly NodeTree _tree;
        private readonly PlayerState _buyer;
        private readonly GameObject _canvas;
        private readonly TMP_Text _goldText;
        private readonly List<NodeRow> _rows = new List<NodeRow>();
        private readonly StringBuilder _builder = new StringBuilder();

        private long _shownGold = -1;
        private int _shownOwned = -1;
        private bool _shownInBattle;

        public NodeConsole(Transform parent, NodeTree tree, PlayerState buyer)
        {
            _tree = tree;
            _buyer = buyer;

            RectTransform canvas = CreateCanvas(parent, "Node Console");
            _canvas = canvas.gameObject;

            RectTransform panel = Panel(Stack(canvas, new Vector2(1, 0)), "Panel", PanelColor);
            Text(panel, "Title", "NODES  ( ` )", 22);
            _goldText = Text(panel, "Gold", string.Empty, 22);

            RectTransform devButtons = Child(panel, "DevGold");
            HorizontalLayout(devButtons, 8);
            DevGoldButton(devButtons, 10);
            DevGoldButton(devButtons, 100);

            Text(panel, "Note", "Buying a node reveals the nodes linked to it.", 18);

            foreach (NodeDefinition node in tree.Nodes)
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
            if (_buyer.Gold == _shownGold && _buyer.OwnedNodes.Count == _shownOwned && _buyer.InBattle == _shownInBattle)
                return;

            _shownGold = _buyer.Gold;
            _shownOwned = _buyer.OwnedNodes.Count;
            _shownInBattle = _buyer.InBattle;
            _goldText.text = $"{_buyer.Id}  Gold {_buyer.Gold}  ({_buyer.OwnedNodes.Count}/{_tree.Nodes.Count} nodes)";

            foreach (NodeRow row in _rows)
            {
                PurchaseResult result = NodePurchase.Check(_buyer, _tree, row.Node.Id);
                bool shown = result != PurchaseResult.Hidden;
                row.Button.gameObject.SetActive(shown);

                if (!shown)
                    continue;

                SetInteractable(row.Button, result == PurchaseResult.Purchased);
                row.Label.text = Describe(row.Node, result);
            }
        }

        private string Describe(NodeDefinition node, PurchaseResult result)
        {
            _builder.Clear();

            switch (result)
            {
                case PurchaseResult.AlreadyOwned: _builder.Append("<color=#7CFC7C>[owned]</color>"); break;
                case PurchaseResult.Purchased: _builder.Append("<color=#FFD24D>[buy]</color>"); break;
                case PurchaseResult.NotEnoughGold: _builder.Append("<color=#FF6B6B>[gold]</color>"); break;
                case PurchaseResult.InBattle: _builder.Append("<color=#808080>[battle]</color>"); break;
                default: _builder.Append('[').Append(result).Append(']'); break;
            }

            _builder.Append("<pos=5em>").Append(node.Id);
            _builder.Append("<pos=13em>").Append(node.Price);

            foreach (Upgrade upgrade in node.Upgrades)
            {
                _builder.Append("<pos=17em>").Append(upgrade.Stat).Append(' ');

                switch (upgrade.Operation)
                {
                    case UpgradeOperation.Add: _builder.Append('+').Append(Number(upgrade.Value)); break;
                    case UpgradeOperation.Percent: _builder.Append('+').Append(Number(upgrade.Value * 100)).Append('%'); break;
                    default: _builder.Append('x').Append(Number(upgrade.Value)); break;
                }
            }

            return _builder.ToString();
        }

        private NodeRow CreateRow(RectTransform parent, NodeDefinition node)
        {
            Button button = ButtonOf(parent, node.Id, string.Empty, RowWidth, () => NodePurchase.TryPurchase(_buyer, _tree, node.Id));
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
            public readonly NodeDefinition Node;
            public readonly Button Button;
            public readonly TMP_Text Label;

            public NodeRow(NodeDefinition node, Button button, TMP_Text label)
            {
                Node = node;
                Button = button;
                Label = label;
            }
        }
    }
}
