using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 블랙홀 성장의 공유 정의: 성장도마다의 판 Level 표와 목표 Level, 이정표(BATTLE_COMPOSITION_PLAN 8절).
    // - 성장도: 판 밖의 진행(PlayerState.GrowthStage). 새 진행은 0이다. Stages[s]가 성장도 s의 판에서 쓰는 표다. 마지막 성장도는 Stages.Count - 1이다.
    // - 판 Level: 매 판 0에서 시작한다. 이번 성장도의 표로 오른다.
    // - 성장도는 결산 때만 오른다: 이번 판이 이번 성장도의 목표 Level에 닿았으면 +1(한 판에 +1까지, 마지막 성장도에서는 그대로).
    // - 이정표: 정해진 성장도와 고정 보상. 이정표 바로 앞 성장도의 판에서 목표 Level에 닿으면 그 Step에서 판이 끝나고, 결산이 번 Gold 대신 보상을 준다.
    // 성장 효과(시간·공급)는 표에 두지 않는다 — 산 노드가 정하고 판 Level업마다 같은 값이 온다.
    public sealed class HqGrowthDefinition
    {
        public const int StartStage = 0;

        public static readonly HqGrowthDefinition None = new HqGrowthDefinition(Array.Empty<GrowthStageDefinition>());

        public IReadOnlyList<GrowthStageDefinition> Stages { get; }
        public int MaxStage => Math.Max(StartStage, Stages.Count - 1);
        // 이정표(성장도가 커지는 순서). 한 성장도에 하나다.
        public IReadOnlyList<HqMilestone> Milestones { get; }

        public HqGrowthDefinition(IReadOnlyList<GrowthStageDefinition> stages, IReadOnlyList<HqMilestone> milestones = null)
        {
            if (stages == null)
                throw new ArgumentNullException(nameof(stages));

            var rows = new GrowthStageDefinition[stages.Count];

            for (int i = 0; i < rows.Length; i++)
            {
                rows[i] = stages[i] ?? throw new ArgumentException($"성장도 {i}의 표가 null이다.", nameof(stages));

                // 마지막 성장도가 아니면 다음 성장도로 가는 목표가 있어야 한다. 목표 0은 그 성장도에서 멈춘다는 뜻이다.
                if (i < rows.Length - 1 && rows[i].GoalLevel == GrowthStageDefinition.NoGoal)
                    throw new ArgumentException($"성장도 {i}의 목표 Level이 0이다. 마지막이 아닌 성장도는 목표 Level이 1 이상이어야 한다.", nameof(stages));
            }

            Stages = Array.AsReadOnly(rows);

            var marks = milestones != null ? new HqMilestone[milestones.Count] : Array.Empty<HqMilestone>();

            for (int i = 0; i < marks.Length; i++)
            {
                HqMilestone mark = milestones[i] ?? throw new ArgumentException($"이정표 {i}가 null이다.", nameof(milestones));

                if (mark.Stage <= StartStage || mark.Stage > MaxStage)
                    throw new ArgumentOutOfRangeException(nameof(milestones), $"이정표 {i}의 성장도 {mark.Stage}는 {StartStage + 1}부터 {MaxStage}까지(성장도 표 안)여야 한다.");

                if (i > 0 && mark.Stage <= marks[i - 1].Stage)
                    throw new ArgumentOutOfRangeException(nameof(milestones), $"이정표 {i}의 성장도 {mark.Stage}는 앞 이정표의 {marks[i - 1].Stage}보다 커야 한다.");

                marks[i] = mark;
            }

            Milestones = Array.AsReadOnly(marks);
        }

        // 성장도 stage의 판에서 쓰는 표. 표가 없는 정의(None)는 Level이 오르지 않는 빈 표다.
        public GrowthStageDefinition StageAt(int stage)
        {
            if (stage < StartStage || stage > MaxStage)
                throw new ArgumentOutOfRangeException(nameof(stage), $"성장도는 {StartStage}부터 {MaxStage}까지다. 받은 값: {stage}.");

            return Stages.Count == 0 ? GrowthStageDefinition.Empty : Stages[stage];
        }

        // 이 성장도에 닿으면 받는 이정표. 없으면 null.
        public HqMilestone MilestoneAt(int stage)
        {
            foreach (HqMilestone mark in Milestones)
            {
                if (mark.Stage == stage)
                    return mark;
            }

            return null;
        }

        // 이 성장도 이하인 이정표의 수(이정표 진행도 n / Milestones.Count).
        public int MilestonesReachedBy(int stage)
        {
            int reached = 0;

            foreach (HqMilestone mark in Milestones)
            {
                if (mark.Stage <= stage)
                    reached++;
            }

            return reached;
        }
    }

    // 성장도 하나의 판 Level 표: LevelExp[i]는 이 판에서 Level (i + 1)에 닿는 누적 EXP(Level 0 = EXP 0에서 센다). 앞 줄보다 커야 한다.
    // 목표 Level: 이 판에서 여기에 닿으면 결산 때 성장도가 1 오른다. 1 이상, 표 안(LevelExp.Count 이하)이다. 0이면 목표가 없다.
    // 표 끝에서는 Level이 더 오르지 않고 EXP만 쌓인다.
    public sealed class GrowthStageDefinition
    {
        public const int StartLevel = 0;
        public const int NoGoal = 0;

        internal static readonly GrowthStageDefinition Empty = new GrowthStageDefinition(Array.Empty<long>(), NoGoal);

        public IReadOnlyList<long> LevelExp { get; }
        public int MaxLevel => StartLevel + LevelExp.Count;
        public int GoalLevel { get; }

        public GrowthStageDefinition(IReadOnlyList<long> levelExp, int goalLevel)
        {
            if (levelExp == null)
                throw new ArgumentNullException(nameof(levelExp));

            var copy = new long[levelExp.Count];

            for (int i = 0; i < copy.Length; i++)
            {
                long exp = levelExp[i];

                if (exp <= 0)
                    throw new ArgumentOutOfRangeException(nameof(levelExp), $"{i}번 줄(Level {i + 1})의 누적 EXP는 양수여야 한다.");

                if (i > 0 && exp <= copy[i - 1])
                    throw new ArgumentOutOfRangeException(nameof(levelExp),
                        $"{i}번 줄(Level {i + 1})의 누적 EXP {exp}는 앞 줄의 {copy[i - 1]}보다 커야 한다.");

                copy[i] = exp;
            }

            LevelExp = Array.AsReadOnly(copy);

            if (goalLevel < NoGoal || goalLevel > MaxLevel)
                throw new ArgumentOutOfRangeException(nameof(goalLevel), $"목표 Level은 0부터 {MaxLevel}까지(표 안)여야 한다. 받은 값: {goalLevel}.");

            GoalLevel = goalLevel;
        }

        // 이 판 EXP가 닿는 Level.
        public int LevelAt(long exp)
        {
            int level = StartLevel;

            while (ExpToReach(level + 1) is long next && exp >= next)
                level++;

            return level;
        }

        // Level level에서 이 판 EXP exp일 때, 그 Level의 임계값에서 다음 임계값까지 몇 %인가(0 ~ 1). 마지막 Level이면 1이다.
        public float ProgressAt(int level, long exp)
        {
            long? next = ExpToReach(level + 1);
            long? from = ExpToReach(level);

            if (!next.HasValue || !from.HasValue)
                return 1;

            return (float)Math.Max(0, Math.Min(1, (double)(exp - from.Value) / (next.Value - from.Value)));
        }

        // 이 Level에 닿는 이 판의 누적 EXP. Level 0은 0이다. 표 밖이면 null이다.
        public long? ExpToReach(int level)
        {
            if (level == StartLevel)
                return 0;

            int index = level - StartLevel - 1;
            return index >= 0 && index < LevelExp.Count ? LevelExp[index] : (long?)null;
        }
    }

    // 이정표 하나: 이 성장도에 닿으면 받는다. 그 앞 성장도의 판이 목표 Level에 닿는 순간 판이 끝나고, 결산이 그 판에서 번 Gold 대신 이 보상을 준다.
    // 금액은 기획자가 정한다 [사용자].
    public sealed class HqMilestone
    {
        public int Stage { get; }
        public long Reward { get; }

        public HqMilestone(int stage, long reward)
        {
            Stage = stage;
            Reward = DefinitionGuard.NotNegative(reward, nameof(reward));
        }
    }

    // 블랙홀이 공개하는 업그레이드 수치 이름. 적 종류마다의 성장 공급은 EnemyUpgradeStats.GrowthSupply다.
    public static class HqUpgradeStats
    {
        // 판 Level업마다 이 판의 제한 시간에 더하는 초. 기본값 0(성장 노드를 사기 전에는 시간이 늘지 않는다).
        public const string GrowthTime = "hq.growth-time";

        // 업그레이드 표로 판 Level업마다 늘어나는 시간을 계산한다. 음수·무한은 예외다(노드 저작 오류, UpgradeContentCheck가 로드 때 찾는다).
        public static float GrowthTimeFrom(UpgradeTable upgrades)
        {
            if (upgrades == null)
                throw new ArgumentNullException(nameof(upgrades));

            float seconds = upgrades.Apply(GrowthTime, 0);

            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0)
                throw new ArgumentOutOfRangeException(nameof(upgrades), $"Level업마다 늘어나는 시간은 0 이상의 유한한 값이어야 한다. 업그레이드 합: {seconds}.");

            return seconds;
        }
    }
}
