using System.Collections.Generic;
using BlackHole.Core;

namespace BlackHole.Sample
{
    // 샘플 콘텐츠. 여기의 값은 전부 [임시]다 — Reference를 돌리기 위해 채운 값이며 기획 결정이 아니다.
    // Core는 이 어셈블리를 참조하지 않는다(D2: 샘플과 Core 규칙의 분리).
    // [임시] 값의 목록과 이유는 docs/v2/PLAN.md 7절과 M5·M6·M7 문서에 있다.
    public static class SampleContent
    {
        public const string LightEnemyId = "sample-light";
        public const string HeavyEnemyId = "sample-heavy";
        public const string ElectricEnemyId = "sample-electric";
        public const string AuraSkillId = "sample-aura";
        public const string LaserSkillId = "sample-laser";

        // [임시] 성장 노드의 임계값(누적 EXP). Lv2부터 Lv10까지.
        private static readonly int[] LevelExp = { 6, 14, 24, 36, 50, 66, 84, 104, 126 };

        // 호출마다 새 데이터를 만든다. 호출자가 고쳐도 다른 호출에 영향이 없다.
        public static ContentData Create() => new ContentData
        {
            // [임시] 한 판의 시작 시간(시간제는 현재 후보). 성장할 때마다 늘어난다.
            Session = new SessionData { TimeLimit = 30 },
            // [임시] HQ 위치: 월드 중앙.
            Hq = new HqData { X = 0, Y = 0 },
            // [임시] Enemy 3종. 같은 행동(OrbitHq)의 수치 변형이다. electric만 사망 효과(연쇄 번개)를 가진다(M7).
            Enemies = new List<EnemyData>
            {
                new EnemyData
                {
                    Id = LightEnemyId, MaxHealth = 10, MoveSpeed = 1.5f, Size = 0.3f,
                    Gold = 2, HqExp = 1,
                    Behavior = new EnemyBehaviorData { Kind = "OrbitHq", Clockwise = false }
                },
                new EnemyData
                {
                    Id = HeavyEnemyId, MaxHealth = 30, MoveSpeed = 0.8f, Size = 0.55f,
                    Gold = 5, HqExp = 3,
                    Behavior = new EnemyBehaviorData { Kind = "OrbitHq", Clockwise = true }
                },
                new EnemyData
                {
                    Id = ElectricEnemyId, MaxHealth = 10, MoveSpeed = 1.2f, Size = 0.35f,
                    Gold = 3, HqExp = 2,
                    Behavior = new EnemyBehaviorData { Kind = "OrbitHq", Clockwise = false },
                    DeathEffect = new DeathEffectData { Kind = "ChainLightning", Damage = 10, Range = 2.5f, Chains = 3 }
                }
            },
            // [임시] HQ에서 4.5 거리, 황금각 간격으로 놓는다.
            Spawn = new SpawnData { Distance = 4.5f, AngleStep = 2.399963f },
            // [임시] 전투 시작 배치: light 8, heavy 2, electric 2.
            StartSupply = new List<SupplyData>
            {
                Supply(LightEnemyId, 8),
                Supply(HeavyEnemyId, 2),
                Supply(ElectricEnemyId, 2)
            },
            // [임시] 성장할 때마다 light 6, heavy 1, electric 1이 추가되고 시간이 5초 늘어난다.
            Growth = new GrowthData { Levels = GrowthLevels() },
            // [임시] Breaker: 소유 Player의 조준점 주변 반경 1.2, 0.5초마다 피해 3.
            // [임시] 관통 레이저: 1.5초마다 한 발, 예고 0.6초, 굵기 0.4, 피해 5. 시작점은 HQ에서 16
            // (카메라 크기 7.5인 16:9 화면의 반대각선 약 15.3보다 멀다). 시작 구성에 넣지 않고 laser-unlock 노드로 얻는다.
            Skills = new List<SkillData>
            {
                new SkillData { Id = AuraSkillId, Kind = "Breaker", Radius = 1.2f, Interval = 0.5f, Damage = 3 },
                new SkillData
                {
                    Id = LaserSkillId, Kind = "PiercingLaser", Interval = 1.5f, Damage = 5,
                    Width = 0.4f, TelegraphDuration = 0.6f, BoundaryRadius = 16
                }
            },
            StartingSkills = new List<string> { AuraSkillId },
            // [임시] 업그레이드 샘플 트리. 원래는 노드 저작 툴이 만들 데이터다(M6).
            Upgrades = new List<UpgradeData>
            {
                Upgrade("breaker-damage", 10, null, Effect("SkillDamageAdd", 2, AuraSkillId)),
                Upgrade("breaker-radius", 15, "breaker-damage", Effect("SkillRadiusAdd", 0.3f, AuraSkillId)),
                Upgrade("growth-supply", 25, "breaker-radius", Effect("GrowthSupplyAdd", 2, LightEnemyId)),
                Upgrade("breaker-speed", 20, "breaker-damage", Effect("SkillIntervalMultiply", 0.8f, AuraSkillId)),
                Upgrade("golden-touch", 40, "breaker-speed", Effect("GoldMultiply", 1.5f)),
                Upgrade("dense-matter", 30, "breaker-damage",
                    Effect("EnemyHealthMultiply", 1.5f),
                    Effect("GoldMultiply", 1.5f),
                    Effect("HqExpMultiply", 1.5f)),
                // [임시] CONTENT_DEFINITION 3.2의 레이저 노드. 해금은 값이 없는 효과라 0을 적는다(AUTHORING_PAIN AP9).
                Upgrade("laser-unlock", 30, "breaker-damage", Effect("SkillUnlock", 0, LaserSkillId)),
                Upgrade("laser-width", 20, "laser-unlock", Effect("LaserWidthAdd", 0.2f, LaserSkillId))
            }
        };

        private static List<GrowthLevelData> GrowthLevels()
        {
            var levels = new List<GrowthLevelData>();

            foreach (int exp in LevelExp)
            {
                levels.Add(new GrowthLevelData
                {
                    Exp = exp,
                    ExtraTime = 5,
                    Supply = new List<SupplyData>
                    {
                        Supply(LightEnemyId, 6),
                        Supply(HeavyEnemyId, 1),
                        Supply(ElectricEnemyId, 1)
                    }
                });
            }

            return levels;
        }

        private static SupplyData Supply(string enemy, int count) =>
            new SupplyData { Enemy = enemy, Count = count };

        private static UpgradeData Upgrade(string id, int price, string requires, params UpgradeEffectData[] effects) =>
            new UpgradeData { Id = id, Price = price, Requires = requires, Effects = new List<UpgradeEffectData>(effects) };

        private static UpgradeEffectData Effect(string kind, float value, string target = null) =>
            new UpgradeEffectData { Kind = kind, Value = value, Target = target };
    }
}
