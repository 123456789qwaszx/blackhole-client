using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 트리 노드 하나: 강화 하나를 참조하고, 선행 노드 목록(Requires)을 가진다.
    // 화면 좌표와 연결선은 게임 규칙이 아니므로 여기에 두지 않는다.
    public sealed class SkillTreeNodeDefinition
    {
        public string Id { get; }
        public string UpgradeId { get; }
        // 이 노드를 열려면 모두 획득해야 하는 노드(AND). 비어 있으면 루트다.
        public IReadOnlyList<string> Requires { get; }

        public SkillTreeNodeDefinition(string id, string upgradeId, IReadOnlyList<string> requires)
        {
            Id = DefinitionGuard.Id(id, nameof(id));
            UpgradeId = DefinitionGuard.Id(upgradeId, nameof(upgradeId));
            int count = requires?.Count ?? 0;
            var copy = new string[count];
            for (int i = 0; i < count; i++)
                copy[i] = DefinitionGuard.Id(requires[i], $"{nameof(requires)}[{i}]");
            Requires = Array.AsReadOnly(copy);
        }
    }

    // 스킬 트리의 공유 정의. 획득 상태는 갖지 않는다 — 획득 원본은 UpgradeState다.
    //
    // 생성자 보장(구현 = SkillTreeInvariants):
    // [1] 노드 ID가 유일하다.
    // [2] 선행 노드가 실재하고 한 노드 안에서 중복되지 않는다.
    // [3] 선행 관계에 순환이 없다(그래서 루트가 반드시 있다).
    // 참조한 강화의 실재와 유일성은 콘텐츠 전체 규칙(ContentInvariants)이 본다.
    //
    // 이 규칙(비순환·AND·루트 무조건)은 팀 기획을 대신하는 최종안이 아니라 최소 검증안이다.
    // OR 조건이나 노드별 최대 단계가 필요해지면 SkillTree의 판정에 추가한다.
    public sealed class SkillTreeDefinition
    {
        public static SkillTreeDefinition Empty { get; } =
            new SkillTreeDefinition(Array.Empty<SkillTreeNodeDefinition>());

        public IReadOnlyList<SkillTreeNodeDefinition> Nodes { get; }

        public SkillTreeDefinition(IReadOnlyList<SkillTreeNodeDefinition> nodes)
        {
            var copy = new SkillTreeNodeDefinition[nodes?.Count ?? 0];
            for (int i = 0; i < copy.Length; i++) copy[i] = nodes[i];
            Nodes = Array.AsReadOnly(copy);

            var diagnostics = new List<ContentDiagnostic>();
            SkillTreeInvariants.Collect(Nodes, diagnostics);
            if (diagnostics.Count > 0)
                throw new ArgumentException(diagnostics[0].ToString());
        }
    }

    // 트리 그래프의 규칙. SkillTreeDefinition 생성자와 ContentLoader가 함께 쓴다.
    internal static class SkillTreeInvariants
    {
        public static void Collect(IReadOnlyList<SkillTreeNodeDefinition> nodes, ICollection<ContentDiagnostic> into)
        {
            int before = into.Count;
            Dictionary<string, SkillTreeNodeDefinition> byId = IndexNodes(nodes, into);
            VerifyRequires(nodes, byId, into);
            // 순환 탐색은 모든 참조가 실재할 때만 의미가 있다.
            if (into.Count == before) VerifyAcyclic(nodes, byId, into);
        }

        private static Dictionary<string, SkillTreeNodeDefinition> IndexNodes(
            IReadOnlyList<SkillTreeNodeDefinition> nodes, ICollection<ContentDiagnostic> into)
        {
            var byId = new Dictionary<string, SkillTreeNodeDefinition>(StringComparer.Ordinal);
            for (int i = 0; i < nodes.Count; i++)
            {
                SkillTreeNodeDefinition node = nodes[i];
                if (node == null)
                    into.Add(new ContentDiagnostic($"SkillTree.Nodes[{i}]", "트리 노드가 null이다."));
                else if (byId.ContainsKey(node.Id))
                    into.Add(new ContentDiagnostic($"SkillTree.Nodes[{i}]", $"노드 ID '{node.Id}'가 중복됐다."));
                else
                    byId.Add(node.Id, node);
            }
            return byId;
        }

        private static void VerifyRequires(IReadOnlyList<SkillTreeNodeDefinition> nodes,
            Dictionary<string, SkillTreeNodeDefinition> byId, ICollection<ContentDiagnostic> into)
        {
            foreach (SkillTreeNodeDefinition node in nodes)
            {
                if (node == null) continue;
                var seen = new HashSet<string>(StringComparer.Ordinal);
                for (int r = 0; r < node.Requires.Count; r++)
                {
                    string required = node.Requires[r];
                    string at = $"SkillTree.Nodes[{node.Id}].Requires[{r}]";
                    if (!byId.ContainsKey(required))
                        into.Add(new ContentDiagnostic(at, $"정의되지 않은 노드 '{required}'."));
                    else if (!seen.Add(required))
                        into.Add(new ContentDiagnostic(at, $"선행 노드 '{required}'가 중복됐다."));
                }
            }
        }

        // 깊이 우선 탐색. 방문 중인 노드를 다시 만나면 순환이다.
        private static void VerifyAcyclic(IReadOnlyList<SkillTreeNodeDefinition> nodes,
            Dictionary<string, SkillTreeNodeDefinition> byId, ICollection<ContentDiagnostic> into)
        {
            var visiting = new HashSet<string>(StringComparer.Ordinal);
            var done = new HashSet<string>(StringComparer.Ordinal);
            var path = new List<string>();
            foreach (SkillTreeNodeDefinition node in nodes)
                if (Visit(node, byId, visiting, done, path, into)) return;
        }

        private static bool Visit(SkillTreeNodeDefinition node, Dictionary<string, SkillTreeNodeDefinition> byId,
            HashSet<string> visiting, HashSet<string> done, List<string> path, ICollection<ContentDiagnostic> into)
        {
            if (done.Contains(node.Id)) return false;
            visiting.Add(node.Id);
            path.Add(node.Id);

            for (int r = 0; r < node.Requires.Count; r++)
            {
                string required = node.Requires[r];
                if (visiting.Contains(required))
                {
                    int start = path.IndexOf(required);
                    string cycle = string.Join(" → ", path.GetRange(start, path.Count - start)) + " → " + required;
                    into.Add(new ContentDiagnostic($"SkillTree.Nodes[{node.Id}].Requires[{r}]",
                        $"선행 관계가 순환한다: {cycle}."));
                    return true;
                }
                if (Visit(byId[required], byId, visiting, done, path, into)) return true;
            }

            path.RemoveAt(path.Count - 1);
            visiting.Remove(node.Id);
            done.Add(node.Id);
            return false;
        }
    }
}
