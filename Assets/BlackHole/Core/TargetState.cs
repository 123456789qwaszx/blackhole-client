using System;

namespace BlackHole.Core
{
    public enum TargetPhase { Orbiting, Defeated, Absorbed }

    // 개체의 HP와 위치, 생명주기를 소유한다. 사망과 흡수는 다른 전이다.
    public sealed class TargetState
    {
        public int Id { get; }
        public TargetDefinition Definition { get; }
        public float Health { get; private set; }
        public float Radius { get; private set; }
        public float Angle { get; private set; }
        public TargetPhase Phase { get; private set; }
        public Point2 Position => Point2.FromPolar(Radius, Angle);

        internal TargetState(int id, TargetDefinition definition, float radius, float angle)
        {
            Id = id;
            Definition = definition;
            Health = definition.MaxHealth;
            Radius = radius;
            Angle = angle;
        }

        internal void Move(float delta, float absorptionRadius)
        {
            if (Phase == TargetPhase.Absorbed) return;

            Angle = (Angle + Definition.AngularSpeed * delta) % ((float)Math.PI * 2);
            // 살아 있는 대상은 안쪽 궤도에 머물고, 죽은 대상은 중심으로 떨어진다.
            float minimum = Phase == TargetPhase.Defeated ? 0 : absorptionRadius + 0.8f;
            float speed = Phase == TargetPhase.Defeated ? 4f : Definition.InwardSpeed;
            Radius = Math.Max(minimum, Radius - speed * delta);
        }

        internal bool ApplyDamage(float damage)
        {
            if (Phase != TargetPhase.Orbiting) return false;
            Health = Math.Max(0, Health - damage);
            if (Health == 0) Phase = TargetPhase.Defeated;
            return true;
        }

        internal void Pull(float distance)
        {
            if (Phase != TargetPhase.Absorbed)
                Radius = Math.Max(0, Radius - distance);
        }

        internal bool TryAbsorb(float radius)
        {
            if (Phase != TargetPhase.Defeated || Radius > radius) return false;
            Phase = TargetPhase.Absorbed;
            return true;
        }
    }
}
