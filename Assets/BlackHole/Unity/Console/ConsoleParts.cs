using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BlackHole.Unity
{
    // 개발용 콘솔 창(지금은 업그레이드 콘솔)이 쓰는 부품.
    // 창마다 게임 UI(UIManager)와 따로 자기 Canvas에 그린다. 게임 화면보다 위에 있고, 창 영역의 클릭은 아래 화면으로 새지 않는다.
    // 모든 창은 같은 키(`)로 함께 숨고 보인다. 글자는 TMP 기본 글꼴(한글 없음)이라 영문이다.
    internal static class ConsoleParts
    {
        public static readonly Color PanelColor = new Color(0, 0, 0, 0.75f);
        public static readonly Color ButtonColor = new Color(0.2f, 0.26f, 0.42f);

        // 창을 숨기고 보이는 키가 이번 프레임에 눌렸는가.
        public static bool TogglePressed()
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.backquoteKey.wasPressedThisFrame;
        }

        public static RectTransform CreateCanvas(Transform parent, string name)
        {
            RectTransform rect = Child(parent, name);

            var canvas = rect.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // 게임 UI보다 위에 그린다.
            canvas.sortingOrder = 1000;

            var scaler = rect.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            rect.gameObject.AddComponent<GraphicRaycaster>();
            return rect;
        }

        // 화면의 모서리(corner: 왼쪽 위 (0, 1), 오른쪽 위 (1, 1), 왼쪽 아래 (0, 0) …)에 붙어 그 안의 창을 쌓는다.
        // 모서리에서 16만큼 떨어지고, 내용 크기만큼 모서리 반대쪽으로 늘어난다.
        public static RectTransform Stack(RectTransform canvas, Vector2 corner)
        {
            bool right = corner.x > 0.5f;
            bool top = corner.y > 0.5f;

            RectTransform stack = Child(canvas, "Stack");
            stack.anchorMin = corner;
            stack.anchorMax = corner;
            stack.pivot = corner;
            stack.anchoredPosition = new Vector2(right ? -16 : 16, top ? -16 : 16);
            VerticalLayout(stack, 0, 8).childAlignment = top
                ? (right ? TextAnchor.UpperRight : TextAnchor.UpperLeft)
                : (right ? TextAnchor.LowerRight : TextAnchor.LowerLeft);

            var fitter = stack.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return stack;
        }

        public static RectTransform Panel(RectTransform parent, string name, Color color)
        {
            RectTransform panel = Child(parent, name);
            panel.gameObject.AddComponent<Image>().color = color;
            VerticalLayout(panel, 16, 6).padding = new RectOffset(16, 16, 12, 12);
            return panel;
        }

        public static Button ButtonOf(RectTransform parent, string name, string text, float width, Action onClick)
        {
            RectTransform rect = Child(parent, name);

            var image = rect.gameObject.AddComponent<Image>();
            image.color = ButtonColor;

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick());

            var element = rect.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = width;
            element.preferredHeight = 44;

            TMP_Text label = Text(rect, "Label", text, 24);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            label.alignment = TextAlignmentOptions.Center;
            return button;
        }

        public static void SetInteractable(Button button, bool interactable)
        {
            if (button.interactable != interactable)
                button.interactable = interactable;
        }

        public static VerticalLayoutGroup VerticalLayout(RectTransform rect, int padding, float spacing)
        {
            var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return layout;
        }

        public static HorizontalLayoutGroup HorizontalLayout(RectTransform rect, float spacing)
        {
            var layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return layout;
        }

        public static TMP_Text Text(RectTransform parent, string name, string text, float size)
        {
            RectTransform rect = Child(parent, name);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.color = Color.white;
            label.raycastTarget = false;
            return label;
        }

        public static RectTransform Child(Transform parent, string name)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return (RectTransform)child.transform;
        }

        public static string Number(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
