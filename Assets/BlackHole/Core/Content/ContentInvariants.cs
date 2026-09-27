using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 콘텐츠 전체의 규칙: 적 종류의 ID는 유일하다. 전체 개체 수 상한이 있고, 전투 시작 공급이 그 안이다.
    // GameContent 생성자(첫 오류로 생성 실패)와 ContentLoader(경로별 진단 수집)가 함께 쓴다.
    // 개별 정의의 수치 규칙은 각 정의 생성자에 있다 — 여기서 다시 보지 않는다.
    // 정의 객체로 해석되는 참조(공급·풀의 적, 단계의 풀)는 ContentLoader가 이 색인으로 해석하며 진단한다.
    internal static class ContentInvariants
    {
        // 전체 개체 수 상한은 0 이상이고, 출현 배치가 있으면(적을 만들 수 있으면) 1 이상이다.
        public static void CheckMaxAlive(EnemyPlacementDefinition placement, int maxAliveEnemies, ICollection<ContentDiagnostic> into)
        {
            if (maxAliveEnemies < 0)
                into.Add(new ContentDiagnostic("MaxAliveEnemies", "0 이상이 필요하다."));
            else if (placement != null && maxAliveEnemies < 1)
                into.Add(new ContentDiagnostic("MaxAliveEnemies", "출현 배치가 있으면 전체 개체 수 상한(1 이상)이 필요하다."));
        }

        // 전투 시작 공급의 합(과 업그레이드가 더할 수 있는 공급 수 extra)이 전체 개체 수 상한 안이다.
        // 전투 시작에는 살아 있는 적이 없으므로, 이 합이 상한을 넘으면 약속한 적이 매 판 시작부터 버려진다.
        // extra는 노드를 모두 산 경우의 더할 공급 수다(UpgradeContentCheck). 콘텐츠만 볼 때는 0이다.
        public static void CheckStartSupplyFits(
            IReadOnlyList<SupplyRequest> startSupply,
            long extra,
            int maxAliveEnemies,
            ICollection<ContentDiagnostic> into)
        {
            long worst = extra;

            foreach (SupplyRequest request in startSupply)
                worst += request.Count;

            if (worst > maxAliveEnemies)
                into.Add(new ContentDiagnostic("MaxAliveEnemies",
                    extra > 0
                        ? $"전투 시작 공급(공급 수 노드를 모두 산 경우)이 {worst}마리인데, 전체 개체 수 상한은 {maxAliveEnemies}마리다."
                        : $"전투 시작 공급이 {worst}마리인데, 전체 개체 수 상한은 {maxAliveEnemies}마리다."));
        }

        public static void CollectEnemies(
            IReadOnlyList<EnemyDefinition> enemies,
            ICollection<ContentDiagnostic> into,
            out Dictionary<string, EnemyDefinition> enemiesById)
        {
            enemiesById = Index(enemies, "Enemies", "적", e => e.Id, into);
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
