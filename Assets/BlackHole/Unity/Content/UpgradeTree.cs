using System;
using System.Collections.Generic;
using BlackHole.Core;
using UnityEngine;

namespace BlackHole.Unity
{
    // 업그레이드 노드 목록의 저작 에셋: 노드마다 ID, 가격, 선행 노드, Grant(적 종류 + 수치 + 연산 + 값).
    // 규칙 칸만 있다 — 트리 화면의 좌표·구역·아이콘 같은 표시 칸은 트리 화면(F02)과 함께 붙는다(SKILL_TREE_PLAN 4.5).
    // Grant는 종류 에셋을 직접 가리킨다 — ID 문자열을 치지 않는다. 가리키는 종류는 적 종류 목록(EnemyCatalog)에 있어야 한다.
    // 검증(선행 노드 실재·순환, 질량 단계 범위, 전체 개체 수 상한)은 ContentLoader가 경로와 함께 보고한다.
    // 노드 저작 툴(F03)이 생기면 그 툴이 이 에셋을 읽고 쓴다.
    [CreateAssetMenu(fileName = "UpgradeTree", menuName = "BlackHole/Upgrade Tree")]
    public sealed class UpgradeTree : ScriptableObject
    {
        [Serializable]
        public struct Grant
        {
            public EnemyKind enemy;
            [Tooltip("MassLevel: 질량 증가(+1씩). GoldenRatio: 황금 비율. GoldenMultiplier: 황금 배율. StartSupply: 전투 시작 공급 수.")]
            public EnemyUpgradeStat stat;
            [Tooltip("여러 노드가 같은 수치를 보정하면 정하기(가장 큰 값) → 더하기 → 곱하기 순서로 합친다. 질량 단계·공급 수는 더하기만.")]
            public GrantOperation operation;
            public float value;
        }

        [Serializable]
        public struct Node
        {
            [Tooltip("저장 키. 정한 뒤에는 바꾸지 않는다(표시 이름이 생겨도 ID는 그대로).")]
            public string id;
            public long price;
            [Tooltip("선행 노드 ID. 비우면 처음부터 살 수 있다.")]
            public string requires;
            public List<Grant> grants;
        }

        [SerializeField] private List<Node> nodes = new List<Node>();

        // Core 저작 형식에 노드를 채운다. 종류 칸이 비어 있는 Grant는 ID 없는 Grant가 되어 로더가 경로와 함께 보고한다.
        public void WriteTo(ContentData data)
        {
            foreach (Node node in nodes)
            {
                var upgrade = new UpgradeData { Id = node.id, Price = node.price, Requires = node.requires };

                foreach (Grant grant in node.grants ?? new List<Grant>())
                {
                    upgrade.Grants.Add(new EnemyGrantData
                    {
                        Enemy = grant.enemy != null ? grant.enemy.Id : null,
                        Stat = grant.stat.ToString(),
                        Operation = grant.operation.ToString(),
                        Value = grant.value,
                    });
                }

                data.Upgrades.Add(upgrade);
            }
        }
    }
}
