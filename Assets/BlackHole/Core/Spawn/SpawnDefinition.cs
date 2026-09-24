using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 출현의 공유 정의: 언제(주기), 얼마나(동시 최대 수), 어디에(HQ로부터의 거리, 각도 간격), 무엇을(종류 순서).
    // 값은 전부 [임시]이며 샘플 콘텐츠가 정한다. 웨이브·확률·HQ 성장에 따른 변화는 미정이다.
    // 종류 순서가 가리키는 Enemy ID의 실재는 ContentInvariants가 본다.
    public sealed class SpawnDefinition
    {
        public float Interval { get; }
        public int MaxAlive { get; }
        public float Distance { get; }
        // 출현마다 더하는 각도(라디안). 연속 출현 위치가 겹치지 않게 한다.
        public float AngleStep { get; }
        public IReadOnlyList<string> Order { get; }

        public SpawnDefinition(float interval, int maxAlive, float distance, float angleStep, IReadOnlyList<string> order)
        {
            Interval = DefinitionGuard.Positive(interval, nameof(interval));
            if (maxAlive <= 0) throw new ArgumentOutOfRangeException(nameof(maxAlive), "양의 정수가 필요하다.");
            MaxAlive = maxAlive;
            Distance = DefinitionGuard.Positive(distance, nameof(distance));
            AngleStep = DefinitionGuard.Finite(angleStep, nameof(angleStep));
            if (order == null || order.Count == 0)
                throw new ArgumentException("출현 순서에 Enemy ID가 하나 이상 필요하다.", nameof(order));
            var copy = new string[order.Count];
            for (int i = 0; i < copy.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(order[i]))
                    throw new ArgumentException($"출현 순서 {i}번이 비어 있다.", nameof(order));
                copy[i] = order[i];
            }
            Order = Array.AsReadOnly(copy);
        }
    }
}
