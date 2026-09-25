using System;

namespace BlackHole.Core
{
    // 업그레이드 노드 하나. 노드 저작 툴이 만들 데이터 중 구매 규칙에 필요한 부분이다.
    // 위치·연결선·구역 같은 배치 정보는 툴과 화면의 일이라 여기에 없다.
    // 노드는 한 번만 산다. 같은 강화의 다음 단계는 별도 노드다.
    //
    // 노드가 전투를 바꾸는 효과는 효과의 대상(Skill·적·공급)과 함께 지웠다.
    // 효과는 SKILL_TREE_PLAN의 Loadout(전투 조립 때 산 노드 → 보정)으로 돌아온다.
    public sealed class UpgradeNodeDefinition
    {
        public string Id { get; }
        public int Price { get; }
        // 선행 노드 ID. null이면 처음부터 살 수 있다. 실재와 순환 여부는 ContentInvariants가 본다.
        public string Requires { get; }

        public UpgradeNodeDefinition(string id, int price, string requires)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID가 비어 있다.", nameof(id));

            if (price <= 0)
                throw new ArgumentOutOfRangeException(nameof(price), "양의 정수가 필요하다.");

            if (id == requires)
                throw new ArgumentException("자기 자신을 선행 노드로 가질 수 없다.", nameof(requires));

            Id = id;
            Price = price;
            Requires = string.IsNullOrWhiteSpace(requires) ? null : requires;
        }
    }
}
