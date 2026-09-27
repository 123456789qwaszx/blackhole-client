using System;

namespace BlackHole.Core
{
    // 판 조립이 받는 적 종류 하나의 판 구성 값(BATTLE_COMPOSITION_PLAN 4.2): 해금, 질량 단계, 황금 비율, 황금 배율, 더할 시작 공급 수, Level업마다의 성장 공급 수.
    // 판 조립 때 업그레이드 표에서 계산된다(From). 보정이 없으면 Base(종류)다.
    public readonly struct EnemyComposition
    {
        // 이 판에 나오는가. 잠긴 종류는 시작 공급과 생성 요청 모두 나오지 않는다(ENEMY_UNLOCK_PLAN 4.2).
        public bool Unlocked { get; }
        // 질량 단계(종류의 질량 단계 표 번호). 질량 증가를 산 수다.
        public int MassLevel { get; }
        // 황금으로 나오는 비율(0 ~ 1).
        public float GoldenRatio { get; }
        // 황금일 때 Gold에 곱하는 값.
        public float GoldenMultiplier { get; }
        // 콘텐츠의 전투 시작 공급에 더해 이 종류를 몇 마리 더 공급하는가.
        public int StartSupplyBonus { get; }
        // 블랙홀이 Level업할 때마다 이 종류를 몇 마리 요청하는가(BLACKHOLE_GROWTH_PLAN 4.3). 해금된 종류만 나온다.
        public int GrowthSupply { get; }

        public EnemyComposition(int massLevel, float goldenRatio, float goldenMultiplier, int startSupplyBonus = 0, bool unlocked = true, int growthSupply = 0)
        {
            if (massLevel < 0)
                throw new ArgumentOutOfRangeException(nameof(massLevel), "0 이상이 필요하다.");

            if (startSupplyBonus < 0)
                throw new ArgumentOutOfRangeException(nameof(startSupplyBonus), "0 이상이 필요하다.");

            if (growthSupply < 0)
                throw new ArgumentOutOfRangeException(nameof(growthSupply), "0 이상이 필요하다.");

            if (float.IsNaN(goldenRatio) || goldenRatio < 0 || goldenRatio > 1)
                throw new ArgumentOutOfRangeException(nameof(goldenRatio), "0부터 1까지다.");

            if (float.IsNaN(goldenMultiplier) || float.IsInfinity(goldenMultiplier) || goldenMultiplier < 0)
                throw new ArgumentOutOfRangeException(nameof(goldenMultiplier), "0 이상의 유한한 값이 필요하다.");

            MassLevel = massLevel;
            GoldenRatio = goldenRatio;
            GoldenMultiplier = goldenMultiplier;
            StartSupplyBonus = startSupplyBonus;
            Unlocked = unlocked;
            GrowthSupply = growthSupply;
        }

        // 보정이 없을 때: 종류의 해금 기본값, 질량 단계 0, 황금 비율 0, 황금 배율은 종류의 기본값, 더할 시작 공급·성장 공급 없음.
        public static EnemyComposition Base(EnemyDefinition kind) =>
            new EnemyComposition(0, 0, kind.GoldenMultiplier, 0, !kind.StartsLocked);

        // 업그레이드 표로 이 종류의 판 구성을 계산한다. 수치 이름은 EnemyUpgradeStats다.
        // 각 수치의 기본값(해금 = 잠긴 채 시작하면 0 아니면 1, 질량 단계 0, 황금 비율 0, 황금 배율 = 종류의 기본값, 더할 공급 0, 성장 공급 0)에
        // 표의 합성 규칙을 적용한 뒤 적 시스템의 한계를 건다:
        // - 해금 값이 1 이상이면 해금이다.
        // - 질량 단계와 더할 공급 수·성장 공급 수는 가장 가까운 정수로 읽는다 [임시]. 질량 단계는 그 종류의 질량 단계 표 안이어야 한다.
        // - 황금 비율은 1을 넘지 않는다. 황금이 되지 않는 종류의 황금 비율은 0보다 클 수 없다.
        // 한계 밖(음수, 표 밖의 질량 단계, 황금이 안 되는 종류의 황금 비율)은 예외다 — 노드 저작 오류이며 UpgradeContentCheck가 로드 때 찾는다.
        public static EnemyComposition From(EnemyDefinition kind, UpgradeTable upgrades)
        {
            if (kind == null)
                throw new ArgumentNullException(nameof(kind));

            if (upgrades == null)
                throw new ArgumentNullException(nameof(upgrades));

            bool unlocked = upgrades.Apply(EnemyUpgradeStats.Unlock(kind.Id), kind.StartsLocked ? 0 : 1) >= 1;
            int massLevel = Whole(upgrades.Apply(EnemyUpgradeStats.MassLevel(kind.Id), 0));
            float goldenRatio = Math.Min(1, upgrades.Apply(EnemyUpgradeStats.GoldenRatio(kind.Id), 0));
            float goldenMultiplier = upgrades.Apply(EnemyUpgradeStats.GoldenMultiplier(kind.Id), kind.GoldenMultiplier);
            int startSupply = Whole(upgrades.Apply(EnemyUpgradeStats.StartSupply(kind.Id), 0));
            int growthSupply = Whole(upgrades.Apply(EnemyUpgradeStats.GrowthSupply(kind.Id), 0));

            if (massLevel >= kind.MassLevels.Count)
                throw new ArgumentOutOfRangeException(
                    nameof(upgrades), $"'{kind.Id}'의 질량 단계는 0부터 {kind.MassLevels.Count - 1}까지다. 업그레이드 합: {massLevel}.");

            if (goldenRatio > 0 && !kind.CanBeGolden)
                throw new ArgumentException($"'{kind.Id}'는 황금이 되지 않아 황금 비율을 올릴 수 없다.", nameof(upgrades));

            return new EnemyComposition(massLevel, goldenRatio, goldenMultiplier, startSupply, unlocked, growthSupply);
        }

        private static int Whole(float value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);
    }

