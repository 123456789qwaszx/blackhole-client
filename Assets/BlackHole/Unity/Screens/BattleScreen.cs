using System;
using TMPro;
using UnityEngine;

namespace BlackHole.Unity
{
    // 전투 화면. 남은 시간과 일시정지 여부를 받아 보여 주고, 일시정지·종료 버튼을 알린다.
    // 전투 Session을 모른다 — 표시 값은 ScreenFlow가 넘긴다.
    public sealed class BattleScreen : UIRoot<BattleScreen.Refs>
    {
        public enum Refs
        {
            RemainingText,
            PauseBtn_Button,
            PauseBtn_Text,
            EndBtn_Button,
        }

        public event Action PauseClicked;
        public event Action EndClicked;

        private TMP_Text _remaining;
        private TMP_Text _pauseLabel;
        private int _shownTenths = -1;
        private bool? _shownPaused;

        protected override void OnInitialize()
        {
            ScreenRefs.WarnMissing<Refs>(this);

            _remaining = View.Text(Refs.RemainingText);
            _pauseLabel = View.Text(Refs.PauseBtn_Text);

            BindEvent(View.Button(Refs.PauseBtn_Button), _ => PauseClicked?.Invoke());
            BindEvent(View.Button(Refs.EndBtn_Button), _ => EndClicked?.Invoke());
        }

        // 매 프레임 불러도 된다. 보이는 값이 바뀔 때만 글자를 고친다.
        public void Show(float remainingSeconds, bool paused)
        {
            int tenths = Mathf.CeilToInt(remainingSeconds * 10);

            if (tenths != _shownTenths && _remaining != null)
            {
                _shownTenths = tenths;
                _remaining.text = (tenths / 10f).ToString("F1");
            }

            if (paused != _shownPaused && _pauseLabel != null)
            {
                _shownPaused = paused;
                _pauseLabel.text = paused ? "Resume" : "Pause";
            }
        }
    }
}
