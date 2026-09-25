using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 저작 형식. 검증 전 값이며 실행에 쓰지 않는다 — ContentLoader만 읽는다.
    // 적 종류·공급·배치는 Unity 쪽 에셋(EnemyKind, EnemySupplySetup)이 채운다.
    // 판 설정과 업그레이드 노드는 아직 BlackHole.Sample의 SampleContent가 코드로 채운다.
    [Serializable]
    public sealed class ContentData
    {
        public SessionData Session;
        public List<EnemyData> Enemies = new List<EnemyData>();
        // 출현 위치. 공급이 하나라도 있으면 필요하다.
        public EnemyPlacementData EnemyPlacement;
        // 전투 시작 공급. 판 조립 때(0초) 한 번 공급한다.
        public List<SupplyData> StartSupply = new List<SupplyData>();
        // 업그레이드 노드. 노드 저작 툴이 만들 데이터다.
        public List<UpgradeData> Upgrades = new List<UpgradeData>();
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
        public float MaxHealth;
        public float MoveSpeed;
        // 반지름.
        public float Size;
        public EnemyBehaviorData Behavior;
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

    // 업그레이드 노드 하나. 툴과 게임이 공유하는 형식 중 구매 규칙에 필요한 칸만 있다(위치·구역 없음).
    [Serializable]
    public sealed class UpgradeData
    {
        public string Id;
        public int Price;
        // 선행 노드 ID. 비어 있으면 처음부터 살 수 있다.
        public string Requires;
    }
}
