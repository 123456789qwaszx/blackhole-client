using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlackHole.Unity
{
    public enum UpgradeNodeState
    {
        // 선행 노드를 아직 사지 않았다.
        Locked,
        Available,
        NotEnoughGold,
        Owned,
    }

    // 업그레이드 화면이 그리는 노드 하나. 화면은 Core의 노드 정의가 아니라 이 값만 받는다.
    public readonly struct UpgradeNodeItem
    {
        public string Id { get; }
        public int Price { get; }
        public UpgradeNodeState State { get; }

        public UpgradeNodeItem(string id, int price, UpgradeNodeState state)
        {
            Id = id;
            Price = price;
            State = state;
        }
    }

    // 업그레이드 화면. 끝난 전투의 결과, Gold, 노드 목록을 받아 보여 주고, 버튼을 알린다.
    // 노드 목록은 NodeItem 자식을 틀로 복제해 만든다. 구매는 노드 ID로만 알린다.
    public sealed class UpgradeScreen : UIRoot<UpgradeScreen.Refs>
    {
        public enum Refs
        {
            ResultText,
            GoldText,
            NodeList,
            NodeItem,
            DevGoldBtn_Button,
            NextBattleBtn_Button,
            TitleBtn_Button,
        }

        public event Action<string> PurchaseClicked;
        // [개발용] 적 처치 보상이 없는 동안 구매를 확인하기 위한 버튼.
        public event Action DevGoldClicked;
        public event Action NextBattleClicked;
        public event Action TitleClicked;

        private readonly List<GameObject> _items = new List<GameObject>();
        private TMP_Text _result;
        private TMP_Text _gold;
        private RectTransform _list;
        private GameObject _itemTemplate;

        protected override void OnInitialize()
        {
            ScreenRefs.WarnMissing<Refs>(this);

            _result = View.Text(Refs.ResultText);
            _gold = View.Text(Refs.GoldText);
            _list = View.Rect(Refs.NodeList);

            RectTransform template = View.Rect(Refs.NodeItem);
            if (template != null)
            {
                _itemTemplate = template.gameObject;
                _itemTemplate.SetActive(false);
            }

            BindEvent(View.Button(Refs.DevGoldBtn_Button), _ => DevGoldClicked?.Invoke());
            BindEvent(View.Button(Refs.NextBattleBtn_Button), _ => NextBattleClicked?.Invoke());
            BindEvent(View.Button(Refs.TitleBtn_Button), _ => TitleClicked?.Invoke());
        }

        public void ShowResult(bool timeExpired, float playedSeconds)
        {
            if (_result != null)
                _result.text = $"{(timeExpired ? "Time up" : "Battle ended")}  {playedSeconds:F1}s";
        }

        public void ShowGold(int gold)
        {
            if (_gold != null)
                _gold.text = $"Gold {gold}";
        }

        public void ShowNodes(IReadOnlyList<UpgradeNodeItem> nodes)
        {
            if (_itemTemplate == null || _list == null)
                return;

            foreach (GameObject item in _items)
                Destroy(item);

            _items.Clear();

            foreach (UpgradeNodeItem node in nodes)
            {
                GameObject item = Instantiate(_itemTemplate, _list);
                item.SetActive(true);

                TMP_Text label = item.GetComponentInChildren<TMP_Text>(includeInactive: true);
                if (label != null)
                    label.text = $"{node.Id}   {Describe(node)}";

                Button button = item.GetComponent<Button>();
                if (button != null)
                    button.interactable = node.State == UpgradeNodeState.Available;

                string id = node.Id;
                BindEvent(button, _ => PurchaseClicked?.Invoke(id));
                _items.Add(item);
            }
        }

        private static string Describe(UpgradeNodeItem node)
        {
            switch (node.State)
            {
                case UpgradeNodeState.Available: return $"Buy {node.Price}G";
                case UpgradeNodeState.NotEnoughGold: return $"{node.Price}G";
                case UpgradeNodeState.Owned: return "Owned";
                default: return "Locked";
            }
        }
    }
}
