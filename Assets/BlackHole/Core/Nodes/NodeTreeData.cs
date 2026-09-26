using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 노드 트리의 저작 형식. 검증 전 값이며 실행에 쓰지 않는다 — NodeTreeLoader만 읽는다.
    // Unity 쪽 노드 목록 에셋(NodeCatalog)이 이 형식을 그대로 담는다.
    // 트리 화면의 좌표·이름·아이콘 같은 표시 칸은 여기에 없다. 트리 화면(F02)과 함께 붙는다.
    [Serializable]
    public sealed class NodeTreeData
    {
        public List<NodeData> Nodes = new List<NodeData>();
    }

    [Serializable]
    public sealed class NodeData
    {
        // 저장 키. 정한 뒤에는 바꾸지 않는다.
        public string Id;
        public long Price;
        // 시작 노드: 산 이웃이 없어도 드러난다.
        public bool Start;
        // 선으로 이어진 노드의 ID. 선은 방향이 없어 한쪽 노드에만 적어도 된다.
        public List<string> Links = new List<string>();
        // 이 노드를 사면 받는 업그레이드.
        public List<UpgradeData> Upgrades = new List<UpgradeData>();
    }

    [Serializable]
    public sealed class UpgradeData
    {
        // 수치 이름. 그 수치를 가져가는 시스템이 정한다.
        public string Stat;
        public UpgradeOperation Operation;
        public float Value;
    }
}
