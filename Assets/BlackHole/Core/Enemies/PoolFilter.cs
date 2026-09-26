using System.Collections.Generic;

namespace BlackHole.Core
{
    // 풀 여과 장치: 공급이 요청한 적 하나가 이 판에 실제로 나올 수 있는지 정한다. 판마다 하나 있고, 판의 단계 풀을 쓴다.
    // 지금 보는 것:
    // 1. 이 판의 단계 풀에 든 종류인가.
    // 2. 그 종류의 살아 있는 수가 풀의 최대 수(MaxAlive)보다 적은가.
    // 3. 살아 있는 적 전체가 전체 개체 수 상한(콘텐츠의 MaxAliveEnemies)보다 적은가. 성능 예산이며,
    //    공급 계기와 노드가 무엇을 약속하든 판의 적 수는 이 수를 넘지 않는다(성능 원칙 §1·§5·§7).
    // 색 비율은 여기가 아니라 여과를 통과한 뒤 몫 방식으로 정한다(World).
    // 거른 요청은 버린다 — 나중에 자리가 나도 다시 나오지 않는다(공급은 사건마다 유한하다).
    internal sealed class PoolFilter
    {
        private readonly Dictionary<EnemyDefinition, int> _maxAlive = new Dictionary<EnemyDefinition, int>();
        private readonly int _maxAliveEnemies;

        public PoolFilter(EnemyPoolDefinition pool, int maxAliveEnemies)
        {
            foreach (EnemyPoolEntry entry in pool.Entries)
                _maxAlive.Add(entry.Enemy, entry.MaxAlive);

            _maxAliveEnemies = maxAliveEnemies;
        }

        public bool Allows(EnemyDefinition enemy, EnemyRoster roster) =>
            roster.Alive.Count < _maxAliveEnemies
            && _maxAlive.TryGetValue(enemy, out int max)
            && roster.CountAlive(enemy) < max;
    }
}
