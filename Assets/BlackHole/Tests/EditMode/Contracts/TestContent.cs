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

        // 풀의 항목 수 제한이 계약에 끼어들지 않게 하는 넉넉한 최대 수.
        public const int RoomyMax = 100;

        // 모든 항목의 최대 수가 넉넉한 풀.
        public static EnemyPoolData Pool(string id, params string[] enemies)
        {
            var pool = new EnemyPoolData { Id = id };

            foreach (string enemy in enemies)
                pool.Entries.Add(Entry(enemy, RoomyMax));

            return pool;
        }

        public static EnemyPoolEntryData Entry(string enemy, int maxAlive) =>
            new EnemyPoolEntryData { Enemy = enemy, MaxAlive = maxAlive };

        // 기본 단계 표의 풀(모든 단계가 쓴다)에 이 종류를 넣는다. 공급된 적은 풀에 있어야 나온다.
        public static void Allow(ContentData data, string enemy, int maxAlive = RoomyMax)
        {
            foreach (EnemyPoolData pool in data.EnemyPools)
            {
                if (pool.Id == PoolId)
                {
                    pool.Entries.Add(Entry(enemy, maxAlive));
                    return;
                }
            }

            throw new System.InvalidOperationException("기본 풀이 없다.");
        }

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

        // 색 등급 한 줄, 질량 단계 한 줄(그 색만, 계수 1)인 종류.
        public static EnemyData Enemy(
            string id, float health = 10, float speed = 1, float size = 0.3f, bool clockwise = false, long gold = 0)
        {
            EnemyData enemy = Tiered(id, speed, clockwise, Tier(health, size, gold));
            enemy.MassLevels.Add(MassLevel(1, 1, 1));
            return enemy;
        }

        // 색 등급을 여러 줄 가진 종류. 질량 단계는 부른 쪽이 MassLevels에 넣는다.
        public static EnemyData Tiered(string id, float speed, bool clockwise, params EnemyTierData[] tiers) =>
            new EnemyData
            {
                Id = id, MoveSpeed = speed, Tiers = new List<EnemyTierData>(tiers),
                Behavior = new EnemyBehaviorData { Kind = "Orbit", Clockwise = clockwise }
            };

        public static EnemyTierData Tier(float health, float size, long gold) =>
            new EnemyTierData { MaxHealth = health, Size = size, Gold = gold };

        public static MassLevelData MassLevel(float health, float gold, params float[] tierRatios) =>
            new MassLevelData { TierRatios = new List<float>(tierRatios), HealthMultiplier = health, GoldMultiplier = gold };

        // 콘텐츠에 없는 종류(판이 거부해야 하는 요청에 쓴다).
        public static EnemyDefinition Stranger() =>
            new EnemyDefinition(
                "stranger", 1,
                new[] { new EnemyTier(1, 1, 0) },
                new[] { new MassLevelDefinition(new[] { 1f }, 1, 1) },
                new OrbitBehaviorDefinition(false));

        public static SupplyData Supply(string enemyId, int count) =>
            new SupplyData { Enemy = enemyId, Count = count };

        public static GameContent Load(ContentData data)
        {
            ContentLoadResult result = ContentLoader.Load(data);
            Expect.True(result.Succeeded,
                result.Diagnostics.Count > 0 ? result.Diagnostics[0].ToString() : "로드 실패");
            return result.Content;
        }

        // 새 진행 상태의 Player 1명으로 첫 단계의 전투를 조립하고 시작한다(전투 시작 공급까지).
        public static GameSession Session(ContentData data, int seed = SessionAssembler.DefaultSeed) =>
            Begun(SessionAssembler.CreateBattle(Load(data), new[] { new PlayerState(First) }, SessionAssembler.FirstStage, seed));

        public static GameSession Begun(GameSession session)
        {
            session.Begin();
            return session;
        }

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
