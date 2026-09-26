using System;

namespace BlackHole.Core
{
    // 시간제 종료의 공유 정의. 시간제는 현재 후보이며 최종 종료 조건은 미정이다.
    public sealed class TimeLimitDefinition
    {
        public float Duration { get; }

        public TimeLimitDefinition(float duration)
        {
            Duration = DefinitionGuard.Positive(duration, nameof(duration));
        }
    }

    // 한 판의 시간제 종료 판정. 판마다 하나 있다. 경과 시간은 GameSession이 가진다.
    // 진행 전 LimitStep, 진행 후 TryEnd, 남은 시간 표시가 모두 같은 Limit을 본다.
    // 성장으로 시간을 늘리던 연장은 성장과 함께 지웠다. 연장이 돌아오면 Limit을 늘리는 자리를 여기에 다시 둔다.
    public sealed class TimeLimitRule
    {
        public TimeLimitDefinition Definition { get; }
        // 이 판의 제한 시간(초).
        public float Limit { get; }

        internal TimeLimitRule(TimeLimitDefinition definition)
        {
            Definition = definition;
            Limit = definition.Duration;
        }

        public float Remaining(float elapsed) => Math.Max(0, Limit - elapsed);

        // 진행 전: 이번 진행이 제한 시간을 넘지 않게 자른다.
        internal float LimitStep(float elapsed, float step) => Math.Min(step, Limit - elapsed);

        // 진행 후: 지금까지의 진행으로 판의 종료를 판정한다.
        internal bool TryEnd(float elapsed, out SessionEndReason reason)
        {
            reason = SessionEndReason.TimeExpired;
            return elapsed >= Limit;
        }
    }
}
