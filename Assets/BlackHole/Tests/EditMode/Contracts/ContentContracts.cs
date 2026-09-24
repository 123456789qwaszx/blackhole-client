using System.Collections.Generic;
using BlackHole.Sample;

namespace BlackHole.Core.Tests
{
    // B9 Content: 오류는 경로와 함께 모두 모으고, 오류가 하나라도 있으면 판을 만들 콘텐츠를 내지 않는다.
    internal static class ContentContracts
    {
        public static IEnumerable<Contract> Cases()
        {
            yield return new Contract("Content.SampleLoads", SampleLoads);
            yield return new Contract("Content.ReportsEveryErrorWithPath", ReportsEveryErrorWithPath);
            yield return new Contract("Content.ReportsMissingSections", ReportsMissingSections);
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
            data.Hq.X = float.NaN;

            ContentLoadResult result = ContentLoader.Load(data);
            Expect.True(!result.Succeeded && result.Content == null, "오류가 있으면 콘텐츠를 만들지 않는다.");
            Expect.Equal(2, result.Diagnostics.Count);
            TestContent.HasDiagnostic(result, "Session.TimeLimit", "duration");
            TestContent.HasDiagnostic(result, "Hq", "유한");
        }

        private static void ReportsMissingSections()
        {
            ContentLoadResult result = ContentLoader.Load(new ContentData());
            Expect.Equal(2, result.Diagnostics.Count);
            TestContent.HasDiagnostic(result, "Session", string.Empty);
            TestContent.HasDiagnostic(result, "Hq", string.Empty);

            Expect.True(!ContentLoader.Load(null).Succeeded, "null 데이터는 실패해야 한다.");
        }
    }
}
