using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // ContentData(저작 형식) → GameContent(검증된 정의).
    //
    // 오류가 하나라도 있으면 Content 없이 모든 진단을 돌려준다(부분 통과 금지).
    // 여기서 새로 두는 규칙은 데이터 모양에 관한 것뿐이다(빠진 칸, 정의되지 않은 참조).
    // 수치 규칙은 정의 생성자를, 콘텐츠 전체 규칙은 ContentInvariants를 그대로 호출해 경로를 붙인다.
    //
    // 두 단계로 읽는다.
    // 1. 개별 정의: 판 설정, 업그레이드 노드.
    // 2. 노드 사이의 규칙(ID 유일, 선행 노드의 실재, 순환 없음). 개별 정의가 모두 올바를 때 본다.
    public static class ContentLoader
    {
        public static ContentLoadResult Load(ContentData data)
        {
            var diagnostics = new List<ContentDiagnostic>();

            if (data == null)
            {
                diagnostics.Add(new ContentDiagnostic(string.Empty, "콘텐츠 데이터가 null이다."));
                return Fail(diagnostics);
            }

            TimeLimitDefinition timeLimit = LoadSession(data.Session, diagnostics);
            List<UpgradeNodeDefinition> upgrades = LoadUpgrades(data.Upgrades, diagnostics);

            if (diagnostics.Count > 0)
                return Fail(diagnostics);

            ContentInvariants.CollectUpgrades(upgrades, diagnostics, out _);

            if (diagnostics.Count > 0)
                return Fail(diagnostics);

            return new ContentLoadResult(new GameContent(timeLimit, upgrades), diagnostics);
        }

        private static TimeLimitDefinition LoadSession(SessionData item, List<ContentDiagnostic> into)
        {
            if (item == null)
                return Missing<TimeLimitDefinition>("Session", into);

            return Guard("Session.TimeLimit", into, () => new TimeLimitDefinition(item.TimeLimit));
        }

        // 없으면 업그레이드 노드가 없다.
        private static List<UpgradeNodeDefinition> LoadUpgrades(List<UpgradeData> items, List<ContentDiagnostic> into)
        {
            var upgrades = new List<UpgradeNodeDefinition>();

            if (items == null)
                return upgrades;

            for (int i = 0; i < items.Count; i++)
            {
                UpgradeData item = items[i];
                string at = At("Upgrades", i, item?.Id);

                if (item == null)
                {
                    into.Add(new ContentDiagnostic(at, "업그레이드 데이터가 null이다."));
                    continue;
                }

                UpgradeNodeDefinition node = Guard(at, into, () =>
                    new UpgradeNodeDefinition(item.Id, item.Price, item.Requires));

                if (node != null)
                    upgrades.Add(node);
            }

            return upgrades;
        }

        // 정의 생성자의 규칙 위반을 그 자리의 진단으로 바꾼다.
        private static T Guard<T>(string at, List<ContentDiagnostic> into, Func<T> create) where T : class
        {
            try { return create(); }
            catch (ArgumentException error)
            {
                into.Add(new ContentDiagnostic(at, error.Message));
                return null;
            }
        }

        private static T Missing<T>(string at, List<ContentDiagnostic> into) where T : class
        {
            into.Add(new ContentDiagnostic(at, "데이터가 없다."));
            return null;
        }

        private static string At(string section, int index, string id) =>
            string.IsNullOrWhiteSpace(id) ? $"{section}[{index}]" : $"{section}[{id}]";

        private static ContentLoadResult Fail(List<ContentDiagnostic> diagnostics) =>
            new ContentLoadResult(null, diagnostics);
    }
}
