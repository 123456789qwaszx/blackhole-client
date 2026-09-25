using System.Collections.Generic;

namespace BlackHole.Core
{
    public enum SessionPhase { Running, Paused, Ended }

    public enum SessionEndReason { TimeExpired, Stopped }

    // 판이 끝날 때 한 번 확정되는 결과. 이후 판 상태가 바뀌어도 변하지 않는 스냅샷이다.
    public sealed class SessionResult
    {
        public SessionEndReason Reason { get; }
        public float PlayedSeconds { get; }

        internal SessionResult(SessionEndReason reason, float playedSeconds)
        {
            Reason = reason;
            PlayedSeconds = playedSeconds;
        }
    }

    // 한 판의 상태(진행/정지/종료), 경과 시간, 결과, 요청 허용 여부를 가진다.
    // 종료 판정은 TimeLimitRule이 한다. 재시작은 같은 객체의 부분 초기화가 아니라 새 조립이다(SessionAssembler).
    //
    // 판 안의 전투 내용(적·Skill·HQ 성장)은 이 판에 없다. 전투 내용이 돌아오면 Advance의 시간 진행에 끼운다.
    public sealed class GameSession
    {
        private readonly IReadOnlyList<PlayerState> _players;

        public TimeLimitRule TimeLimit { get; }
        public SessionPhase Phase { get; private set; }
        public float Elapsed { get; private set; }
        public float Remaining => TimeLimit.Remaining(Elapsed);
        // 판이 끝나기 전에는 null이다.
        public SessionResult Result { get; private set; }

        internal GameSession(TimeLimitRule timeLimit, IReadOnlyList<PlayerState> players)
        {
            TimeLimit = timeLimit;
            _players = players;
        }

        // 진행 중일 때만 시간이 흐른다. 제한 시간을 넘겨 진행하지 않고, 닿으면 그 자리에서 끝난다.
        public void Advance(float delta)
        {
            DefinitionGuard.Delta(delta);

            if (Phase != SessionPhase.Running || delta == 0)
                return;

            Elapsed += TimeLimit.LimitStep(Elapsed, delta);

            if (TimeLimit.TryEnd(Elapsed, out SessionEndReason reason))
                End(reason);
        }

        public void TogglePause()
        {
            if (Phase == SessionPhase.Ended)
                return;

            Phase = Phase == SessionPhase.Running ? SessionPhase.Paused : SessionPhase.Running;
        }

        public void Stop() => End(SessionEndReason.Stopped);

        // 전투가 끝나면 PlayerState를 전투에서 풀어 준다. 그때부터 구매할 수 있다.
        private void End(SessionEndReason reason)
        {
            if (Phase == SessionPhase.Ended)
                return;

            Result = new SessionResult(reason, Elapsed);
            Phase = SessionPhase.Ended;

            foreach (PlayerState player in _players)
            {
                player.LeaveBattle();
            }
        }
    }
}
