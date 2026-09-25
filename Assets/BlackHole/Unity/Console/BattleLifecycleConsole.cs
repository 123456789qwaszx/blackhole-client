using System;
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
    // 시작·종료 버튼은 오케스트레이터(BattleOrchestrator)에 요청할 뿐이다. 순서와 책임은 오케스트레이터에 있다.
    // 버튼 아래에는 적·전투 시스템의 시작·종료 체크리스트가, 그 아래에는 마지막 판의 원자료가 나온다.
    // 종료 사유는 지금 시간 종료로 통일한다.
    //
    // ` 키로 다른 콘솔 창과 함께 숨고 보인다. GameHost가 에디터와 개발 빌드에서만 만든다.
    internal sealed class BattleLifecycleConsole : IDisposable
    {
        // 버튼 너비. 창의 너비도 이것으로 정해진다(체크리스트와 원자료는 이 너비 안에서 줄을 나눠 쓴다).
        private const float ButtonWidth = 344;

        private readonly BattleOrchestrator _orchestrator;
        private readonly BattleSystem _battle;
        private readonly GameObject _canvas;
        private readonly Button _startButton;
        private readonly Button _endButton;
        private readonly TMP_Text _startStepsText;
        private readonly TMP_Text _endStepsText;
        private readonly TMP_Text _rawDataText;
        private readonly StringBuilder _builder = new StringBuilder();

        private int _shownStartVersion = -1;
        private int _shownEndVersion = -1;
        private BattleRawData _shownRawData;
        private bool _rawDataShown;

        public BattleLifecycleConsole(Transform parent, BattleOrchestrator orchestrator, BattleSystem battle)
        {
            _orchestrator = orchestrator;
            _battle = battle;

            RectTransform canvas = CreateCanvas(parent, "Battle Lifecycle Console");
            _canvas = canvas.gameObject;

            RectTransform panel = Panel(Stack(canvas, new Vector2(0, 1)), "Panel", PanelColor);
            Text(panel, "Title", "BATTLE START / END  ( ` )", 22);

            _startButton = ButtonOf(panel, "StartBattle", "Start battle", ButtonWidth, () => _orchestrator.RequestStart());
            _startStepsText = Text(panel, "StartSteps", string.Empty, 20);
            _endButton = ButtonOf(panel, "EndBattle", "End battle", ButtonWidth,
                () => _orchestrator.RequestEnd(SessionEndReason.TimeExpired));
            _endStepsText = Text(panel, "EndSteps", string.Empty, 20);
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

            if (_battle.StartSteps.Version != _shownStartVersion)
            {
                _shownStartVersion = _battle.StartSteps.Version;
                _startStepsText.text = Describe(_battle.StartSteps);
            }

            if (_battle.EndSteps.Version != _shownEndVersion)
            {
                _shownEndVersion = _battle.EndSteps.Version;
                _endStepsText.text = Describe(_battle.EndSteps);
            }

            BattleRawData raw = _battle.LastRawData;

            if (!_rawDataShown || raw != _shownRawData)
            {
                _rawDataShown = true;
                _shownRawData = raw;
                _rawDataText.text = Describe(raw);
            }
        }

        private string Describe(Checklist steps)
        {
            _builder.Clear();

            for (int i = 0; i < steps.Count; i++)
            {
                if (i > 0)
                    _builder.Append('\n');

                switch (steps.StateOf(i))
                {
                    case StepState.Done: _builder.Append("  <color=#7CFC7C>[x]</color> "); break;
                    case StepState.Failed: _builder.Append("  <color=#FF6B6B>[!]</color> "); break;
                    default: _builder.Append("  <color=#808080>[ ]</color> "); break;
                }

                _builder.Append(steps.NameOf(i));
            }

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
            _builder.Append("\n  Reason<pos=6em>").Append(raw.EndReason);
            _builder.Append("\n  Time<pos=6em>").Append(Number(raw.PlayedSeconds)).Append('s');
            _builder.Append("\n  Kills<pos=6em>").Append(raw.TotalKills);

            foreach (EnemyKillCount kill in raw.Kills)
                _builder.Append("\n    ").Append(kill.Enemy.Id).Append("<pos=9em>").Append(kill.Count);

            return _builder.ToString();
        }
    }
}
