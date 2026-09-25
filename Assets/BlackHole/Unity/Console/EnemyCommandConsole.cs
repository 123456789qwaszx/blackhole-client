using System;
using System.Collections.Generic;
using BlackHole.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static BlackHole.Unity.ConsoleParts;
using Object = UnityEngine.Object;

namespace BlackHole.Unity
{
    // 적 명령 콘솔(개발용, 왼쪽 아래 창). 적 명령 흐름(EnemyCommands)의 세 경우를 버튼으로 보낸다.
    // 콘솔은 버튼과 표시만 맡는다. 무엇을 요청할지는 명령 흐름이, 요청을 처리하는 규칙은 판(World)이 가진다.
    //
    // 종류 체크박스: 명령을 받을 적 종류(콘텐츠의 종류 전체). 버튼을 누르면 체크한 종류마다 명령을 한 번씩 보낸다.
    // 체크한 종류가 없으면 버튼을 누를 수 없다. 체크 상태는 전투가 바뀌어도 남는다.
    // 버튼은 진행 중이거나 정지한 판이 있을 때만 누를 수 있다. 정지 중에 누르면 요청이 쌓여 있는 것이 보이고,
    // 재개하면 다음 Step 하나에서 처리된다.
    // 아래 줄: 마지막으로 보낸 명령(체크한 종류의 합: 파괴 요청 수 / 의도한 수, 생성 요청 수)과
    // 판의 지금 상태(살아 있는 수, 쌓인 요청, 처치 수).
    //
    // ` 키로 다른 콘솔 창과 함께 숨고 보인다. GameHost가 에디터와 개발 빌드에서만 만든다.
    internal sealed class EnemyCommandConsole : IDisposable
    {
        private const float ButtonWidth = 344;
        // 종류 체크박스 칸: 한 줄에 3개(3 × 110 + 간격 7 × 2 = 버튼 너비).
        private static readonly Vector2 KindCell = new Vector2(110, 30);
        private const int KindColumns = 3;
        private static readonly Color CheckColor = new Color(0.85f, 0.9f, 1f);

        private readonly BattleSystem _battle;
        private readonly EnemyCommands _commands = new EnemyCommands();
        private readonly GameObject _canvas;
        private readonly List<KindToggle> _kinds = new List<KindToggle>();
        private readonly Button[] _buttons;
        private readonly TMP_Text _lastText;
        private readonly TMP_Text _stateText;

        // 상태 줄이 마지막으로 그린 값. 바뀔 때만 다시 쓴다.
        private bool _stateShown;
        private bool _shownOpen;
        private int _shownAlive;
        private int _shownDestroys;
        private int _shownSpawns;
        private int _shownKills;

