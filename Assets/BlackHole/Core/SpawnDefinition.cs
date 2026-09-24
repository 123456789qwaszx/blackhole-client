using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 출현 순서와 간격의 공유 정의. 대상 ID 참조의 실재는 ContentInvariants가 본다.
    public sealed class SpawnDefinition
    {
        public IReadOnlyList<string> TargetOrder { get; }
        public float Interval { get; }
        public float Radius { get; }
        public int Capacity { get; }

        public SpawnDefinition(IReadOnlyList<string> targetOrder, float interval, float radius, int capacity)
        {
            if (targetOrder == null || targetOrder.Count == 0)
                throw new ArgumentException("출현 순서에 대상 ID가 하나 이상 필요하다.", nameof(targetOrder));
            var order = new string[targetOrder.Count];
            for (int i = 0; i < order.Length; i++)
                order[i] = DefinitionGuard.Id(targetOrder[i], $"{nameof(targetOrder)}[{i}]");

            TargetOrder = Array.AsReadOnly(order);
            Interval = DefinitionGuard.Positive(interval, nameof(interval));
            Radius = DefinitionGuard.Positive(radius, nameof(radius));
            Capacity = DefinitionGuard.Positive(capacity, nameof(capacity));
        }
    }
}
