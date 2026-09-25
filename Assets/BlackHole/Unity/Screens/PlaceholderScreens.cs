using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace BlackHole.Unity
{
    // 화면 프리팹이 연결되기 전에 쓰는 임시 화면. Canvas와 화면 4개를 코드로 만든다.
    // 자식 이름은 각 화면 클래스의 Refs와 같다 — 실제 프리팹도 같은 이름을 쓰면 화면 클래스가 그대로 붙는다.
    // GameHost에 사용자 UI를 연결하면 쓰지 않는다. 글자는 TMP 기본 글꼴(한글 없음)이라 영문이다.
    internal static class PlaceholderScreens
    {
        private static readonly Color Background = new Color(0.03f, 0.04f, 0.07f);
        private static readonly Color ButtonColor = new Color(0.2f, 0.26f, 0.42f);
        private static readonly Vector2 MenuButton = new Vector2(420, 72);
        private static readonly Vector2 NodeButton = new Vector2(640, 56);

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

            var views = new UIBase[]
            {
                BuildTitle(rootLayer),
                BuildSettings(rootLayer),
                BuildBattle(rootLayer),
                BuildUpgrade(rootLayer),
            };

            return new Result(rootLayer, panelLayer, views);
        }

        #region 화면

        // 자식을 모두 만든 뒤 화면 클래스를 붙인다. 화면 클래스는 붙는 순간 자식을 이름으로 찾는다.
        private static UIBase BuildTitle(RectTransform layer)
        {
            RectTransform screen = Screen(layer, nameof(TitleScreen));
            Label(screen, "Heading", "BLACK HOLE", 72, new Vector2(0.5f, 0.75f));

            RectTransform menu = Column(screen, "Menu", new Vector2(0.5f, 0.4f), 16);
            MenuButtonOf(menu, nameof(TitleScreen.Refs.StartBtn_Button), "Start");
            MenuButtonOf(menu, nameof(TitleScreen.Refs.SettingsBtn_Button), "Settings");
            MenuButtonOf(menu, nameof(TitleScreen.Refs.QuitBtn_Button), "Quit");

            return screen.gameObject.AddComponent<TitleScreen>();
        }

        private static UIBase BuildSettings(RectTransform layer)
        {
            RectTransform screen = Screen(layer, nameof(SettingsScreen));
            Label(screen, "Heading", "SETTINGS", 64, new Vector2(0.5f, 0.75f));
            Label(screen, "Empty", "No settings yet", 32, new Vector2(0.5f, 0.55f));

            RectTransform menu = Column(screen, "Menu", new Vector2(0.5f, 0.3f), 16);
            MenuButtonOf(menu, nameof(SettingsScreen.Refs.BackBtn_Button), "Back");

            return screen.gameObject.AddComponent<SettingsScreen>();
        }

        // 전투 화면은 배경이 없다. 뒤의 전투 장면(적)이 보여야 한다.
        private static UIBase BuildBattle(RectTransform layer)
        {
            RectTransform screen = Screen(layer, nameof(BattleScreen), opaque: false);
            Label(screen, "Heading", "BATTLE", 40, new Vector2(0.5f, 0.95f));
            Label(screen, nameof(BattleScreen.Refs.RemainingText), string.Empty, 96, new Vector2(0.5f, 0.6f));

            RectTransform menu = Row(screen, "Menu", new Vector2(0.5f, 0.15f), 24);
            MenuButtonOf(menu, nameof(BattleScreen.Refs.PauseBtn_Button), "Pause");
            MenuButtonOf(menu, nameof(BattleScreen.Refs.EndBtn_Button), "End battle");

            return screen.gameObject.AddComponent<BattleScreen>();
        }

        private static UIBase BuildUpgrade(RectTransform layer)
        {
            RectTransform screen = Screen(layer, nameof(UpgradeScreen));
            Label(screen, "Heading", "UPGRADES", 56, new Vector2(0.5f, 0.93f));
            Label(screen, nameof(UpgradeScreen.Refs.ResultText), string.Empty, 32, new Vector2(0.5f, 0.86f));
            Label(screen, nameof(UpgradeScreen.Refs.GoldText), string.Empty, 40, new Vector2(0.5f, 0.8f));

            RectTransform list = Column(screen, nameof(UpgradeScreen.Refs.NodeList), new Vector2(0.5f, 0.47f), 8);
            ButtonOf(list, nameof(UpgradeScreen.Refs.NodeItem), "Node", NodeButton, "NodeItem_Text");

            RectTransform menu = Row(screen, "Menu", new Vector2(0.5f, 0.09f), 24);
            MenuButtonOf(menu, nameof(UpgradeScreen.Refs.DevGoldBtn_Button), "+100 Gold (dev)");
            MenuButtonOf(menu, nameof(UpgradeScreen.Refs.NextBattleBtn_Button), "Next battle");
            MenuButtonOf(menu, nameof(UpgradeScreen.Refs.TitleBtn_Button), "Title");

            return screen.gameObject.AddComponent<UpgradeScreen>();
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

        private static RectTransform Screen(RectTransform layer, string name, bool opaque = true)
        {
            RectTransform screen = Stretch(Child(layer, name));

            if (opaque)
                screen.gameObject.AddComponent<Image>().color = Background;

            return screen;
        }

        private static TMP_Text Label(RectTransform parent, string name, string text, float size, Vector2 anchor)
        {
            RectTransform rect = Child(parent, name);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.sizeDelta = new Vector2(1600, size * 1.6f);
            return Text(rect, text, size);
        }

        private static RectTransform Column(RectTransform parent, string name, Vector2 anchor, float spacing)
        {
            RectTransform rect = Group(parent, name, anchor);
            var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            Configure(layout, spacing);
            return rect;
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

        // 버튼 글자의 이름은 프레임워크 예제의 관례를 따른다: StartBtn_Button → StartBtn_Text.
        private static void MenuButtonOf(RectTransform parent, string name, string text) =>
            ButtonOf(parent, name, text, MenuButton, name.Replace("_Button", "_Text"));

        private static void ButtonOf(RectTransform parent, string name, string text, Vector2 size, string labelName)
        {
            RectTransform rect = Child(parent, name);

            var image = rect.gameObject.AddComponent<Image>();
            image.color = ButtonColor;
            rect.gameObject.AddComponent<Button>().targetGraphic = image;

            var element = rect.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = size.x;
            element.preferredHeight = size.y;

            Text(Stretch(Child(rect, labelName)), text, 32);
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
