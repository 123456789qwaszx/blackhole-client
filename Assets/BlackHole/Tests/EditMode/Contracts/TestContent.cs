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
        public const string SkillId = "test-skill";

        // 기본: Enemy 1종(HP 10, 속도 1, 크기 0.3, 반시계 공전)이 판 시작에 HQ에서 거리 3, 각도 0에 1마리. 성장 노드 없음.
        // 시작 Skill 1개(조준점 기준, 반경 1, 0.5초마다, 피해 1). 조준점을 넣지 않으면 아무도 맞지 않는다.
        public static ContentData Data(float timeLimit = 60, float hqX = 0, float hqY = 0) => new ContentData
        {
            Session = new SessionData { TimeLimit = timeLimit },
            Hq = new HqData { X = hqX, Y = hqY },
            Enemies = new List<EnemyData> { Enemy(EnemyId, 10, 1, 0.3f) },
            Spawn = new SpawnData { Distance = 3, AngleStep = 1 },
            StartSupply = new List<SupplyData> { Supply(EnemyId, 1) },
            Growth = new GrowthData(),
            Skills = new List<SkillData> { Skill(SkillId, radius: 1, interval: 0.5f, damage: 1) },
            StartingSkills = new List<string> { SkillId }
        };

        // Skill 계약용: 거의 움직이지 않는 Enemy(HP 100)가 판 시작에 HQ(0, 0)에서 거리 3에 enemies마리.
        // 각도 간격이 π라 첫째는 (3, 0), 둘째는 (-3, 0)이다.
        public static ContentData SkillArena(int enemies, float radius, float interval, float damage)
        {
            ContentData data = Data();
            data.Enemies = new List<EnemyData> { Enemy(EnemyId, 100, 0.0001f, 0.3f) };
            data.Spawn = new SpawnData { Distance = 3, AngleStep = (float)System.Math.PI };
            data.StartSupply = new List<SupplyData> { Supply(EnemyId, enemies) };
            data.Skills = new List<SkillData> { Skill(SkillId, radius, interval, damage) };
            return data;
        }

        public static SkillData Skill(string id, float radius, float interval, float damage) =>
            new SkillData { Id = id, Kind = "Breaker", Radius = radius, Interval = interval, Damage = damage };

        public static SkillData Laser(string id, float interval, float damage, float width, float telegraph, float boundaryRadius = 10) =>
            new SkillData
            {
                Id = id, Kind = "PiercingLaser", Interval = interval, Damage = damage,
                Width = width, TelegraphDuration = telegraph, BoundaryRadius = boundaryRadius
            };

        // 레이저 계약용: 거의 움직이지 않는 Enemy(HP 100)가 판 시작에 HQ(0, 0)에서 거리 3에 enemies마리.
        // n번째는 각도 n × angleStep에 있다. 시작 Skill은 레이저 하나이고 전투 영역 반지름은 10이다.
        public static ContentData LaserArena(int enemies, float angleStep, float interval, float damage, float width, float telegraph)
        {
            ContentData data = SkillArena(enemies, radius: 1, interval: 1, damage: 1);
            data.Spawn = new SpawnData { Distance = 3, AngleStep = angleStep };
            data.Skills = new List<SkillData> { Laser(LaserId, interval, damage, width, telegraph) };
            data.StartingSkills = new List<string> { LaserId };
            return data;
        }

        public const string LaserId = "test-laser";

        public static BreakerSkill Breaker(Player player, int index = 0) => (BreakerSkill)player.Skills[index];

        public static PiercingLaserSkill LaserSkill(Player player, int index = 0) => (PiercingLaserSkill)player.Skills[index];

        public static EnemyData Enemy(string id, float health, float speed, float size, bool clockwise = false) =>
            new EnemyData
            {
                Id = id, MaxHealth = health, MoveSpeed = speed, Size = size,
                Behavior = new EnemyBehaviorData { Kind = "OrbitHq", Clockwise = clockwise }
            };

        public static DeathEffectData ChainLightning(float damage, float range, int chains) =>
            new DeathEffectData { Kind = "ChainLightning", Damage = damage, Range = range, Chains = chains };

        public static SupplyData Supply(string enemyId, int count) =>
            new SupplyData { Enemy = enemyId, Count = count };

        // 성장 노드 하나: 임계값, 시간 연장, 추가 공급.
        public static GrowthLevelData Level(int exp, float extraTime = 0, params SupplyData[] supply) =>
            new GrowthLevelData { Exp = exp, ExtraTime = extraTime, Supply = new List<SupplyData>(supply) };

        public static UpgradeData Upgrade(string id, int price, string requires, params UpgradeEffectData[] effects) =>
            new UpgradeData { Id = id, Price = price, Requires = requires, Effects = new List<UpgradeEffectData>(effects) };

        public static UpgradeEffectData Effect(string kind, float value, string target = null) =>
            new UpgradeEffectData { Kind = kind, Value = value, Target = target };

        public static GameContent Load(ContentData data)
        {
            ContentLoadResult result = ContentLoader.Load(data);
            Expect.True(result.Succeeded,
                result.Diagnostics.Count > 0 ? result.Diagnostics[0].ToString() : "로드 실패");
            return result.Content;
        }

        // 참가자를 따로 주지 않으면 Player 1명.
        public static GameSession Session(ContentData data, params PlayerId[] participants) =>
            SessionAssembler.Create(Load(data), participants.Length == 0 ? new[] { First } : participants);

        // Player 1명, seed를 정한 전투.
        public static GameSession Session(ContentData data, int seed) =>
            SessionAssembler.CreateBattle(Load(data), new[] { new PlayerState(First) }, seed);

        public static void HasDiagnostic(ContentLoadResult result, string path, string reason)
        {
            foreach (ContentDiagnostic diagnostic in result.Diagnostics)
                if (diagnostic.Path == path && diagnostic.Message.Contains(reason)) return;
            throw new System.InvalidOperationException(
                $"진단 없음: {path} ({reason}). 받은 진단: {string.Join(" | ", result.Diagnostics)}");
        }

        public static float DistanceToHq(GameSession game, Enemy enemy) =>
            (float)System.Math.Sqrt(enemy.Position.DistanceSquared(game.World.Hq.Position));
    }
}
