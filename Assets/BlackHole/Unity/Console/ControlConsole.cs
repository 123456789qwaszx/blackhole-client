using System;
using System.Collections.Generic;
using System.Text;
using BlackHole.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BlackHole.Unity
{
    // 조종 콘솔(개발용). 진행도(적의 강도 단계)와 전투의 seed를 보여 주고, 진행도만 바꾼다.
    // 진행도를 바꾸면 다음에 조립하는 전투부터 쓰인다. 진행 중인 전투는 바뀌지 않는다.
    //
    // 아래에는 적 풀과 풀의 종류마다 "지금 살아 있는 수 / 동시 최대 수"를 보여 준다.
    // - 전투 화면에서는 그 전투의 단계 풀과 지금 살아 있는 수(매 프레임).
    // - 그 밖의 화면에서는 선택한 진행도의 풀. 전투가 없으므로 살아 있는 수는 '-'다.
    //
    // 게임 UI(UIManager)와 따로 자기 Canvas에 그린다. 게임 화면보다 위에 있고, 콘솔 영역의 클릭은 아래 화면으로 새지 않는다.
    // ` 키로 숨기고 보인다. GameHost가 에디터와 개발 빌드에서만 만든다. 글자는 TMP 기본 글꼴(한글 없음)이라 영문이다.
    internal sealed class ControlConsole : IDisposable
    {
        private static readonly Color PanelColor = new Color(0, 0, 0, 0.75f);
        private static readonly Color ButtonColor = new Color(0.2f, 0.26f, 0.42f);

        private readonly ScreenFlow _flow;
        private readonly GameObject _canvas;
        private readonly TMP_Text _stageText;
        private readonly TMP_Text _seedText;
        private readonly TMP_Text _poolText;
        private readonly TMP_Text _poolEntriesText;
        private readonly StringBuilder _builder = new StringBuilder();
        private int _shownStage = -1;
        private int? _shownSeed;
        private bool _seedShown;
        // 풀 표시가 마지막으로 그린 것. 바뀔 때만 다시 쓴다.
        private EnemyPoolDefinition _shownPool;
        private int _shownPoolStage;
        private bool _shownLive;
        private int[] _shownCounts = new int[0];

        public ControlConsole(Transform parent, ScreenFlow flow)
        {
            _flow = flow;

            RectTransform canvas = CreateCanvas(parent);
            _canvas = canvas.gameObject;

            RectTransform panel = Child(canvas, "Panel");
            panel.anchorMin = Vector2.one;
            panel.anchorMax = Vector2.one;
            panel.pivot = Vector2.one;
            panel.anchoredPosition = new Vector2(-16, -16);
            panel.gameObject.AddComponent<Image>().color = PanelColor;

            var column = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            column.padding = new RectOffset(16, 16, 12, 12);
            column.spacing = 6;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = false;
            column.childForceExpandHeight = false;

            var fitter = panel.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Text(panel, "Title", "CONTROL CONSOLE  ( ` )", 22);
            _stageText = Text(panel, "Stage", string.Empty, 28);

            RectTransform buttons = Child(panel, "StageButtons");
            var row = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 8;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            StageButton(buttons, -10);
            StageButton(buttons, -1);
            StageButton(buttons, +1);
            StageButton(buttons, +10);

            _seedText = Text(panel, "Seed", string.Empty, 28);
            Text(panel, "Note", "Stage applies from the next battle.", 18);

            _poolText = Text(panel, "Pool", string.Empty, 28);
            _poolEntriesText = Text(panel, "PoolEntries", string.Empty, 22);

            Refresh();
        }

        // 한 프레임. 표시 여부를 바꾸고, 바뀐 값만 다시 쓴다.
        public void Tick()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard != null && keyboard.backquoteKey.wasPressedThisFrame)
                _canvas.SetActive(!_canvas.activeSelf);

            Refresh();
        }

        public void Dispose() => Object.Destroy(_canvas);

        private void Refresh()
        {
            if (_flow.Stage != _shownStage)
            {
                _shownStage = _flow.Stage;
                _stageText.text = $"Stage  {_shownStage} / {_flow.StageCount}";
            }

            int? seed = _flow.BattleSeed;

            if (!_seedShown || seed != _shownSeed)
            {
                _seedShown = true;
                _shownSeed = seed;
                _seedText.text = seed.HasValue ? $"Seed  {seed.Value}" : "Seed  -";
            }

            RefreshPool();
        }

        private void RefreshPool()
        {
            GameSession battle = _flow.ActiveBattle;
            bool live = battle != null;
            EnemyPoolDefinition pool;
            int stage;

            if (live)
            {
                pool = battle.World.Pool;
                stage = battle.Stage;
            }
            else
            {
                StageDefinition selected = _flow.SelectedStage;
                pool = selected.Pool;
                stage = selected.Number;
            }

            IReadOnlyList<EnemyPoolEntry> entries = pool.Entries;
            bool changed = pool != _shownPool || stage != _shownPoolStage || live != _shownLive;

            if (changed)
                _shownCounts = new int[entries.Count];

            for (int i = 0; i < entries.Count; i++)
            {
                int count = live ? battle.World.CountAlive(entries[i].Enemy) : -1;

                if (count != _shownCounts[i])
                {
                    _shownCounts[i] = count;
                    changed = true;
                }
            }

            if (!changed)
                return;

            _shownPool = pool;
            _shownPoolStage = stage;
            _shownLive = live;
            _poolText.text = live ? $"Pool  {pool.Id}  (battle, stage {stage})" : $"Pool  {pool.Id}  (stage {stage})";

            _builder.Clear();

            for (int i = 0; i < entries.Count; i++)
            {
                if (i > 0)
                    _builder.Append('\n');

                _builder.Append("  ").Append(entries[i].Enemy.Id).Append("<pos=9em>");
                _builder.Append(live ? _shownCounts[i].ToString() : "-");
                _builder.Append(" / ").Append(entries[i].MaxAlive);
            }

            _poolEntriesText.text = _builder.ToString();
        }

        private void StageButton(RectTransform parent, int delta)
        {
            RectTransform rect = Child(parent, $"Stage{delta:+0;-0}");

            var image = rect.gameObject.AddComponent<Image>();
            image.color = ButtonColor;

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => _flow.SetStage(_flow.Stage + delta));

            var element = rect.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = 80;
            element.preferredHeight = 44;

            TMP_Text label = Text(rect, "Label", $"{delta:+0;-0}", 24);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            label.alignment = TextAlignmentOptions.Center;
        }

        private static RectTransform CreateCanvas(Transform parent)
        {
            RectTransform rect = Child(parent, "Control Console");

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

        private static TMP_Text Text(RectTransform parent, string name, string text, float size)
        {
            RectTransform rect = Child(parent, name);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
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
    }
}
