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
        public float SessionDuration;
        public List<TargetData> Targets = new List<TargetData>();
        public List<SkillData> Skills = new List<SkillData>();
        public GrowthData Growth;
        public SpawnData Spawn;
    }

    [Serializable]
    public sealed class TargetData
    {
        public string Id;
        public float MaxHealth;
        public float AngularSpeed;
        public float InwardSpeed;
        public int Reward;
    }

    [Serializable]
    public sealed class SkillData
    {
        public string Id;
        // 이름 문자열. 가능한 값은 ContentLoader의 해석 목록에 있다.
        public string Kind;
        public float Cooldown;
        public float Damage;
        public float Radius;
        public float PullDistance;
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
        public int Capacity;
    }
}
