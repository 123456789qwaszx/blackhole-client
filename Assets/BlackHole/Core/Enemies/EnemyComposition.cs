using System;

namespace BlackHole.Core
{
    // 판 조립이 받는 적 종류 하나의 판 구성 값(BATTLE_COMPOSITION_PLAN 4.2): 질량 단계, 황금 비율, 황금 배율, 더할 시작 공급 수.
    // 산 노드에서 계산된다(Loadout.EnemiesFor). 노드 보정이 없으면 Base(종류)다.
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

        // 노드 보정이 없을 때: 질량 단계 0, 황금 비율 0, 황금 배율은 종류의 기본값, 더할 시작 공급 없음.
        public static EnemyComposition Base(EnemyDefinition kind) => new EnemyComposition(0, 0, kind.GoldenMultiplier);
    }

    // 노드가 보정할 수 있는 적 종류의 판 구성 수치. 적 시스템이 공개하는 목록이며, 노드 데이터(Grant)는 이 이름으로 가리킨다.
    public enum EnemyUpgradeStat
    {
        // 질량 증가. 더하기만 쓴다(한 노드 = 질량 단계 +1).
        MassLevel,
        // 황금 소행성 추가(정하기)와 행성 노드의 자릿수 올리기(곱하기).
        GoldenRatio,
        // 황금 배율 올리기.
        GoldenMultiplier,
        // 전투 시작 공급 수 늘리기(원작 "spawn more asteroids"). 더하기만 쓴다. 전체 개체 수 상한 안이어야 한다.
        // 원작의 이 강화가 시작 공급인지 성장 공급인지는 미확인이다. 성장 공급이 생기면 그쪽 수치를 따로 더한다.
        StartSupply,
    }
}
