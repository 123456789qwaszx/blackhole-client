namespace BlackHole.Unity
{
    // 개발용 콘솔 ↔ 화면 흐름. 콘솔 버튼은 같은 일을 하는 화면 버튼의 핸들로 간다.
    internal sealed partial class ScreenFlow
    {
        // 전투 시작·종료 콘솔: 업그레이드 화면의 Start battle, 전투 화면의 Pause·End battle과 같다.
        internal void HandleLifecycleStartBattleClicked() => HandleUpgradeStartBattleClicked();
        internal void HandleLifecyclePauseClicked() => HandleBattlePauseClicked();
        internal void HandleLifecycleEndBattleClicked() => HandleBattleEndClicked();

        // 업그레이드 콘솔: 진행 상태(Gold, 산 노드)를 바꿨다. 업그레이드 화면이 열려 있으면 다시 그린다.
        internal void HandleUpgradeConsoleProgressChanged() => RefreshUpgrade();
    }
}
