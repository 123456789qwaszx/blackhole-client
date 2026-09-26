using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 콘텐츠 전체의 규칙: 적 종류·적 풀·업그레이드 노드의 ID는 유일하다. 노드의 선행 노드는 실재하고,
    // 선행을 따라가면 시작 노드에 닿는다. 한 종류의 질량 증가 노드를 모두 사도 그 종류의 질량 단계 표 안이다.
    // GameContent 생성자(첫 오류로 생성 실패)와 ContentLoader(경로별 진단 수집)가 함께 쓴다.
    // 개별 정의의 수치 규칙은 각 정의 생성자에 있다 — 여기서 다시 보지 않는다.
    // 정의 객체로 해석되는 참조(공급·풀의 적, 단계의 풀, Grant의 적)는 ContentLoader가 이 색인으로 해석하며 진단한다.
    internal static class ContentInvariants
    {
        public static void CollectUpgrades(
            IReadOnlyList<UpgradeNodeDefinition> upgrades,
            ICollection<ContentDiagnostic> into,
            out Dictionary<string, UpgradeNodeDefinition> upgradesById)
        {
            upgradesById = Index(upgrades, "Upgrades", "업그레이드", u => u.Id, into);
            var massByEnemy = new Dictionary<EnemyDefinition, float>();

            for (int i = 0; i < upgrades.Count; i++)
            {
                UpgradeNodeDefinition node = upgrades[i];

                if (node == null)
                    continue;

                foreach (EnemyGrant grant in node.Grants)
                {
                    if (grant.Stat == EnemyUpgradeStat.MassLevel)
                        massByEnemy[grant.Enemy] = (massByEnemy.TryGetValue(grant.Enemy, out float mass) ? mass : 0) + grant.Value;
                }

                if (node.Requires == null)
                    continue;

                string at = $"Upgrades[{i}].Requires";

                if (!upgradesById.ContainsKey(node.Requires))
                    into.Add(new ContentDiagnostic(at, $"정의되지 않은 선행 노드 ID '{node.Requires}'."));
                else if (!ReachesRoot(node, upgradesById))
                    into.Add(new ContentDiagnostic(at, $"선행 노드를 따라가면 시작 노드에 닿지 않는다(순환): '{node.Id}'."));
            }

            foreach (KeyValuePair<EnemyDefinition, float> mass in massByEnemy)
            {
                int top = mass.Key.MassLevels.Count - 1;

                if (mass.Value > top)
                    into.Add(new ContentDiagnostic("Upgrades",
                        $"'{mass.Key.Id}'의 질량 증가 노드를 모두 사면 질량 단계 {mass.Value}가 되는데, 질량 단계 표는 {top}까지다."));
            }
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

        public static void CollectEnemies(
            IReadOnlyList<EnemyDefinition> enemies,
            ICollection<ContentDiagnostic> into,
            out Dictionary<string, EnemyDefinition> enemiesById)
        {
            enemiesById = Index(enemies, "Enemies", "적", e => e.Id, into);
        }

        public static void CollectPools(
            IReadOnlyList<EnemyPoolDefinition> pools,
            ICollection<ContentDiagnostic> into,
            out Dictionary<string, EnemyPoolDefinition> poolsById)
        {
            poolsById = Index(pools, "EnemyPools", "적 풀", p => p.Id, into);
        }

        private static Dictionary<string, T> Index<T>(
            IReadOnlyList<T> items,
            string section,
            string label,
            Func<T, string> idOf,
            ICollection<ContentDiagnostic> into) where T : class
        {
            var byId = new Dictionary<string, T>(StringComparer.Ordinal);

            for (int i = 0; i < items.Count; i++)
            {
                T item = items[i];

                if (item == null)
                    into.Add(new ContentDiagnostic($"{section}[{i}]", $"{label} 정의가 null이다."));
                else if (byId.ContainsKey(idOf(item)))
                    into.Add(new ContentDiagnostic($"{section}[{i}]", $"{label} ID '{idOf(item)}'가 중복됐다."));
                else
                    byId.Add(idOf(item), item);
            }

            return byId;
        }
    }
}
