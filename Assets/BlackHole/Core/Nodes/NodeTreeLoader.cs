using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 로드 결과. 오류가 하나라도 있으면 Tree는 null이다(부분 통과 금지).
    public sealed class NodeTreeLoadResult
    {
        public NodeTree Tree { get; }
        public IReadOnlyList<ContentDiagnostic> Diagnostics { get; }
        public bool Succeeded => Tree != null;

        internal NodeTreeLoadResult(NodeTree tree, List<ContentDiagnostic> diagnostics)
        {
            Tree = tree;
            Diagnostics = diagnostics.AsReadOnly();
        }
    }

    // NodeTreeData(저작 형식) → NodeTree(검증된 트리).
    // 오류가 하나라도 있으면 트리 없이 모든 진단을 돌려준다. 경로는 "Nodes[a].Links[0]"처럼 고칠 자리를 가리킨다.
    //
    // 세 단계로 읽는다. 앞 단계에 오류가 있으면 뒤 단계를 보지 않는다(잘못된 노드가 거짓 연결 오류를 만들지 않게).
    // 1. 노드 하나씩: ID, 가격, 업그레이드. 수치 규칙은 NodeDefinition·Upgrade 생성자를 그대로 부른다.
    // 2. 노드 사이: ID 유일, 선이 가리키는 노드가 있고 자기 자신이 아니다.
    // 3. 트리 전체: 노드가 있으면 시작 노드가 하나 이상이고, 모든 노드가 시작 노드에서 선을 따라 닿는다.
    public static class NodeTreeLoader
    {
        public static NodeTreeLoadResult Load(NodeTreeData data)
        {
            var diagnostics = new List<ContentDiagnostic>();

            if (data == null)
            {
                diagnostics.Add(new ContentDiagnostic(string.Empty, "노드 트리 데이터가 null이다."));
                return Fail(diagnostics);
            }

            List<NodeData> items = data.Nodes ?? new List<NodeData>();
            var nodes = new List<NodeDefinition>(items.Count);

            for (int i = 0; i < items.Count; i++)
                nodes.Add(LoadNode(items[i], At(i, items[i]?.Id), diagnostics));

            if (diagnostics.Count > 0)
                return Fail(diagnostics);

            Dictionary<NodeDefinition, HashSet<NodeDefinition>> neighbors = LoadLinks(items, nodes, diagnostics);

            if (diagnostics.Count > 0)
                return Fail(diagnostics);

            VerifyReachable(nodes, neighbors, diagnostics);

            if (diagnostics.Count > 0)
                return Fail(diagnostics);

            var readOnly = new Dictionary<NodeDefinition, IReadOnlyList<NodeDefinition>>();

            foreach (KeyValuePair<NodeDefinition, HashSet<NodeDefinition>> pair in neighbors)
                readOnly.Add(pair.Key, new List<NodeDefinition>(pair.Value).AsReadOnly());

            return new NodeTreeLoadResult(new NodeTree(nodes, readOnly), diagnostics);
        }

        private static NodeDefinition LoadNode(NodeData item, string at, List<ContentDiagnostic> into)
        {
            if (item == null)
            {
                into.Add(new ContentDiagnostic(at, "노드 데이터가 null이다."));
                return null;
            }

            var upgrades = new List<Upgrade>();
            List<UpgradeData> upgradeItems = item.Upgrades ?? new List<UpgradeData>();

            for (int j = 0; j < upgradeItems.Count; j++)
            {
                UpgradeData upgrade = upgradeItems[j];

                if (upgrade == null)
                {
                    into.Add(new ContentDiagnostic($"{at}.Upgrades[{j}]", "업그레이드 데이터가 null이다."));
                    continue;
                }

                try { upgrades.Add(new Upgrade(upgrade.Stat, upgrade.Operation, upgrade.Value)); }
                catch (ArgumentException error) { into.Add(new ContentDiagnostic($"{at}.Upgrades[{j}]", error.Message)); }
            }

            try { return new NodeDefinition(item.Id, item.Price, item.Start, upgrades); }
            catch (ArgumentException error)
            {
                into.Add(new ContentDiagnostic(at, error.Message));
                return null;
            }
        }

        // 선은 방향이 없다. 한쪽에만 적어도, 양쪽에 적어도 같은 선 하나다.
        private static Dictionary<NodeDefinition, HashSet<NodeDefinition>> LoadLinks(
            List<NodeData> items, List<NodeDefinition> nodes, List<ContentDiagnostic> into)
        {
            var byId = new Dictionary<string, NodeDefinition>(StringComparer.Ordinal);
            var neighbors = new Dictionary<NodeDefinition, HashSet<NodeDefinition>>();

            for (int i = 0; i < nodes.Count; i++)
            {
                neighbors.Add(nodes[i], new HashSet<NodeDefinition>());

                if (byId.ContainsKey(nodes[i].Id))
                    into.Add(new ContentDiagnostic($"Nodes[{i}]", $"노드 ID '{nodes[i].Id}'가 중복됐다."));
                else
                    byId.Add(nodes[i].Id, nodes[i]);
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                NodeDefinition node = nodes[i];
                List<string> links = items[i].Links ?? new List<string>();

                for (int k = 0; k < links.Count; k++)
                {
                    string at = $"{At(i, node.Id)}.Links[{k}]";

                    if (string.IsNullOrWhiteSpace(links[k]))
                        into.Add(new ContentDiagnostic(at, "이을 노드 ID가 비어 있다."));
                    else if (links[k] == node.Id)
                        into.Add(new ContentDiagnostic(at, "자기 자신과 이을 수 없다."));
                    else if (!byId.TryGetValue(links[k], out NodeDefinition other))
                        into.Add(new ContentDiagnostic(at, $"정의되지 않은 노드 ID다: '{links[k]}'."));
                    else
                    {
                        neighbors[node].Add(other);
                        neighbors[other].Add(node);
                    }
                }
            }

            return neighbors;
        }

        private static void VerifyReachable(
            List<NodeDefinition> nodes, Dictionary<NodeDefinition, HashSet<NodeDefinition>> neighbors, List<ContentDiagnostic> into)
        {
            if (nodes.Count == 0)
                return;

            var reached = new HashSet<NodeDefinition>();
            var frontier = new Queue<NodeDefinition>();

            foreach (NodeDefinition node in nodes)
            {
                if (node.IsStart && reached.Add(node))
                    frontier.Enqueue(node);
            }

            if (reached.Count == 0)
            {
                into.Add(new ContentDiagnostic("Nodes", "시작 노드가 하나 이상 필요하다."));
                return;
            }

            while (frontier.Count > 0)
            {
                foreach (NodeDefinition neighbor in neighbors[frontier.Dequeue()])
                {
                    if (reached.Add(neighbor))
                        frontier.Enqueue(neighbor);
                }
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                if (!reached.Contains(nodes[i]))
                    into.Add(new ContentDiagnostic(At(i, nodes[i].Id), "시작 노드에서 선을 따라 닿지 않는다."));
            }
        }

        private static string At(int index, string id) =>
            string.IsNullOrWhiteSpace(id) ? $"Nodes[{index}]" : $"Nodes[{id}]";

        private static NodeTreeLoadResult Fail(List<ContentDiagnostic> diagnostics) =>
            new NodeTreeLoadResult(null, diagnostics);
    }
}
