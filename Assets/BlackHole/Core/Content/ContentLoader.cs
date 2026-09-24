using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // ContentData(저작 형식) → GameContent(검증된 정의).
    //
    // 오류가 하나라도 있으면 Content 없이 모든 진단을 돌려준다(부분 통과 금지).
    // 여기서 새로 두는 규칙은 데이터 모양에 관한 것뿐이다(빠진 칸). 수치 규칙은 정의 생성자를
    // 그대로 호출해 경로를 붙인다.
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
            HqDefinition hq = LoadHq(data.Hq, diagnostics);
            if (diagnostics.Count > 0) return Fail(diagnostics);

            return new ContentLoadResult(new GameContent(timeLimit, hq), diagnostics);
        }

        private static TimeLimitDefinition LoadSession(SessionData item, List<ContentDiagnostic> into)
        {
            if (item == null) return Missing<TimeLimitDefinition>("Session", into);
            return Guard("Session.TimeLimit", into, () => new TimeLimitDefinition(item.TimeLimit));
        }

        private static HqDefinition LoadHq(HqData item, List<ContentDiagnostic> into)
        {
            if (item == null) return Missing<HqDefinition>("Hq", into);
            return Guard("Hq", into, () => new HqDefinition(new Point2(item.X, item.Y)));
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

        private static ContentLoadResult Fail(List<ContentDiagnostic> diagnostics) =>
            new ContentLoadResult(null, diagnostics);
    }
}
