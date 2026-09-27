using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 블랙홀 성장의 공유 정의: Level 표(BLACKHOLE_GROWTH_PLAN 4.2).
    // 새 진행은 Level 1(누적 EXP 0)이다. LevelExp[i]는 Level (i + 2)에 닿는 누적 EXP이며, 앞 줄보다 커야 한다. Level은 판을 넘어 이어진다.
    // 성장 효과(시간·공급)는 표에 두지 않는다 — 산 노드가 정하고 Level업마다 같은 값이 온다(4.3).
    // 이정표(BLACKHOLE_LEVEL_PLAN 4.4): 정해진 Level과 그 고정 보상(Gold). 판 중 그 Level에 닿으면 판이 바로 끝나고 결산이 번 Gold 대신 보상을 준다.
    public sealed class HqGrowthDefinition
    {
        public const int StartLevel = 1;

        public static readonly HqGrowthDefinition None = new HqGrowthDefinition(Array.Empty<long>());

        public IReadOnlyList<long> LevelExp { get; }
        public int MaxLevel => StartLevel + LevelExp.Count;
        // 이정표(Level이 커지는 순서). 한 Level에 하나다.
        public IReadOnlyList<HqMilestone> Milestones { get; }

        public HqGrowthDefinition(IReadOnlyList<long> levelExp, IReadOnlyList<HqMilestone> milestones = null)
        {
            if (levelExp == null)
                throw new ArgumentNullException(nameof(levelExp));

            var copy = new long[levelExp.Count];

            for (int i = 0; i < copy.Length; i++)
            {
                long exp = levelExp[i];

                if (exp <= 0)
                    throw new ArgumentOutOfRangeException(nameof(levelExp), $"{i}번 줄(Level {i + StartLevel + 1})의 누적 EXP는 양수여야 한다.");

                if (i > 0 && exp <= copy[i - 1])
                    throw new ArgumentOutOfRangeException(nameof(levelExp),
                        $"{i}번 줄(Level {i + StartLevel + 1})의 누적 EXP {exp}는 앞 줄의 {copy[i - 1]}보다 커야 한다.");

                copy[i] = exp;
            }

            LevelExp = Array.AsReadOnly(copy);

            var marks = milestones != null ? new HqMilestone[milestones.Count] : Array.Empty<HqMilestone>();

            for (int i = 0; i < marks.Length; i++)
            {
                HqMilestone mark = milestones[i] ?? throw new ArgumentException($"이정표 {i}가 null이다.", nameof(milestones));

                if (mark.Level <= StartLevel || mark.Level > MaxLevel)
                    throw new ArgumentOutOfRangeException(nameof(milestones), $"이정표 {i}의 Level {mark.Level}은 {StartLevel + 1}부터 {MaxLevel}까지(Level 표 안)여야 한다.");

                if (i > 0 && mark.Level <= marks[i - 1].Level)
                    throw new ArgumentOutOfRangeException(nameof(milestones), $"이정표 {i}의 Level {mark.Level}은 앞 이정표의 {marks[i - 1].Level}보다 커야 한다.");

                marks[i] = mark;
            }

            Milestones = Array.AsReadOnly(marks);
        }

        // 이 Level 이하인 이정표의 수(이정표 진행도 n / Milestones.Count).
        public int MilestonesReachedBy(int level)
        {
            int reached = 0;

            foreach (HqMilestone mark in Milestones)
            {
                if (mark.Level <= level)
                    reached++;
            }

            return reached;
        }

        // 이 누적 EXP가 닿는 Level. 판 밖(진행 상태의 EXP)에서 Level을 볼 때와 판을 시작할 때 쓴다.
        public int LevelAt(long exp)
        {
            int level = StartLevel;

            while (ExpToReach(level + 1) is long next && exp >= next)
                level++;

            return level;
        }

        // Level level에서 누적 EXP exp일 때, 그 Level의 임계값에서 다음 임계값까지 몇 %인가(0 ~ 1). 마지막 Level이면 1이다.
        // 판 밖(진행 상태)에서는 LevelAt(exp)와 함께 부른다.
        public float ProgressAt(int level, long exp)
        {
            long? next = ExpToReach(level + 1);
            long? from = ExpToReach(level);

            if (!next.HasValue || !from.HasValue)
                return 1;

            return (float)Math.Max(0, Math.Min(1, (double)(exp - from.Value) / (next.Value - from.Value)));
        }

        // 이 Level에 닿는 누적 EXP. 시작 Level은 0이다. 표 밖이면 null이다.
        public long? ExpToReach(int level)
        {
            if (level == StartLevel)
                return 0;

            int index = level - StartLevel - 1;
            return index >= 0 && index < LevelExp.Count ? LevelExp[index] : (long?)null;
        }
    }

    // 이정표 하나: 이 Level에 닿으면 판이 끝나고, 결산이 그 판에서 번 Gold 대신 이 보상을 준다. 금액은 기획자가 정한다 [사용자].
    public sealed class HqMilestone
    {
        public int Level { get; }
        public long Reward { get; }

        public HqMilestone(int level, long reward)
        {
            Level = level;
            Reward = DefinitionGuard.NotNegative(reward, nameof(reward));
        }
    }

    // 블랙홀이 공개하는 업그레이드 수치 이름(BLACKHOLE_GROWTH_PLAN 4.3). 적 종류마다의 성장 공급은 EnemyUpgradeStats.GrowthSupply다.
    public static class HqUpgradeStats
    {
        // Level업마다 이 판의 제한 시간에 더하는 초. 기본값 0(성장 노드를 사기 전에는 시간이 늘지 않는다).
        public const string GrowthTime = "hq.growth-time";

        // 업그레이드 표로 Level업마다 늘어나는 시간을 계산한다. 음수·무한은 예외다(노드 저작 오류, UpgradeContentCheck가 로드 때 찾는다).
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
