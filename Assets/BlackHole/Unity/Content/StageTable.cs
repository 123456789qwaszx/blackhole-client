using System;
using System.Collections.Generic;
using BlackHole.Core;
using UnityEngine;

namespace BlackHole.Unity
{
    // 진행도(적의 강도 단계) 표의 저작 에셋. i번째 줄이 (i + 1)단계이고, 줄 수가 단계의 수다. HQ 성장 단계와 다르다.
    // 줄마다 그 단계에서 쓰는 적 풀을 가리킨다. 체력·크기 계수는 단계 표에 더해질 때 줄에 붙는다.
    [CreateAssetMenu(fileName = "StageTable", menuName = "BlackHole/Stage Table")]
    public sealed class StageTable : ScriptableObject
    {
        [Serializable]
        public struct Stage
        {
            public EnemyPool pool;
        }

        [SerializeField] private List<Stage> stages = new List<Stage>();

        // Core 저작 형식에 적 풀(단계가 가리키는 풀, 중복 없이)과 단계 표를 채운다. 검증은 ContentLoader가 한다.
        // 풀 칸이 비어 있는 단계는 ID 없는 참조가 되어 로더가 경로와 함께 보고한다.
        public void WriteTo(ContentData data)
        {
            var pools = new List<EnemyPool>();

            foreach (Stage stage in stages)
            {
                if (stage.pool != null && !pools.Contains(stage.pool))
                    pools.Add(stage.pool);

                data.Stages.Add(new StageData { Pool = stage.pool != null ? stage.pool.Id : null });
            }

            foreach (EnemyPool pool in pools)
                data.EnemyPools.Add(pool.ToData());
        }
    }
}
