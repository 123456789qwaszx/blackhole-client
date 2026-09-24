namespace BlackHole.Core
{
    // 이동 정의를 실행 규칙으로 해석하는 유일한 자리.
    // ContentInvariants가 모든 대상의 이동이 해석되는지 확인하므로, 판 안에서는 Create가 실패하지 않는다.
    internal static class MovementRuleFactory
    {
        public static bool TryCreate(MovementDefinition definition, out IMovementRule rule)
        {
            switch (definition)
            {
                case OrbitMovementDefinition orbit:
                    rule = new OrbitMovement(orbit);
                    return true;
                case DiveMovementDefinition dive:
                    rule = new DiveMovement(dive);
                    return true;
                default:
                    rule = null;
                    return false;
            }
        }

        public static IMovementRule Create(MovementDefinition definition)
        {
            if (!TryCreate(definition, out IMovementRule rule))
                throw new System.ArgumentException(
                    $"실행 규칙이 연결되지 않은 이동 종류 '{definition?.GetType().Name}'.", nameof(definition));
            return rule;
        }
    }
}
