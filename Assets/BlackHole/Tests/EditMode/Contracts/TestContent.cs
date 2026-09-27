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

        // 기본: 제한 시간만 있다. 적 종류, 공급과 업그레이드 노드는 없다.
        public static ContentData Data(float timeLimit = 60) =>
            new ContentData { Session = new SessionData { TimeLimit = timeLimit } };

        // 전체 개체 수 상한이 계약에 끼어들지 않게 하는 넉넉한 상한.
        public const int RoomyTotal = 1000;

        // 적이 있는 판: 출현 띠 [minDistance, maxDistance], 넉넉한 전체 개체 수 상한, 전투 시작 공급.
        public static ContentData Arena(float minDistance, float maxDistance, params SupplyData[] supply)
        {
            ContentData data = Data();
            data.EnemyPlacement = new EnemyPlacementData { MinDistance = minDistance, MaxDistance = maxDistance };
            data.MaxAliveEnemies = RoomyTotal;
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

        // 이 업그레이드들을 가진 시작 노드 하나의 노드 트리. state가 그 노드를 산 것으로 한다(개발용 치트라 전투 밖에서만).
        public static NodeTree Owned(PlayerState state, params Upgrade[] upgrades)
        {
            var node = new NodeData { Id = "owned", Price = 1, Start = true };

            foreach (Upgrade upgrade in upgrades)
                node.Upgrades.Add(new UpgradeData { Stat = upgrade.Stat, Operation = upgrade.Operation, Value = upgrade.Value });

            var data = new NodeTreeData();
            data.Nodes.Add(node);
            NodeTree tree = NodeTreeLoader.Load(data).Tree;
            ProgressCheats.UnlockAllNodes(state, tree);
            return tree;
        }

        // 콘텐츠에 없는 종류(판이 거부해야 하는 요청에 쓴다).
        public static EnemyDefinition Stranger() =>
            new EnemyDefinition(
                "stranger", 1,
                new[] { new EnemyTier(1, 1, 0) },
                new[] { new MassLevelDefinition(new[] { 1f }, 1, 1) },
                0,
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

        // 새 진행 상태의 Player 1명으로 전투를 조립하고 시작한다(전투 시작 공급까지).
        public static GameSession Session(ContentData data, int seed = SessionAssembler.DefaultSeed) =>
            Begun(SessionAssembler.CreateBattle(Load(data), new PlayerState(First), seed));

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
