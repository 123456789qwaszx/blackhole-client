namespace BlackHole.Core
{
    // 실험용 콘텐츠와 조립 위치. 제품 기획/밸런스로 확정한 수치가 아니다.
    public static class ReferenceGame
    {
        public const string StrikeId = "focused-strike";
        public const string PulseId = "gravity-pulse";
        public const float PulseRadius = 2.4f;

        public static GameSession CreateSession(float duration = 60)
        {
            var targets = new[]
            {
                new TargetDefinition("shard", 12, 0.6f, 0.25f, 2),
                new TargetDefinition("heavy", 30, -0.28f, 0.13f, 5)
            };
            var skills = new[]
            {
                new SkillDefinition(StrikeId, 0.35f, new FocusedStrike(14, 0.9f)),
                new SkillDefinition(PulseId, 2f, new GravityPulse(9, PulseRadius, 0.8f))
            };
            var field = new Playfield(
                new SpawnSchedule(targets, 0.8f, 5.5f, 32),
                new SkillLoadout(skills),
                new GrowthState(new GrowthDefinition(6, 5, 0.35f)));
            return new GameSession(field, duration);
        }
    }
}
