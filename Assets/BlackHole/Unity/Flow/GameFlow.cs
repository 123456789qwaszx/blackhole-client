using System;
using System.Collections.Generic;
using BlackHole.Core;

namespace BlackHole.Unity
{
    // 한 판의 상태에 맞는 화면을 선택한다. 버튼 명령은 ScreenFlow가 필요한 시스템에 직접 전달한다.
    internal sealed class GameFlow : IDisposable
    {
        private readonly ScreenFlow _screens;
        private readonly BattleSystem _battle;
        private readonly BattleOrchestrator _orchestrator;
        private readonly NodeTree _tree;
        private readonly PlayerState _player;
        private readonly SettlementState _settlement;
        private readonly IReadOnlyList<NodeTreeView.NodeItem> _nodes;

        public GameFlow(ScreenFlow screens, BattleSystem battle, BattleOrchestrator orchestrator,
            NodeTree tree, NodeTreeData layout, PlayerState player, SettlementState settlement)
        {
            _screens = screens;
            _battle = battle;
            _orchestrator = orchestrator;
            _tree = tree;
            _player = player;
            _settlement = settlement;

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
        }

        public void Tick()
        {
            FollowBattle();
            ShowBattle();
            _screens.RefreshUpgrade();
        }

        // 시작·정리 중에는 기존 화면을 유지한다. 실패 시에도 기존 화면을 유지한다.
        // 개발용 콘솔이 결산 중 새 판을 시작했다면, 이전 결산 대기를 버리고 새 판을 보여 준다.
        private void FollowBattle()
        {
            if (_orchestrator.Busy)
                return;

            if (_battle.IsRunning)
            {
                _settlement.Clear();
                if (!_screens.IsBattleOpen)
                    _screens.GoToBattle();
            }
            else if (_settlement.Pending != null)
            {
                if (!_screens.IsSettlementOpen)
                {
                    BattleRawData raw = _settlement.Pending;
                    _screens.GoToSettlement(raw.EndReason, raw.PlayedSeconds, raw.TotalKills,
                        raw.Kills, raw.EarnedGold, _player.Gold);
                }
            }
            else if (_battle.IsIdle && !_screens.IsUpgradeOpen)
            {
                _screens.GoToUpgrade(_nodes, _tree.Graph.Links);
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

        private void OnBattleCompleted(BattleRawData raw) => _settlement.Set(raw);

        public void Dispose()
        {
            _orchestrator.BattleCompleted -= OnBattleCompleted;
        }
    }

    // 결산 확인 상태를 화면 선택과 Continue 버튼이 공유한다. 결산 계산은 BattleSystem이 끝낸다.
    internal sealed class SettlementState
    {
        public BattleRawData Pending { get; private set; }
        public void Set(BattleRawData raw) => Pending = raw;
        public void Clear() => Pending = null;
    }
}
