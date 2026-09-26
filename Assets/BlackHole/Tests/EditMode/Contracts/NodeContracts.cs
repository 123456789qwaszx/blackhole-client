using System;
using System.Collections.Generic;
using BlackHole.Core;

namespace BlackHole.Core.Tests
{
    // 노드 트리: 시작 노드와 선으로 드러나고, 전투 밖에서 Gold로 산다. 산 노드의 업그레이드는 UpgradeTable이 된다.
    internal static class NodeContracts
    {
        public static IEnumerable<Contract> Cases()
        {
            yield return new Contract("Node.RevealedByStartOrAnyOwnedNeighbor", RevealedByStartOrAnyOwnedNeighbor);
            yield return new Contract("Node.FailedPurchaseChangesNothing", FailedPurchaseChangesNothing);
            yield return new Contract("Node.CannotPurchaseDuringBattle", CannotPurchaseDuringBattle);
            yield return new Contract("Node.OwnedNodesBecomeTheUpgradeTable", OwnedNodesBecomeTheUpgradeTable);
            yield return new Contract("Node.LoaderReportsBrokenNodesAndLinks", LoaderReportsBrokenNodesAndLinks);
            yield return new Contract("Node.EveryNodeIsReachableFromAStartNode", EveryNodeIsReachableFromAStartNode);
        }

        // 시작 노드는 처음부터 드러나고, 나머지는 산 이웃이 하나라도 있으면 드러난다.
        // 선은 방향이 없어 한쪽에만 적어도 양쪽을 잇고, 순환(a-b-d-c 묶음)도 된다.
        private static void RevealedByStartOrAnyOwnedNeighbor()
        {
            NodeTree tree = Load(
                Node("s", 1, true, "a"),
                Node("a", 1, false, "b", "c"),
                Node("b", 1),
                Node("c", 1),
                Node("d", 50, false, "b", "c", "far"),
                Node("far", 1));
            var state = new PlayerState(TestContent.First);
            state.EarnGold(10);

            Expect.Equal(NodeState.Purchasable, tree.StateOf(state, "s"));
            Expect.Equal(NodeState.Hidden, tree.StateOf(state, "a"));

            tree.TryPurchase(state, "s");
            Expect.Equal(NodeState.Owned, tree.StateOf(state, "s"));
            Expect.Equal(NodeState.Purchasable, tree.StateOf(state, "a"));

            tree.TryPurchase(state, "a");
            tree.TryPurchase(state, "b");
            // d의 이웃 가운데 b만 샀다. Gold가 모자라도 보이기는 한다.
            Expect.Equal(NodeState.Revealed, tree.StateOf(state, "d"));
            Expect.Equal(NodeState.Purchasable, tree.StateOf(state, "c"));
            Expect.Equal(NodeState.Hidden, tree.StateOf(state, "far"));
        }

        // 실패한 구매는 Gold와 산 노드를 바꾸지 않는다. 화면·콘솔은 노드 ID로 요청한다.
        private static void FailedPurchaseChangesNothing()
        {
            NodeTree tree = Load(Node("s", 10, true, "a"), Node("a", 10));
            var state = new PlayerState(TestContent.First);
            state.EarnGold(15);

            Expect.Equal(PurchaseResult.Hidden, tree.TryPurchase(state, "a"));
            Expect.Equal(PurchaseResult.UnknownNode, tree.TryPurchase(state, "nope"));
            Expect.Equal(15L, state.Gold);

            Expect.Equal(PurchaseResult.Purchased, tree.TryPurchase(state, "s"));
            Expect.Equal(5L, state.Gold);

            Expect.Equal(PurchaseResult.AlreadyOwned, tree.TryPurchase(state, "s"));
            Expect.Equal(PurchaseResult.NotEnoughGold, tree.TryPurchase(state, "a"));
            Expect.Equal(5L, state.Gold);
            Expect.True(!state.Owns("a"), "Gold가 모자라면 사면 안 된다.");
            Expect.Equal(1, state.OwnedNodes.Count);
        }

        // 전투에 들어가 있는 동안에는 살 수 없다. 전투가 끝나면 다시 살 수 있다.
        private static void CannotPurchaseDuringBattle()
        {
            GameContent content = TestContent.Load(TestContent.Data());
            NodeTree tree = Load(Node("s", 1, true));
            var state = new PlayerState(TestContent.First);
            state.EarnGold(5);

            GameSession battle = SessionAssembler.CreateBattle(content, new[] { state });
            Expect.Equal(PurchaseResult.InBattle, tree.TryPurchase(state, "s"));
            Expect.Equal(NodeState.Revealed, tree.StateOf(state, "s"));
            Expect.Equal(5L, state.Gold);

            battle.RequestEnd(SessionEndReason.TimeExpired);
            Expect.Equal(PurchaseResult.Purchased, tree.TryPurchase(state, "s"));
        }

