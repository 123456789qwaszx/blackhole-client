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
        // 기본 단계 표가 쓰는 풀과 그 풀의 적 종류. 공급하지 않으므로 판에 나오지 않는다.
        public const string PoolId = "test-pool";
        public const string PoolEnemyId = "pool-enemy";
        public const int StageCount = 50;

        // 기본: 제한 시간과 단계 표(1~50단계, 모두 같은 풀)만 있다. 공급과 업그레이드 노드는 없다.
        public static ContentData Data(float timeLimit = 60)
        {
            var data = new ContentData { Session = new SessionData { TimeLimit = timeLimit } };
            data.Enemies.Add(Enemy(PoolEnemyId));
            data.EnemyPools.Add(Pool(PoolId, PoolEnemyId));
            AddStages(data, PoolId, StageCount);
            return data;
        }

        public static EnemyPoolData Pool(string id, params string[] enemies) =>
            new EnemyPoolData { Id = id, Enemies = new List<string>(enemies) };

        // 단계 표 뒤에 pool을 쓰는 단계를 count개 붙인다.
        public static void AddStages(ContentData data, string pool, int count)
        {
            for (int i = 0; i < count; i++)
                data.Stages.Add(new StageData { Pool = pool });
        }

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
