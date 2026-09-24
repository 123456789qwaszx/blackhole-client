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
            SessionDuration = 60,
            Targets = new List<TargetData>
            {
                new TargetData { Id = "shard", MaxHealth = 12, AngularSpeed = 0.6f, InwardSpeed = 0.25f, Reward = 2 },
                new TargetData { Id = "heavy", MaxHealth = 30, AngularSpeed = -0.28f, InwardSpeed = 0.13f, Reward = 5 }
            },
            Skills = new List<SkillData>
            {
                new SkillData { Id = StrikeId, Kind = "FocusedStrike", Cooldown = 0.35f, Damage = 14, Radius = 0.9f },
                new SkillData { Id = PulseId, Kind = "GravityPulse", Cooldown = 2, Damage = 9, Radius = 2.4f, PullDistance = 0.8f }
            },
            Growth = new GrowthData { UpgradeCost = 6, MaxPowerLevel = 5, PowerPerLevel = 0.35f },
            Spawn = new SpawnData
            {
                TargetOrder = new List<string> { "shard", "heavy" },
                Interval = 0.8f,
                Radius = 5.5f,
                Capacity = 32
            }
        };
    }
}
