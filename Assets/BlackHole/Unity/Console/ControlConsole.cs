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
    // 조종 콘솔(개발용). 진행도(적의 강도 단계)와 전투의 seed를 보여 주고, 진행도를 바꾼다.
    // 진행도를 바꾸면 다음에 조립하는 전투부터 쓰인다. 진행 중인 전투는 바뀌지 않는다.
    // 전투 시작·종료는 다른 창(BattleLifecycleConsole)이다.
    //
    // 아래에는 적 풀과 풀의 종류마다 "지금 살아 있는 수 / 동시 최대 수"를 보여 준다.
    // - 판이 있으면 그 판의 단계 풀과 지금 살아 있는 수(매 프레임).
    // - 판이 없으면 선택한 진행도의 풀. 살아 있는 수는 '-'다.
    // 종류 줄을 누르면 그 종류의 수치·형태·특성이 아래의 설명창에 나온다. 같은 줄을 다시 누르면 닫힌다.
    // 적의 수치는 전투 Session이 시작되기 전에 정해지고 전투 중에는 바뀌지 않는다. 그래서 설명창은
    // 판이 있으면 그 판의 수치를, 없으면 종류의 기본 수치를 보여 준다(업그레이드 보정은 판 조립 때 반영된다).
    //
    // ` 키로 다른 콘솔 창과 함께 숨고 보인다. GameHost가 에디터와 개발 빌드에서만 만든다.
    internal sealed class ControlConsole : IDisposable
    {
        private static readonly Color DetailColor = new Color(0.04f, 0.06f, 0.12f, 0.85f);
        private static readonly Color RowColor = new Color(1, 1, 1, 0);
        private static readonly Color SelectedRowColor = new Color(0.35f, 0.45f, 0.75f, 0.45f);

        private readonly BattleOrchestrator _orchestrator;
        private readonly BattleSystem _battle;
        private readonly EnemyLooks _looks;
        private readonly GameObject _canvas;
        private readonly TMP_Text _stageText;
        private readonly TMP_Text _seedText;
        private readonly TMP_Text _poolText;
        private readonly RectTransform _poolRows;
        private readonly List<PoolRow> _rows = new List<PoolRow>();
        private readonly GameObject _detailPanel;
        private readonly TMP_Text _detailTitle;
        private readonly Image _detailForm;
        private readonly TMP_Text _detailFormText;
        private readonly TMP_Text _detailStats;
        private readonly TMP_Text _detailSource;
        private readonly StringBuilder _builder = new StringBuilder();

        private int _shownStage = -1;
        private int? _shownSeed;
        private bool _seedShown;
        // 풀 표시가 마지막으로 그린 것. 바뀔 때만 다시 쓴다.
        private EnemyPoolDefinition _shownPool;
        private int _shownPoolStage;
        private bool _shownLive;
        // 설명창에 보일 종류. 없으면 설명창을 닫는다.
        private EnemyDefinition _selected;
        private bool _detailDirty;
        private GameSession _shownDetailBattle;

        public ControlConsole(Transform parent, BattleOrchestrator orchestrator, BattleSystem battle, EnemyLooks looks)
        {
            _orchestrator = orchestrator;
            _battle = battle;
            _looks = looks;

            RectTransform canvas = CreateCanvas(parent, "Control Console");
            _canvas = canvas.gameObject;

            // 콘솔 패널과 설명창을 오른쪽 위에서 아래로 쌓는다.
            RectTransform stack = Stack(canvas, Vector2.one);

            RectTransform panel = Panel(stack, "Panel", PanelColor);
            Text(panel, "Title", "CONTROL CONSOLE  ( ` )", 22);
            _stageText = Text(panel, "Stage", string.Empty, 28);

            RectTransform buttons = Child(panel, "StageButtons");
            HorizontalLayout(buttons, 8);
            StageButton(buttons, -10);
            StageButton(buttons, -1);
            StageButton(buttons, +1);
            StageButton(buttons, +10);

            _seedText = Text(panel, "Seed", string.Empty, 28);
            Text(panel, "Note", "Stage applies from the next battle.", 18);

            _poolText = Text(panel, "Pool", string.Empty, 28);
            _poolRows = Child(panel, "PoolRows");
            VerticalLayout(_poolRows, 0, 2).childForceExpandWidth = true;

            RectTransform detail = Panel(stack, "Detail", DetailColor);
            _detailPanel = detail.gameObject;
            _detailTitle = Text(detail, "Kind", string.Empty, 28);

            RectTransform form = Child(detail, "Form");
            HorizontalLayout(form, 12).childAlignment = TextAnchor.MiddleLeft;
            Text(form, "Label", "Form", 22);
            _detailForm = FormPreview(form);
            _detailFormText = Text(form, "Shape", string.Empty, 22);

            _detailStats = Text(detail, "Stats", string.Empty, 22);
            _detailSource = Text(detail, "Source", string.Empty, 18);
            _detailPanel.SetActive(false);

            Refresh();
        }

        // 한 프레임. 표시 여부를 바꾸고, 바뀐 값만 다시 쓴다.
        public void Tick()
        {
            if (TogglePressed())
                _canvas.SetActive(!_canvas.activeSelf);

            Refresh();
        }

        public void Dispose() => Object.Destroy(_canvas);

        private void Refresh()
        {
            if (_orchestrator.Stage != _shownStage)
            {
                _shownStage = _orchestrator.Stage;
                _stageText.text = $"Stage  {_shownStage} / {_orchestrator.StageCount}";
            }

            GameSession battle = _battle.Session;
            int? seed = battle?.Seed;

            if (!_seedShown || seed != _shownSeed)
            {
                _seedShown = true;
                _shownSeed = seed;
                _seedText.text = seed.HasValue ? $"Seed  {seed.Value}" : "Seed  -";
            }

            RefreshPool(battle);
            RefreshDetail(battle);
        }

        #region 풀

        private void RefreshPool(GameSession battle)
        {
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
                StageDefinition selected = _orchestrator.SelectedStage;
                pool = selected.Pool;
                stage = selected.Number;
            }

            if (pool != _shownPool || stage != _shownPoolStage || live != _shownLive)
            {
                _shownPool = pool;
                _shownPoolStage = stage;
                _shownLive = live;
                _poolText.text = live ? $"Pool  {pool.Id}  (battle, stage {stage})" : $"Pool  {pool.Id}  (stage {stage})";
                BuildRows(pool);
            }

            for (int i = 0; i < _rows.Count; i++)
            {
                PoolRow row = _rows[i];
                int count = live ? battle.World.CountAlive(row.Entry.Enemy) : -1;

                if (count == row.ShownCount && row.Shown)
                    continue;

                row.ShownCount = count;
                row.Shown = true;
                row.Label.text = $"  {row.Entry.Enemy.Id}<pos=9em>{(live ? count.ToString() : "-")} / {row.Entry.MaxAlive}";
            }
        }

        // 풀이 바뀌면 종류 줄을 새로 만든다. 선택한 종류가 새 풀에 없으면 설명창을 닫는다.
        private void BuildRows(EnemyPoolDefinition pool)
        {
            foreach (PoolRow row in _rows)
                Object.Destroy(row.Root);

            _rows.Clear();
            bool selectedInPool = false;

            foreach (EnemyPoolEntry entry in pool.Entries)
            {
                _rows.Add(CreateRow(entry));
                selectedInPool |= entry.Enemy == _selected;
            }

            if (!selectedInPool)
                Select(null);
            else
                PaintRows();
        }

        private PoolRow CreateRow(EnemyPoolEntry entry)
        {
            RectTransform rect = Child(_poolRows, entry.Enemy.Id);
            HorizontalLayout(rect, 0).padding = new RectOffset(0, 8, 2, 2);

            var image = rect.gameObject.AddComponent<Image>();
            image.color = RowColor;

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            EnemyDefinition kind = entry.Enemy;
            button.onClick.AddListener(() => Select(_selected == kind ? null : kind));

            return new PoolRow(rect.gameObject, image, Text(rect, "Label", string.Empty, 22), entry);
        }

        private void PaintRows()
        {
            foreach (PoolRow row in _rows)
                row.Background.color = row.Entry.Enemy == _selected ? SelectedRowColor : RowColor;
        }

        #endregion

        #region 설명창

        private void Select(EnemyDefinition kind)
        {
            _selected = kind;
            _detailDirty = true;
            PaintRows();
        }

        // 선택이 바뀌었거나, 수치의 출처(진행 중인 판 / 기본 수치)가 바뀌었을 때만 다시 쓴다.
        // 판의 수치는 판 동안 바뀌지 않으므로 매 프레임 다시 쓸 필요가 없다.
        private void RefreshDetail(GameSession battle)
        {
            if (!_detailDirty && battle == _shownDetailBattle)
                return;

            _detailDirty = false;
            _shownDetailBattle = battle;
            _detailPanel.SetActive(_selected != null);

            if (_selected == null)
                return;

            EnemyDefinition kind = _selected;
            EnemyStats stats = battle != null ? battle.World.StatsOf(kind) : kind.BaseStats;

            _detailTitle.text = kind.Id;
            _detailForm.sprite = _looks.SpriteOf(kind.Id);
            _detailForm.color = _looks.ColorOf(kind.Id);
            _detailFormText.text = $"radius {Number(stats.Size)}";

            _builder.Clear();
            _builder.Append("Health<pos=6em>").Append(Number(stats.MaxHealth)).Append('\n');
            _builder.Append("Speed<pos=6em>").Append(Number(stats.MoveSpeed)).Append('\n');
            _builder.Append("Size<pos=6em>").Append(Number(stats.Size)).Append('\n');
            _builder.Append("Behavior<pos=6em>").Append(Describe(kind.Behavior)).Append('\n');
            // 특성(전기·폭발·처치 버프)은 종류에 붙는다. 특성 시스템이 붙기 전에는 없다.
            _builder.Append("Traits<pos=6em>none");
            _detailStats.text = _builder.ToString();

            _detailSource.text = battle != null
                ? $"Battle stats (stage {battle.Stage}), fixed at battle start."
                : "Base stats. Upgrades apply at battle start.";
        }

        // 설명창이 옆으로 늘어나지 않게 짧게 쓴다. 지금 행동은 HQ 공전 하나라 방향만 적는다.
        private static string Describe(EnemyBehaviorDefinition behavior)
        {
            switch (behavior)
            {
                case OrbitBehaviorDefinition orbit:
                    return orbit.Clockwise ? "clockwise" : "counterclockwise";
                default:
                    return behavior.GetType().Name;
            }
        }

        #endregion

        #region 부품

        private void StageButton(RectTransform parent, int delta) =>
            ButtonOf(parent, $"Stage{delta:+0;-0}", $"{delta:+0;-0}", 80,
                () => _orchestrator.SetStage(_orchestrator.Stage + delta));

        private static Image FormPreview(RectTransform parent)
        {
            RectTransform rect = Child(parent, "Preview");
            var image = rect.gameObject.AddComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;

            var element = rect.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = 40;
            element.preferredHeight = 40;
            return image;
        }

        #endregion

        // 풀의 종류 한 줄: 누르면 그 종류를 설명창에 띄운다.
        private sealed class PoolRow
        {
            public readonly GameObject Root;
            public readonly Image Background;
            public readonly TMP_Text Label;
            public readonly EnemyPoolEntry Entry;
            public int ShownCount;
            public bool Shown;

            public PoolRow(GameObject root, Image background, TMP_Text label, EnemyPoolEntry entry)
            {
                Root = root;
                Background = background;
                Label = label;
                Entry = entry;
            }
        }
    }
}
