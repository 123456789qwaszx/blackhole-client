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
    // 블랙홀 EXP 버튼(+100·+1K·+10K)은 진행 중인 판의 블랙홀에 EXP를 더한다(BattleCheats). 판 Level·목표·이정표 시험용이며 판 중에만 누를 수 있다.
    // 버튼 아래에는 진행 중인 판의 업그레이드 표, Gold와 마지막 판의 원자료가 나온다.
    // Gold는 두 줄이다: 진행 상태의 Gold(결산 때만 바뀐다)와, 진행 중인 판이 지금까지 번 Gold(적이 죽는 순간 오른다).
    // 진행 중인 판의 업그레이드 표(방장의 산 노드) 중 산 노드가 바꾼 수치만 나온다:
    // 수치마다 기본값 0과 1일 때의 값이다(실제 기본값은 가져가는 시스템이 가진다).
    //
    // ` 키로 다른 콘솔 창과 함께 숨고 보인다. GameHost가 에디터와 개발 빌드에서만 만든다.
    internal sealed class BattleLifecycleConsole : IDisposable
    {
        // 버튼 너비. 창의 너비도 이것으로 정해진다(업그레이드 표와 원자료는 이 너비 안에서 줄을 나눠 쓴다).
        private const float ButtonWidth = 344;
        // 블랙홀 EXP 버튼(한 줄에 셋): 판을 치르며 판 Level·목표·이정표를 시험한다. 진행 중이거나 정지한 판에서만 누를 수 있다.
        private const float ExpButtonWidth = 110;
        private static readonly (string Label, long Amount)[] ExpSteps = { ("100", 100L), ("1K", 1_000L), ("10K", 10_000L) };

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
        private readonly List<Button> _expButtons = new List<Button>();
        private readonly TMP_Text _rawDataText;
        private readonly TMP_Text _upgradesText;
        private readonly SortedSet<string> _stats = new SortedSet<string>(StringComparer.Ordinal);
        private readonly StringBuilder _builder = new StringBuilder();

        private bool? _shownPaused;
        private long _shownGold = -1;
        private long _shownEarned = -1;
        private long _shownExp = -1;
        private int _shownLevel = -1;
        private BattleRawData _shownRawData;
        private bool _rawDataShown;
        private GameSession _shownSession;
        private bool _upgradesShown;

        public BattleLifecycleConsole(Transform parent, BattleOrchestrator orchestrator, BattleSystem battle,
            NodeTree nodes, Action startClicked, Action pauseClicked, Action endClicked)
        {
            _orchestrator = orchestrator;
            _battle = battle;
            _startClicked = startClicked;
            _pauseClicked = pauseClicked;
            _endClicked = endClicked;

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

            RectTransform exp = Child(panel, "HqExp");
            HorizontalLayout(exp, 7);

            foreach ((string label, long amount) in ExpSteps)
                _expButtons.Add(ButtonOf(exp, "HqExp" + label, "EXP +" + label, ExpButtonWidth, () => AddHqExp(amount)));

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

        // 진행 중인 판의 블랙홀에 EXP를 더한다(개발용 전투 치트). Level은 다음 Step에서 오른다.
        private void AddHqExp(long amount)
        {
            GameSession session = _battle.IsRunning ? _battle.Session : null;

            if (session != null && session.Phase != SessionPhase.Ended)
                BattleCheats.AddHqExp(session, amount);
        }

        private void Refresh()
        {
            SetInteractable(_startButton, _orchestrator.CanStart);
            SetInteractable(_endButton, _orchestrator.CanEnd);

            GameSession session = _battle.IsRunning ? _battle.Session : null;
            bool paused = session != null && session.Phase == SessionPhase.Paused;
            SetInteractable(_pauseButton, session != null && session.Phase != SessionPhase.Ended);

            foreach (Button button in _expButtons)
                SetInteractable(button, session != null && session.Phase != SessionPhase.Ended);

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

        // 진행 상태(방장의 것)의 Gold 한 줄, 판이 있으면 그 판이 번 Gold 한 줄과 블랙홀 한 줄
        // (성장도, Level / 목표 Level, EXP / 다음 임계값, Level업마다 늘어나는 시간). 값이 바뀐 프레임에만 다시 쓴다.
        private void RefreshGold()
        {
            PlayerState progress = _orchestrator.Progress;
            Hq hq = _battle.Session?.World.Hq;
            long earned = _battle.Session != null ? _battle.Session.World.EarnedGold : -1;
            long exp = hq != null ? hq.Exp : -1;
            int level = hq != null ? hq.Level : -1;

            if (progress.Gold == _shownGold && earned == _shownEarned && exp == _shownExp && level == _shownLevel)
                return;

            _shownGold = progress.Gold;
            _shownEarned = earned;
            _shownExp = exp;
            _shownLevel = level;
            _builder.Clear();
            _builder.Append("Gold");
            _builder.Append("\n  ").Append(progress.Id).Append("<pos=7em>").Append(_shownGold);

            if (earned >= 0)
                _builder.Append("\n  This battle<pos=7em>+").Append(earned);

            if (hq != null)
            {
                _builder.Append("\nHq  Stage ").Append(hq.Stage).Append("  Lv ").Append(hq.Level);
                _builder.Append(hq.GoalLevel > 0 ? " / goal " + hq.GoalLevel : " (no goal)");
                _builder.Append("\n  EXP ").Append(hq.Exp);
                _builder.Append(hq.NextLevelExp.HasValue ? " / " + hq.NextLevelExp.Value : " (max)");
                _builder.Append("  +").Append(Number(hq.GrowthTime)).Append("s per level");
            }

            _goldText.text = _builder.ToString();
        }

        // 진행 중인 판의 업그레이드 표(방장의 산 노드). 산 노드가 바꾼 수치마다 "기본값 0일 때 / 1일 때"다.
        private string DescribeUpgrades(GameSession session)
        {
            if (session == null)
                return "Upgrades  -";

            _builder.Clear();
            _builder.Append("Upgrades  base 0 / base 1");
            UpgradeTable table = session.Upgrades;
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
            _builder.Append("\n  Seed<pos=6em>").Append(raw.Seed);
            _builder.Append("\n  Time<pos=6em>").Append(Number(raw.PlayedSeconds)).Append('s');
            _builder.Append("\n  Gold<pos=6em>+").Append(raw.EarnedGold);

            if (raw.ReachedMilestone)
                _builder.Append("\n  Milestone<pos=6em>Stage ").Append(raw.Milestone.Stage)
                    .Append("  settled +").Append(raw.SettledGold);
            _builder.Append("\n  Hq<pos=6em>Lv ").Append(raw.ReachedLevel).Append("  EXP ").Append(raw.Exp);
            _builder.Append("\n  Stage<pos=6em>").Append(raw.Stage).Append(" -> ").Append(raw.NextStage);
            _builder.Append("\n  Kills<pos=6em>").Append(raw.TotalKills);

            foreach (EnemyKillCount kill in raw.Kills)
                _builder.Append("\n    ").Append(kill.Enemy.Id).Append("<pos=9em>").Append(kill.Count);

            return _builder.ToString();
        }
    }
}