    // 적 시스템이 공개하는 업그레이드 수치 이름. 노드의 업그레이드(Upgrade.Stat)가 이 이름으로 적 종류의 판 구성을 보정한다.
    // 업그레이드 시스템은 이 이름을 해석하지 않는다. 적 종류마다 이름이 다르다(종류 ID가 들어간다).
    public static class EnemyUpgradeStats
    {
        // 종류 해금. 한 노드 = 더하기 1. 기본값은 잠긴 채 시작하면 0, 아니면 1이고, 1 이상이면 판에 나온다.
        public static string Unlock(string kindId) => $"enemy.{kindId}.unlock";
        // 질량 증가. 한 노드 = 더하기 1.
        public static string MassLevel(string kindId) => $"enemy.{kindId}.mass-level";
        // 황금 소행성 추가(더하기)와 행성 노드의 자릿수 올리기(곱하기).
        public static string GoldenRatio(string kindId) => $"enemy.{kindId}.golden-ratio";
        // 황금 배율 올리기. 기본값은 그 종류의 황금 배율이다.
        public static string GoldenMultiplier(string kindId) => $"enemy.{kindId}.golden-multiplier";
        // 전투 시작 공급 수 늘리기(원작 "spawn more asteroids"). 전체 개체 수 상한 안이어야 한다.
        // 원작 도전 과제 "50개 이상으로 시작"이 이 강화를 가리킨다. Level업마다의 공급은 GrowthSupply다.
        public static string StartSupply(string kindId) => $"enemy.{kindId}.start-supply";
        // 블랙홀이 Level업할 때마다 이 종류를 더 요청하는 수(원작 "성장 때 소행성 더"). 기본값 0.
        public static string GrowthSupply(string kindId) => $"enemy.{kindId}.growth-supply";
    }
}
