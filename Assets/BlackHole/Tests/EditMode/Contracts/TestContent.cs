using System.Collections.Generic;
using BlackHole.Core;

namespace BlackHole.Core.Tests
{
    // 계약용 콘텐츠와 판 조립. 샘플 콘텐츠의 [임시] 값에 기대지 않도록, 계약은 필요한 값을 여기서 직접 정한다.
    internal static class TestContent
    {
        public static readonly PlayerId First = new PlayerId(1);
        public static readonly PlayerId Second = new PlayerId(2);
        public const string EnemyId = "test-enemy";

        public const int StageCount = 50;

        // 기본: 제한 시간과 단계 수(1~50)만 있고 적과 업그레이드 노드는 없다.
        public static ContentData Data(float timeLimit = 60) => new ContentData
        {
            Session = new SessionData { TimeLimit = timeLimit },
            StageCount = StageCount
        };

        // 적이 있는 판: 출현 띠 [minDistance, maxDistance]와 전투 시작 공급.
        public static ContentData Arena(float minDistance, float maxDistance, params SupplyData[] supply)
        {
            ContentData data = Data();
            data.EnemyPlacement = new EnemyPlacementData { MinDistance = minDistance, MaxDistance = maxDistance };
            data.StartSupply = new List<SupplyData>(supply);
            return data;
        }

        public static EnemyData Enemy(string id, float health = 10, float speed = 1, float size = 0.3f, bool clockwise = false) =>
            new EnemyData
            {
                Id = id, MaxHealth = health, MoveSpeed = speed, Size = size,
                Behavior = new EnemyBehaviorData { Kind = "Orbit", Clockwise = clockwise }
            };

        public static SupplyData Supply(string enemyId, int count) =>
            new SupplyData { Enemy = enemyId, Count = count };

        public static UpgradeData Upgrade(string id, int price, string requires) =>
            new UpgradeData { Id = id, Price = price, Requires = requires };

        public static GameContent Load(ContentData data)
        {
            ContentLoadResult result = ContentLoader.Load(data);
            Expect.True(result.Succeeded,
                result.Diagnostics.Count > 0 ? result.Diagnostics[0].ToString() : "로드 실패");
            return result.Content;
        }

        // 새 진행 상태의 Player 1명으로 첫 단계의 전투를 조립한다.
        public static GameSession Session(ContentData data, int seed = SessionAssembler.DefaultSeed) =>
            SessionAssembler.CreateBattle(Load(data), new[] { new PlayerState(First) }, SessionAssembler.FirstStage, seed);

        public static float DistanceToHq(Point2 point) =>
            (float)System.Math.Sqrt(point.DistanceSquared(BattleSpace.Origin));

        public static void HasDiagnostic(ContentLoadResult result, string path, string reason)
        {
            foreach (ContentDiagnostic diagnostic in result.Diagnostics)
                if (diagnostic.Path == path && diagnostic.Message.Contains(reason)) return;
            throw new System.InvalidOperationException(
                $"진단 없음: {path} ({reason}). 받은 진단: {string.Join(" | ", result.Diagnostics)}");
        }
    }
}
