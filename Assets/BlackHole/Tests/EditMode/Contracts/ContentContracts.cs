using System.Collections.Generic;
using BlackHole.Sample;

namespace BlackHole.Core.Tests
{
    // Content: 오류는 경로와 함께 모두 모으고, 오류가 하나라도 있으면 판을 만들 콘텐츠를 내지 않는다.
    internal static class ContentContracts
    {
        public static IEnumerable<Contract> Cases()
        {
            yield return new Contract("Content.SampleLoads", SampleLoads);
            yield return new Contract("Content.ReportsEveryErrorWithPath", ReportsEveryErrorWithPath);
            yield return new Contract("Content.ReportsMissingSections", ReportsMissingSections);
            yield return new Contract("Content.ReportsUpgradeErrorsWithPath", ReportsUpgradeErrorsWithPath);
        }

        // 샘플 값 자체는 [임시]라서 검사하지 않는다. 샘플이 로드된다는 것만 본다.
        private static void SampleLoads()
        {
            ContentLoadResult result = ContentLoader.Load(SampleContent.Create());
            Expect.True(result.Succeeded, "샘플 콘텐츠가 로드되어야 한다: " + string.Join(" | ", result.Diagnostics));
        }

        private static void ReportsEveryErrorWithPath()
        {
            ContentData data = TestContent.Data(timeLimit: 0);
            data.Upgrades.Add(TestContent.Upgrade("bad-price", 0, null));

            ContentLoadResult result = ContentLoader.Load(data);
            Expect.True(!result.Succeeded && result.Content == null, "오류가 있으면 콘텐츠를 만들지 않는다.");
            Expect.Equal(2, result.Diagnostics.Count);
            TestContent.HasDiagnostic(result, "Session.TimeLimit", "duration");
            TestContent.HasDiagnostic(result, "Upgrades[bad-price]", "price");
        }

        private static void ReportsMissingSections()
        {
            ContentLoadResult result = ContentLoader.Load(new ContentData());
            Expect.Equal(1, result.Diagnostics.Count);
            TestContent.HasDiagnostic(result, "Session", string.Empty);

            Expect.True(!ContentLoader.Load(null).Succeeded, "null 데이터는 실패해야 한다.");
        }

        // 노드 하나의 값은 정의 생성자가 경로와 함께 보고한다.
        // 노드 사이의 규칙(ID 유일, 선행 노드 실재, 순환)은 노드가 모두 올바를 때 본다.
        private static void ReportsUpgradeErrorsWithPath()
        {
            ContentData values = TestContent.Data();
            values.Upgrades.Add(TestContent.Upgrade("bad-price", 0, null));
            values.Upgrades.Add(TestContent.Upgrade("self", 5, "self"));
            values.Upgrades.Add(TestContent.Upgrade(" ", 5, null));
            ContentLoadResult result = ContentLoader.Load(values);
            Expect.Equal(3, result.Diagnostics.Count);
            TestContent.HasDiagnostic(result, "Upgrades[bad-price]", "price");
            TestContent.HasDiagnostic(result, "Upgrades[self]", "requires");
            TestContent.HasDiagnostic(result, "Upgrades[2]", "id");

            ContentData links = TestContent.Data();
            links.Upgrades.Add(TestContent.Upgrade("a", 5, "b"));
            links.Upgrades.Add(TestContent.Upgrade("b", 5, "a"));
            links.Upgrades.Add(TestContent.Upgrade("c", 5, "ghost"));
            links.Upgrades.Add(TestContent.Upgrade("c", 5, null));
            result = ContentLoader.Load(links);
            Expect.Equal(4, result.Diagnostics.Count);
            TestContent.HasDiagnostic(result, "Upgrades[3]", "c");
            TestContent.HasDiagnostic(result, "Upgrades[2].Requires", "ghost");
            TestContent.HasDiagnostic(result, "Upgrades[0].Requires", "순환");
            TestContent.HasDiagnostic(result, "Upgrades[1].Requires", "순환");
        }
    }
}
