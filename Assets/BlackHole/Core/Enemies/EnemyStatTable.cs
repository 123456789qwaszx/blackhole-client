using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 한 판의 판 구성 중 적 종류의 몫: 종류마다 이 판의 판 구성(질량 단계·황금 비율·황금 배율)과 색 비율,
    // (종류, 색 등급, 황금)마다 실행 수치.
    // 판 조립 때 — 전투 Session이 시작되기 전 — 한 번 정해지고, 판이 끝날 때까지 바뀌지 않는다(BATTLE_COMPOSITION_PLAN 4.2).
    // 적의 수치를 바꾸는 것은 전투 밖(업그레이드)뿐이며, 그 결과는 판 구성(EnemyComposition)으로 여기에 들어온다.
    // 판 구성은 산 노드에서 계산된다(Loadout.EnemiesFor). 받지 않은 종류는 EnemyComposition.Base(종류)다.
    // - 황금 비율은 황금이 되는 종류(기본 황금 배율 > 0)만 0보다 클 수 있다.
    // - 실행 수치: EnemyDefinition.StatsAt(판 구성, 색 등급, 황금). 그래서 같은 판의 같은 색은 HP·크기·Gold가 정확히 같다.
    // 단계 계수(체력·크기) 같은 다른 보정은 아직 없다. 붙으면 이 표를 만들 때 계산한다.
    // 출현하는 적은 이 표의 수치를 받는다. 화면·콘솔도 이 표를 읽어 이 판의 값을 보여 준다.
    public sealed class EnemyStatTable
    {
        private readonly Dictionary<EnemyDefinition, Row> _rows = new Dictionary<EnemyDefinition, Row>();

        // 이 판의 종류(콘텐츠 순서).
        public IReadOnlyList<EnemyDefinition> Kinds { get; }

        // 콘텐츠에 없는 종류, 표 밖의 질량 단계, 황금이 되지 않는 종류의 황금 비율(0 초과)은 예외다.
        internal EnemyStatTable(
            IReadOnlyList<EnemyDefinition> enemies,
            IReadOnlyDictionary<EnemyDefinition, EnemyComposition> compositions)
        {
            var kinds = new EnemyDefinition[enemies.Count];

            for (int i = 0; i < kinds.Length; i++)
            {
                EnemyDefinition kind = enemies[i];
                EnemyComposition composition = compositions != null && compositions.TryGetValue(kind, out EnemyComposition chosen)
                    ? chosen
                    : EnemyComposition.Base(kind);

                if (composition.GoldenRatio > 0 && !kind.CanBeGolden)
                    throw new ArgumentException($"'{kind.Id}'는 황금이 되지 않는다.", nameof(compositions));

                var stats = new EnemyStats[kind.Tiers.Count];
                EnemyStats[] goldenStats = kind.CanBeGolden ? new EnemyStats[kind.Tiers.Count] : null;

                for (int tier = 0; tier < stats.Length; tier++)
                {
                    stats[tier] = kind.StatsAt(composition, tier);

                    if (goldenStats != null)
                        goldenStats[tier] = kind.StatsAt(composition, tier, golden: true);
                }

                _rows.Add(kind, new Row(composition, stats, goldenStats));
                kinds[i] = kind;
            }

            if (compositions != null)
            {
                foreach (EnemyDefinition kind in compositions.Keys)
                {
                    if (kind == null || !_rows.ContainsKey(kind))
                        throw new ArgumentException($"이 판의 적 종류가 아니다: '{kind?.Id}'.", nameof(compositions));
                }
            }

            Kinds = Array.AsReadOnly(kinds);
        }

        // 이 판에서 이 종류의 판 구성(질량 단계·황금 비율·황금 배율).
        public EnemyComposition CompositionOf(EnemyDefinition kind) => RowOf(kind).Composition;

        // 이 판에서 이 종류의 색 비율(색 등급 표 순서).
        public IReadOnlyList<float> TierRatiosOf(EnemyDefinition kind) =>
            kind.MassLevels[RowOf(kind).Composition.MassLevel].TierRatios;

        // 이 판에서 이 종류·색 등급(황금이면 황금)이 받는 수치.
        public EnemyStats Of(EnemyDefinition kind, int tier, bool golden = false)
        {
            Row row = RowOf(kind);

            if (tier < 0 || tier >= row.Stats.Length)
                throw new ArgumentOutOfRangeException(nameof(tier), $"'{kind.Id}'의 색 등급은 0부터 {row.Stats.Length - 1}까지다. 받은 값: {tier}.");

            if (!golden)
                return row.Stats[tier];

            if (row.GoldenStats == null)
                throw new ArgumentException($"'{kind.Id}'는 황금이 되지 않는다.", nameof(golden));

            return row.GoldenStats[tier];
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
            public readonly EnemyComposition Composition;
            public readonly EnemyStats[] Stats;
            // 황금이 되지 않는 종류는 null이다.
            public readonly EnemyStats[] GoldenStats;

            public Row(EnemyComposition composition, EnemyStats[] stats, EnemyStats[] goldenStats)
            {
                Composition = composition;
                Stats = stats;
                GoldenStats = goldenStats;
            }
        }
    }
}
