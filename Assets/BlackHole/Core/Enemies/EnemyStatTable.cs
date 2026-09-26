using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 한 판의 판 구성 중 적 종류의 몫: 종류마다 이 판의 질량 단계·색 비율·황금 비율, (종류, 색 등급, 황금)마다 실행 수치.
    // 판 조립 때 — 전투 Session이 시작되기 전 — 한 번 정해지고, 판이 끝날 때까지 바뀌지 않는다(BATTLE_COMPOSITION_PLAN 4.2).
    // 적의 수치를 바꾸는 것은 전투 밖(업그레이드 화면)뿐이며, 그 결과는 여기서 반영된다.
    // - 질량 단계: 판 조립이 받는다. 지금은 개발용 콘솔이 고르고, 업그레이드 시스템이 돌아오면 산 노드(종류별 질량 증가)가 정한다.
    // - 황금 비율: 판 조립이 받는다(0 ~ 1). 황금이 되는 종류(황금 배율 > 0)만 0보다 클 수 있다.
    //   원작은 "황금 소행성 추가" 노드로 열리고 행성 노드가 자릿수 단위로 올린다. 지금은 개발용 콘솔이 고른다.
    // - 실행 수치: EnemyDefinition.StatsAt(질량 단계, 색 등급, 황금). 그래서 같은 판의 같은 색은 HP·크기·Gold가 정확히 같다.
    // 단계 계수(체력·크기)와 돈 배율 같은 다른 노드 보정은 아직 없다. 붙으면 이 표를 만들 때 계산한다.
    // 출현하는 적은 이 표의 수치를 받는다. 화면·콘솔도 이 표를 읽어 이 판의 값을 보여 준다.
    public sealed class EnemyStatTable
    {
        private readonly Dictionary<EnemyDefinition, Row> _rows = new Dictionary<EnemyDefinition, Row>();

        // 이 판의 종류(콘텐츠 순서).
        public IReadOnlyList<EnemyDefinition> Kinds { get; }

        // massLevels·goldenRatios에 없는 종류는 질량 단계 0, 황금 비율 0이다.
        // 콘텐츠에 없는 종류, 범위 밖의 단계·비율, 황금이 되지 않는 종류의 황금 비율(0 초과)은 예외다.
        internal EnemyStatTable(
            IReadOnlyList<EnemyDefinition> enemies,
            IReadOnlyDictionary<EnemyDefinition, int> massLevels,
            IReadOnlyDictionary<EnemyDefinition, float> goldenRatios)
        {
            var kinds = new EnemyDefinition[enemies.Count];

            for (int i = 0; i < kinds.Length; i++)
            {
                EnemyDefinition kind = enemies[i];
                int level = massLevels != null && massLevels.TryGetValue(kind, out int chosen) ? chosen : 0;
                float golden = goldenRatios != null && goldenRatios.TryGetValue(kind, out float ratio) ? ratio : 0;

                if (float.IsNaN(golden) || golden < 0 || golden > 1)
                    throw new ArgumentOutOfRangeException(nameof(goldenRatios), $"'{kind.Id}'의 황금 비율은 0부터 1까지다. 받은 값: {golden}.");

                if (golden > 0 && !kind.CanBeGolden)
                    throw new ArgumentException($"'{kind.Id}'는 황금이 되지 않는다.", nameof(goldenRatios));

                var stats = new EnemyStats[kind.Tiers.Count];
                EnemyStats[] goldenStats = kind.CanBeGolden ? new EnemyStats[kind.Tiers.Count] : null;

                for (int tier = 0; tier < stats.Length; tier++)
                {
                    stats[tier] = kind.StatsAt(level, tier);

                    if (goldenStats != null)
                        goldenStats[tier] = kind.StatsAt(level, tier, golden: true);
                }

                _rows.Add(kind, new Row(level, golden, stats, goldenStats));
                kinds[i] = kind;
            }

            RequireKnown(massLevels?.Keys, nameof(massLevels));
            RequireKnown(goldenRatios?.Keys, nameof(goldenRatios));
            Kinds = Array.AsReadOnly(kinds);
        }

        // 이 판에서 이 종류의 질량 단계.
        public int MassLevelOf(EnemyDefinition kind) => RowOf(kind).MassLevel;

        // 이 판에서 이 종류의 색 비율(색 등급 표 순서).
        public IReadOnlyList<float> TierRatiosOf(EnemyDefinition kind) => kind.MassLevels[RowOf(kind).MassLevel].TierRatios;

        // 이 판에서 이 종류가 황금으로 나오는 비율(0 ~ 1).
        public float GoldenRatioOf(EnemyDefinition kind) => RowOf(kind).GoldenRatio;

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

        private void RequireKnown(IEnumerable<EnemyDefinition> kinds, string name)
        {
            if (kinds == null)
                return;

            foreach (EnemyDefinition kind in kinds)
            {
                if (kind == null || !_rows.ContainsKey(kind))
                    throw new ArgumentException($"이 판의 적 종류가 아니다: '{kind?.Id}'.", name);
            }
        }

        private readonly struct Row
        {
            public readonly int MassLevel;
            public readonly float GoldenRatio;
            public readonly EnemyStats[] Stats;
            // 황금이 되지 않는 종류는 null이다.
            public readonly EnemyStats[] GoldenStats;

            public Row(int massLevel, float goldenRatio, EnemyStats[] stats, EnemyStats[] goldenStats)
            {
                MassLevel = massLevel;
                GoldenRatio = goldenRatio;
                Stats = stats;
                GoldenStats = goldenStats;
            }
        }
    }
}
