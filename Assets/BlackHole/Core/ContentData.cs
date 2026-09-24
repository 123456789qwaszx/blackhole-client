using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 저작 형식. 검증 전 값이며 실행에 쓰지 않는다 — ContentLoader만 읽는다.
    // 지금은 ReferenceGame이 코드로 채운다. SO 저작(S5 이후)이 들어오면 SO가 이 모양을 채우고
    // 코드 샘플은 지운다. 같은 콘텐츠를 두 곳에서 관리하지 않는다.
    [Serializable]
    public sealed class ContentData
    {
        public float TimeLimit;
        public TargetRulesData TargetRules;
        public List<TargetData> Targets = new List<TargetData>();
        public List<SkillData> Skills = new List<SkillData>();
        public GrowthData Growth;
        public SpawnData Spawn;
    }

    [Serializable]
    public sealed class TargetRulesData
    {
        public float AliveMargin;
        public float FallSpeed;
    }

    [Serializable]
    public sealed class TargetData
    {
        public string Id;
        public float MaxHealth;
        public int Reward;
        public MovementData Movement;
    }

    // 이동 종류마다 쓰는 칸이 다르다. 쓰지 않는 칸은 0이어야 한다(ContentLoader가 확인).
    [Serializable]
    public sealed class MovementData
    {
        // 이름 문자열. 가능한 값은 ContentLoader의 해석 목록에 있다.
        public string Kind;
        // Orbit
        public float AngularSpeed;
        public float InwardSpeed;
        // Dive
        public float InitialSpeed;
        public float Acceleration;
    }

    [Serializable]
    public sealed class SkillData
    {
        public string Id;
        public float Cooldown;
        public SelectionData Selection;
        // 적용 순서대로.
        public List<EffectData> Effects = new List<EffectData>();
    }

    [Serializable]
    public sealed class SelectionData
    {
        // 이름 문자열. 가능한 값은 ContentLoader의 해석 목록에 있다.
        public string Kind;
        public float Radius;
    }

    [Serializable]
    public sealed class EffectData
    {
        // 이름 문자열. 가능한 값은 ContentLoader의 해석 목록에 있다.
        public string Kind;
        // Damage: 피해량. Pull: 당김 거리.
        public float Amount;
    }

    [Serializable]
    public sealed class GrowthData
    {
        public int UpgradeCost;
        public int MaxPowerLevel;
        public float PowerPerLevel;
    }

    [Serializable]
    public sealed class SpawnData
    {
        public List<string> TargetOrder = new List<string>();
        public float Interval;
        public float Radius;
        public float AngleStep;
        public int Capacity;
    }
}
