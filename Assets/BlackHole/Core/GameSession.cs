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

    // 한 판의 수명과 외부 요청의 허용 여부만 소유한다.
    // 재시작은 같은 객체의 부분 초기화가 아니라 새 Session 조립이다.
    public sealed class GameSession
    {
        private const float MaxStep = 1f / 30f;
        private readonly float _duration;
        public Playfield Field { get; }
        public SessionPhase Phase { get; private set; }
        public float Elapsed { get; private set; }
        public float Remaining => Math.Max(0, _duration - Elapsed);
        public SessionResult Result { get; private set; }

        public GameSession(Playfield field, float duration)
        {
            Field = field ?? throw new ArgumentNullException(nameof(field));
            _duration = DefinitionGuard.Positive(duration, nameof(duration));
        }

        public void Advance(float delta)
        {
            DefinitionGuard.Delta(delta);
            if (Phase != SessionPhase.Running || delta == 0) return;

            float remaining = Math.Min(delta, Remaining);
            // 긴 프레임에도 이동/흡수/생성 순서가 한 번에 건너뛰지 않게 제한한다.
            while (remaining > 0)
            {
                float step = Math.Min(remaining, MaxStep);
                Field.Advance(step);
                remaining -= step;
            }
            Elapsed = Math.Min(_duration, Elapsed + delta);
            if (Elapsed >= _duration) End(SessionEndReason.TimeExpired);
        }

        public CastResult TryCast(string skillId, Point2 aim) =>
            Phase == SessionPhase.Running
                ? Field.TryCast(skillId, aim)
                : CastResult.SessionInactive;

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
