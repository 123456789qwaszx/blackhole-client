namespace BlackHole.Core
{
    // 한 판의 HQ 성장 진행. HQ가 새로 도달한 Level을 낮은 것부터 한 번씩 보고, 그 Level의 성장 효과를 실행한다.
    // HQ는 EXP와 Level만 가진다. 공급 요청과 시간 연장은 여기서 한다(GAME_RULES 4·9절).
    internal sealed class GrowthProgression
    {
        private readonly HqGrowthDefinition _growth;
        private readonly TimeLimitRule _timeLimit;
        // 여기까지의 Level은 효과를 처리했다. Level은 내려가지 않으므로 효과는 처음 도달했을 때 한 번뿐이다.
        private int _handledLevel = HqGrowthDefinition.StartLevel;

        public GrowthProgression(
            HqGrowthDefinition growth,
            TimeLimitRule timeLimit)
        {
            _growth = growth;
            _timeLimit = timeLimit;
        }

        public void Advance(Hq hq, EnemySupply supply)
        {
            while (_handledLevel < hq.Level)
            {
                _handledLevel++;

                if (!_growth.TryGetLevel(_handledLevel, out HqLevelDefinition level))
                    continue;

                supply.Request(level.Supply, SupplySource.Growth);
                _timeLimit.Extend(level.ExtraTime);
            }
        }
    }
}
