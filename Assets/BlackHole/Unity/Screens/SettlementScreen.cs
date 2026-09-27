using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using BlackHole.Core;
using TMPro;
using UnityEngine.EventSystems;

namespace BlackHole.Unity
{
    // 결산 화면: 끝난 판의 결과(진행 시간, 블랙홀의 도달 Level, 처치 수, 번 Gold)와 결산 뒤의 진행 상태 Gold를 보여 주고, 계속하기를 알린다.
    // 보여 주기만 한다 — Gold는 이 화면이 열리기 전에 결산(GameSession.Settle)이 이미 더했다. ScreenFlow가 완료 사건에서 표시 값을 넘긴다.
    public sealed class SettlementScreen : UIRoot<SettlementScreen.Refs>
    {
        public enum Refs
        {
            ResultText,
            TimeText,
            KillsText,
            EarnedGoldText,
            TotalGoldText,
            ContinueBtn_Button,
        }

        public event Action ContinueClicked;

        private readonly StringBuilder _builder = new StringBuilder();
        private TMP_Text _result;
        private TMP_Text _time;
        private TMP_Text _kills;
        private TMP_Text _earned;
        private TMP_Text _total;

        protected override void OnInitialize()
        {
            ScreenRefs.WarnMissing<Refs>(this);

            _result = View.Text(Refs.ResultText);
            _time = View.Text(Refs.TimeText);
            _kills = View.Text(Refs.KillsText);
            _earned = View.Text(Refs.EarnedGoldText);
            _total = View.Text(Refs.TotalGoldText);

            BindEvent(View.Button(Refs.ContinueBtn_Button), HandleContinueClicked);
        }

        private void HandleContinueClicked(PointerEventData _) => ContinueClicked?.Invoke();

        public void ShowResult(float playedSeconds, int reachedLevel)
        {
            if (_result != null)
                _result.text = "Battle over";

            if (_time != null)
                _time.text = "Time  " + playedSeconds.ToString("F1", CultureInfo.InvariantCulture) + " s    Black hole  Lv "
                    + reachedLevel.ToString(CultureInfo.InvariantCulture);
        }

        // 처치 수 합계와 종류별 처치 수(처음 처치한 순서).
        public void ShowKills(int total, IReadOnlyList<EnemyKillCount> kills)
        {
            if (_kills == null)
                return;

            _builder.Clear();
            _builder.Append("Kills  ").Append(total.ToString("N0", CultureInfo.InvariantCulture));

            foreach (EnemyKillCount kill in kills)
                _builder.Append("\n  ").Append(kill.Enemy.Id).Append("  ").Append(kill.Count.ToString("N0", CultureInfo.InvariantCulture));

            _kills.text = _builder.ToString();
        }

        // earned: 이 판이 번 Gold. total: 결산을 마친 뒤 진행 상태의 Gold.
        public void ShowGold(long earned, long total)
        {
            if (_earned != null)
                _earned.text = "Earned  +" + earned.ToString("N0", CultureInfo.InvariantCulture) + " Gold";

            if (_total != null)
                _total.text = "Total  " + total.ToString("N0", CultureInfo.InvariantCulture) + " Gold";
        }
    }
}