        // 산 노드의 업그레이드만 모인다. 사지 않은 노드의 업그레이드는 섞이지 않는다.
        private static void OwnedNodesBecomeTheUpgradeTable()
        {
            NodeTree tree = Load(
                Grants(Node("s", 1, true, "a", "b"), Up("damage", UpgradeOperation.Add, 2)),
                Grants(Node("a", 1), Up("damage", UpgradeOperation.Percent, 0.5f)),
                Grants(Node("b", 1), Up("damage", UpgradeOperation.Multiply, 10)));
            var state = new PlayerState(TestContent.First);
            state.EarnGold(10);

            Expect.Equal(10f, tree.UpgradesFor(state).Apply("damage", 10));

            tree.TryPurchase(state, "s");
            tree.TryPurchase(state, "a");

            // (10 + 2) × 1.5
            Expect.Near(18f, tree.UpgradesFor(state).Apply("damage", 10));
        }

        // 노드 하나의 오류(ID, 가격, 업그레이드)와 노드 사이의 오류(중복 ID, 선)를 경로와 함께 모두 보고한다.
        private static void LoaderReportsBrokenNodesAndLinks()
        {
            NodeTreeLoadResult nodes = NodeTreeLoader.Load(Tree(
                Node("", 1, true),
                Node("free", 0),
                Grants(Node("nan", 1), Up("damage", UpgradeOperation.Add, float.NaN)),
                Grants(Node("nameless", 1), Up(" ", UpgradeOperation.Add, 1))));

            Expect.True(!nodes.Succeeded, "잘못된 노드가 있으면 트리가 없어야 한다.");
            HasDiagnostic(nodes, "Nodes[0]", "ID가 비어 있다");
            HasDiagnostic(nodes, "Nodes[free]", "양의 정수");
            HasDiagnostic(nodes, "Nodes[nan].Upgrades[0]", "유한한 값");
            HasDiagnostic(nodes, "Nodes[nameless].Upgrades[0]", "수치 이름이 비어 있다");

            NodeTreeLoadResult links = NodeTreeLoader.Load(Tree(
                Node("s", 1, true, "s", "ghost", ""),
                Node("s", 1)));

            Expect.True(!links.Succeeded, "잘못된 선이 있으면 트리가 없어야 한다.");
            HasDiagnostic(links, "Nodes[1]", "중복");
            HasDiagnostic(links, "Nodes[s].Links[0]", "자기 자신");
            HasDiagnostic(links, "Nodes[s].Links[1]", "정의되지 않은 노드 ID");
            HasDiagnostic(links, "Nodes[s].Links[2]", "비어 있다");
        }

        // 노드가 있으면 시작 노드가 하나 이상 있고, 모든 노드가 시작 노드에서 선을 따라 닿아야 한다. 빈 트리는 된다.
        private static void EveryNodeIsReachableFromAStartNode()
        {
            HasDiagnostic(NodeTreeLoader.Load(Tree(Node("a", 1, false, "b"), Node("b", 1))), "Nodes", "시작 노드가 하나 이상");

            NodeTreeLoadResult island = NodeTreeLoader.Load(Tree(Node("s", 1, true), Node("x", 1, false, "y"), Node("y", 1)));
            HasDiagnostic(island, "Nodes[x]", "닿지 않는다");
            HasDiagnostic(island, "Nodes[y]", "닿지 않는다");

            Expect.True(NodeTreeLoader.Load(new NodeTreeData()).Succeeded, "빈 트리는 불러와져야 한다.");
        }

        private static NodeData Node(string id, long price, bool start = false, params string[] links) =>
            new NodeData { Id = id, Price = price, Start = start, Links = new List<string>(links) };

        private static NodeData Grants(NodeData node, params UpgradeData[] upgrades)
        {
            node.Upgrades.AddRange(upgrades);
            return node;
        }

        private static UpgradeData Up(string stat, UpgradeOperation operation, float value) =>
            new UpgradeData { Stat = stat, Operation = operation, Value = value };

        private static NodeTreeData Tree(params NodeData[] nodes) =>
            new NodeTreeData { Nodes = new List<NodeData>(nodes) };

        private static NodeTree Load(params NodeData[] nodes)
        {
            NodeTreeLoadResult result = NodeTreeLoader.Load(Tree(nodes));
            Expect.True(result.Succeeded, result.Diagnostics.Count > 0 ? result.Diagnostics[0].ToString() : "로드 실패");
            return result.Tree;
        }

        private static void HasDiagnostic(NodeTreeLoadResult result, string path, string reason)
        {
            foreach (ContentDiagnostic diagnostic in result.Diagnostics)
                if (diagnostic.Path == path && diagnostic.Message.Contains(reason)) return;
            throw new InvalidOperationException(
                $"진단 없음: {path} ({reason}). 받은 진단: {string.Join(" | ", result.Diagnostics)}");
        }
    }
}
