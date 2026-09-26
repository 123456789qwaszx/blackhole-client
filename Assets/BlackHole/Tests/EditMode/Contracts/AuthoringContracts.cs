using System.Collections.Generic;
using BlackHole.Authoring;
using BlackHole.Core;

namespace BlackHole.Core.Tests
{
    // 노드 도구의 편집 규칙: 선은 그은 것만이고, 놓기·옮기기는 선을 건드리지 않는다. 격자 이웃 잇기는 그 순간만 잇는 저작 명령이다.
    internal static class AuthoringContracts
    {
        public static IEnumerable<Contract> Cases()
        {
            yield return new Contract("Authoring.PlacingNeverLinks", PlacingNeverLinks);
            yield return new Contract("Authoring.LinkNeighborsLinksOnlyAdjacentSelectedPairs", LinkNeighborsLinksOnlyAdjacentSelectedPairs);
            yield return new Contract("Authoring.MovingNeverChangesLinks", MovingNeverChangesLinks);
            yield return new Contract("Authoring.SelectionCommandsCutOnlyWhatTheyName", SelectionCommandsCutOnlyWhatTheyName);
            yield return new Contract("Authoring.RenameAndRemoveKeepLinksConsistent", RenameAndRemoveKeepLinksConsistent);
            yield return new Contract("Authoring.ToolWarnsOverlap", ToolWarnsOverlap);
        }

        // 이웃 칸에 놓아도 선은 생기지 않는다. 첫 노드만 시작 노드다.
        private static void PlacingNeverLinks()
        {
            var tree = new NodeTreeData();
            NodeData a = NodeTreeAuthoring.Place(tree, 0, 0);
            NodeData b = NodeTreeAuthoring.Place(tree, 1, 0);
            NodeTreeAuthoring.Place(tree, 1, 1);

            Expect.True(a.Start && !b.Start, "첫 노드만 시작 노드여야 한다.");
            Expect.Equal(0, NodeTreeAuthoring.Links(tree).Count);
            Expect.True(NodeTreeAuthoring.Place(tree, 0, 0) == null, "찬 칸에는 놓을 수 없다.");
        }

        // 고른 노드 가운데 상하좌우로 붙은 쌍만 잇는다. 대각선과 고르지 않은 이웃은 잇지 않고, 다시 눌러도 겹쳐 잇지 않는다.
        private static void LinkNeighborsLinksOnlyAdjacentSelectedPairs()
        {
            var tree = new NodeTreeData();
            NodeData a = NodeTreeAuthoring.Place(tree, 0, 0);
            NodeData b = NodeTreeAuthoring.Place(tree, 1, 0);
            NodeData c = NodeTreeAuthoring.Place(tree, 1, 1);
            NodeData d = NodeTreeAuthoring.Place(tree, 0, 1);
            NodeData outside = NodeTreeAuthoring.Place(tree, 2, 0);
            var square = new[] { a, b, c, d };

            Expect.Equal(4, NodeTreeAuthoring.LinkNeighbors(square));
            Expect.True(NodeTreeAuthoring.AreLinked(a, b) && NodeTreeAuthoring.AreLinked(b, c), "붙은 쌍은 이어져야 한다.");
            Expect.True(NodeTreeAuthoring.AreLinked(c, d) && NodeTreeAuthoring.AreLinked(d, a), "붙은 쌍은 이어져야 한다.");
            Expect.True(!NodeTreeAuthoring.AreLinked(a, c) && !NodeTreeAuthoring.AreLinked(b, d), "대각선은 이어지면 안 된다.");
            Expect.True(!NodeTreeAuthoring.AreLinked(b, outside), "고르지 않은 이웃은 이어지면 안 된다.");
            Expect.Equal(0, NodeTreeAuthoring.LinkNeighbors(square));

            Expect.True(NodeTreeAuthoring.Link(b, outside), "손으로 이을 수 있어야 한다.");
            Expect.True(!NodeTreeAuthoring.Link(outside, b), "이미 이어진 선은 겹쳐 잇지 않는다.");
            Expect.True(NodeTreeLoader.Load(tree).Succeeded, "순환이 있는 묶음도 불러와져야 한다.");
        }

