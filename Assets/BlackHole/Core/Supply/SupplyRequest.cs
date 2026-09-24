using System;

namespace BlackHole.Core
{
    // Enemy 공급 한 건: 어떤 종류를 몇 마리.
    // 전투 시작 배치와 성장 공급이 같은 모양을 쓴다.
    public readonly struct SupplyRequest
    {
        public EnemyDefinition Enemy { get; }
        public int Count { get; }

        public SupplyRequest(EnemyDefinition enemy, int count)
        {
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count), "양의 정수가 필요하다.");

            Enemy = enemy ?? throw new ArgumentNullException(nameof(enemy));
            Count = count;
        }
    }
}
