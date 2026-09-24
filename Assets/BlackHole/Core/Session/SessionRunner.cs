using System;

namespace BlackHole.Core
{
    // 한 판의 실행 시계와 시간 분할을 소유한다. 판 안 처리 순서는 Playfield, 종료 판정은 모드가 맡는다.
    // 요청 허용 여부와 판 상태(실행/정지/종료)는 GameSession이 소유한다.
    internal sealed class SessionRunner
    {
        // 실행 설정: 긴 프레임에도 이동/흡수/생성 순서가 한 번에 건너뛰지 않게 한 단계를 제한한다.
        private const float MaxStep = 1f / 30f;

        private readonly Playfield _field;
        private readonly TimeLimitMode _mode;

        public float Elapsed { get; private set; }

        public SessionRunner(Playfield field, TimeLimitMode mode)
        {
            _field = field;
            _mode = mode;
        }

        // 한 프레임을 단계로 나누어 진행한다. 모드가 끝을 판정하면 그 단계에서 멈추고 true.
        // 판정 정밀도는 실행 단계(최대 MaxStep) 단위다.
        public bool Advance(float delta, out SessionEndReason reason)
        {
            float remaining = delta;
            while (remaining > 0)
            {
                // 진행 전: 모드가 허용하는 시간보다 더 진행하지 않는다.
                float step = _mode.LimitStep(Elapsed, Math.Min(remaining, MaxStep));

                _field.Advance(step);
                Elapsed += step;
                remaining -= step;

                // 진행 후: 이번 단계에서 달성한 결과로 다음 단계 진행 여부를 정한다.
                if (_mode.TryEnd(Elapsed, out reason)) return true;
            }

            reason = default;
            return false;
        }
    }
}
