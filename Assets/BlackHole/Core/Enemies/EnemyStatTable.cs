using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 한 판의 판 구성 중 적 종류의 몫: 종류마다 이 판의 질량 단계와 색 비율, (종류, 색 등급)마다 실행 수치.
    // 판 조립 때 — 전투 Session이 시작되기 전 — 한 번 정해지고, 판이 끝날 때까지 바뀌지 않는다(BATTLE_COMPOSITION_PLAN 4.2).
    // 적의 수치를 바꾸는 것은 전투 밖(업그레이드 화면)뿐이며, 그 결과는 여기서 반영된다.
    // - 질량 단계: 판 조립이 받는다. 지금은 개발용 콘솔이 고르고, 업그레이드 시스템이 돌아오면 산 노드(종류별 질량 증가)가 정한다.
    // - 실행 수치: EnemyDefinition.StatsAt(질량 단계, 색 등급). 그래서 같은 판의 같은 색은 HP·크기·Gold가 정확히 같다.
    // 단계 계수(체력·크기)와 돈 배율 같은 다른 노드 보정은 아직 없다. 붙으면 이 표를 만들 때 계산한다.
    // 출현하는 적은 이 표의 수치를 받는다. 화면·콘솔도 이 표를 읽어 이 판의 값을 보여 준다.
    public sealed class EnemyStatTable
    {
        private readonly Dictionary<EnemyDefinition, Row> _rows = new Dictionary<EnemyDefinition, Row>();

        // 이 판의 종류(콘텐츠 순서).
        public IReadOnlyList<EnemyDefinition> Kinds { get; }

        // massLevels에 없는 종류는 질량 단계 0이다. 콘텐츠에 없는 종류나 범위 밖의 단계는 예외다.
        internal EnemyStatTable(IReadOnlyList<EnemyDefinition> enemies, IReadOnlyDictionary<EnemyDefinition, int> massLevels)
        {
            var kinds = new EnemyDefinition[enemies.Count];

            for (int i = 0; i < kinds.Length; i++)
            {
                EnemyDefinition kind = enemies[i];
                int level = massLevels != null && massLevels.TryGetValue(kind, out int chosen) ? chosen : 0;
                var stats = new EnemyStats[kind.Tiers.Count];

                for (int tier = 0; tier < stats.Length; tier++)
                    stats[tier] = kind.StatsAt(level, tier);

                _rows.Add(kind, new Row(level, stats));
                kinds[i] = kind;
            }

            if (massLevels != null)
            {
                foreach (EnemyDefinition kind in massLevels.Keys)
                {
                    if (!_rows.ContainsKey(kind))
                        throw new ArgumentException($"이 판의 적 종류가 아니다: '{kind?.Id}'.", nameof(massLevels));
                }
            }

            Kinds = Array.AsReadOnly(kinds);
        }

        // 이 판에서 이 종류의 질량 단계.
        public int MassLevelOf(EnemyDefinition kind) => RowOf(kind).MassLevel;

        // 이 판에서 이 종류의 색 비율(색 등급 표 순서).
        public IReadOnlyList<float> TierRatiosOf(EnemyDefinition kind) => kind.MassLevels[RowOf(kind).MassLevel].TierRatios;

        // 이 판에서 이 종류·색 등급이 받는 수치.
        public EnemyStats Of(EnemyDefinition kind, int tier)
        {
            EnemyStats[] stats = RowOf(kind).Stats;

            if (tier < 0 || tier >= stats.Length)
                throw new ArgumentOutOfRangeException(nameof(tier), $"'{kind.Id}'의 색 등급은 0부터 {stats.Length - 1}까지다. 받은 값: {tier}.");

            return stats[tier];
        }

        // 이 판의 종류가 아니면 예외다.
        internal void Require(EnemyDefinition kind) => RowOf(kind);

        private Row RowOf(EnemyDefinition kind)
        {
            if (kind == null || !_rows.TryGetValue(kind, out Row row))
                throw new ArgumentException($"이 판의 적 종류가 아니다: '{kind?.Id}'.", nameof(kind));

            return row;
        }

        private readonly struct Row
        {
            public readonly int MassLevel;
            public readonly EnemyStats[] Stats;

            public Row(int massLevel, EnemyStats[] stats)
            {
                MassLevel = massLevel;
                Stats = stats;
            }
        }
    }
}
