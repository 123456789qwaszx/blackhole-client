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
    // 전투 시작·종료 콘솔(개발용). 조종 콘솔과 다른 창이다(왼쪽 위).
    //
    // 시작·일시정지·종료 버튼은 조립 때 받은 ScreenFlow의 콘솔 핸들(ScreenFlow.Editor)을 부른다.
    // 그 핸들은 같은 일을 하는 화면 버튼의 핸들로 가므로, 화면 버튼을 누른 것과 같은 길이다.
    // 일시정지 버튼은 판이 없거나 끝났으면 누를 수 없다.
    // 버튼 아래에는 진행 중인 판의 업그레이드 표, Gold와 마지막 판의 원자료가 나온다.
    // Gold는 두 줄이다: 진행 상태의 Gold(결산 때만 바뀐다)와, 진행 중인 판이 지금까지 번 Gold(적이 죽는 순간 오른다).
    // 진행 중인 판의 업그레이드 표(보는 참가자의 것) 중 산 노드가 바꾼 수치만 나온다:
    // 수치마다 기본값 0과 1일 때의 값이다(실제 기본값은 가져가는 시스템이 가진다).
    //
    // ` 키로 다른 콘솔 창과 함께 숨고 보인다. GameHost가 에디터와 개발 빌드에서만 만든다.
    internal sealed class BattleLifecycleConsole : IDisposable
    {
        // 버튼 너비. 창의 너비도 이것으로 정해진다(업그레이드 표와 원자료는 이 너비 안에서 줄을 나눠 쓴다).
        private const float ButtonWidth = 344;

        private readonly BattleOrchestrator _orchestrator;
        private readonly BattleSystem _battle;
        private readonly Action _startClicked;
        private readonly Action _pauseClicked;
        private readonly Action _endClicked;
        private readonly GameObject _canvas;
        private readonly Button _startButton;
        private readonly Button _pauseButton;
        private readonly TMP_Text _pauseLabel;
        private readonly Button _endButton;
        private readonly TMP_Text _goldText;
        private readonly TMP_Text _rawDataText;
        private readonly TMP_Text _upgradesText;
        private readonly PlayerId _viewer;
        private readonly SortedSet<string> _stats = new SortedSet<string>(StringComparer.Ordinal);
        private readonly StringBuilder _builder = new StringBuilder();

        private bool? _shownPaused;
        private long[] _shownGold;
        private long _shownEarned = -1;
        private BattleRawData _shownRawData;
        private bool _rawDataShown;
        private GameSession _shownSession;
        private bool _upgradesShown;

        public BattleLifecycleConsole(Transform parent, BattleOrchestrator orchestrator, BattleSystem battle,
            NodeTree nodes, PlayerId viewer, Action startClicked, Action pauseClicked, Action endClicked)
        {
            _orchestrator = orchestrator;
            _battle = battle;
            _startClicked = startClicked;
            _pauseClicked = pauseClicked;
            _endClicked = endClicked;
            _viewer = viewer;

            foreach (NodeDefinition node in nodes.Nodes)
            {
                foreach (Upgrade upgrade in node.Upgrades)
                    _stats.Add(upgrade.Stat);
            }

            RectTransform canvas = CreateCanvas(parent, "Battle Lifecycle Console");
            _canvas = canvas.gameObject;

            RectTransform panel = Panel(Stack(canvas, new Vector2(0, 1)), "Panel", PanelColor);
            Text(panel, "Title", "BATTLE START / END  ( ` )", 22);

            _startButton = ButtonOf(panel, "StartBattle", "Start battle", ButtonWidth, _startClicked);
            _upgradesText = Text(panel, "Upgrades", string.Empty, 20);
            _pauseButton = ButtonOf(panel, "PauseBattle", "Pause", ButtonWidth, _pauseClicked);
            _pauseLabel = _pauseButton.GetComponentInChildren<TMP_Text>();
            _endButton = ButtonOf(panel, "EndBattle", "End battle", ButtonWidth, _endClicked);
            _goldText = Text(panel, "Gold", string.Empty, 20);
            _rawDataText = Text(panel, "RawData", string.Empty, 20);

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
            SetInteractable(_startButton, _orchestrator.CanStart);
            SetInteractable(_endButton, _orchestrator.CanEnd);

            GameSession session = _battle.IsRunning ? _battle.Session : null;
            bool paused = session != null && session.Phase == SessionPhase.Paused;
            SetInteractable(_pauseButton, session != null && session.Phase != SessionPhase.Ended);

            if (paused != _shownPaused)
            {
                _shownPaused = paused;
                _pauseLabel.text = paused ? "Resume" : "Pause";
            }

            if (!_upgradesShown || _battle.Session != _shownSession)
            {
                _upgradesShown = true;
                _shownSession = _battle.Session;
                _upgradesText.text = DescribeUpgrades(_shownSession);
            }

            RefreshGold();

            BattleRawData raw = _battle.LastRawData;

            if (!_rawDataShown || raw != _shownRawData)
            {
                _rawDataShown = true;
                _shownRawData = raw;
                _rawDataText.text = Describe(raw);
            }
        }

        // 참가자마다 진행 상태의 Gold 한 줄, 판이 있으면 그 판이 번 Gold 한 줄. 값이 바뀐 프레임에만 다시 쓴다.
        private void RefreshGold()
        {
            IReadOnlyList<PlayerState> progress = _orchestrator.Progress;
            int count = progress?.Count ?? 0;
            long earned = _battle.Session != null ? _battle.Session.World.EarnedGold : -1;
            bool changed = _shownGold == null || _shownGold.Length != count || earned != _shownEarned;

            for (int i = 0; !changed && i < count; i++)
                changed = _shownGold[i] != progress[i].Gold;

            if (!changed)
                return;

            _shownGold = new long[count];
            _shownEarned = earned;
            _builder.Clear();
            _builder.Append(count == 0 ? "Gold  -" : "Gold");

            for (int i = 0; i < count; i++)
            {
                _shownGold[i] = progress[i].Gold;
                _builder.Append("\n  ").Append(progress[i].Id).Append("<pos=7em>").Append(_shownGold[i]);
            }

            if (earned >= 0)
                _builder.Append("\n  This battle<pos=7em>+").Append(earned);

            _goldText.text = _builder.ToString();
        }

        // 진행 중인 판이 보는 참가자에게 준 업그레이드 표. 산 노드가 바꾼 수치마다 "기본값 0일 때 / 1일 때"다.
        private string DescribeUpgrades(GameSession session)
        {
            if (session == null)
                return "Upgrades  -";

            _builder.Clear();
            _builder.Append("Upgrades (").Append(_viewer).Append(")  base 0 / base 1");
            UpgradeTable table = session.UpgradesOf(_viewer);
            bool any = false;

            foreach (string stat in _stats)
            {
                float fromZero = table.Apply(stat, 0);
                float fromOne = table.Apply(stat, 1);

                if (fromZero == 0 && fromOne == 1)
                    continue;

                any = true;
                _builder.Append("\n  ").Append(stat).Append("<pos=13em>")
                    .Append(Number(fromZero)).Append(" / ").Append(Number(fromOne));
            }

            if (!any)
                _builder.Append("\n  (none)");

            return _builder.ToString();
        }

        // 창이 옆으로 늘어나지 않게 항목마다 한 줄씩 쓴다.
        private string Describe(BattleRawData raw)
        {
            if (raw == null)
                return "Last battle  -";

            _builder.Clear();
            _builder.Append("Last battle");
            _builder.Append("\n  Stage<pos=6em>").Append(raw.Stage);
            _builder.Append("\n  Seed<pos=6em>").Append(raw.Seed);
            _builder.Append("\n  Time<pos=6em>").Append(Number(raw.PlayedSeconds)).Append('s');
            _builder.Append("\n  Gold<pos=6em>+").Append(raw.EarnedGold);
            _builder.Append("\n  Kills<pos=6em>").Append(raw.TotalKills);

            foreach (EnemyKillCount kill in raw.Kills)
                _builder.Append("\n    ").Append(kill.Enemy.Id).Append("<pos=9em>").Append(kill.Count);

            return _builder.ToString();
        }
    }
}
