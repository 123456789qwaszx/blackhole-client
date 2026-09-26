using System;
using System.Collections.Generic;
using BlackHole.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BlackHole.Unity
{
    // 스킬의 화면. 매 프레임 판의 참가자를 읽어 그린다. 게임 상태를 바꾸지 않고, 피해를 다시 계산하지 않는다.
    // - 범위 원: 참가자의 조준점에 Breaker 반지름으로 늘 그린다. 판정과 같은 값이다(GAME_RULES 6절). 조준점이 없으면 숨긴다.
    // - Tick 원: Breaker Tick 기록마다 한 번 굵게 그렸다가 옅어진다. 빈 Tick(조준점 없음)은 그리지 않는다.
    //   정지 중에는 판이 기록을 비우지 않으므로, 이미 그린 Tick은 번호로 걸러 두 번 그리지 않는다.
    // 규칙 평면은 장면의 z = 0이고 x·y는 같다(EnemyView와 같다).
    internal sealed class SkillView : IDisposable
    {
        private const int CircleSegments = 48;
        private const int SortingOrder = 10;
        private const float RangeWidth = 0.03f;
        private const float PulseWidth = 0.08f;
        private const float PulseSeconds = 0.3f;
        private static readonly Color RangeColor = new Color(0.3f, 1f, 0.55f, 0.45f);
        private static readonly Color PulseColor = new Color(0.3f, 1f, 0.55f, 1f);

        private readonly Transform _root;
        private readonly Material _material;
        private readonly Dictionary<PlayerId, LineRenderer> _ranges = new Dictionary<PlayerId, LineRenderer>();
        private readonly Dictionary<PlayerId, int> _drawnTicks = new Dictionary<PlayerId, int>();
        private readonly List<Pulse> _pulses = new List<Pulse>();

        private sealed class Pulse
        {
            public LineRenderer Line;
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
            AgePulses(delta);
            IReadOnlyList<BattlePlayer> players = world.Players;

            // 매 프레임 경로: IReadOnlyList를 인덱스로 돈다(인터페이스 foreach는 열거자를 할당한다).
            for (int i = 0; i < players.Count; i++)
            {
                BattlePlayer player = players[i];
                BreakerSkill breaker = player.Breaker;

                if (breaker == null)
                    continue;

                ShowRange(player.Id, player.AimPoint, breaker.Definition.Radius);
                _drawnTicks.TryGetValue(player.Id, out int drawn);
                IReadOnlyList<BreakerTick> ticks = breaker.Ticks;

                for (int j = 0; j < ticks.Count; j++)
                {
                    BreakerTick tick = ticks[j];

                    if (tick.Number <= drawn)
                        continue;

                    drawn = tick.Number;

                    if (tick.Center.HasValue)
                        AddPulse(tick.Center.Value, tick.Radius);
                }

                _drawnTicks[player.Id] = drawn;
            }
        }

        // 그리는 선이 없고, 지운 객체도 장면에서 모두 사라졌는가.
        // 지운 객체는 프레임 끝에 사라지므로, Reset 뒤 한 프레임이 지나야 true가 된다.
        public bool IsClear => _ranges.Count == 0 && _pulses.Count == 0 && _root.childCount == 0;

        // 판이 바뀌거나 판을 정리할 때 모든 선을 지운다.
        public void Reset()
        {
            foreach (LineRenderer range in _ranges.Values)
                Object.Destroy(range.gameObject);

            foreach (Pulse pulse in _pulses)
                Object.Destroy(pulse.Line.gameObject);

            _ranges.Clear();
            _pulses.Clear();
            _drawnTicks.Clear();
        }

        public void Dispose()
        {
            Object.Destroy(_root.gameObject);
            Object.Destroy(_material);
        }

        private void ShowRange(PlayerId player, Point2? aim, float radius)
        {
            if (!_ranges.TryGetValue(player, out LineRenderer range))
            {
                range = CreateCircle($"Breaker Range ({player})", radius, RangeWidth, RangeColor);
                _ranges.Add(player, range);
            }

            range.enabled = aim.HasValue;

            if (aim.HasValue)
                range.transform.localPosition = new Vector3(aim.Value.X, aim.Value.Y, 0);
        }

        private void AddPulse(Point2 center, float radius)
        {
            Pulse pulse = null;

            foreach (Pulse candidate in _pulses)
            {
                if (candidate.Remaining <= 0)
                {
                    pulse = candidate;
                    break;
                }
            }

            if (pulse == null)
            {
                pulse = new Pulse { Line = CreateCircle("Breaker Tick", radius, PulseWidth, PulseColor) };
                _pulses.Add(pulse);
            }

            SetCircle(pulse.Line, radius);
            pulse.Line.transform.localPosition = new Vector3(center.X, center.Y, 0);
            pulse.Line.enabled = true;
            pulse.Remaining = PulseSeconds;
            SetColor(pulse.Line, PulseColor);
        }

        private void AgePulses(float delta)
        {
            foreach (Pulse pulse in _pulses)
            {
                if (pulse.Remaining <= 0)
                    continue;

                pulse.Remaining -= delta;

                if (pulse.Remaining <= 0)
                {
                    pulse.Line.enabled = false;
                    continue;
                }

                Color color = PulseColor;
                color.a *= pulse.Remaining / PulseSeconds;
                SetColor(pulse.Line, color);
            }
        }

        private LineRenderer CreateCircle(string name, float radius, float width, Color color)
        {
            var line = new GameObject(name).AddComponent<LineRenderer>();
            line.transform.SetParent(_root, false);
            line.sharedMaterial = _material;
            line.useWorldSpace = false;
            line.loop = true;
            line.widthMultiplier = width;
            line.sortingOrder = SortingOrder;
            SetCircle(line, radius);
            SetColor(line, color);
            return line;
        }

        private static void SetCircle(LineRenderer line, float radius)
        {
            line.positionCount = CircleSegments;

            for (int i = 0; i < CircleSegments; i++)
            {
                float angle = i * 2 * Mathf.PI / CircleSegments;
                line.SetPosition(i, new Vector3(radius * Mathf.Cos(angle), radius * Mathf.Sin(angle), 0));
            }
        }

        private static void SetColor(LineRenderer line, Color color)
        {
            line.startColor = color;
            line.endColor = color;
        }
    }
}
