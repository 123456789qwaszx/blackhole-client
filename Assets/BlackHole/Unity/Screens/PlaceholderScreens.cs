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
        private static readonly Color ButtonColor = new Color(0.2f, 0.26f, 0.42f);
        private static readonly Vector2 MenuButton = new Vector2(420, 72);

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

            var views = new UIBase[0];
            return new Result(rootLayer, panelLayer, views);
        }

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
        private static void MenuButtonOf(RectTransform parent, string name, string text)
        {
            RectTransform rect = Child(parent, name);

            var image = rect.gameObject.AddComponent<Image>();
            image.color = ButtonColor;
            rect.gameObject.AddComponent<Button>().targetGraphic = image;

            var element = rect.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = MenuButton.x;
            element.preferredHeight = MenuButton.y;

            Text(Stretch(Child(rect, name.Replace("_Button", "_Text"))), text, 32);
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
