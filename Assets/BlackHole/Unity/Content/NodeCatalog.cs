using System.Collections.Generic;
using BlackHole.Core;
using UnityEngine;

namespace BlackHole.Unity
{
    // 게임에 등록된 노드 전체의 목록(노드 트리의 규칙 칸): 노드마다 ID, 가격, 시작 노드인가, 이어진 노드, 사면 받는 업그레이드.
    // Core 저작 형식(NodeData)을 그대로 담는다. 검증(ID 중복, 선, 시작 노드에서 닿는가)은 NodeTreeLoader가 경로와 함께 보고한다.
    // 좌표·이름·아이콘 같은 표시 칸은 트리 화면(F02)과 함께 붙는다. 노드 저작 도구(F03)가 생기면 그 도구가 이 에셋을 읽고 쓴다.
    // 지금 에셋의 노드는 [임시] 샘플이다. 수치 이름(breaker.speed 등)을 읽어 가는 시스템은 아직 없다.
    [CreateAssetMenu(fileName = "NodeCatalog", menuName = "BlackHole/Node Catalog")]
    public sealed class NodeCatalog : ScriptableObject
    {
        [SerializeField] private List<NodeData> nodes = new List<NodeData>();

        public NodeTreeData ToData() => new NodeTreeData { Nodes = new List<NodeData>(nodes) };
    }
}
