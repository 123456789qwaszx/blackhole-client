using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 업그레이드 노드 하나. 노드 저작 툴이 만들 데이터 중 게임 규칙에 필요한 부분이다.
    // 위치·연결선·구역 같은 배치 정보는 툴과 화면의 일이라 여기에 없다.
    // 노드는 한 번만 산다. 같은 강화의 다음 단계는 별도 노드다.
    public sealed class UpgradeNodeDefinition
    {
        public string Id { get; }
        public int Price { get; }
        // 선행 노드 ID. null이면 처음부터 살 수 있다. 실재와 순환 여부는 ContentInvariants가 본다.
        public string Requires { get; }
        public IReadOnlyList<UpgradeEffect> Effects { get; }

        public UpgradeNodeDefinition(
            string id,
            int price,
            string requires,
            IReadOnlyList<UpgradeEffect> effects)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID가 비어 있다.", nameof(id));

            if (price <= 0)
                throw new ArgumentOutOfRangeException(nameof(price), "양의 정수가 필요하다.");

            if (id == requires)
                throw new ArgumentException("자기 자신을 선행 노드로 가질 수 없다.", nameof(requires));

            if (effects == null || effects.Count == 0)
                throw new ArgumentException("효과가 하나 이상 필요하다.", nameof(effects));

            var copy = new UpgradeEffect[effects.Count];

            for (int i = 0; i < copy.Length; i++)
            {
                copy[i] = effects[i]
                    ?? throw new ArgumentException($"Effects[{i}]가 비어 있다.", nameof(effects));
            }

            Id = id;
            Price = price;
            Requires = string.IsNullOrWhiteSpace(requires) ? null : requires;
            Effects = Array.AsReadOnly(copy);
        }
    }
}
