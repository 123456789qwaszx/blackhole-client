using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace BlackHole.Unity
{
    // 화면 프리팹을 연결하지 않았을 때 쓰는 임시 화면(개발용 대체). Canvas와 화면(업그레이드, 전투, 결산)을 코드로 만든다.
    // 자식 이름은 화면 클래스의 Refs와 같다 — 실제 프리팹도 같은 이름을 쓰면 화면 클래스가 그대로 붙는다.
    // 씬의 화면 프리팹(Assets/BlackHole/Prefabs/Screens)은 처음에 이 배치로 만들었다. 지금 정상 실행은 프리팹을 쓴다.
    // 글자는 TMP 기본 글꼴(한글 없음)이라 영문이다.
    internal static class PlaceholderScreens
    {
        private static readonly Color ButtonColor = new Color(0.2f, 0.26f, 0.42f);
        private static readonly Vector2 MenuButton = new Vector2(420, 72);
        private static readonly Vector2 BarButton = new Vector2(220, 64);
        private static readonly Color Backdrop = new Color(0.08f, 0.08f, 0.11f);
        private static readonly Color GoldColor = new Color(1f, 0.84f, 0.35f);
        private const float HeaderHeight = 110;
        private const float FooterHeight = 110;

        public readonly struct Result
        {
            public RectTransform RootLayer { get; }
            public RectTransform PanelLayer { get; }
            public UIBase[] Views { get; }

            public Result(RectTransform rootLayer, RectTransform panelLayer, UIBase[] views)
            {
                RootLayer = rootLayer;
                PanelLayer = panelLayer;
                Views = views;
            }
        }

        public static Result Build(Transform parent)
        {
            EnsureEventSystem(parent);

            RectTransform canvas = CreateCanvas(parent);
            RectTransform rootLayer = Stretch(Child(canvas, "RootLayer"));
            RectTransform panelLayer = Stretch(Child(canvas, "PanelLayer"));

            var views = new UIBase[] { BuildUpgrade(rootLayer), BuildBattle(rootLayer), BuildSettlement(rootLayer) };
            return new Result(rootLayer, panelLayer, views);
        }

        #region 화면

        // 자식을 모두 만든 뒤 화면 클래스를 붙인다. 화면 클래스는 붙는 순간 자식을 이름으로 찾는다.
        // 업그레이드 화면: 어두운 배경, 위쪽 글자(Gold, 제목, 산 노드 수), 가운데 트리 영역(트리 보기), 아래쪽 조작 안내와 전투 시작 버튼.
        private static UIBase BuildUpgrade(RectTransform layer)
        {
            RectTransform screen = Stretch(Child(layer, nameof(UpgradeScreen)));
            screen.gameObject.AddComponent<Image>().color = Backdrop;

            RectTransform viewport = Stretch(Child(screen, nameof(UpgradeScreen.Refs.TreeViewport)));
            viewport.offsetMin = new Vector2(0, FooterHeight);
            viewport.offsetMax = new Vector2(0, -HeaderHeight);
            viewport.gameObject.AddComponent<Image>().color = Color.clear;
            viewport.gameObject.AddComponent<RectMask2D>();
            viewport.gameObject.AddComponent<NodeTreeView>();

            Label(screen, nameof(UpgradeScreen.Refs.GoldText), string.Empty, 36, new Vector2(0.2f, 0.955f)).color = GoldColor;
            Label(screen, "Heading", "UPGRADES", 40, new Vector2(0.5f, 0.955f));
            Label(screen, nameof(UpgradeScreen.Refs.ProgressText), string.Empty, 36, new Vector2(0.8f, 0.955f));
            Label(screen, "Hint", "Click a lit node to buy it  ·  drag to pan  ·  scroll to zoom", 24, new Vector2(0.5f, 0.12f))
                .color = new Color(1, 1, 1, 0.5f);

            RectTransform footer = Row(screen, "Footer", new Vector2(0.5f, 0.05f), 24);
            MenuButtonOf(footer, nameof(UpgradeScreen.Refs.StartBattleBtn_Button), "Start battle", MenuButton);

            return screen.gameObject.AddComponent<UpgradeScreen>();
        }

        // 전투 화면은 배경이 없다. 뒤의 전투 장면(적)이 보여야 하므로 글자와 버튼을 위쪽 띠에만 둔다:
        // 왼쪽에 이 판이 번 Gold, 가운데에 남은 시간, 오른쪽에 일시정지·전투 끝내기.
        private static UIBase BuildBattle(RectTransform layer)
        {
            RectTransform screen = Stretch(Child(layer, nameof(BattleScreen)));
            Label(screen, nameof(BattleScreen.Refs.EarnedGoldText), string.Empty, 40, new Vector2(0.15f, 0.945f)).color = GoldColor;
            Label(screen, nameof(BattleScreen.Refs.RemainingText), string.Empty, 64, new Vector2(0.5f, 0.945f));

            RectTransform menu = Row(screen, "Menu", new Vector2(0.86f, 0.945f), 16);
            MenuButtonOf(menu, nameof(BattleScreen.Refs.PauseBtn_Button), "Pause", BarButton);
            MenuButtonOf(menu, nameof(BattleScreen.Refs.EndBtn_Button), "End battle", BarButton);

            return screen.gameObject.AddComponent<BattleScreen>();
        }

        // 결산 화면: 어두운 배경, 가운데 결과 글자(끝난 사유, 시간, 번 Gold, 결산 뒤 Gold, 처치 수), 아래쪽 계속하기 버튼.
        private static UIBase BuildSettlement(RectTransform layer)
        {
            RectTransform screen = Stretch(Child(layer, nameof(SettlementScreen)));
            screen.gameObject.AddComponent<Image>().color = Backdrop;

            Label(screen, "Heading", "BATTLE RESULT", 48, new Vector2(0.5f, 0.86f));
            Label(screen, nameof(SettlementScreen.Refs.ResultText), string.Empty, 36, new Vector2(0.5f, 0.77f));
            Label(screen, nameof(SettlementScreen.Refs.TimeText), string.Empty, 32, new Vector2(0.5f, 0.7f));
            Label(screen, nameof(SettlementScreen.Refs.EarnedGoldText), string.Empty, 44, new Vector2(0.5f, 0.61f)).color = GoldColor;
            Label(screen, nameof(SettlementScreen.Refs.TotalGoldText), string.Empty, 32, new Vector2(0.5f, 0.535f));

            TMP_Text kills = Label(screen, nameof(SettlementScreen.Refs.KillsText), string.Empty, 28, new Vector2(0.5f, 0.34f));
            kills.rectTransform.sizeDelta = new Vector2(1600, 320);
            kills.alignment = TextAlignmentOptions.Top;

            RectTransform footer = Row(screen, "Footer", new Vector2(0.5f, 0.08f), 24);
            MenuButtonOf(footer, nameof(SettlementScreen.Refs.ContinueBtn_Button), "Continue", MenuButton);

            return screen.gameObject.AddComponent<SettlementScreen>();
        }

        #endregion

        #region 부품

        private static void EnsureEventSystem(Transform parent)
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null)
                return;

            var eventSystem = new GameObject("EventSystem");
            eventSystem.transform.SetParent(parent, false);
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        private static RectTransform CreateCanvas(Transform parent)
        {
            RectTransform rect = Child(parent, "UI Canvas");

            var canvas = rect.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = rect.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            rect.gameObject.AddComponent<GraphicRaycaster>();
            return rect;
        }

        private static TMP_Text Label(RectTransform parent, string name, string text, float size, Vector2 anchor)
        {
            RectTransform rect = Child(parent, name);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.sizeDelta = new Vector2(1600, size * 1.6f);
            return Text(rect, text, size);
        }

        private static RectTransform Row(RectTransform parent, string name, Vector2 anchor, float spacing)
        {
            RectTransform rect = Group(parent, name, anchor);
            var layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            Configure(layout, spacing);
            return rect;
        }

        private static RectTransform Group(RectTransform parent, string name, Vector2 anchor)
        {
            RectTransform rect = Child(parent, name);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;

            var fitter = rect.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return rect;
        }

        private static void Configure(HorizontalOrVerticalLayoutGroup layout, float spacing)
        {
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        // 버튼 글자의 이름은 프레임워크 예제의 관례를 따른다: PauseBtn_Button → PauseBtn_Text.
        private static void MenuButtonOf(RectTransform parent, string name, string text, Vector2 size)
        {
            RectTransform rect = Child(parent, name);

            var image = rect.gameObject.AddComponent<Image>();
            image.color = ButtonColor;
            rect.gameObject.AddComponent<Button>().targetGraphic = image;

            var element = rect.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = size.x;
            element.preferredHeight = size.y;

            Text(Stretch(Child(rect, name.Replace("_Button", "_Text"))), text, size.y * 0.45f);
        }

        private static TMP_Text Text(RectTransform rect, string text, float size)
        {
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;
            return label;
        }

        private static RectTransform Child(Transform parent, string name)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return (RectTransform)child.transform;
        }

        private static RectTransform Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        #endregion
    }
}
