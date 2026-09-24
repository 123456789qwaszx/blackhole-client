using System;

namespace BlackHole.Core
{
    public sealed class TargetDefinition
    {
        public string Id { get; }
        public float MaxHealth { get; }
        public float AngularSpeed { get; }
        public float InwardSpeed { get; }
        public int Reward { get; }

        public TargetDefinition(string id, float maxHealth, float angularSpeed,
            float inwardSpeed, int reward)
        {
            Id = DefinitionGuard.Id(id);
            MaxHealth = DefinitionGuard.Positive(maxHealth, nameof(maxHealth));
            if (float.IsNaN(angularSpeed) || float.IsInfinity(angularSpeed))
                throw new ArgumentOutOfRangeException(nameof(angularSpeed));
            AngularSpeed = angularSpeed;
            InwardSpeed = DefinitionGuard.Positive(inwardSpeed, nameof(inwardSpeed));
            if (reward <= 0) throw new ArgumentOutOfRangeException(nameof(reward));
            Reward = reward;
        }
    }
}
