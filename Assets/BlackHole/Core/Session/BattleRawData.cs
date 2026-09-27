using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 한 종류의 처치 수.
    public readonly struct EnemyKillCount
    {
        public EnemyDefinition Enemy { get; }
        public int Count { get; }

        public EnemyKillCount(EnemyDefinition enemy, int count)
        {
            Enemy = enemy ?? throw new ArgumentNullException(nameof(enemy));
            Count = count;
        }
    }

    // 끝난 전투 한 판의 원자료: 어떤 조건(seed)으로 조립됐고, 얼마나 진행했으며, 무엇을 얼마나 처치하고 얼마를 벌었는가, 블랙홀이 얼마나 컸는가.
    // 판을 정리한 뒤에도 남는 유일한 기록이다. 결산·업그레이드 같은 다음 단계가 이것을 읽는다.
    public sealed class BattleRawData
    {
        public int Seed { get; }
        public float PlayedSeconds { get; }
        // 종류별 처치 수(처음 처치한 순서). 기록일 뿐이며 Gold 계산의 입력이 아니다.
        // 전투 정리로 사라진 적은 처치가 아니므로 들지 않는다.
        public IReadOnlyList<EnemyKillCount> Kills { get; }
        public int TotalKills { get; }
        // 이 판에서 번 Gold. 사망 순간마다 그 적의 Gold가 더해진 합계다. 이정표로 끝나지 않았으면 결산이 진행 상태에 더한 값(SettledGold)과 같다.
        public long EarnedGold { get; }
        // 이 판이 끝났을 때 블랙홀의 Level과 누적 EXP(이 판 앞의 EXP 포함). 결산이 누적 EXP를 진행 상태에 돌려놓는다.
        public int ReachedLevel { get; }
        public long Exp { get; }
        // 이 판에서 닿은 이정표(없으면 비어 있다). 있으면 판은 그 Step에서 끝났다.
        public IReadOnlyList<HqMilestone> Milestones { get; }
        // 결산이 진행 상태에 더한 Gold: 이정표로 끝났으면 이정표의 보상, 아니면 번 Gold(EarnedGold).
        public long SettledGold { get; }

        internal BattleRawData(
            int seed,
            float playedSeconds,
            IReadOnlyList<EnemyKillCount> kills,
            long earnedGold,
            int reachedLevel,
            long exp,
            IReadOnlyList<HqMilestone> milestones,
            long settledGold)
        {
            Seed = seed;
            PlayedSeconds = playedSeconds;
            Kills = kills;
            EarnedGold = earnedGold;
            ReachedLevel = reachedLevel;
            Exp = exp;
            Milestones = new List<HqMilestone>(milestones).AsReadOnly();
            SettledGold = settledGold;

            foreach (EnemyKillCount kill in kills)
                TotalKills += kill.Count;
        }
    }
}
