using System;
using System.Collections.Generic;
using BlackHole.Core;

namespace BlackHole.Unity
{
    // 화면 전환과, 화면이 전투 시스템·오케스트레이터·노드 트리와 만나는 경계.
    // 화면(View)은 버튼 사건을 알리고 표시 값을 받을 뿐이다.
    //
    // 화면은 업그레이드 화면과 전투 화면 둘이다. 화면은 전투 시스템의 상태를 따른다(UPGRADE_LINK_PLAN 4절):
    // 판이 돌고 있으면 전투 화면, 판이 없으면(정리까지 끝나면) 업그레이드 화면이다. 원작의 트리 → 전투 → 트리 루프다.
    // 전투의 시작과 정리는 화면이 아니라 오케스트레이터(BattleOrchestrator)가 순서대로 한다. 화면은 요청할 뿐이다.
    // 화면마다 partial 파일 하나가 전환과 사건 연결을 가진다. 연결은 화면이 닫힐 때 모두 푼다.
    internal sealed partial class ScreenFlow : IDisposable
    {
        private readonly UIManager _ui;
        private readonly BattleSystem _battle;
        private readonly BattleOrchestrator _orchestrator;
        private readonly NodeTree _tree;
        // 업그레이드 화면을 보는 참가자(지금은 로컬 Player 1명).
        private readonly PlayerState _player;
        private readonly UIPresentationSpec _battlePresentation;
        private readonly UIPresentationSpec _upgradePresentation;
        // 노드 ID → 격자 칸. 표시용이라 노드 트리(규칙)가 아니라 저작 데이터에서 읽는다.
        private readonly Dictionary<string, (int X, int Y)> _cells = new Dictionary<string, (int X, int Y)>(StringComparer.Ordinal);

        public ScreenFlow(
            UIManager ui,
            BattleSystem battle,
            BattleOrchestrator orchestrator,
            NodeTree tree,
            NodeTreeData layout,
            PlayerState player,
            UIPresentationSpec battlePresentation,
            UIPresentationSpec upgradePresentation)
        {
            _ui = ui;
            _battle = battle;
            _orchestrator = orchestrator;
            _tree = tree;
            _player = player;
            _battlePresentation = battlePresentation;
            _upgradePresentation = upgradePresentation;

            foreach (NodeData node in layout.Nodes)
            {
                if (node?.Id != null && !_cells.ContainsKey(node.Id))
                    _cells.Add(node.Id, (node.X, node.Y));
            }
        }

        // 한 프레임. 전투 시스템의 상태에 맞는 화면을 연 뒤, 열린 화면의 표시 값을 맞춘다.
        public void Tick()
        {
            FollowBattle();
            ShowBattle();
            TickUpgrade();
        }

        // 시작·정리 중(Starting, ShuttingDown)이나 정리 실패(Faulted)에는 지금 화면을 그대로 둔다.
        private void FollowBattle()
        {
            if (_battle.IsRunning && _battleScreen == null)
                OpenBattleScreen();
            else if (_battle.IsIdle && _upgradeScreen == null)
                OpenUpgradeScreen();
        }

        #region 연결

        private readonly Dictionary<UIBase, List<Action>> _cleanupByScreen = new Dictionary<UIBase, List<Action>>();

        private void BindView<T>(T screen, Action<T> apply) where T : UIBase
        {
            Unbind(screen);
            apply(screen);
        }

        private void AddBinding<T>(T screen, Action<T> attach, Action<T> detach) where T : UIBase
        {
            attach(screen);
            AddCleanup(screen, () => detach(screen));
        }

        private void AddCleanup(UIBase screen, Action cleanup)
        {
            if (!_cleanupByScreen.TryGetValue(screen, out List<Action> cleanups))
            {
                cleanups = new List<Action>();
                _cleanupByScreen[screen] = cleanups;
            }

            cleanups.Add(cleanup);
        }

        private void Unbind(UIBase screen)
        {
            if (!_cleanupByScreen.TryGetValue(screen, out List<Action> cleanups))
                return;

            _cleanupByScreen.Remove(screen);
            RunCleanups(cleanups);
        }

        private static void RunCleanups(List<Action> cleanups)
        {
            for (int i = cleanups.Count - 1; i >= 0; i--)
                cleanups[i]?.Invoke();
        }

        public void Dispose()
        {
            foreach (List<Action> cleanups in _cleanupByScreen.Values)
                RunCleanups(cleanups);

            _cleanupByScreen.Clear();
        }

        #endregion
    }
}
