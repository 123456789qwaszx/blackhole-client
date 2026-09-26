using System;
using System.Collections.Generic;
using BlackHole.Core;

namespace BlackHole.Core.Tests
{
    // 노드 트리: 그래프(시작 노드와 선으로 드러남)와 구매(Gold로 산다).
    internal static class NodeContracts
    {
        private static readonly PlayerId First = new PlayerId(1);

        public static IEnumerable<Contract> Cases()
        {
            yield return new Contract("Node.RevealedByStartOrAnyOwnedNeighbor", RevealedByStartOrAnyOwnedNeighbor);
            yield return new Contract("Node.FailedPurchaseChangesNothing", FailedPurchaseChangesNothing);
            yield return new Contract("Node.LoaderReportsBrokenNodesAndLinks", LoaderReportsBrokenNodesAndLinks);
            yield return new Contract("Node.EveryNodeIsReachableFromAStartNode", EveryNodeIsReachableFromAStartNode);
            yield return new Contract("Node.GraphListsEachLinkOnce", GraphListsEachLinkOnce);
        }

        // 화면이 그리는 선 목록: 양쪽 노드에 적은 선도 한 번만 나온다.
        private static void GraphListsEachLinkOnce()
        {
            NodeTree tree = Load(Node("s", 1, true, "a"), Node("a", 1, false, "s", "b"), Node("b", 1));
            IReadOnlyList<(string A, string B)> links = tree.Graph.Links;

            Expect.Equal(2, links.Count);
            Expect.True(Has(links, "s", "a") && Has(links, "a", "b"), "선마다 한 번씩 있어야 한다.");
        }

        private static bool Has(IReadOnlyList<(string A, string B)> links, string a, string b)
        {
            foreach ((string A, string B) link in links)
            {
                if ((link.A == a && link.B == b) || (link.A == b && link.B == a))
                    return true;
            }

            return false;
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
            var state = new PlayerState(First);
            state.EarnGold(10);

            Expect.Equal(NodeState.Purchasable, NodePurchase.StateOf(state, tree, "s"));
            Expect.Equal(NodeState.Hidden, NodePurchase.StateOf(state, tree, "a"));

            NodePurchase.TryPurchase(state, tree, "s");
            Expect.Equal(NodeState.Owned, NodePurchase.StateOf(state, tree, "s"));
            Expect.Equal(NodeState.Purchasable, NodePurchase.StateOf(state, tree, "a"));

            NodePurchase.TryPurchase(state, tree, "a");
            NodePurchase.TryPurchase(state, tree, "b");
            // d의 이웃 가운데 b만 샀다. Gold가 모자라도 보이기는 한다.
            Expect.Equal(NodeState.Revealed, NodePurchase.StateOf(state, tree, "d"));
            Expect.Equal(NodeState.Purchasable, NodePurchase.StateOf(state, tree, "c"));
            Expect.Equal(NodeState.Hidden, NodePurchase.StateOf(state, tree, "far"));

            // 그래프는 구매를 모른다. 무엇을 가졌는지만 받아 같은 답을 한다.
            Expect.True(tree.Graph.IsRevealed("d", id => id == "b"), "이웃 하나를 가지면 드러나야 한다.");
            Expect.True(!tree.Graph.IsRevealed("far", id => id == "b"), "가진 이웃이 없으면 숨어야 한다.");
        }

        // 실패한 구매는 Gold와 산 노드를 바꾸지 않는다. 화면·콘솔은 노드 ID로 요청한다.
        private static void FailedPurchaseChangesNothing()
        {
            NodeTree tree = Load(Node("s", 10, true, "a"), Node("a", 10));
            var state = new PlayerState(First);
            state.EarnGold(15);

            Expect.Equal(PurchaseResult.Hidden, NodePurchase.TryPurchase(state, tree, "a"));
            Expect.Equal(PurchaseResult.UnknownNode, NodePurchase.TryPurchase(state, tree, "nope"));
            Expect.Equal(15L, state.Gold);

            Expect.Equal(PurchaseResult.Purchased, NodePurchase.TryPurchase(state, tree, "s"));
            Expect.Equal(5L, state.Gold);

            Expect.Equal(PurchaseResult.AlreadyOwned, NodePurchase.TryPurchase(state, tree, "s"));
            Expect.Equal(PurchaseResult.NotEnoughGold, NodePurchase.TryPurchase(state, tree, "a"));
            Expect.Equal(5L, state.Gold);
            Expect.True(!state.Owns("a"), "Gold가 모자라면 사면 안 된다.");
            Expect.Equal(1, state.OwnedNodes.Count);
        }

        // 노드 하나의 오류(ID, 가격)와 노드 사이의 오류(중복 ID, 선)를 경로와 함께 모두 보고한다.
        private static void LoaderReportsBrokenNodesAndLinks()
        {
            NodeTreeLoadResult nodes = NodeTreeLoader.Load(Tree(
                Node("", 1, true),
                Node("free", 0)));

            Expect.True(!nodes.Succeeded, "잘못된 노드가 있으면 트리가 없어야 한다.");
            HasDiagnostic(nodes, "Nodes[0]", "ID가 비어 있다");
            HasDiagnostic(nodes, "Nodes[free]", "양의 정수");

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
