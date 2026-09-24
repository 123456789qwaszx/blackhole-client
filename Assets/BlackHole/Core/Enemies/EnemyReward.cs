using System;

namespace BlackHole.Core
{
    // 적 하나의 사망 보상. 기본값은 종류 정의에 있고, 실행 값은 출현 때 구매 보정을 반영해 확정한다.
    public readonly struct EnemyReward
    {
        public int Gold { get; }
        public int HqExp { get; }

        public EnemyReward(int gold, int hqExp)
        {
            if (gold < 0)
                throw new ArgumentOutOfRangeException(nameof(gold));

            if (hqExp < 0)
                throw new ArgumentOutOfRangeException(nameof(hqExp));

            Gold = gold;
            HqExp = hqExp;
        }
    }
}
