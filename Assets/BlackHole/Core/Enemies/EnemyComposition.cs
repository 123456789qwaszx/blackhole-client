using System;

namespace BlackHole.Core
{
    // 판 조립이 받는 적 종류 하나의 판 구성 값(BATTLE_COMPOSITION_PLAN 4.2): 질량 단계, 황금 비율, 황금 배율, 더할 시작 공급 수.
    // 판 조립 때 업그레이드 표에서 계산된다(From). 보정이 없으면 Base(종류)다.
    public readonly struct EnemyComposition
    {
        // 질량 단계(종류의 질량 단계 표 번호). 질량 증가를 산 수다.
        public int MassLevel { get; }
        // 황금으로 나오는 비율(0 ~ 1).
        public float GoldenRatio { get; }
        // 황금일 때 Gold에 곱하는 값.
        public float GoldenMultiplier { get; }
        // 콘텐츠의 전투 시작 공급에 더해 이 종류를 몇 마리 더 공급하는가.
        public int StartSupplyBonus { get; }

        public EnemyComposition(int massLevel, float goldenRatio, float goldenMultiplier, int startSupplyBonus = 0)
        {
            if (massLevel < 0)
                throw new ArgumentOutOfRangeException(nameof(massLevel), "0 이상이 필요하다.");

            if (startSupplyBonus < 0)
                throw new ArgumentOutOfRangeException(nameof(startSupplyBonus), "0 이상이 필요하다.");

            if (float.IsNaN(goldenRatio) || goldenRatio < 0 || goldenRatio > 1)
                throw new ArgumentOutOfRangeException(nameof(goldenRatio), "0부터 1까지다.");

            if (float.IsNaN(goldenMultiplier) || float.IsInfinity(goldenMultiplier) || goldenMultiplier < 0)
                throw new ArgumentOutOfRangeException(nameof(goldenMultiplier), "0 이상의 유한한 값이 필요하다.");

            MassLevel = massLevel;
            GoldenRatio = goldenRatio;
            GoldenMultiplier = goldenMultiplier;
            StartSupplyBonus = startSupplyBonus;
        }

        // 보정이 없을 때: 질량 단계 0, 황금 비율 0, 황금 배율은 종류의 기본값, 더할 시작 공급 없음.
        public static EnemyComposition Base(EnemyDefinition kind) => new EnemyComposition(0, 0, kind.GoldenMultiplier);

        // 업그레이드 표로 이 종류의 판 구성을 계산한다. 수치 이름은 EnemyUpgradeStats다.
        // 각 수치의 기본값(질량 단계 0, 황금 비율 0, 황금 배율 = 종류의 기본값, 더할 공급 0)에 표의 합성 규칙을 적용한 뒤 적 시스템의 한계를 건다:
        // - 질량 단계와 더할 공급 수는 가장 가까운 정수로 읽는다 [임시]. 질량 단계는 그 종류의 질량 단계 표 안이어야 한다.
        // - 황금 비율은 1을 넘지 않는다. 황금이 되지 않는 종류의 황금 비율은 0보다 클 수 없다.
        // 한계 밖(음수, 표 밖의 질량 단계, 황금이 안 되는 종류의 황금 비율)은 예외다 — 노드 저작 오류이며 UpgradeContentCheck가 로드 때 찾는다.
        public static EnemyComposition From(EnemyDefinition kind, UpgradeTable upgrades)
        {
            if (kind == null)
                throw new ArgumentNullException(nameof(kind));

            if (upgrades == null)
                throw new ArgumentNullException(nameof(upgrades));

            int massLevel = Whole(upgrades.Apply(EnemyUpgradeStats.MassLevel(kind.Id), 0));
            float goldenRatio = Math.Min(1, upgrades.Apply(EnemyUpgradeStats.GoldenRatio(kind.Id), 0));
            float goldenMultiplier = upgrades.Apply(EnemyUpgradeStats.GoldenMultiplier(kind.Id), kind.GoldenMultiplier);
            int startSupply = Whole(upgrades.Apply(EnemyUpgradeStats.StartSupply(kind.Id), 0));

            if (massLevel >= kind.MassLevels.Count)
                throw new ArgumentOutOfRangeException(
                    nameof(upgrades), $"'{kind.Id}'의 질량 단계는 0부터 {kind.MassLevels.Count - 1}까지다. 업그레이드 합: {massLevel}.");

            if (goldenRatio > 0 && !kind.CanBeGolden)
                throw new ArgumentException($"'{kind.Id}'는 황금이 되지 않아 황금 비율을 올릴 수 없다.", nameof(upgrades));

            return new EnemyComposition(massLevel, goldenRatio, goldenMultiplier, startSupply);
        }

        private static int Whole(float value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);
    }

    // 적 시스템이 공개하는 업그레이드 수치 이름. 노드의 업그레이드(Upgrade.Stat)가 이 이름으로 적 종류의 판 구성을 보정한다.
    // 업그레이드 시스템은 이 이름을 해석하지 않는다. 적 종류마다 이름이 다르다(종류 ID가 들어간다).
    public static class EnemyUpgradeStats
    {
        // 질량 증가. 한 노드 = 더하기 1.
        public static string MassLevel(string kindId) => $"enemy.{kindId}.mass-level";
        // 황금 소행성 추가(더하기)와 행성 노드의 자릿수 올리기(곱하기).
        public static string GoldenRatio(string kindId) => $"enemy.{kindId}.golden-ratio";
        // 황금 배율 올리기. 기본값은 그 종류의 황금 배율이다.
        public static string GoldenMultiplier(string kindId) => $"enemy.{kindId}.golden-multiplier";
        // 전투 시작 공급 수 늘리기(원작 "spawn more asteroids"). 전체 개체 수 상한 안이어야 한다.
        // 원작의 이 강화가 시작 공급인지 성장 공급인지는 미확인이다. 성장 공급이 생기면 그쪽 수치를 따로 둔다.
        public static string StartSupply(string kindId) => $"enemy.{kindId}.start-supply";
    }
}
