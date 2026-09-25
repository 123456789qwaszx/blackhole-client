using System.Collections.Generic;
using BlackHole.Core;

namespace BlackHole.Core.Tests
{
    // 계약용 콘텐츠와 판 조립. 샘플 콘텐츠의 [임시] 값에 기대지 않도록, 계약은 필요한 값을 여기서 직접 정한다.
    internal static class TestContent
    {
        public static readonly PlayerId First = new PlayerId(1);
        public static readonly PlayerId Second = new PlayerId(2);

        // 기본: 제한 시간만 있고 업그레이드 노드는 없다.
        public static ContentData Data(float timeLimit = 60) => new ContentData
        {
            Session = new SessionData { TimeLimit = timeLimit }
        };

        public static UpgradeData Upgrade(string id, int price, string requires) =>
            new UpgradeData { Id = id, Price = price, Requires = requires };

        public static GameContent Load(ContentData data)
        {
            ContentLoadResult result = ContentLoader.Load(data);
            Expect.True(result.Succeeded,
                result.Diagnostics.Count > 0 ? result.Diagnostics[0].ToString() : "로드 실패");
            return result.Content;
        }

        // 새 진행 상태의 Player 1명으로 전투를 조립한다.
        public static GameSession Session(ContentData data) =>
            SessionAssembler.CreateBattle(Load(data), new[] { new PlayerState(First) });

        public static void HasDiagnostic(ContentLoadResult result, string path, string reason)
        {
            foreach (ContentDiagnostic diagnostic in result.Diagnostics)
                if (diagnostic.Path == path && diagnostic.Message.Contains(reason)) return;
            throw new System.InvalidOperationException(
                $"진단 없음: {path} ({reason}). 받은 진단: {string.Join(" | ", result.Diagnostics)}");
        }
    }
}