        // 옮겨도 선은 그대로다. 함께 옮기는 노드끼리는 서로의 빈 칸으로 들어갈 수 있고, 다른 노드가 있는 칸으로는 아무것도 옮기지 않는다.
        private static void MovingNeverChangesLinks()
        {
            var tree = new NodeTreeData();
            NodeData a = NodeTreeAuthoring.Place(tree, 0, 0);
            NodeData b = NodeTreeAuthoring.Place(tree, 1, 0);
            NodeTreeAuthoring.Place(tree, 3, 0);
            NodeTreeAuthoring.Link(a, b);

            Expect.True(NodeTreeAuthoring.MoveBy(tree, new[] { a }, 0, 5), "빈 칸으로는 옮겨져야 한다.");
            Expect.True(NodeTreeAuthoring.AreLinked(a, b), "떨어뜨려도 선은 남아야 한다.");
            Expect.True(!NodeTreeAuthoring.MoveBy(tree, new[] { a }, 1, -5), "찬 칸으로는 옮길 수 없다.");
            Expect.True(a.X == 0 && a.Y == 5, "옮기지 못하면 자리가 그대로여야 한다.");

            NodeTreeAuthoring.MoveBy(tree, new[] { a }, 0, -5);
            Expect.True(NodeTreeAuthoring.MoveBy(tree, new[] { a, b }, 1, 0), "함께 옮기는 노드의 칸으로는 들어갈 수 있다.");
            Expect.True(a.X == 1 && b.X == 2, "둘 다 한 칸씩 옮겨져야 한다.");
            Expect.True(!NodeTreeAuthoring.MoveBy(tree, new[] { a, b }, 1, 0), "하나라도 막히면 옮기지 않는다.");
            Expect.True(a.X == 1 && b.X == 2, "막히면 아무것도 옮기지 않아야 한다.");
            Expect.Equal(1, NodeTreeAuthoring.Links(tree).Count);
        }

        // "선택끼리 끊기"는 고른 노드 사이만, "선 모두 지우기"는 고른 노드에 닿은 선을 모두(없는 노드를 가리키던 선 포함) 끊는다.
        private static void SelectionCommandsCutOnlyWhatTheyName()
        {
            var tree = new NodeTreeData();
            NodeData a = NodeTreeAuthoring.Place(tree, 0, 0);
            NodeData b = NodeTreeAuthoring.Place(tree, 1, 0);
            NodeData c = NodeTreeAuthoring.Place(tree, 2, 0);
            NodeData d = NodeTreeAuthoring.Place(tree, 3, 0);
            NodeTreeAuthoring.LinkNeighbors(new[] { a, b, c, d });

            Expect.Equal(1, NodeTreeAuthoring.UnlinkAmong(new[] { a, b }));
            Expect.True(NodeTreeAuthoring.AreLinked(b, c), "고르지 않은 노드와의 선은 남아야 한다.");

            d.Links.Add("ghost");
            Expect.Equal(1, NodeTreeAuthoring.ClearLinks(tree, new[] { d }));
            Expect.True(d.Links.Count == 0 && !NodeTreeAuthoring.AreLinked(c, d), "고른 노드의 선이 모두 없어져야 한다.");
            Expect.Equal(1, NodeTreeAuthoring.Links(tree).Count);
        }

        // ID를 바꾸면 다른 노드에 적힌 선도 바뀐다. 지우면 그 노드와의 선이 모두 사라진다.
        private static void RenameAndRemoveKeepLinksConsistent()
        {
            var tree = new NodeTreeData();
            NodeData a = NodeTreeAuthoring.Place(tree, 0, 0);
            NodeData b = NodeTreeAuthoring.Place(tree, 1, 0);
            NodeData c = NodeTreeAuthoring.Place(tree, 2, 0);
            NodeTreeAuthoring.LinkNeighbors(new[] { a, b, c });

            Expect.True(NodeTreeAuthoring.Rename(tree, b, "hub"), "새 ID로 바뀌어야 한다.");
            Expect.True(!NodeTreeAuthoring.Rename(tree, c, "hub"), "이미 쓰는 ID로는 바꿀 수 없다.");
            Expect.True(!NodeTreeAuthoring.Rename(tree, a, " "), "빈 ID로는 바꿀 수 없다.");
            Expect.True(NodeTreeAuthoring.AreLinked(a, b) && NodeTreeAuthoring.AreLinked(b, c), "바뀐 ID로도 이어져 있어야 한다.");
            Expect.True(NodeTreeLoader.Load(tree).Succeeded, "바꾼 뒤에도 불러와져야 한다.");

            NodeTreeAuthoring.Remove(tree, b);
            Expect.True(!a.Links.Contains("hub") && !c.Links.Contains("hub"), "지운 노드와의 선이 남으면 안 된다.");
            Expect.Equal(0, NodeTreeAuthoring.Links(tree).Count);
        }

        // 한 칸에 겹친 노드를 경로와 함께 알린다.
        private static void ToolWarnsOverlap()
        {
            var tree = new NodeTreeData();
            tree.Nodes.Add(new NodeData { Id = "a", Price = 1, Start = true });
            tree.Nodes.Add(new NodeData { Id = "b", Price = 1 });
            tree.Nodes.Add(new NodeData { Id = "c", Price = 1, X = 1 });

            List<ContentDiagnostic> diagnostics = NodeTreeAuthoring.Check(tree);
            Expect.True(diagnostics.Exists(d => d.Path == "Nodes[b]" && d.Message.Contains("'a'와 겹친다")), "겹친 칸을 알려야 한다.");
            Expect.Equal(1, diagnostics.Count);
        }
    }
}
