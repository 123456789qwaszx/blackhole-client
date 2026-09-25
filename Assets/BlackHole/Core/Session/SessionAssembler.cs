using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 검증된 콘텐츠와 참가자의 진행 상태로 한 전투를 새로 조립하는 유일한 진입점.
    // 정의는 공유하고, 전투의 실행 상태(시간, 상태, 결과)는 전투마다 새로 만든다.
    // 누가 참가하는지는 콘텐츠가 아니라 판 설정이다 — 호스트가 넘긴다(지금은 로컬 1명).
    //
    // 진행 상태(Gold, 산 노드)는 전투 사이에 이어진다. 새 진행을 시작할지는 호출하는 쪽이 새 PlayerState로 정한다.
    // 산 노드가 전투를 바꾸는 효과는 효과의 대상(Skill·적·공급)과 함께 지웠다.
    public static class SessionAssembler
    {
        public static GameSession CreateBattle(GameContent content, IReadOnlyList<PlayerState> states)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            VerifyParticipants(states);

            var players = new List<PlayerState>(states);
            var session = new GameSession(new TimeLimitRule(content.TimeLimit), players.AsReadOnly());

            // 모든 검사를 통과한 뒤에 전투에 들인다. 조립이 실패하면 PlayerState는 묶이지 않는다.
            foreach (PlayerState state in players)
            {
                state.EnterBattle();
            }

            return session;
        }

        private static void VerifyParticipants(IReadOnlyList<PlayerState> states)
        {
            if (states == null || states.Count == 0)
                throw new ArgumentException(
                    "참가 Player가 한 명 이상 필요하다.", nameof(states));

            var ids = new HashSet<PlayerId>();

            foreach (PlayerState state in states)
            {
                if (state == null)
                    throw new ArgumentException("PlayerState가 비어 있다.", nameof(states));

                if (!ids.Add(state.Id))
                    throw new ArgumentException(
                        $"{state.Id}가 두 번 참가했다.", nameof(states));

                if (state.InBattle)
                    throw new InvalidOperationException($"{state.Id}는 이미 진행 중인 전투에 들어가 있다.");
            }
        }
    }
}
