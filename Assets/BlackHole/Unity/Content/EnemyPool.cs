using System;
using System.Collections.Generic;
using BlackHole.Core;
using UnityEngine;

namespace BlackHole.Unity
{
    // 적 풀 하나의 저작 에셋: 어떤 단계에서 나올 수 있는 적 종류와, 종류마다 동시에 살아 있을 수 있는 최대 수.
    // 단계 표(StageTable)가 가리키고, 여러 단계가 같은 풀을 쓸 수 있다. 풀의 종류는 적 종류 목록(EnemyCatalog)에 있어야 한다.
    // 공급된 적은 이 풀을 거쳐서만 나온다: 풀에 없는 종류와 최대 수에 닿은 종류는 나오지 않는다.
    // 판 전체의 적 수는 따로 전체 개체 수 상한(적 공급 설정)을 넘지 않는다. 종류 안의 색 비율은 종류 에셋의 질량 단계가 정한다.
    [CreateAssetMenu(fileName = "EnemyPool", menuName = "BlackHole/Enemy Pool")]
    public sealed class EnemyPool : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public EnemyKind kind;
            [Tooltip("이 종류가 판에 동시에 살아 있을 수 있는 최대 수.")]
            public int maxAlive;
        }

        [Tooltip("단계 표와 도구가 이 풀을 가리키는 식별자. 정한 뒤에는 바꾸지 않는다.")]
        [SerializeField] private string id;

        [SerializeField] private List<Entry> entries = new List<Entry>();

        public string Id => id;

        // Core 저작 형식으로 옮긴다. 비어 있는 종류 칸은 ID 없는 항목이 되어 ContentLoader가 경로와 함께 보고한다.
        internal EnemyPoolData ToData()
        {
            var data = new EnemyPoolData { Id = id };

            foreach (Entry entry in entries)
            {
                data.Entries.Add(new EnemyPoolEntryData
                {
                    Enemy = entry.kind != null ? entry.kind.Id : null,
                    MaxAlive = entry.maxAlive,
                });
            }

            return data;
        }
    }
}
