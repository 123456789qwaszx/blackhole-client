using System.Collections.Generic;

namespace BlackHole.Core
{
    // 실험용 샘플 콘텐츠의 출발점. 제품 기획/밸런스로 확정한 수치가 아니다.
    // 검증과 조립은 ContentLoader → SessionAssembler가 한다. SO 저작이 들어오면 이 파일을 대체한다.
    public static class ReferenceGame
    {
        public const string StrikeId = "focused-strike";
        public const string PulseId = "gravity-pulse";

        // 호출마다 새 데이터를 만든다. 호출자가 고쳐도 다른 호출에 영향이 없다.
        public static ContentData CreateContent() => new ContentData
        {
            TimeLimit = 60,
            TargetRules = new TargetRulesData { AliveMargin = 0.8f, FallSpeed = 4 },
            // 두 대상은 같은 궤도 규칙의 수치 변형이다.
            Targets = new List<TargetData>
            {
                new TargetData { Id = "shard", MaxHealth = 12, Reward = 2, Movement = Orbit(0.6f, 0.25f) },
                new TargetData { Id = "heavy", MaxHealth = 30, Reward = 5, Movement = Orbit(-0.28f, 0.13f) }
            },
            // 집중 공격 = 단일 대상 선택 + 피해. 중력파 = 범위 선택 + 피해 + 당김.
            Skills = new List<SkillData>
            {
                new SkillData
                {
                    Id = StrikeId, Cooldown = 0.35f,
                    Selection = Select("NearestInRadius", 0.9f),
                    Effects = { Effect("Damage", 14) }
                },
                new SkillData
                {
                    Id = PulseId, Cooldown = 2,
                    Selection = Select("AllInRadius", 2.4f),
                    Effects = { Effect("Damage", 9), Effect("Pull", 0.8f) }
                }
            },
            Growth = new GrowthData { UpgradeCost = 6, MaxPowerLevel = 5, PowerPerLevel = 0.35f },
            Spawn = new SpawnData
            {
                TargetOrder = new List<string> { "shard", "heavy" },
                Interval = 0.8f,
                Radius = 5.5f,
                AngleStep = 2.399963f,
                Capacity = 32
            }
        };

        private static MovementData Orbit(float angularSpeed, float inwardSpeed) =>
            new MovementData { Kind = "Orbit", AngularSpeed = angularSpeed, InwardSpeed = inwardSpeed };

        private static SelectionData Select(string kind, float radius) =>
            new SelectionData { Kind = kind, Radius = radius };

        private static EffectData Effect(string kind, float amount) =>
            new EffectData { Kind = kind, Amount = amount };
    }
}
