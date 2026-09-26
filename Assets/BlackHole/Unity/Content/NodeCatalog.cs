using System.Collections.Generic;
using BlackHole.Core;
using UnityEngine;

namespace BlackHole.Unity
{
    // 게임에 등록된 노드 전체의 목록: 노드마다 ID, 가격, 시작 노드인가, 격자 칸, 이어진 노드.
    // Core 저작 형식(NodeTreeData)을 그대로 담는다. 검증(ID 중복, 선, 시작 노드에서 닿는가)은 NodeTreeLoader가 경로와 함께 보고한다.
    // 노드 도구(메뉴 BlackHole > Node Tree)로 고친다. 이름·아이콘 같은 표시 칸은 트리 화면(F02)과 함께 붙는다.
    // 지금 에셋의 노드와 가격은 [임시] 샘플이다.
    [CreateAssetMenu(fileName = "NodeCatalog", menuName = "BlackHole/Node Catalog")]
    public sealed class NodeCatalog : ScriptableObject
    {
        [SerializeField] private NodeTreeData tree = new NodeTreeData();

        // 노드 도구가 고치는 원본. 게임 코드는 ToData()로 읽는다.
        internal NodeTreeData Tree => tree;

        public NodeTreeData ToData() => new NodeTreeData { Nodes = new List<NodeData>(tree.Nodes) };
    }
}
