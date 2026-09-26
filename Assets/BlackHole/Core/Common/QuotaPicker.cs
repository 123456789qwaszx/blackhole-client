using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 비율을 거의 정확히 지키며 하나씩 고르는 몫 방식(BATTLE_COMPOSITION_PLAN 4.5).
    // 원작은 판마다 비율이 거의 같다 — 그래서 개체마다 독립으로 굴리지 않는다.
    //
    // 고를 때마다 앞 칸부터 차례로 "이 칸 차례인가"를 본다. 칸마다 몫이 있어, 그 칸까지 넘어오면
    // 남은 칸들 가운데 그 칸의 비율만큼 몫을 더하고, 몫이 1 이상이면 그 칸을 고르고 1을 뺀다.
    // 아니면 다음 칸으로 넘어가고, 마지막 칸은 넘어오면 고른다. 비율이 0인 칸은 고르지 않는다.
    // 처음 몫만 판의 난수로 [0, 1)에 흩뜨린다. 그래서 고르는 순서가 판마다 다르고, 한 판에 한 번도
    // 차례가 오지 않을 만큼 작은 비율(예: 황금)도 판마다 기대한 만큼(확률로) 나온다.
    // 첫 칸은 어느 시점에도 (비율 × 고른 수)에서 1개 넘게 벗어나지 않는다. 칸이 둘이면 두 칸 모두 그렇다.
    // 뒤 칸은 조금 더 벗어날 수 있다(측정은 BATTLE_COMPOSITION_PLAN 4.5).
    internal sealed class QuotaPicker
    {
        // 비율이 0보다 큰 칸의 번호(앞에서부터)와, 그 칸까지 넘어왔을 때 그 칸을 고를 비율(남은 비율 가운데 그 칸의 몫).
        private readonly int[] _entries;
        private readonly double[] _shares;
        private readonly double[] _quota;

        // ratios는 합이 0보다 커야 한다(합이 1이 아니어도 된다). 처음 몫을 흩뜨리는 데 random을 칸 수만큼 쓴다.
        public QuotaPicker(IReadOnlyList<float> ratios, BattleRandom random)
        {
            double remaining = 0;
            int count = 0;

            for (int i = 0; i < ratios.Count; i++)
            {
                remaining += ratios[i];

                if (ratios[i] > 0)
                    count++;
            }

            if (!(remaining > 0))
                throw new ArgumentException("비율의 합이 0보다 커야 한다.", nameof(ratios));

            _entries = new int[count];
            _shares = new double[count];
            _quota = new double[count];

            for (int i = 0, entry = 0; i < ratios.Count; i++)
            {
                float start = random.NextFloat();

                if (ratios[i] <= 0)
                    continue;

                _entries[entry] = i;
                _shares[entry] = ratios[i] / remaining;
                _quota[entry] = start;
                remaining -= ratios[i];
                entry++;
            }
        }

        public int Pick()
        {
            int last = _entries.Length - 1;

            for (int entry = 0; entry < last; entry++)
            {
                _quota[entry] += _shares[entry];

                if (_quota[entry] >= 1)
                {
                    _quota[entry] -= 1;
                    return _entries[entry];
                }
            }

            return _entries[last];
        }
    }
}
