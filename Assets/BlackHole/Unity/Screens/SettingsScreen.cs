using System;

namespace BlackHole.Unity
{
    // 설정 화면. 설정 항목은 아직 없다(소리·화면 설정은 해당 시스템이 생길 때 더한다).
    public sealed class SettingsScreen : UIRoot<SettingsScreen.Refs>
    {
        public enum Refs
        {
            BackBtn_Button,
        }

        public event Action BackClicked;

        protected override void OnInitialize()
        {
            ScreenRefs.WarnMissing<Refs>(this);

            BindEvent(View.Button(Refs.BackBtn_Button), _ => BackClicked?.Invoke());
        }
    }
}
