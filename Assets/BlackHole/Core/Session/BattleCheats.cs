using System;

namespace BlackHole.Core
{
    // 개발용 치트: 진행 중인 판을 규칙을 거치지 않고 바꾼다. 개발용 전투 콘솔만 부른다. 게임 규칙이 아니다.
    // 진행 상태는 건드리지 않는다 — 판이 바꾼 값은 결산이 평소처럼 진행 상태에 반영한다.
    public static class BattleCheats
    {
        // 판의 블랙홀에 EXP를 더한다(줄이지는 못한다). Level은 다음 Step의 5 자리에서 오르고, 이정표·성장 효과도 평소와 같다.
        // 판을 치르며 Level·이정표를 시험할 때 쓴다. 끝난 판은 거부한다.
        public static void AddHqExp(GameSession session, long exp)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            if (session.Phase == SessionPhase.Ended)
                throw new InvalidOperationException("끝난 판은 바꿀 수 없다.");

            if (exp < 0)
                throw new ArgumentOutOfRangeException(nameof(exp), "0 이상이어야 한다.");

            session.World.Hq.AddExp(exp);
        }
    }
}
