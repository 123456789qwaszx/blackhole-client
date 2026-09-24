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
        public List<SkillData> Skills = new List<SkillData>();
        // 모든 Player가 판 시작 때 가지는 Skill ID(Character 1종, 고정 구성). 획득 구조가 아니다.
        public List<string> StartingSkills = new List<string>();
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
        public EnemyBehaviorData Behavior;
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

    [Serializable]
    public sealed class SkillData
    {
        public string Id;
        // 이름 문자열. 가능한 값은 ContentLoader의 해석 목록에 있다.
        public string Origin;
        public float Radius;
        public float Interval;
        public float Damage;
    }

    [Serializable]
    public sealed class SpawnData
    {
        public float Interval;
        public int MaxAlive;
        public float Distance;
        public float AngleStep;
        public List<string> Order = new List<string>();
    }
}
