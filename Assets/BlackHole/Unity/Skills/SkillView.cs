using System;
using System.Collections.Generic;
using BlackHole.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BlackHole.Unity
{
    // 스킬의 화면. 매 프레임 판의 참가자를 읽어 그린다. 게임 상태를 바꾸지 않고, 피해를 다시 계산하지 않는다.
    // - Breaker 범위 원: 참가자의 조준점에 Breaker 반지름으로 늘 그린다. 판정과 같은 값이다(GAME_RULES 6절). 조준점이 없으면 숨긴다.
    // - Breaker Tick 원: Tick 기록마다 한 번 굵게 그렸다가 옅어진다. 빈 Tick(조준점 없음)은 그리지 않는다.
    // - 레이저 예고선: 예고 중인 발사마다 얇은 선. 발사에 가까울수록 진해진다(CONTENT_DEFINITION 5.2).
    // - 레이저 발사선: 발사 기록마다 판정 굵기 그대로의 선이 잠깐 보였다가 옅어진다.
    // 정지 중에는 판이 기록을 비우지 않으므로, 이미 그린 Tick·발사는 번호로 걸러 두 번 그리지 않는다.
    // 규칙 평면은 장면의 z = 0이고 x·y는 같다(EnemyView와 같다).
    internal sealed class SkillView : IDisposable
    {
        private const int CircleSegments = 48;
        private const int SortingOrder = 10;
        private const float RangeWidth = 0.03f;
        private const float TickWidth = 0.08f;
        private const float TickSeconds = 0.3f;
        private const float TelegraphWidth = 0.04f;
        private const float FireSeconds = 0.2f;
        private static readonly Color BreakerColor = new Color(0.3f, 1f, 0.55f, 1f);
        private static readonly Color RangeColor = new Color(0.3f, 1f, 0.55f, 0.45f);
        private static readonly Color TelegraphColor = new Color(1f, 0.9f, 0.3f, 1f);
        private static readonly Color FireColor = new Color(0.35f, 0.9f, 1f, 1f);

        private readonly Transform _root;
        private readonly Material _material;
        private readonly Dictionary<PlayerId, LineRenderer> _ranges = new Dictionary<PlayerId, LineRenderer>();
        private readonly Dictionary<PlayerId, int> _drawnTicks = new Dictionary<PlayerId, int>();
        private readonly Dictionary<PlayerId, int> _drawnFires = new Dictionary<PlayerId, int>();
        // 예고선. 이번 프레임에 쓰지 않은 선은 숨겨 두었다가 다음 예고에 다시 쓴다.
        private readonly List<LineRenderer> _telegraphs = new List<LineRenderer>();
        private readonly List<Flash> _flashes = new List<Flash>();

        // 잠깐 보였다가 옅어지는 선(Tick 원, 발사선).
        private sealed class Flash
        {
            public LineRenderer Line;
            public Color Color;
            public float Duration;
            public float Remaining;
        }

        public SkillView(Transform parent)
        {
            _root = new GameObject("Skill View").transform;
            _root.SetParent(parent, false);
            _material = new Material(Shader.Find("Sprites/Default")) { name = "Skill Lines" };
        }

        public void Synchronize(World world, float delta)
        {
            AgeFlashes(delta);
            int telegraphs = 0;
            IReadOnlyList<BattlePlayer> players = world.Players;

            // 매 프레임 경로: IReadOnlyList를 인덱스로 돈다(인터페이스 foreach는 열거자를 할당한다).
            for (int i = 0; i < players.Count; i++)
            {
                BattlePlayer player = players[i];

                if (player.Breaker != null)
                    ShowBreaker(player, player.Breaker);

                if (player.Laser != null)
                    telegraphs = ShowLaser(player.Id, player.Laser, telegraphs);
            }

            for (int i = telegraphs; i < _telegraphs.Count; i++)
                _telegraphs[i].enabled = false;
        }

        // 그리는 선이 없고, 지운 객체도 장면에서 모두 사라졌는가.
        // 지운 객체는 프레임 끝에 사라지므로, Reset 뒤 한 프레임이 지나야 true가 된다.
        public bool IsClear => _ranges.Count == 0 && _telegraphs.Count == 0 && _flashes.Count == 0 && _root.childCount == 0;

        // 판이 바뀌거나 판을 정리할 때 모든 선을 지운다.
        public void Reset()
        {
            foreach (LineRenderer range in _ranges.Values)
                Object.Destroy(range.gameObject);

            foreach (LineRenderer telegraph in _telegraphs)
                Object.Destroy(telegraph.gameObject);

            foreach (Flash flash in _flashes)
                Object.Destroy(flash.Line.gameObject);

            _ranges.Clear();
            _telegraphs.Clear();
            _flashes.Clear();
            _drawnTicks.Clear();
            _drawnFires.Clear();
        }

        public void Dispose()
        {
            Object.Destroy(_root.gameObject);
            Object.Destroy(_material);
        }

        #region Breaker

        private void ShowBreaker(BattlePlayer player, BreakerSkill breaker)
        {
            if (!_ranges.TryGetValue(player.Id, out LineRenderer range))
            {
                range = CreateLine($"Breaker Range ({player.Id})", RangeWidth, RangeColor);
                SetCircle(range, breaker.Definition.Radius);
                _ranges.Add(player.Id, range);
            }

            range.enabled = player.AimPoint.HasValue;

            if (player.AimPoint.HasValue)
                range.transform.localPosition = ToScene(player.AimPoint.Value);

            _drawnTicks.TryGetValue(player.Id, out int drawn);
            IReadOnlyList<BreakerTick> ticks = breaker.Ticks;

            for (int i = 0; i < ticks.Count; i++)
            {
                BreakerTick tick = ticks[i];

                if (tick.Number <= drawn)
                    continue;

                drawn = tick.Number;

                if (tick.Center.HasValue)
                {
                    LineRenderer line = AddFlash("Breaker Tick", TickWidth, BreakerColor, TickSeconds);
                    SetCircle(line, tick.Radius);
                    line.transform.localPosition = ToScene(tick.Center.Value);
                }
            }

            _drawnTicks[player.Id] = drawn;
        }

        #endregion

        #region 레이저

        // 예고선을 used번째부터 채우고, 다음에 쓸 예고선 번호를 돌려준다.
        private int ShowLaser(PlayerId player, LaserSkill laser, int used)
        {
            IReadOnlyList<LaserShot> pending = laser.PendingShots;

            for (int i = 0; i < pending.Count; i++)
            {
                LaserShot shot = pending[i];

                if (used == _telegraphs.Count)
                    _telegraphs.Add(CreateLine("Laser Telegraph", TelegraphWidth, TelegraphColor));

                LineRenderer line = _telegraphs[used++];
                SetSegment(line, shot.Start, shot.End);
                Color color = TelegraphColor;
                color.a = Mathf.Lerp(0.25f, 1f, 1 - Mathf.Clamp01(shot.Remaining / laser.Definition.TelegraphDuration));
                SetColor(line, color);
                line.enabled = true;
            }

            _drawnFires.TryGetValue(player, out int drawn);
            IReadOnlyList<LaserFire> fires = laser.Fires;

            for (int i = 0; i < fires.Count; i++)
            {
                LaserFire fire = fires[i];

                if (fire.Number <= drawn)
                    continue;

                drawn = fire.Number;
                LineRenderer line = AddFlash("Laser Fire", fire.Width, FireColor, FireSeconds);
                SetSegment(line, fire.Start, fire.End);
            }

            _drawnFires[player] = drawn;
            return used;
        }

        #endregion

        #region 선

        // 다 옅어진 선이 있으면 다시 쓰고, 없으면 만든다. 위치와 모양은 부르는 쪽이 정한다.
        private LineRenderer AddFlash(string name, float width, Color color, float duration)
        {
            Flash flash = null;

            foreach (Flash candidate in _flashes)
            {
                if (candidate.Remaining <= 0)
                {
                    flash = candidate;
                    break;
                }
            }

            if (flash == null)
            {
                flash = new Flash { Line = CreateLine(name, width, color) };
                _flashes.Add(flash);
            }

            flash.Line.gameObject.name = name;
            flash.Line.widthMultiplier = width;
            flash.Line.transform.localPosition = Vector3.zero;
            flash.Line.enabled = true;
            flash.Color = color;
            flash.Duration = duration;
            flash.Remaining = duration;
            SetColor(flash.Line, color);
            return flash.Line;
        }

        private void AgeFlashes(float delta)
        {
            foreach (Flash flash in _flashes)
            {
                if (flash.Remaining <= 0)
                    continue;

                flash.Remaining -= delta;

                if (flash.Remaining <= 0)
                {
                    flash.Line.enabled = false;
                    continue;
                }

                Color color = flash.Color;
                color.a *= flash.Remaining / flash.Duration;
                SetColor(flash.Line, color);
            }
        }

        private LineRenderer CreateLine(string name, float width, Color color)
        {
            var line = new GameObject(name).AddComponent<LineRenderer>();
            line.transform.SetParent(_root, false);
            line.sharedMaterial = _material;
            line.useWorldSpace = false;
            line.widthMultiplier = width;
            line.sortingOrder = SortingOrder;
            SetColor(line, color);
            return line;
        }

        private static void SetCircle(LineRenderer line, float radius)
        {
            line.loop = true;
            line.positionCount = CircleSegments;

            for (int i = 0; i < CircleSegments; i++)
            {
                float angle = i * 2 * Mathf.PI / CircleSegments;
                line.SetPosition(i, new Vector3(radius * Mathf.Cos(angle), radius * Mathf.Sin(angle), 0));
            }
        }

        // 선분은 부모(원점) 기준 좌표로 그린다. 선의 위치는 원점에 둔다.
        private static void SetSegment(LineRenderer line, Point2 start, Point2 end)
        {
            line.loop = false;
            line.positionCount = 2;
            line.transform.localPosition = Vector3.zero;
            line.SetPosition(0, ToScene(start));
            line.SetPosition(1, ToScene(end));
        }

        private static void SetColor(LineRenderer line, Color color)
        {
            line.startColor = color;
            line.endColor = color;
        }

        private static Vector3 ToScene(Point2 point) => new Vector3(point.X, point.Y, 0);

        #endregion
    }
}
