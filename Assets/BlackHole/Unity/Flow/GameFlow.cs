using System;
using System.Collections.Generic;
using BlackHole.Core;

namespace BlackHole.Unity
{
    // 한 판의 화면 전환 조건과 UI 명령을 소유한다. ScreenFlow에는 표시 값과 클릭만 전달한다.
    internal sealed class GameFlow : IDisposable
    {
        private readonly ScreenFlow _screens;
        private readonly BattleSystem _battle;
        private readonly BattleOrchestrator _orchestrator;
        private readonly NodeTree _tree;
        private readonly PlayerState _player;
        private readonly IReadOnlyList<NodeTreeView.NodeItem> _nodes;
        private BattleRawData _pendingSettlement;
        private long _shownGold;
        private int _shownOwned;

        public GameFlow(ScreenFlow screens, BattleSystem battle, BattleOrchestrator orchestrator,
            NodeTree tree, NodeTreeData layout, PlayerState player)
        {
            _screens = screens;
            _battle = battle;
            _orchestrator = orchestrator;
            _tree = tree;
            _player = player;

            var cells = new Dictionary<string, (int X, int Y)>(StringComparer.Ordinal);
            foreach (NodeData node in layout.Nodes)
            {
                if (node?.Id != null && !cells.ContainsKey(node.Id))
                    cells.Add(node.Id, (node.X, node.Y));
            }

            var nodes = new List<NodeTreeView.NodeItem>(tree.Nodes.Count);
            foreach (NodeDefinition node in tree.Nodes)
            {
                (int x, int y) = cells.TryGetValue(node.Id, out (int X, int Y) cell) ? cell : (0, 0);
                nodes.Add(new NodeTreeView.NodeItem(node.Id, x, y, node.Price));
            }
            _nodes = nodes;

            _orchestrator.BattleCompleted += OnBattleCompleted;
            _screens.PauseClicked += HandleBattlePauseClicked;
            _screens.EndClicked += HandleBattleEndClicked;
            _screens.NodeClicked += HandleUpgradeNodeClicked;
            _screens.StartBattleClicked += HandleUpgradeStartBattleClicked;
            _screens.ContinueClicked += HandleSettlementContinueClicked;
        }

        public void Tick()
        {
            FollowBattle();
            ShowBattle();
            TickUpgrade();
        }

        // 시작·정리 중에는 기존 화면을 유지한다. 실패 시에도 기존 화면을 유지한다.
        // 개발용 콘솔이 결산 중 새 판을 시작했다면, 이전 결산 대기를 버리고 새 판을 보여 준다.
        private void FollowBattle()
        {
            if (_orchestrator.Busy)
                return;

            if (_battle.IsRunning)
            {
                _pendingSettlement = null;
                if (!_screens.IsBattleOpen)
                    _screens.GoToBattle();
            }
            else if (_pendingSettlement != null)
            {
                if (!_screens.IsSettlementOpen)
                {
                    BattleRawData raw = _pendingSettlement;
                    _screens.GoToSettlement(raw.EndReason, raw.PlayedSeconds, raw.TotalKills,
                        raw.Kills, raw.EarnedGold, _player.Gold);
                }
            }
            else if (_battle.IsIdle && !_screens.IsUpgradeOpen)
            {
                _screens.GoToUpgrade(_nodes, _tree.Graph.Links);
                ShowUpgrade();
            }
        }

        private void ShowBattle()
        {
            if (!_screens.IsBattleOpen)
                return;

            GameSession session = _battle.Session;
            if (session == null)
                _screens.ShowBattleIdle();
            else
                _screens.ShowBattle(session.Remaining, session.World.EarnedGold, session.Phase == SessionPhase.Paused);
        }

        private void TickUpgrade()
        {
            if (_screens.IsUpgradeOpen && (_player.Gold != _shownGold || _player.OwnedNodes.Count != _shownOwned))
                ShowUpgrade();
        }

        private void ShowUpgrade()
        {
            if (!_screens.IsUpgradeOpen)
                return;

            _shownGold = _player.Gold;
            _shownOwned = _player.OwnedNodes.Count;
            _screens.ShowUpgrade(_shownGold, _shownOwned, _tree.Nodes.Count,
                id => NodePurchase.StateOf(_player, _tree, id));
        }

        private void OnBattleCompleted(BattleRawData raw) => _pendingSettlement = raw;
        private void HandleBattlePauseClicked() => _battle.TogglePause();
        private void HandleBattleEndClicked() => _orchestrator.RequestEnd(SessionEndReason.TimeExpired);
        private void HandleUpgradeStartBattleClicked() => _orchestrator.RequestStart();
        private void HandleSettlementContinueClicked() => _pendingSettlement = null;

        private void HandleUpgradeNodeClicked(string id)
        {
            NodePurchase.TryPurchase(_player, _tree, id);
            ShowUpgrade();
        }

        public void Dispose()
        {
            _orchestrator.BattleCompleted -= OnBattleCompleted;
            _screens.PauseClicked -= HandleBattlePauseClicked;
            _screens.EndClicked -= HandleBattleEndClicked;
            _screens.NodeClicked -= HandleUpgradeNodeClicked;
            _screens.StartBattleClicked -= HandleUpgradeStartBattleClicked;
            _screens.ContinueClicked -= HandleSettlementContinueClicked;
        }
    }
}
