using System;

namespace BlackHole.Core
{
    // 한 판의 블랙홀(HQ): 누적 EXP와 Level, 이 판의 Level업마다 늘어나는 시간(BLACKHOLE_GROWTH_PLAN 4, BLACKHOLE_LEVEL_PLAN 4.1).
    // 판마다 만들지만 진행 상태의 누적 EXP(PlayerState.HqExp)에서 시작한다 — Level은 판을 넘어 이어진다. 결산이 EXP를 돌려놓는다.
    // - EXP: 사망이 확정되는 순간 그 적의 EXP가 든다(World가 넣는다). 누가 부쉈는지 보지 않는다. 전투 정리는 주지 않는다.
    // - Level: Step의 5 자리에서만 오른다(RaiseLevels). 한 번에 여러 임계값을 넘으면 여러 Level이 오른다. 표 끝에서는 EXP만 쌓인다.
    // 블랙홀은 판에 하나이고 판 안의 모든 참가자가 함께 키운다. 그림의 크기는 화면만의 것이다 — 공간 규칙은 바뀌지 않는다.
    public sealed class Hq
    {
        public HqGrowthDefinition Growth { get; }
        // Level업마다 이 판의 제한 시간에 더하는 초. 판 조립 때 산 노드로 정해졌다(hq.growth-time).
        public float GrowthTime { get; }
        public long Exp { get; private set; }
        public int Level { get; private set; } = HqGrowthDefinition.StartLevel;
        // 판을 시작할 때의 Level. 이 판의 판 구성(색 비율)이 이 Level로 정해졌다. 판 중에 Level이 올라도 그대로다.
        public int StartLevel { get; }
        public bool IsMaxLevel => Level >= Growth.MaxLevel;
        // 다음 Level에 닿는 누적 EXP. 마지막 Level이면 null이다.
        public long? NextLevelExp => Growth.ExpToReach(Level + 1);

        // 지금 Level의 임계값에서 다음 임계값까지 몇 %인가(0 ~ 1). 마지막 Level이면 1이다.
        // EXP는 사망 순간에 들고 Level은 Step의 5 자리에서 오르므로, 그 사이에는 1에서 멈춘다.
        public float Progress
        {
            get
            {
                long? next = NextLevelExp;

                if (!next.HasValue)
                    return 1;

                long from = Growth.ExpToReach(Level).Value;
                return (float)Math.Min(1, (double)(Exp - from) / (next.Value - from));
            }
        }

        // exp: 판을 시작할 때의 누적 EXP(진행 상태의 것). 그 EXP가 닿는 Level에서 시작한다 — 이 Level들의 성장 효과는 이미 지난 판의 것이다.
        internal Hq(HqGrowthDefinition growth, float growthTime, long exp = 0)
        {
            Growth = growth ?? throw new ArgumentNullException(nameof(growth));

            if (float.IsNaN(growthTime) || float.IsInfinity(growthTime) || growthTime < 0)
                throw new ArgumentOutOfRangeException(nameof(growthTime), "0 이상의 유한한 값이 필요하다.");

            GrowthTime = growthTime;
            Exp = DefinitionGuard.NotNegative(exp, nameof(exp));
            Level = growth.LevelAt(Exp);
            StartLevel = Level;
        }

        internal void AddExp(long exp) => Exp = checked(Exp + exp);

        // 5. HQ EXP / Level 반영: 쌓인 EXP로 닿은 Level까지 올리고, 오른 Level 수를 돌려준다.
        internal int RaiseLevels()
        {
            int raised = 0;

            while (NextLevelExp is long next && Exp >= next)
            {
                Level++;
                raised++;
            }

            return raised;
        }
    }
}
