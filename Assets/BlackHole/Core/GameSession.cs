using System;

namespace BlackHole.Core
{
    public enum SessionPhase { Running, Paused, Ended }
    public enum SessionEndReason { TimeExpired, Stopped }

    public sealed class SessionResult
    {
        public SessionEndReason Reason { get; }
        public float PlayedSeconds { get; }
        public int Mass { get; }
        public int AbsorbedCount { get; }

        internal SessionResult(SessionEndReason reason, float elapsed, GrowthState growth)
        {
            Reason = reason;
            PlayedSeconds = elapsed;
            Mass = growth.Mass;
            AbsorbedCount = growth.AbsorbedCount;
        }
    }

    // 한 판의 상태(실행/정지/종료), 종료 결과, 외부 요청의 허용 여부만 소유한다.
    // 경과 시간과 시간 분할은 SessionRunner, 종료 판정은 모드가 맡는다.
    // 재시작은 같은 객체의 부분 초기화가 아니라 새 Session 조립이다(SessionAssembler).
    public sealed class GameSession
    {
        private readonly SessionRunner _runner;
        public Playfield Field { get; }
        public TimeLimitMode Mode { get; }
        public SessionPhase Phase { get; private set; }
        public float Elapsed => _runner.Elapsed;
        public float Remaining => Mode.Remaining(Elapsed);
        public SessionResult Result { get; private set; }

        internal GameSession(Playfield field, TimeLimitMode mode)
        {
            Field = field;
            Mode = mode;
            _runner = new SessionRunner(field, mode);
        }

        public void Advance(float delta)
        {
            DefinitionGuard.Delta(delta);
            if (Phase != SessionPhase.Running || delta == 0) return;

            if (_runner.Advance(delta, out SessionEndReason reason))
                End(reason);
        }

        public CastResult TryCast(string skillId, Point2 aim) => TryCast(skillId, aim, out _);

        // report는 Cast일 때만 의미가 있다. 화면은 이것으로 연출한다.
        public CastResult TryCast(string skillId, Point2 aim, out CastReport report)
        {
            report = default;
            return Phase == SessionPhase.Running
                ? Field.TryCast(skillId, aim, out report)
                : CastResult.SessionInactive;
        }

        public UpgradeResult TryUpgrade() =>
            Phase == SessionPhase.Running
                ? Field.Growth.TryUpgrade()
                : UpgradeResult.SessionInactive;

        public void TogglePause()
        {
            if (Phase == SessionPhase.Ended) return;
            Phase = Phase == SessionPhase.Running ? SessionPhase.Paused : SessionPhase.Running;
        }

        public void Stop() => End(SessionEndReason.Stopped);

        private void End(SessionEndReason reason)
        {
            if (Phase == SessionPhase.Ended) return;
            Result = new SessionResult(reason, Elapsed, Field.Growth);
            Phase = SessionPhase.Ended;
        }
    }
}
