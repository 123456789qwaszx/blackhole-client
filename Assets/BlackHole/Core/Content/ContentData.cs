using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 저작 형식. 검증 전 값이며 실행에 쓰지 않는다 — ContentLoader만 읽는다.
    // 적 종류·공급·배치·전체 개체 수 상한은 Unity 쪽 에셋(EnemyCatalog, EnemySupplySetup)이,
    // 스킬은 스킬 설정 에셋(SkillSetup)이 채운다. 판 설정은 아직 BlackHole.Sample의 SampleContent가 코드로 채운다.
    // 업그레이드 노드는 판 조립 콘텐츠가 아니다 — 노드 목록 에셋(NodeCatalog → NodeTreeData)이 따로 가진다.
    [Serializable]
    public sealed class ContentData
    {
        public SessionData Session;
        // 스킬은 종류마다 칸이 따로 있다. 비어 있으면 판에 그 스킬이 없다.
        public BreakerData Breaker;
        public LaserData Laser;
        public List<EnemyData> Enemies = new List<EnemyData>();
        // 출현 위치. 공급이 하나라도 있으면 필요하다.
        public EnemyPlacementData EnemyPlacement;
        // 한 판에 동시에 살아 있을 수 있는 적의 전체 최대 수(성능 예산). 출현 배치가 있으면 1 이상이어야 한다.
        public int MaxAliveEnemies;
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
    public sealed class BreakerData
    {
        public float Damage;
        // 공격 주기(초).
        public float Interval;
        // 조준점을 중심으로 한 공격 원의 반지름.
        public float Radius;
        // 한 Tick이 치명타일 확률(0 ~ 1).
        public float CritChance;
        // 치명타 Tick의 피해 배율(1 이상).
        public float CritMultiplier;
    }

    [Serializable]
    public sealed class LaserData
    {
        public float Damage;
        // 예고를 시작하는 주기(초).
        public float Interval;
        // 발사선의 굵기.
        public float Width;
        // 예고가 보이는 시간(초).
        public float TelegraphDuration;
        // 시작점이 놓이는 경계 원의 반지름(HQ 중심).
        public float BoundaryRadius;
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
        // 없거나 종류 이름이 비어 있으면 사망 효과가 없다.
        public DeathEffectData DeathEffect;
        // 잠긴 채 시작하는가. 그러면 해금 노드(enemy.<종류>.unlock)를 사야 판에 나온다. 기본은 처음부터 나온다.
        public bool StartsLocked;
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

    // 사망 효과 종류마다 쓰는 칸이 다르다. 종류가 쓰지 않는 칸은 읽지 않는다.
    [Serializable]
    public sealed class DeathEffectData
    {
        // 종류 이름. 가능한 값은 ContentLoader의 해석 목록에 있다. 비어 있으면 효과가 없다.
        public string Kind;
        // ChainLightning, Explosion
        public float Damage;
        // ChainLightning(한 번 옮겨 가는 거리), Explosion(반경)
        public float Radius;
        // ChainLightning(옮겨 가는 최대 횟수)
        public int MaxTargets;
        // AttackHaste, GuaranteedCritical(버프 시간, 초)
        public float Duration;
        // AttackHaste(공격 주기 배율, 0 ~ 1)
        public float IntervalMultiplier;
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
}
