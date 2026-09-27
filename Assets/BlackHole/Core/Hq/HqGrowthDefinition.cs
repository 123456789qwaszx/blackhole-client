using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 블랙홀 성장의 공유 정의: Level 표(BLACKHOLE_GROWTH_PLAN 4.2).
    // 새 진행은 Level 1(누적 EXP 0)이다. LevelExp[i]는 Level (i + 2)에 닿는 누적 EXP이며, 앞 줄보다 커야 한다. Level은 판을 넘어 이어진다.
    // 성장 효과(시간·공급)는 표에 두지 않는다 — 산 노드가 정하고 Level업마다 같은 값이 온다(4.3).
    public sealed class HqGrowthDefinition
    {
        public const int StartLevel = 1;

        public static readonly HqGrowthDefinition None = new HqGrowthDefinition(Array.Empty<long>());

        public IReadOnlyList<long> LevelExp { get; }
        public int MaxLevel => StartLevel + LevelExp.Count;

        public HqGrowthDefinition(IReadOnlyList<long> levelExp)
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
        }

        // 이 누적 EXP가 닿는 Level. 판 밖(진행 상태의 EXP)에서 Level을 볼 때와 판을 시작할 때 쓴다.
        public int LevelAt(long exp)
        {
            int level = StartLevel;

            while (ExpToReach(level + 1) is long next && exp >= next)
                level++;

            return level;
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
