using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace BlackHole.Unity
{
    // 화면 프리팹이 연결되기 전에 쓰는 임시 화면. Canvas와 화면을 코드로 만든다.
    // 자식 이름은 화면 클래스의 Refs와 같다 — 실제 프리팹도 같은 이름을 쓰면 화면 클래스가 그대로 붙는다.
    // GameHost에 사용자 UI를 연결하면 쓰지 않는다. 글자는 TMP 기본 글꼴(한글 없음)이라 영문이다.
    internal static class PlaceholderScreens
    {
        private static readonly Color Backdrop = new Color(0.08f, 0.08f, 0.11f);
        private const float HeaderHeight = 110;

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

            var views = new UIBase[] { BuildUpgrade(rootLayer) };
            return new Result(rootLayer, panelLayer, views);
        }

        #region 화면

        // 자식을 모두 만든 뒤 화면 클래스를 붙인다. 화면 클래스는 붙는 순간 자식을 이름으로 찾는다.
        // 업그레이드 화면: 어두운 배경, 위쪽 글자(Gold, 제목, 산 노드 수), 그 아래 전부가 트리 영역이다.
        private static UIBase BuildUpgrade(RectTransform layer)
        {
            RectTransform screen = Stretch(Child(layer, nameof(UpgradeScreen)));
            screen.gameObject.AddComponent<Image>().color = Backdrop;

            RectTransform viewport = Stretch(Child(screen, nameof(UpgradeScreen.Refs.TreeViewport)));
            viewport.offsetMax = new Vector2(0, -HeaderHeight);

            Label(screen, nameof(UpgradeScreen.Refs.GoldText), string.Empty, 36, new Vector2(0.2f, 0.955f));
            Label(screen, "Heading", "UPGRADES", 40, new Vector2(0.5f, 0.955f));
            Label(screen, nameof(UpgradeScreen.Refs.ProgressText), string.Empty, 36, new Vector2(0.8f, 0.955f));

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

        private static TMP_Text Label(RectTransform parent, string name, string text, float size, Vector2 anchor)
        {
            RectTransform rect = Child(parent, name);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.sizeDelta = new Vector2(1600, size * 1.6f);
            return Text(rect, text, size);
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
