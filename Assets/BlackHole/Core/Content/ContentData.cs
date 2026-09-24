using System;

namespace BlackHole.Core
{
    // 저작 형식. 검증 전 값이며 실행에 쓰지 않는다 — ContentLoader만 읽는다.
    // 지금은 BlackHole.Sample의 SampleContent가 코드로 채운다(D2). 저작 방식(SO 등)은 v2의 검증 대상이 아니다.
    [Serializable]
    public sealed class ContentData
    {
        public SessionData Session;
        public HqData Hq;
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
}
