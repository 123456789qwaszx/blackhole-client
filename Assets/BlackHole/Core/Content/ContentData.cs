using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 저작 형식. 검증 전 값이며 실행에 쓰지 않는다 — ContentLoader만 읽는다.
    // 지금은 BlackHole.Sample의 SampleContent가 코드로 채운다. 저작 방식(SO 등)은 팀이 정한다.
    // 전투 내용(적·Skill·HQ 성장·공급)의 칸은 그 시스템과 함께 지웠다.
    [Serializable]
    public sealed class ContentData
    {
        public SessionData Session;
        // 업그레이드 노드. 노드 저작 툴이 만들 데이터다.
        public List<UpgradeData> Upgrades = new List<UpgradeData>();
    }

    [Serializable]
    public sealed class SessionData
    {
        // 시간제 종료(현재 후보). 초 단위.
        public float TimeLimit;
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
