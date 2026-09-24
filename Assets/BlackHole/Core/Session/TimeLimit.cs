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

    // 한 판의 시간제 종료 판정. 판마다 하나 있다. 경과 시간은 SessionRunner가 가진다.
    // 제한 시간은 공유 정의의 시간에서 시작해 성장 효과로 늘어난다. 공유 정의는 바꾸지 않는다.
    // 진행 전 LimitStep, 진행 후 TryEnd, 남은 시간 표시가 모두 같은 Limit을 본다.
    // 다른 종료 조건이 생기면 같은 두 경계에 연결한다. 두 번째 조건이 실제로 생기기 전에는 교체 인터페이스를 만들지 않는다.
    public sealed class TimeLimitRule
    {
        public TimeLimitDefinition Definition { get; }
        // 이 판의 제한 시간(초) = 공유 정의의 시간 + 이 판에서 늘어난 시간.
        public float Limit { get; private set; }

        internal TimeLimitRule(TimeLimitDefinition definition)
        {
            Definition = definition;
            Limit = definition.Duration;
        }

        public float Remaining(float elapsed) => Math.Max(0, Limit - elapsed);

        // 진행 전: 이번 단계가 제한 시간을 넘지 않게 자른다.
        internal float LimitStep(float elapsed, float step) => Math.Min(step, Limit - elapsed);

        // 진행 후: 이번 단계까지의 결과로 판의 종료를 판정한다.
        internal bool TryEnd(float elapsed, out SessionEndReason reason)
        {
            reason = SessionEndReason.TimeExpired;
            return elapsed >= Limit;
        }

        internal void Extend(float seconds)
        {
            Limit += DefinitionGuard.NonNegative(seconds, nameof(seconds));
        }
    }
}
