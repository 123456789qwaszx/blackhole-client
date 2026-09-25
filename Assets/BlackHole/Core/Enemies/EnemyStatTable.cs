using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 한 판의 적 종류별 실행 수치. 판 조립 때 — 전투 Session이 시작되기 전 — 한 번 정해지고, 판이 끝날 때까지 바뀌지 않는다.
    // 적의 수치를 바꾸는 것은 전투 밖(업그레이드 화면)뿐이며, 그 결과는 여기서 반영된다.
    // 지금은 업그레이드 보정과 단계 계수(체력·크기)가 아직 없어 종류의 기본 수치 그대로다. 그것들이 붙으면 이 표를 만들 때 계산한다.
    // 출현하는 적은 이 표의 수치를 받는다. 화면·콘솔도 이 표를 읽어 이 판의 수치를 보여 준다.
    public sealed class EnemyStatTable
    {
        private readonly Dictionary<EnemyDefinition, EnemyStats> _stats = new Dictionary<EnemyDefinition, EnemyStats>();

        internal EnemyStatTable(IReadOnlyList<EnemyDefinition> enemies)
        {
            foreach (EnemyDefinition enemy in enemies)
                _stats.Add(enemy, enemy.BaseStats);
        }

        // 이 판에서 이 종류가 받는 수치. 콘텐츠에 없는 종류면 예외다.
        public EnemyStats Of(EnemyDefinition kind)
        {
            if (kind == null || !_stats.TryGetValue(kind, out EnemyStats stats))
                throw new ArgumentException($"이 판의 적 종류가 아니다: '{kind?.Id}'.", nameof(kind));

            return stats;
        }
    }
}
