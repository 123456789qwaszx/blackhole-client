using TMPro;
using UnityEngine;

namespace BlackHole.Unity
{
    // 전투 화면. 남은 시간을 받아 보여 준다.
    // 전투 Session을 모른다 — 표시 값은 ScreenFlow가 넘긴다. 진행 중인 판이 없으면 비어 있는 표시(ShowIdle)다.
    // 일시정지와 전투 시작·종료는 지금 전투 시작·종료 콘솔(개발용)에 있다.
    public sealed class BattleScreen : UIRoot<BattleScreen.Refs>
    {
        public enum Refs
        {
            RemainingText,
        }

        private TMP_Text _remaining;
        private int _shownTenths = -1;

        protected override void OnInitialize()
        {
            ScreenRefs.WarnMissing<Refs>(this);
            _remaining = View.Text(Refs.RemainingText);
        }

        // 진행 중인 판이 없을 때의 표시. 매 프레임 불러도 된다.
        public void ShowIdle()
        {
            if (_shownTenths != int.MinValue && _remaining != null)
            {
                _shownTenths = int.MinValue;
                _remaining.text = "-";
            }
        }

        // 매 프레임 불러도 된다. 보이는 값이 바뀔 때만 글자를 고친다.
        public void Show(float remainingSeconds)
        {
            int tenths = Mathf.CeilToInt(remainingSeconds * 10);

            if (tenths != _shownTenths && _remaining != null)
            {
                _shownTenths = tenths;
                _remaining.text = (tenths / 10f).ToString("F1");
            }
        }
    }
}
