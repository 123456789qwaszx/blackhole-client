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

    // 한 판의 상태(실행/정지/종료), 결과, 요청 허용 여부만 가진다(B8).
    // 경과 시간과 시간 분할은 SessionRunner, 종료 판정은 TimeLimitRule, 판 안의 대상은 World가 가진다.
    // 재시작은 같은 객체의 부분 초기화가 아니라 새 조립이다(SessionAssembler).
    public sealed class GameSession
    {
        private readonly SessionRunner _runner;

        public World World { get; }
        public TimeLimitRule TimeLimit { get; }
        public SessionPhase Phase { get; private set; }
        public float Elapsed => _runner.Elapsed;
        public float Remaining => TimeLimit.Remaining(Elapsed);
        public SessionResult Result { get; private set; }

        internal GameSession(World world, TimeLimitRule timeLimit)
        {
            World = world;
            TimeLimit = timeLimit;
            _runner = new SessionRunner(world, timeLimit);
        }

        public void Advance(float delta)
        {
            DefinitionGuard.Delta(delta);
            
            if (Phase != SessionPhase.Running || delta == 0)
                return;

            World.BeginAdvance();
            
            if (_runner.Advance(delta, out SessionEndReason reason))
                End(reason);
        }

        // 호스트가 Player의 조준점을 넣는다(매 프레임, 진행 전). 누가 채우는지는 Core가 모른다.
        // 조준점은 입력 상태일 뿐 게임 상태를 바꾸지 않으므로 일시정지·종료 중에도 받는다.
        public bool SetAimPoint(PlayerId player, Point2? aimPoint)
        {
            if (!World.TryGetPlayer(player, out Player target)) return false;
            target.SetAimPoint(aimPoint);
            return true;
        }

        public void TogglePause()
        {
            if (Phase == SessionPhase.Ended) return;
            Phase = Phase == SessionPhase.Running ? SessionPhase.Paused : SessionPhase.Running;
        }

        public void Stop() => End(SessionEndReason.Stopped);

        // 전투가 끝나면 PlayerState를 전투에서 풀어 준다. 그때부터 구매할 수 있다.
        private void End(SessionEndReason reason)
        {
            if (Phase == SessionPhase.Ended) return;
            Result = new SessionResult(reason, Elapsed);
            Phase = SessionPhase.Ended;

            foreach (Player player in World.Players)
            {
                player.State.LeaveBattle();
            }
        }
    }
}
