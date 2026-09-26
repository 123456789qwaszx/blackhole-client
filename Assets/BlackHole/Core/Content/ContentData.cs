using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 저작 형식. 검증 전 값이며 실행에 쓰지 않는다 — ContentLoader만 읽는다.
    // 적 종류·공급·배치와 단계 표는 Unity 쪽 에셋(EnemyCatalog, EnemySupplySetup, StageTable)이 채운다.
    // 판 설정은 아직 BlackHole.Sample의 SampleContent가 코드로 채운다.
    [Serializable]
    public sealed class ContentData
    {
        public SessionData Session;
        public List<EnemyData> Enemies = new List<EnemyData>();
        // 적 풀. 단계 표가 ID로 가리킨다.
        public List<EnemyPoolData> EnemyPools = new List<EnemyPoolData>();
        // 진행도(적의 강도 단계) 표. Stages[i]가 (i + 1)단계다. 줄 수가 단계의 수다. HQ 성장 단계와 다르다.
        public List<StageData> Stages = new List<StageData>();
        // 출현 위치. 공급이 하나라도 있으면 필요하다.
        public EnemyPlacementData EnemyPlacement;
        // 전투 시작 공급. 전투를 시작할 때(0초) 한 번 공급한다.
        public List<SupplyData> StartSupply = new List<SupplyData>();
    }

    [Serializable]
    public sealed class SessionData
    {
        // 시간제 종료(현재 후보). 초 단위.
        public float TimeLimit;
    }

    [Serializable]
    public sealed class EnemyData
    {
        public string Id;
        public float MoveSpeed;
        // 색 등급 표. 색이 없는 종류는 한 줄이다.
        public List<EnemyTierData> Tiers = new List<EnemyTierData>();
        // 질량 단계 표. MassLevels[i]가 질량 단계 i다(0 = 질량 증가를 사지 않음). 하나 이상.
        public List<MassLevelData> MassLevels = new List<MassLevelData>();
        // 황금일 때 Gold에 곱하는 값. 0이면 황금이 되지 않는다.
        public float GoldenMultiplier;
        public EnemyBehaviorData Behavior;
    }

    // 색 등급 한 줄.
    [Serializable]
    public sealed class EnemyTierData
    {
        public float MaxHealth;
        // 반지름.
        public float Size;
        // 사망 때 판의 합계에 드는 Gold의 기본값. 0 이상.
        public long Gold;
    }

    // 질량 단계 한 줄: 색마다 나오는 비율(색 등급 표와 같은 순서·길이)과 HP·Gold 계수.
    [Serializable]
    public sealed class MassLevelData
    {
        public List<float> TierRatios = new List<float>();
        public float HealthMultiplier;
        public float GoldMultiplier;
    }

    // 행동 종류마다 쓰는 칸이 다르다. 지금은 Orbit 하나다.
    [Serializable]
    public sealed class EnemyBehaviorData
    {
        // 종류 이름. 가능한 값은 ContentLoader의 해석 목록에 있다.
        public string Kind;
        // Orbit
        public bool Clockwise;
    }

    // 적 풀 하나: 이 풀의 항목(적 종류와 동시 최대 수).
    [Serializable]
    public sealed class EnemyPoolData
    {
        public string Id;
        public List<EnemyPoolEntryData> Entries = new List<EnemyPoolEntryData>();
    }

    [Serializable]
    public sealed class EnemyPoolEntryData
    {
        // 적 종류의 ID.
        public string Enemy;
        // 이 종류가 판에 동시에 살아 있을 수 있는 최대 수(출현 제한).
        public int MaxAlive;
    }

    // 진행도 한 단계. 체력·크기 계수는 단계 표에 더해질 때 이 줄에 붙는다.
    [Serializable]
    public sealed class StageData
    {
        // 이 단계에서 쓰는 적 풀의 ID.
        public string Pool;
    }

    // HQ(원점)를 둘러싼 출현 띠.
    [Serializable]
    public sealed class EnemyPlacementData
    {
        public float MinDistance;
        public float MaxDistance;
    }

    // 적 공급 한 건: 어떤 종류를 몇 마리.
    [Serializable]
    public sealed class SupplyData
    {
        public string Enemy;
        public int Count;
    }
}
