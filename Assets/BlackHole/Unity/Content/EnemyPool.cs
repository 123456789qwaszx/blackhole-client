using System.Collections.Generic;
using BlackHole.Core;
using UnityEngine;

namespace BlackHole.Unity
{
    // 적 풀 하나의 저작 에셋: 어떤 단계에서 나올 수 있는 적 종류의 묶음. 단계 표(StageTable)가 가리킨다.
    // 여러 단계가 같은 풀을 쓸 수 있다. 풀의 종류는 적 종류 목록(EnemyCatalog)에 있어야 한다.
    // 풀에 든 종류가 모두 그대로 나오지는 않는다 — 적 비율·출현 제한 같은 거르는 규칙이 붙을 자리다(아직 없다).
    [CreateAssetMenu(fileName = "EnemyPool", menuName = "BlackHole/Enemy Pool")]
    public sealed class EnemyPool : ScriptableObject
    {
        [Tooltip("단계 표와 도구가 이 풀을 가리키는 식별자. 정한 뒤에는 바꾸지 않는다.")]
        [SerializeField] private string id;

        [SerializeField] private List<EnemyKind> kinds = new List<EnemyKind>();

        public string Id => id;

        // Core 저작 형식으로 옮긴다. 비어 있는 칸은 ID 없는 항목이 되어 ContentLoader가 경로와 함께 보고한다.
        internal EnemyPoolData ToData()
        {
            var data = new EnemyPoolData { Id = id };

            foreach (EnemyKind kind in kinds)
                data.Enemies.Add(kind != null ? kind.Id : null);

            return data;
        }
    }
}
