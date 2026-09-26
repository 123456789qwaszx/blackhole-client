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

    // 끝난 전투 한 판의 원자료: 어떤 조건(단계·seed)으로 조립됐고, 어떻게 끝났으며, 무엇을 얼마나 처치하고 얼마를 벌었는가.
    // 판을 정리한 뒤에도 남는 유일한 기록이다. 결산·업그레이드 같은 다음 단계가 이것을 읽는다.
    public sealed class BattleRawData
    {
        public int Stage { get; }
        public int Seed { get; }
        public SessionEndReason EndReason { get; }
        public float PlayedSeconds { get; }
        // 종류별 처치 수(처음 처치한 순서). 기록일 뿐이며 Gold 계산의 입력이 아니다.
        // 전투 정리로 사라진 적은 처치가 아니므로 들지 않는다.
        public IReadOnlyList<EnemyKillCount> Kills { get; }
        public int TotalKills { get; }
        // 이 판에서 번 Gold. 사망 순간마다 그 적의 Gold가 더해진 합계이며, 결산(GameSession.Settle)이 진행 상태에 더한 값과 같다.
        public long EarnedGold { get; }

        internal BattleRawData(
            int stage,
            int seed,
            SessionEndReason endReason,
            float playedSeconds,
            IReadOnlyList<EnemyKillCount> kills,
            long earnedGold)
        {
            Stage = stage;
            Seed = seed;
            EndReason = endReason;
            PlayedSeconds = playedSeconds;
            Kills = kills;
            EarnedGold = earnedGold;

            foreach (EnemyKillCount kill in kills)
                TotalKills += kill.Count;
        }
    }
}
