using System;

namespace BlackHole.Core
{
    // 노드가 파는 것: ID와 가격. 노드는 한 번만 산다.
    // 선과 시작 노드는 그래프(NodeGraph)가 가진다. 사면 받는 효과(업그레이드)는 feature/업그레이드연결에서 붙는다.
    public sealed class NodeDefinition
    {
        public string Id { get; }
        // 원작 가격은 T(조) 단위까지 오르므로 Gold와 같은 long이다.
        public long Price { get; }

        internal NodeDefinition(string id, long price)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID가 비어 있다.", nameof(id));

            if (price <= 0)
                throw new ArgumentOutOfRangeException(nameof(price), "양의 정수가 필요하다.");

            Id = id;
            Price = price;
        }
    }
}
