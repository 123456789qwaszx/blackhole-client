using System;
using System.Globalization;
using TMPro;
using UnityEngine.EventSystems;

namespace BlackHole.Unity
{
    // 업그레이드 화면(플레이어가 보는 노드 트리, F02의 임시 모양). Gold, 산 노드 수, 전투 시작 버튼을 가진다.
    // 노드 트리의 규칙을 모른다 — ScreenFlow가 표시 값을 넘기고, 전투 시작은 사건으로 알린다.
    // 트리 보기(NodeTreeView)는 이 화면의 프리팹 안에 있지만 화면 클래스는 모른다. GameBootstrap이 찾아 ScreenFlow에 넘긴다.
    public sealed class UpgradeScreen : UIRoot<UpgradeScreen.Refs>
    {
        public enum Refs
        {
            GoldText,
            ProgressText,
            StartBattleBtn_Button,
        }

        public event Action StartBattleClicked;

        private TMP_Text _gold;
        private TMP_Text _progress;

        protected override void OnInitialize()
        {
            ScreenRefs.WarnMissing<Refs>(this);

            _gold = View.Text(Refs.GoldText);
            _progress = View.Text(Refs.ProgressText);

            BindEvent(View.Button(Refs.StartBattleBtn_Button), HandleStartBattleClicked);
        }

        private void HandleStartBattleClicked(PointerEventData _) => StartBattleClicked?.Invoke();

        public void ShowGold(long gold)
        {
            if (_gold != null)
                _gold.text = "Gold " + gold.ToString("N0", CultureInfo.InvariantCulture);
        }

        public void ShowProgress(int owned, int total)
        {
            if (_progress != null)
                _progress.text = $"{owned} / {total} nodes";
        }
    }
}
