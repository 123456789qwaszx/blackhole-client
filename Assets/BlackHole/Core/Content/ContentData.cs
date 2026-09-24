using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 저작 형식. 검증 전 값이며 실행에 쓰지 않는다 — ContentLoader만 읽는다.
    // 지금은 BlackHole.Sample의 SampleContent가 코드로 채운다(D2). 저작 방식(SO 등)은 v2의 검증 대상이 아니다.
    [Serializable]
    public sealed class ContentData
    {
        public SessionData Session;
        public HqData Hq;
        public List<EnemyData> Enemies = new List<EnemyData>();
        public SpawnData Spawn;
        // 전투 시작 배치. 판 조립 때(0초) 한 번 공급한다.
        public List<SupplyData> StartSupply = new List<SupplyData>();
        // HQ 성장 노드. 노드가 없으면 HQ는 시작 Level에 머문다.
        public GrowthData Growth = new GrowthData();
        public List<SkillData> Skills = new List<SkillData>();
        // 모든 Player가 판 시작 때 가지는 Skill ID(Character 1종, 고정 구성). 획득 구조가 아니다.
        public List<string> StartingSkills = new List<string>();
        // 업그레이드 노드. 노드 저작 툴이 만들 데이터다.
        public List<UpgradeData> Upgrades = new List<UpgradeData>();
    }

    // 업그레이드 노드 하나. 툴과 게임이 공유하는 형식 중 게임 규칙에 필요한 칸만 있다(위치·구역 없음).
    [Serializable]
    public sealed class UpgradeData
    {
        public string Id;
        public int Price;
        // 선행 노드 ID. 비어 있으면 처음부터 살 수 있다.
        public string Requires;
        public List<UpgradeEffectData> Effects = new List<UpgradeEffectData>();
    }

    [Serializable]
    public sealed class UpgradeEffectData
    {
        // 효과 종류 이름(UpgradeEffectKind).
        public string Kind;
        public float Value;
        // 대상 ID. Skill 효과는 Skill ID, 공급 효과는 Enemy ID, 적 수치·보상 효과는 Enemy ID(비우면 모든 종류).
        public string Target;
    }

    [Serializable]
    public sealed class SessionData
    {
        // 시간제 종료(현재 후보). 초 단위.
        public float TimeLimit;
    }

    [Serializable]
    public sealed class HqData
    {
        public float X;
        public float Y;
    }

    [Serializable]
    public sealed class EnemyData
    {
        public string Id;
        public float MaxHealth;
        public float MoveSpeed;
        public float Size;
        public int Gold;
        public int HqExp;
        public EnemyBehaviorData Behavior;
        // 비어 있으면 사망 효과가 없다.
        public DeathEffectData DeathEffect;
    }

    // 사망 효과 종류마다 쓰는 칸이 다르다. 지금은 ChainLightning 하나다.
    [Serializable]
    public sealed class DeathEffectData
    {
        // 이름 문자열. 가능한 값은 ContentLoader의 해석 목록에 있다.
        public string Kind;
        // ChainLightning
        public float Damage;
        public float Range;
        public int Chains;
    }

    // 행동 종류마다 쓰는 칸이 다르다. 지금은 OrbitHq 하나다.
    [Serializable]
    public sealed class EnemyBehaviorData
    {
        // 이름 문자열. 가능한 값은 ContentLoader의 해석 목록에 있다.
        public string Kind;
        // OrbitHq
        public bool Clockwise;
    }

    // Skill 종류마다 쓰는 칸이 다르다. 지금은 Breaker와 PiercingLaser 둘이다. 쓰지 않는 칸은 읽지 않는다.
    [Serializable]
    public sealed class SkillData
    {
        public string Id;
        // 종류 이름. 가능한 값은 ContentLoader의 해석 목록에 있다.
        public string Kind;
        // Breaker
        public float Radius;
        // Breaker, PiercingLaser
        public float Interval;
        public float Damage;
        // PiercingLaser
        public float Width;
        public float TelegraphDuration;
        public float BoundaryRadius;
    }

    // 출현 위치 규칙. 무엇을·얼마나는 공급(StartSupply, 성장 노드)이 정한다.
    [Serializable]
    public sealed class SpawnData
    {
        public float Distance;
        public float AngleStep;
    }

    // Enemy 공급 한 건: 어떤 종류를 몇 마리.
    [Serializable]
    public sealed class SupplyData
    {
        public string Enemy;
        public int Count;
    }

    // HQ 성장 노드 목록. 시작 Level은 1이고 Levels[i]는 Level (i + 2)다.
    [Serializable]
    public sealed class GrowthData
    {
        public List<GrowthLevelData> Levels = new List<GrowthLevelData>();
    }

    // 성장 노드 하나: 도달 임계값과, 처음 도달했을 때의 성장 효과.
    [Serializable]
    public sealed class GrowthLevelData
    {
        // 이 Level에 도달하는 누적 EXP. 앞 노드보다 커야 한다.
        public int Exp;
        // 판 시간 연장(초).
        public float ExtraTime;
        // 추가 공급.
        public List<SupplyData> Supply = new List<SupplyData>();
    }
}
