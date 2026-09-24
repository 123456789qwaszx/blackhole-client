using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 출현 순서·간격·배치의 공유 정의. 대상 ID 참조의 실재는 ContentInvariants가 본다.
    public sealed class SpawnDefinition
    {
        public IReadOnlyList<string> TargetOrder { get; }
        public float Interval { get; }
        public float Radius { get; }
        // 출현마다 더하는 각도(라디안). 샘플은 황금각을 써서 연속 출현 위치가 겹치지 않게 한다.
        public float AngleStep { get; }
        public int Capacity { get; }

        public SpawnDefinition(IReadOnlyList<string> targetOrder, float interval, float radius,
            float angleStep, int capacity)
        {
            if (targetOrder == null || targetOrder.Count == 0)
                throw new ArgumentException("출현 순서에 대상 ID가 하나 이상 필요하다.", nameof(targetOrder));
            var order = new string[targetOrder.Count];
            for (int i = 0; i < order.Length; i++)
                order[i] = DefinitionGuard.Id(targetOrder[i], $"{nameof(targetOrder)}[{i}]");

            TargetOrder = Array.AsReadOnly(order);
            Interval = DefinitionGuard.Positive(interval, nameof(interval));
            Radius = DefinitionGuard.Positive(radius, nameof(radius));
            AngleStep = DefinitionGuard.Finite(angleStep, nameof(angleStep));
            Capacity = DefinitionGuard.Positive(capacity, nameof(capacity));
        }
    }
}