        public EnemyCommandConsole(Transform parent, BattleSystem battle, IReadOnlyList<EnemyDefinition> kinds)
        {
            _battle = battle;

            RectTransform canvas = CreateCanvas(parent, "Enemy Command Console");
            _canvas = canvas.gameObject;

            RectTransform panel = Panel(Stack(canvas, Vector2.zero), "Panel", PanelColor);
            Text(panel, "Title", "ENEMY COMMANDS  ( ` )", 22);
            Text(panel, "Note", "Runs once per checked kind. Random targets.", 18);

            RectTransform grid = Child(panel, "Kinds");
            var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = KindCell;
            layout.spacing = new Vector2((ButtonWidth - KindCell.x * KindColumns) / (KindColumns - 1), 6);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = KindColumns;

            foreach (EnemyDefinition kind in kinds)
                _kinds.Add(new KindToggle(kind, Checkbox(grid, kind.Id)));

            _buttons = new[]
            {
                CommandButton(panel, "DestroyFive", "Destroy 5", _commands.DestroyFive),
                CommandButton(panel, "DestroyTenSpawnEight", "Destroy 10, spawn 8", _commands.DestroyTenSpawnEight),
                CommandButton(panel, "DestroyOneSpawnTen", "Destroy 1, spawn 10", _commands.DestroyOneSpawnTen),
            };

            _lastText = Text(panel, "Last", "Last  -", 20);
            _stateText = Text(panel, "State", string.Empty, 20);

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

        // 명령을 받을 판. 진행 중이거나 정지한 판만 받는다(준비 중·끝난 판은 없음).
        private World OpenWorld()
        {
            GameSession session = _battle.Session;

            if (session == null)
                return null;

            return session.Phase == SessionPhase.Running || session.Phase == SessionPhase.Paused ? session.World : null;
        }

        private Button CommandButton(
            RectTransform parent, string name, string label, Func<World, EnemyDefinition, EnemyCommands.CommandResult> command) =>
            ButtonOf(parent, name, label, ButtonWidth, () => Send(label, command));

        // 체크한 종류마다 명령을 한 번씩 보낸다(콘텐츠의 종류 순서).
        private void Send(string label, Func<World, EnemyDefinition, EnemyCommands.CommandResult> command)
        {
            World world = OpenWorld();

            if (world == null)
                return;

            int kinds = 0, destroyed = 0, intended = 0, spawned = 0;

            foreach (KindToggle kind in _kinds)
            {
                if (!kind.Toggle.isOn)
                    continue;

                EnemyCommands.CommandResult result = command(world, kind.Kind);
                kinds++;
                destroyed += result.Destroyed;
                intended += result.IntendedDestroys;
                spawned += result.Spawned;
            }

            if (kinds > 0)
                _lastText.text = $"Last  {label}  ({kinds} kind{(kinds > 1 ? "s" : "")})\n  destroy {destroyed} / {intended}  spawn {spawned}";
        }

        private void Refresh()
        {
            World world = OpenWorld();
            bool open = world != null;
            bool anyChecked = false;

            foreach (KindToggle kind in _kinds)
                anyChecked |= kind.Toggle.isOn;

            foreach (Button button in _buttons)
                SetInteractable(button, open && anyChecked);

            int alive = open ? world.Enemies.Count : 0;
            int destroys = open ? world.PendingDestroys.Count : 0;
            int spawns = open ? QueuedSpawns(world) : 0;
            int kills = open ? world.TotalKills : 0;

            if (_stateShown && open == _shownOpen && alive == _shownAlive && destroys == _shownDestroys
                && spawns == _shownSpawns && kills == _shownKills)
                return;

            _stateShown = true;
            _shownOpen = open;
            _shownAlive = alive;
            _shownDestroys = destroys;
            _shownSpawns = spawns;
            _shownKills = kills;
            _stateText.text = open
                ? $"Alive {alive}  Queue -{destroys} / +{spawns}  Kills {kills}"
                : "Alive -  Queue -  Kills -";
        }

        // 쌓인 생성 요청의 마릿수 합.
        private static int QueuedSpawns(World world)
        {
            int count = 0;

            for (int i = 0; i < world.PendingSpawns.Count; i++)
                count += world.PendingSpawns[i].Count;

            return count;
        }

        // 체크박스 하나: 왼쪽에 상자(체크하면 안쪽이 채워진다), 오른쪽에 글자. 칸 어디를 눌러도 바뀐다. 처음에는 꺼져 있다.
        private static Toggle Checkbox(RectTransform parent, string text)
        {
            RectTransform rect = Child(parent, text);
            rect.gameObject.AddComponent<Image>().color = Color.clear;

            RectTransform box = Child(rect, "Box");
            box.anchorMin = new Vector2(0, 0.5f);
            box.anchorMax = new Vector2(0, 0.5f);
            box.pivot = new Vector2(0, 0.5f);
            box.sizeDelta = new Vector2(24, 24);
            var boxImage = box.gameObject.AddComponent<Image>();
            boxImage.color = ButtonColor;

            RectTransform check = Child(box, "Check");
            check.anchorMin = Vector2.zero;
            check.anchorMax = Vector2.one;
            check.offsetMin = new Vector2(5, 5);
            check.offsetMax = new Vector2(-5, -5);
            var checkImage = check.gameObject.AddComponent<Image>();
            checkImage.color = CheckColor;
            checkImage.raycastTarget = false;

            TMP_Text label = Text(rect, "Label", text, 20);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(30, 0);
            labelRect.offsetMax = Vector2.zero;
            label.alignment = TextAlignmentOptions.MidlineLeft;

            var toggle = rect.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = boxImage;
            toggle.graphic = checkImage;
            toggle.isOn = false;
            return toggle;
        }

        private sealed class KindToggle
        {
            public readonly EnemyDefinition Kind;
            public readonly Toggle Toggle;

            public KindToggle(EnemyDefinition kind, Toggle toggle)
            {
                Kind = kind;
                Toggle = toggle;
            }
        }
    }
}
