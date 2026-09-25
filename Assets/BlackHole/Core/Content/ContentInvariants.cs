using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 콘텐츠 전체의 규칙: 업그레이드 노드 ID는 유일하고, 선행 노드는 실재하며, 선행을 따라가면 시작 노드에 닿는다.
    // GameContent 생성자(첫 오류로 생성 실패)와 ContentLoader(경로별 진단 수집)가 함께 쓴다.
    // 개별 정의의 수치 규칙은 각 정의 생성자에 있다 — 여기서 다시 보지 않는다.
    internal static class ContentInvariants
    {
        public static void CollectUpgrades(
            IReadOnlyList<UpgradeNodeDefinition> upgrades,
            ICollection<ContentDiagnostic> into,
            out Dictionary<string, UpgradeNodeDefinition> upgradesById)
        {
            upgradesById = Index(upgrades, into);

            for (int i = 0; i < upgrades.Count; i++)
            {
                UpgradeNodeDefinition node = upgrades[i];

                if (node?.Requires == null)
                    continue;

                string at = $"Upgrades[{i}].Requires";

                if (!upgradesById.ContainsKey(node.Requires))
                    into.Add(new ContentDiagnostic(at, $"정의되지 않은 선행 노드 ID '{node.Requires}'."));
                else if (!ReachesRoot(node, upgradesById))
                    into.Add(new ContentDiagnostic(at, $"선행 노드를 따라가면 시작 노드에 닿지 않는다(순환): '{node.Id}'."));
            }
        }

        private static Dictionary<string, UpgradeNodeDefinition> Index(
            IReadOnlyList<UpgradeNodeDefinition> upgrades,
            ICollection<ContentDiagnostic> into)
        {
            var byId = new Dictionary<string, UpgradeNodeDefinition>(StringComparer.Ordinal);

            for (int i = 0; i < upgrades.Count; i++)
            {
                UpgradeNodeDefinition node = upgrades[i];

                if (node == null)
                    into.Add(new ContentDiagnostic($"Upgrades[{i}]", "업그레이드 정의가 null이다."));
                else if (byId.ContainsKey(node.Id))
                    into.Add(new ContentDiagnostic($"Upgrades[{i}]", $"업그레이드 ID '{node.Id}'가 중복됐다."));
                else
                    byId.Add(node.Id, node);
            }

            return byId;
        }

        private static bool ReachesRoot(UpgradeNodeDefinition node, Dictionary<string, UpgradeNodeDefinition> byId)
        {
            UpgradeNodeDefinition current = node;

            for (int steps = 0; steps <= byId.Count; steps++)
            {
                if (current.Requires == null)
                    return true;

                if (!byId.TryGetValue(current.Requires, out current))
                    return false;
            }

            return false;
        }
    }
}
