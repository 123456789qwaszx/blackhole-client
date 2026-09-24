using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    public enum NodeStatus { Acquired, Available, Locked }

    // 스킬 트리의 판 안 규칙. 자체 가변 상태가 없다 — 획득 여부는 UpgradeState에서 읽는다.
    // 노드 획득 = 참조한 강화가 1단계 이상. 트리에 따로 IsUnlocked 원본을 두지 않는다.
    // 구매 자격 판정(AllowsPurchase)은 UpgradePurchase가 모든 구매 요청에 적용한다.
    public sealed class SkillTree
    {
        private readonly UpgradeState _upgrades;
        private readonly Dictionary<string, SkillTreeNodeDefinition> _byId =
            new Dictionary<string, SkillTreeNodeDefinition>(StringComparer.Ordinal);
        private readonly Dictionary<string, SkillTreeNodeDefinition> _byUpgrade =
            new Dictionary<string, SkillTreeNodeDefinition>(StringComparer.Ordinal);

        public SkillTreeDefinition Definition { get; }

        internal SkillTree(SkillTreeDefinition definition, UpgradeState upgrades)
        {
            Definition = definition;
            _upgrades = upgrades;
            foreach (SkillTreeNodeDefinition node in definition.Nodes)
            {
                _byId.Add(node.Id, node);
                _byUpgrade.Add(node.UpgradeId, node);
            }
        }

        public NodeStatus Status(string nodeId)
        {
            if (!_byId.TryGetValue(nodeId ?? string.Empty, out SkillTreeNodeDefinition node))
                throw new ArgumentException($"정의되지 않은 노드 '{nodeId}'.", nameof(nodeId));
            if (IsAcquired(node)) return NodeStatus.Acquired;
            return RequirementsMet(node) ? NodeStatus.Available : NodeStatus.Locked;
        }

        // 이 강화를 참조하는 노드가 있는가.
        public bool References(string upgradeId) => upgradeId != null && _byUpgrade.ContainsKey(upgradeId);

        // 트리 선행 조건 때문에 이 강화를 살 수 없는가. 트리에 없는 강화는 잠기지 않는다.
        public bool IsUpgradeLocked(string upgradeId) =>
            upgradeId != null && _byUpgrade.TryGetValue(upgradeId, out SkillTreeNodeDefinition node) &&
            !RequirementsMet(node);

        internal bool TryGetNode(string nodeId, out SkillTreeNodeDefinition node)
        {
            node = null;
            return nodeId != null && _byId.TryGetValue(nodeId, out node);
        }

        private bool IsAcquired(SkillTreeNodeDefinition node) => _upgrades.Level(node.UpgradeId) >= 1;

        // AND: 선행 노드를 모두 획득해야 한다. 루트는 선행 노드가 없어 늘 충족한다.
        private bool RequirementsMet(SkillTreeNodeDefinition node)
        {
            for (int i = 0; i < node.Requires.Count; i++)
                if (!IsAcquired(_byId[node.Requires[i]])) return false;
            return true;
        }
    }
}
