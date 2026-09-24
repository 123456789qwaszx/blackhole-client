using System;

namespace BlackHole.Core
{
    public enum TargetPhase { Alive, Defeated, Absorbed }

    // 개체의 HP·위치·생명주기를 소유한다. 사망과 흡수는 다른 전이다.
    // 위치의 원본은 여기 하나다. 이동 규칙은 다음 위치를 계산만 하고, 화면은 읽기만 한다.
    //
    // 생명주기별 이동:
    //   Alive    — 정의의 이동 규칙을 따르되 블랙홀 하한 안으로 들어가지 않는다.
    //   Defeated — 이동 규칙의 각도 변화는 유지하고, 반경은 공통 낙하 속도로 중심까지 줄어든다.
    //   Absorbed — 움직이지 않는다. 같은 단계에 TargetWorld 목록에서 제거된다.
    public sealed class TargetState
    {
        private readonly IMovementRule _movement;

        public int Id { get; }
        public TargetDefinition Definition { get; }
        public float Health { get; private set; }
        public float Radius { get; private set; }
        public float Angle { get; private set; }
        // 출현 후 흐른 판 시간.
        public float Age { get; private set; }
        public TargetPhase Phase { get; private set; }
        public Point2 Position => Point2.FromPolar(Radius, Angle);

        internal TargetState(int id, TargetDefinition definition, IMovementRule movement, float radius, float angle)
        {
            Id = id;
            Definition = definition;
            _movement = movement;
            Health = definition.MaxHealth;
            Radius = radius;
            Angle = angle;
        }

        internal void Move(float delta, float aliveFloor, float fallSpeed)
        {
            if (Phase == TargetPhase.Absorbed) return;

            PolarPoint next = _movement.Next(new PolarPoint(Radius, Angle), Age, delta);
            Angle = next.Angle;
            Radius = Phase == TargetPhase.Defeated
                ? Math.Max(0, Radius - fallSpeed * delta)
                : Math.Max(aliveFloor, next.Radius);
            Age += delta;
        }

        internal bool ApplyDamage(float damage)
        {
            if (Phase != TargetPhase.Alive) return false;
            Health = Math.Max(0, Health - damage);
            if (Health == 0) Phase = TargetPhase.Defeated;
            return true;
        }

        // 당김은 하한을 무시한다. 살아 있는 대상은 다음 이동에서 하한으로 돌아간다.
        internal bool Pull(float distance)
        {
            if (Phase == TargetPhase.Absorbed) return false;
            Radius = Math.Max(0, Radius - distance);
            return true;
        }

        internal bool TryAbsorb(float radius)
        {
            if (Phase != TargetPhase.Defeated || Radius > radius) return false;
            Phase = TargetPhase.Absorbed;
            return true;
        }
    }
}
