using System;

namespace BlackHole.Core
{
    // 한 판을 조립하는 데 필요한 검증된 공유 정의 묶음. 읽기 전용이며 여러 판이 함께 쓴다.
    // 오류가 있는 콘텐츠의 경로별 보고는 ContentLoader가 맡는다.
    public sealed class GameContent
    {
        public TimeLimitDefinition TimeLimit { get; }
        public HqDefinition Hq { get; }

        public GameContent(TimeLimitDefinition timeLimit, HqDefinition hq)
        {
            TimeLimit = timeLimit ?? throw new ArgumentNullException(nameof(timeLimit));
            Hq = hq ?? throw new ArgumentNullException(nameof(hq));
        }
    }
}
