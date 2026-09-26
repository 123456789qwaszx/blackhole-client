using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 업그레이드 노드 하나. 노드 저작 툴이 만들 데이터 중 구매 규칙과 효과에 필요한 부분이다.
    // 위치·연결선·구역 같은 배치 정보는 툴과 화면의 일이라 여기에 없다.
    // 노드는 한 번만 산다. 같은 강화의 다음 단계는 별도 노드다.
    //
    // 효과는 Grant 목록이다. 지금 있는 Grant는 적 종류의 판 구성 보정뿐이다(BATTLE_COMPOSITION_PLAN BC-005).
    // Skill 보정 같은 다른 Grant는 그 시스템이 이 브랜치에 붙을 때 더한다(SKILL_TREE_PLAN 4.3).
    public sealed class UpgradeNodeDefinition
    {
        public string Id { get; }
        // 원작 가격은 T(조) 단위까지 오르므로 Gold와 같은 long이다.
        public long Price { get; }
        // 선행 노드 ID. null이면 처음부터 살 수 있다. 실재와 순환 여부는 ContentInvariants가 본다.
        public string Requires { get; }
        public IReadOnlyList<EnemyGrant> Grants { get; }

        public UpgradeNodeDefinition(string id, long price, string requires, IReadOnlyList<EnemyGrant> grants)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID가 비어 있다.", nameof(id));

            if (price <= 0)
                throw new ArgumentOutOfRangeException(nameof(price), "양의 정수가 필요하다.");

            if (id == requires)
                throw new ArgumentException("자기 자신을 선행 노드로 가질 수 없다.", nameof(requires));

            var copy = new EnemyGrant[grants?.Count ?? 0];

            for (int i = 0; i < copy.Length; i++)
                copy[i] = grants[i];

            Id = id;
            Price = price;
            Requires = string.IsNullOrWhiteSpace(requires) ? null : requires;
            Grants = Array.AsReadOnly(copy);
        }
    }
}
