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
    // 적 요청 콘솔(개발용, 왼쪽 아래 창). 진행 중인 판에 적의 생성 요청과 파괴 요청을 넣는다.
    // 요청은 판(World)에 쌓였다가 다음 Step에서 처리된다 — 파괴 요청(사망 확정)이 먼저, 생성 요청(풀 여과 → 생성)이 나중이다.
    // 일시정지 중에는 Step이 없으므로 요청이 쌓여 있는 것을 볼 수 있다.
    // 이 콘솔은 요청을 넣기만 한다. 피해·HP·효과를 계산하지 않으며, 요청을 처리하는 규칙은 판이 가진다.
    //
    // 줄마다 콘텐츠의 적 종류 하나: 살아 있는 수, 쌓인 생성 요청 수(+), 쌓인 파괴 요청 수(-)와 버튼.
    // - Spawn: 그 종류 1마리의 생성 요청. 판의 풀에 없거나 최대 수에 닿은 종류는 처리 때 걸러져 버려진다.
    // - Destroy: 그 종류의 살아 있는 적 하나의 파괴 요청. 이미 요청한 적을 빼고 가장 먼저 나온 적을 고른다(콘솔의 선택).
    // - Destroy all: 아직 요청하지 않은 살아 있는 적 모두의 파괴 요청. 판 정리(처치 아님)와 달리 하나하나 처치로 센다.
    // 버튼은 진행 중이거나 정지한 판이 있을 때만 누를 수 있다.
    //
    // ` 키로 다른 콘솔 창과 함께 숨고 보인다. GameHost가 에디터와 개발 빌드에서만 만든다.
    internal sealed class EnemyRequestConsole : IDisposable
    {
        // 줄의 글자 칸과 버튼 너비. 숫자가 바뀌어도 창의 너비가 흔들리지 않게 고정한다.
        private const float LabelWidth = 220;
        private const float ButtonWidth = 110;
        private const float RowSpacing = 8;

        private readonly BattleSystem _battle;
        private readonly GameObject _canvas;
        private readonly List<KindRow> _rows = new List<KindRow>();
        private readonly Button _destroyAllButton;

        public EnemyRequestConsole(Transform parent, BattleSystem battle, IReadOnlyList<EnemyDefinition> kinds)
        {
            _battle = battle;

            RectTransform canvas = CreateCanvas(parent, "Enemy Request Console");
            _canvas = canvas.gameObject;

            RectTransform panel = Panel(Stack(canvas, Vector2.zero), "Panel", PanelColor);
            Text(panel, "Title", "ENEMY REQUESTS  ( ` )", 22);
            Text(panel, "Note", "Requests run at the next battle step.", 18);
            Text(panel, "Header", "Kind<pos=5em>Alive<pos=7.5em>Queue", 20);

            foreach (EnemyDefinition kind in kinds)
                _rows.Add(CreateRow(panel, kind));

            _destroyAllButton = ButtonOf(panel, "DestroyAll", "Destroy all",
                LabelWidth + ButtonWidth * 2 + RowSpacing * 2, DestroyAll);

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

        // 요청을 받을 판. 진행 중이거나 정지한 판만 받는다(준비 중·끝난 판은 없음).
        private World OpenWorld()
        {
            GameSession session = _battle.Session;

            if (session == null)
                return null;

            return session.Phase == SessionPhase.Running || session.Phase == SessionPhase.Paused ? session.World : null;
        }

        private void Refresh()
        {
            World world = OpenWorld();
            bool anyTarget = false;

            foreach (KindRow row in _rows)
            {
                Enemy target = world != null ? NextTarget(world, row.Kind) : null;
                anyTarget |= target != null;

                SetInteractable(row.Spawn, world != null);
                SetInteractable(row.Destroy, target != null);

                int alive = world != null ? world.CountAlive(row.Kind) : -1;
                int spawns = world != null ? QueuedSpawns(world, row.Kind) : -1;
                int destroys = world != null ? QueuedDestroys(world, row.Kind) : -1;

                if (row.Shown && alive == row.ShownAlive && spawns == row.ShownSpawns && destroys == row.ShownDestroys)
                    continue;

                row.Shown = true;
                row.ShownAlive = alive;
                row.ShownSpawns = spawns;
                row.ShownDestroys = destroys;
                row.Label.text = world != null
                    ? $"{row.Kind.Id}<pos=5em>{alive}<pos=7.5em>+{spawns}<pos=9.5em>-{destroys}"
                    : $"{row.Kind.Id}<pos=5em>-<pos=7.5em>-";
            }

            SetInteractable(_destroyAllButton, anyTarget);
        }

        #region 요청

        private void RequestSpawn(EnemyDefinition kind) =>
            OpenWorld()?.RequestSpawn(new SupplyRequest(kind, 1));

        private void RequestDestroy(EnemyDefinition kind)
        {
            World world = OpenWorld();
            Enemy target = world != null ? NextTarget(world, kind) : null;

            if (target != null)
                world.RequestDestroy(target);
        }

        private void DestroyAll()
        {
            World world = OpenWorld();

            if (world == null)
                return;

            IReadOnlyList<Enemy> enemies = world.Enemies;

            for (int i = 0; i < enemies.Count; i++)
            {
                if (!IsRequested(world, enemies[i]))
                    world.RequestDestroy(enemies[i]);
            }
        }

        // 이 종류에서 아직 파괴를 요청하지 않은, 가장 먼저 나온 살아 있는 적. 없으면 null.
        private static Enemy NextTarget(World world, EnemyDefinition kind)
        {
            IReadOnlyList<Enemy> enemies = world.Enemies;

            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i].Definition == kind && !IsRequested(world, enemies[i]))
                    return enemies[i];
            }

            return null;
        }

        private static bool IsRequested(World world, Enemy enemy)
        {
            IReadOnlyList<Enemy> requested = world.PendingDestroys;

            for (int i = 0; i < requested.Count; i++)
            {
                if (requested[i] == enemy)
                    return true;
            }

            return false;
        }

        private static int QueuedSpawns(World world, EnemyDefinition kind)
        {
            IReadOnlyList<SupplyRequest> requests = world.PendingSpawns;
            int count = 0;

            for (int i = 0; i < requests.Count; i++)
            {
                if (requests[i].Enemy == kind)
                    count += requests[i].Count;
            }

            return count;
        }

        private static int QueuedDestroys(World world, EnemyDefinition kind)
        {
            IReadOnlyList<Enemy> requested = world.PendingDestroys;
            int count = 0;

            for (int i = 0; i < requested.Count; i++)
            {
                if (requested[i].Definition == kind)
                    count++;
            }

            return count;
        }

        #endregion

        private KindRow CreateRow(RectTransform parent, EnemyDefinition kind)
        {
            RectTransform rect = Child(parent, kind.Id);
            HorizontalLayout(rect, RowSpacing).childAlignment = TextAnchor.MiddleLeft;

            TMP_Text label = Text(rect, "Label", string.Empty, 20);
            label.gameObject.AddComponent<LayoutElement>().preferredWidth = LabelWidth;

            Button spawn = ButtonOf(rect, "Spawn", "Spawn", ButtonWidth, () => RequestSpawn(kind));
            Button destroy = ButtonOf(rect, "Destroy", "Destroy", ButtonWidth, () => RequestDestroy(kind));
            return new KindRow(kind, label, spawn, destroy);
        }

        // 적 종류 한 줄: 수와 요청 버튼. 보이는 값이 바뀔 때만 글자를 고친다.
        private sealed class KindRow
        {
            public readonly EnemyDefinition Kind;
            public readonly TMP_Text Label;
            public readonly Button Spawn;
            public readonly Button Destroy;
            public bool Shown;
            public int ShownAlive;
            public int ShownSpawns;
            public int ShownDestroys;

            public KindRow(EnemyDefinition kind, TMP_Text label, Button spawn, Button destroy)
            {
                Kind = kind;
                Label = label;
                Spawn = spawn;
                Destroy = destroy;
            }
        }
    }
}
