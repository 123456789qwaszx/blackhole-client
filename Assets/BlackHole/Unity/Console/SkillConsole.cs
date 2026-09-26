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
    // 스킬 콘솔(개발용, 왼쪽 가운데). 콘텐츠의 스킬마다 한 줄: 켜기·끄기 버튼과 이름 버튼.
    // - 켜기·끄기: 고른 상태를 진행 중인 판의 로컬 참가자 스킬에 맞춘다. 고른 상태는 판 사이에 이어져, 새 판이 시작되면 그 판에도 맞춘다.
    //   끄면 돌던 주기와 예고 중인 발사를 버리고, 다시 켜면 처음부터 돈다(BreakerSkill·LaserSkill.SetEnabled).
    // - 이름: 그 스킬의 수치가 아래 설명창에 나온다. 같은 이름을 다시 누르면 닫힌다. 수치는 콘텐츠의 기본 수치다(업그레이드는 아직 잇지 않았다).
    // - 버프: 진행 중인 판에서 로컬 참가자의 Breaker에 붙은 처치 버프(공격 주기 감소·확정 치명타)의 남은 시간.
    // 새 판의 첫 Step 전에 맞추도록 GameHost가 이 콘솔을 전투 시스템보다 먼저 부른다(끈 스킬이 판 시작에 한 번 공격하지 않게).
    //
    // ` 키로 다른 콘솔 창과 함께 숨고 보인다. GameHost가 에디터와 개발 빌드에서만 만든다.
    internal sealed class SkillConsole : IDisposable
    {
        private const float ToggleWidth = 88;
        private const float NameWidth = 220;
        private static readonly Color OnColor = new Color(0.2f, 0.6f, 0.3f);
        private static readonly Color OffColor = new Color(0.35f, 0.35f, 0.35f);
        private static readonly Color DetailColor = new Color(0.04f, 0.06f, 0.12f, 0.85f);

        private readonly BattleSystem _battle;
        private readonly PlayerId _player;
        private readonly GameObject _canvas;
        private readonly List<Row> _rows = new List<Row>();
        private readonly GameObject _detailPanel;
        private readonly TMP_Text _detailText;
        private readonly TMP_Text _buffText;
        private Row _selected;
        // 버프 표시가 마지막으로 그린 값(0.1초 단위). 바뀔 때만 다시 쓴다.
        private int _shownHaste = -1;
        private int _shownCritical = -1;

        // 스킬 한 줄. 스킬 종류마다 켜짐을 읽고 바꾸는 방법만 다르다.
        private sealed class Row
        {
            public string Name;
            public string Stats;
            public bool On = true;
            public Button Toggle;
            public Func<BattlePlayer, bool?> IsEnabled;
            public Action<BattlePlayer, bool> SetEnabled;
        }

        public SkillConsole(Transform parent, GameContent content, BattleSystem battle, PlayerId player)
        {
            _battle = battle;
            _player = player;

            RectTransform canvas = CreateCanvas(parent, "Skill Console");
            _canvas = canvas.gameObject;

            // 콘솔 패널과 설명창을 왼쪽 가운데에 쌓는다(위·아래 모서리는 다른 콘솔이 쓴다).
            RectTransform stack = Stack(canvas, new Vector2(0, 0.5f));
            RectTransform panel = Panel(stack, "Panel", PanelColor);
            Text(panel, "Title", "SKILL CONSOLE  ( ` )", 22);

            if (content.Breaker != null)
            {
                BreakerDefinition breaker = content.Breaker;
                AddRow(panel, new Row
                {
                    Name = "Breaker",
                    Stats = $"Damage {Number(breaker.Damage)}\nInterval {Number(breaker.Interval)} s\nRadius {Number(breaker.Radius)}\n" +
                        $"Crit {Number(breaker.CritChance * 100)}% x{Number(breaker.CritMultiplier)}",
                    IsEnabled = p => p.Breaker?.Enabled,
                    SetEnabled = (p, on) => p.Breaker?.SetEnabled(on),
                });
            }

            if (content.Laser != null)
            {
                LaserDefinition laser = content.Laser;
                AddRow(panel, new Row
                {
                    Name = "Laser",
                    Stats = $"Damage {Number(laser.Damage)}\nInterval {Number(laser.Interval)} s\nWidth {Number(laser.Width)}\n" +
                        $"Telegraph {Number(laser.TelegraphDuration)} s\nBoundary {Number(laser.BoundaryRadius)}",
                    IsEnabled = p => p.Laser?.Enabled,
                    SetEnabled = (p, on) => p.Laser?.SetEnabled(on),
                });
            }

            if (_rows.Count == 0)
                Text(panel, "None", "No skills in content.", 20);

            _buffText = Text(panel, "Buffs", string.Empty, 20);
            ShowBuffs(0, 0);

            RectTransform detail = Panel(stack, "Detail", DetailColor);
            _detailPanel = detail.gameObject;
            _detailText = Text(detail, "Stats", string.Empty, 22);
            _detailPanel.SetActive(false);
        }

        public void Tick()
        {
            if (TogglePressed())
                _canvas.SetActive(!_canvas.activeSelf);

            Apply();

            BreakerSkill breaker = _battle.IsRunning ? _battle.Session.World.PlayerOf(_player).Breaker : null;
            ShowBuffs(breaker?.HasteRemaining ?? 0, breaker?.GuaranteedCriticalRemaining ?? 0);
        }

        public void Dispose() => Object.Destroy(_canvas);

        // 고른 켜짐 상태를 진행 중인 판의 로컬 참가자에게 맞춘다. 콘솔이 숨어 있어도 맞춘다.
        private void Apply()
        {
            if (!_battle.IsRunning)
                return;

            BattlePlayer player = _battle.Session.World.PlayerOf(_player);

            foreach (Row row in _rows)
            {
                if (row.IsEnabled(player) != row.On)
                    row.SetEnabled(player, row.On);
            }
        }

        private void AddRow(RectTransform panel, Row row)
        {
            RectTransform line = Child(panel, row.Name);
            HorizontalLayout(line, 8);
            row.Toggle = ButtonOf(line, "Toggle", string.Empty, ToggleWidth, () => Flip(row));
            ButtonOf(line, "Name", row.Name, NameWidth, () => Select(row));
            _rows.Add(row);
            ShowToggle(row);
        }

        private void Flip(Row row)
        {
            row.On = !row.On;
            ShowToggle(row);
            Apply();
        }

        private void Select(Row row)
        {
            _selected = _selected == row ? null : row;
            _detailPanel.SetActive(_selected != null);

            if (_selected != null)
                _detailText.text = $"{_selected.Name}\n{_selected.Stats}";
        }

        private void ShowBuffs(float haste, float critical)
        {
            int hasteTenths = Mathf.CeilToInt(haste * 10);
            int criticalTenths = Mathf.CeilToInt(critical * 10);

            if (hasteTenths == _shownHaste && criticalTenths == _shownCritical)
                return;

            _shownHaste = hasteTenths;
            _shownCritical = criticalTenths;
            string hasteText = hasteTenths > 0 ? $"{hasteTenths / 10f:0.0} s" : "-";
            string criticalText = criticalTenths > 0 ? $"{criticalTenths / 10f:0.0} s" : "-";
            _buffText.text = $"Breaker haste  {hasteText}\nBreaker always crit  {criticalText}";
        }

        private static void ShowToggle(Row row)
        {
            ((Image)row.Toggle.targetGraphic).color = row.On ? OnColor : OffColor;
            row.Toggle.GetComponentInChildren<TMP_Text>().text = row.On ? "ON" : "OFF";
        }
    }
}
