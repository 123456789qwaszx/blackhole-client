using System;
using System.Collections.Generic;
using BlackHole.Core;
using UnityEngine;

namespace BlackHole.Unity
{
    // 적 공급·배치 설정 에셋: HQ를 둘러싼 출현 띠와 전투 시작 공급(SYSTEM_CATALOG S08).
    // 공급은 종류 에셋을 직접 가리킨다 — ID 문자열을 치지 않는다. 판에 쓰이는 적 종류는 공급이 가리키는 종류다.
    [CreateAssetMenu(fileName = "EnemySupplySetup", menuName = "BlackHole/Enemy Supply Setup")]
    public sealed class EnemySupplySetup : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public EnemyKind kind;
            public int count;
        }

        [Header("출현 배치: HQ(0,0)로부터의 거리 띠")]
        [SerializeField] private float minDistance = 2;
        [SerializeField] private float maxDistance = 4.2f;

        [Header("전투 시작 공급 (요청 순서대로 나온다)")]
        [SerializeField] private List<Entry> startSupply = new List<Entry>();

        // 공급이 가리키는 적 종류. 중복 없이, 처음 나온 순서다.
        public IReadOnlyList<EnemyKind> Kinds()
        {
            var kinds = new List<EnemyKind>();

            foreach (Entry entry in startSupply)
            {
                if (entry.kind != null && !kinds.Contains(entry.kind))
                    kinds.Add(entry.kind);
            }

            return kinds;
        }

        // Core 저작 형식에 적 종류·출현 배치·전투 시작 공급을 채운다. 검증은 ContentLoader가 한다.
        // 종류 칸이 비어 있는 공급은 ID 없는 공급이 되어 로더가 경로와 함께 보고한다.
        public void WriteTo(ContentData data)
        {
            foreach (EnemyKind kind in Kinds())
                data.Enemies.Add(kind.ToData());

            data.EnemyPlacement = new EnemyPlacementData { MinDistance = minDistance, MaxDistance = maxDistance };

            foreach (Entry entry in startSupply)
            {
                data.StartSupply.Add(new SupplyData
                {
                    Enemy = entry.kind != null ? entry.kind.Id : null,
                    Count = entry.count,
                });
            }
        }
    }
}
