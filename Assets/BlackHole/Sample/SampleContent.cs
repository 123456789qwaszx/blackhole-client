using System.Collections.Generic;
using BlackHole.Core;

namespace BlackHole.Sample
{
    // 샘플 콘텐츠. 여기의 값은 전부 [임시]다 — Reference를 돌리기 위해 채운 값이며 기획 결정이 아니다.
    // Core는 이 어셈블리를 참조하지 않는다(D2: 샘플과 Core 규칙의 분리).
    // [임시] 값의 목록과 이유는 docs/v2/PLAN.md 5절에 있다.
    public static class SampleContent
    {
        public const string LightEnemyId = "sample-light";
        public const string HeavyEnemyId = "sample-heavy";
        public const string AuraSkillId = "sample-aura";

        // 호출마다 새 데이터를 만든다. 호출자가 고쳐도 다른 호출에 영향이 없다.
        public static ContentData Create() => new ContentData
        {
            // [임시] 한 판 길이(시간제는 현재 후보).
            Session = new SessionData { TimeLimit = 60 },
            // [임시] HQ 위치: 월드 중앙.
            Hq = new HqData { X = 0, Y = 0 },
            // [임시] Enemy 2종. 같은 행동(OrbitHq)의 수치 변형이다.
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
                }
            },
            // [임시] 1초마다, 최대 20마리, HQ에서 4.5 거리, 황금각 간격, 두 종류를 번갈아.
            Spawn = new SpawnData
            {
                Interval = 1, MaxAlive = 20, Distance = 4.5f, AngleStep = 2.399963f,
                Order = new List<string> { LightEnemyId, HeavyEnemyId }
            },
            // [임시] 첫 Passive Skill: 소유 Player의 조준점 주변 반경 1.2, 0.5초마다 피해 3.
            Skills = new List<SkillData>
            {
                new SkillData { Id = AuraSkillId, Origin = "OwnerAimPoint", Radius = 1.2f, Interval = 0.5f, Damage = 3 }
            },
            StartingSkills = new List<string> { AuraSkillId }
        };
    }
}
