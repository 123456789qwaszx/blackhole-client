using System;

namespace BlackHole.Core
{
    // 한 판의 실행 시계와 시간 분할을 가진다. 한 단계 안의 처리 순서는 World, 종료 판정은 TimeLimitRule이 맡는다.
    // 판 상태(실행/정지/종료)와 요청 허용 여부는 GameSession이 가진다.
    internal sealed class SessionRunner
    {
        // 실행 설정: 긴 프레임에도 단계 안의 처리 순서가 한 번에 건너뛰지 않게 한 단계를 제한한다.
        private const float MaxStep = 1f / 30f;

        private readonly World _world;
        private readonly TimeLimitRule _timeLimit;

        public float Elapsed { get; private set; }

        public SessionRunner(World world, TimeLimitRule timeLimit)
        {
            _world = world;
            _timeLimit = timeLimit;
        }

        // 한 프레임을 단계로 나누어 진행한다. 종료가 판정되면 그 단계에서 멈추고 true.
        // 판정 정밀도는 실행 단계(최대 MaxStep) 단위다.
        public bool Advance(float delta, out SessionEndReason reason)
        {
            float remaining = delta;
            while (remaining > 0)
            {
                // 진행 전: 제한 시간보다 더 진행하지 않는다.
                float step = _timeLimit.LimitStep(Elapsed, Math.Min(remaining, MaxStep));

                _world.Step(step);
                Elapsed += step;
                remaining -= step;

                // 진행 후: 이번 단계의 결과로 다음 단계 진행 여부를 정한다.
                if (_timeLimit.TryEnd(Elapsed, out reason)) return true;
            }

            reason = default;
            return false;
        }
    }
}
