namespace BlackHole.Core
{
    // 대상 한 종류의 공유 정의. 개별 HP·위치는 TargetState가 소유한다.
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
            Id = DefinitionGuard.Id(id, nameof(id));
            MaxHealth = DefinitionGuard.Positive(maxHealth, nameof(maxHealth));
            AngularSpeed = DefinitionGuard.Finite(angularSpeed, nameof(angularSpeed));
            InwardSpeed = DefinitionGuard.Positive(inwardSpeed, nameof(inwardSpeed));
            Reward = DefinitionGuard.Positive(reward, nameof(reward));
        }
    }
}
