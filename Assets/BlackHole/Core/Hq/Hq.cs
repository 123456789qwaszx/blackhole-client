using System;

namespace BlackHole.Core
{
    // 한 판의 블랙홀(HQ): 이 판의 성장도, 이 판의 EXP와 Level, 판 Level업마다 늘어나는 시간(BATTLE_COMPOSITION_PLAN 8절).
    // 판마다 새로 만든다. Level·EXP는 매 판 0에서 시작하고 판이 끝나면 버린다. 판을 넘어 남는 것은 성장도다(결산이 NextStage를 진행 상태에 반영한다).
    // - 성장도: 판을 시작할 때 진행 상태의 것. 판 동안 바뀌지 않는다. 이 판의 Level 표와 목표 Level을 정한다.
    // - EXP: 사망이 확정되는 순간 그 적의 EXP가 든다(World가 넣는다). 누가 부쉈는지 보지 않는다. 전투 정리는 주지 않는다.
    // - Level: Step의 5 자리에서만 오른다(RaiseLevels). 한 번에 여러 임계값을 넘으면 여러 Level이 오른다. 표 끝에서는 EXP만 쌓인다.
    // - 이정표: 다음 성장도가 이정표이고 목표 Level에 닿으면 이 판의 이정표로 남는다. 판은 그 Step에서 끝난다(World·GameSession).
    // 블랙홀은 판에 하나이고 판 안의 모든 참가자가 함께 키운다. 그림의 크기는 화면만의 것이다 — 공간 규칙은 바뀌지 않는다.
    public sealed class Hq
    {
        public HqGrowthDefinition Growth { get; }
        // 이 판의 성장도와 그 Level 표.
        public int Stage { get; }
        public GrowthStageDefinition StageTable { get; }
        // 판 Level업마다 이 판의 제한 시간에 더하는 초. 판 조립 때 산 노드로 정해졌다(hq.growth-time).
        public float GrowthTime { get; }
        public long Exp { get; private set; }
        public int Level { get; private set; } = GrowthStageDefinition.StartLevel;
        public bool IsMaxLevel => Level >= StageTable.MaxLevel;
        // 다음 Level에 닿는 이 판의 누적 EXP. 마지막 Level이면 null이다.
        public long? NextLevelExp => StageTable.ExpToReach(Level + 1);
        // 이번 성장도의 목표 Level. 0이면 목표가 없다(마지막 성장도).
        public int GoalLevel => StageTable.GoalLevel;
        // 이 판이 목표 Level에 닿았는가. 닿았으면 결산이 성장도를 1 올린다.
        public bool ReachedGoal => GoalLevel != GrowthStageDefinition.NoGoal && Level >= GoalLevel;
        // 결산 뒤의 성장도: 목표에 닿았으면 +1(마지막 성장도면 그대로), 아니면 그대로.
        public int NextStage => ReachedGoal && Stage < Growth.MaxStage ? Stage + 1 : Stage;
        // 이 판에서 닿은 이정표. 없으면 null이다. 판을 시작할 때 이미 지난 이정표는 다시 오지 않는다 — 성장도는 줄지 않는다.
        public HqMilestone Milestone { get; private set; }
        public bool ReachedMilestone => Milestone != null;
        // 이 판에서 닿은 이정표의 보상. 결산이 번 Gold 대신 이것을 준다.
        public long MilestoneReward => Milestone?.Reward ?? 0;

        // 지금 Level의 임계값에서 다음 임계값까지 몇 %인가(0 ~ 1). 마지막 Level이면 1이다.
        // EXP는 사망 순간에 들고 Level은 Step의 5 자리에서 오르므로, 그 사이에는 1에서 멈춘다.
        public float Progress => StageTable.ProgressAt(Level, Exp);

        // stage: 판을 시작할 때의 성장도(진행 상태의 것). Level 0, EXP 0에서 시작한다.
        internal Hq(HqGrowthDefinition growth, float growthTime, int stage = HqGrowthDefinition.StartStage)
        {
            Growth = growth ?? throw new ArgumentNullException(nameof(growth));

            if (float.IsNaN(growthTime) || float.IsInfinity(growthTime) || growthTime < 0)
                throw new ArgumentOutOfRangeException(nameof(growthTime), "0 이상의 유한한 값이 필요하다.");

            GrowthTime = growthTime;
            StageTable = growth.StageAt(stage);
            Stage = stage;
        }

        internal void AddExp(long exp) => Exp = checked(Exp + exp);

        // 5. HQ EXP / Level 반영: 쌓인 EXP로 닿은 Level까지 올리고, 오른 Level 수를 돌려준다.
        // 목표 Level에 닿아 다음 성장도가 이정표면 이 판의 이정표로 남긴다.
        internal int RaiseLevels()
        {
            int raised = 0;

            while (NextLevelExp is long next && Exp >= next)
            {
                Level++;
                raised++;
            }

            if (Milestone == null && NextStage > Stage)
                Milestone = Growth.MilestoneAt(NextStage);

            return raised;
        }
    }
}
