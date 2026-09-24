using System;

namespace BlackHole.Core
{
    // 시간제 목표의 공유 정의.
    public sealed class TimeLimitDefinition
    {
        public float Duration { get; }

        public TimeLimitDefinition(float duration)
        {
            Duration = DefinitionGuard.Positive(duration, nameof(duration));
        }
    }

    // 시간제 목표의 판정 규칙. 가변 상태가 없다 — 경과 시간은 SessionRunner가 소유한다.
    // 새 목표(예: 질량 도달)는 같은 두 경계에 연결한다:
    // 진행 전 LimitStep(목표를 넘겨 진행하지 않게), 진행 후 TryEnd(달성 판정).
    public sealed class TimeLimitMode
    {
        public TimeLimitDefinition Definition { get; }

        internal TimeLimitMode(TimeLimitDefinition definition)
        {
            Definition = definition;
        }

        public float Remaining(float elapsed) => Math.Max(0, Definition.Duration - elapsed);

        // 진행 전: 이번 단계가 제한 시간을 넘지 않게 자른다.
        internal float LimitStep(float elapsed, float step) => Math.Min(step, Definition.Duration - elapsed);

        // 진행 후: 이번 단계까지의 결과로 판의 종료를 판정한다.
        internal bool TryEnd(float elapsed, out SessionEndReason reason)
        {
            reason = SessionEndReason.TimeExpired;
            return elapsed >= Definition.Duration;
        }
    }
}
