using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 콘텐츠 전체의 규칙: ID는 유일하고, 참조는 실재하며, 모든 스킬과 대상 이동은 실행 규칙으로 해석된다.
    // ContentCatalog 생성자(첫 오류로 생성 실패)와 ContentLoader(경로별 진단 수집)가 함께 쓴다.
    // 개별 정의의 수치 규칙은 각 정의 생성자에 있다 — 여기서 다시 보지 않는다.
    internal static class ContentInvariants
    {
        public static void Collect(
            IReadOnlyList<TargetDefinition> targets,
            IReadOnlyList<SkillDefinition> skills,
            IReadOnlyList<UpgradeDefinition> upgrades,
            SkillTreeDefinition skillTree,
            SpawnDefinition spawn,
            ICollection<ContentDiagnostic> into,
            out Dictionary<string, TargetDefinition> targetsById)
        {
            targetsById = IndexTargets(targets, into);
            VerifySkills(skills, into);
            VerifyUpgrades(upgrades, into);
            VerifySkillTree(skillTree, upgrades, into);
            VerifySpawn(spawn, targetsById, into);
        }

        private static Dictionary<string, TargetDefinition> IndexTargets(
            IReadOnlyList<TargetDefinition> targets, ICollection<ContentDiagnostic> into)
        {
            var byId = new Dictionary<string, TargetDefinition>(StringComparer.Ordinal);
            if (targets.Count == 0)
                into.Add(new ContentDiagnostic("Targets", "대상 정의가 하나 이상 필요하다."));

            for (int i = 0; i < targets.Count; i++)
            {
                TargetDefinition target = targets[i];
                if (target == null)
                {
                    into.Add(new ContentDiagnostic($"Targets[{i}]", "대상 정의가 null이다."));
                    continue;
                }
                if (!MovementRuleFactory.TryCreate(target.Movement, out _))
                    into.Add(new ContentDiagnostic($"Targets[{target.Id}].Movement",
                        $"실행 규칙이 연결되지 않은 이동 종류 '{target.Movement.GetType().Name}'."));
                if (byId.ContainsKey(target.Id))
                {
                    into.Add(new ContentDiagnostic($"Targets[{i}]", $"대상 ID '{target.Id}'가 중복됐다."));
                    continue;
                }
                byId.Add(target.Id, target);
            }
            return byId;
        }

        private static void VerifySkills(
            IReadOnlyList<SkillDefinition> skills, ICollection<ContentDiagnostic> into)
        {
            if (skills.Count == 0)
                into.Add(new ContentDiagnostic("Skills", "스킬 정의가 하나 이상 필요하다."));

            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < skills.Count; i++)
            {
                SkillDefinition skill = skills[i];
                if (skill == null)
                {
                    into.Add(new ContentDiagnostic($"Skills[{i}]", "스킬 정의가 null이다."));
                    continue;
                }
                if (!ids.Add(skill.Id))
                    into.Add(new ContentDiagnostic($"Skills[{i}]", $"스킬 ID '{skill.Id}'가 중복됐다."));

                if (!SkillRuleFactory.TryCreateSelector(skill.Selection, out _))
                    into.Add(new ContentDiagnostic($"Skills[{skill.Id}].Selection",
                        SkillRuleFactory.Describe(skill.Selection)));
                for (int e = 0; e < skill.Effects.Count; e++)
                    if (!SkillRuleFactory.TryCreateEffect(skill.Effects[e], out _))
                        into.Add(new ContentDiagnostic($"Skills[{skill.Id}].Effects[{e}]",
                            SkillRuleFactory.Describe(skill.Effects[e])));
            }
        }

        // 강화는 없어도 된다. 있으면 ID가 유일해야 한다.
        private static void VerifyUpgrades(
            IReadOnlyList<UpgradeDefinition> upgrades, ICollection<ContentDiagnostic> into)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < upgrades.Count; i++)
            {
                UpgradeDefinition upgrade = upgrades[i];
                if (upgrade == null)
                    into.Add(new ContentDiagnostic($"Upgrades[{i}]", "강화 정의가 null이다."));
                else if (!ids.Add(upgrade.Id))
                    into.Add(new ContentDiagnostic($"Upgrades[{i}]", $"강화 ID '{upgrade.Id}'가 중복됐다."));
            }
        }

        // 트리 그래프 자체의 규칙은 SkillTreeDefinition이 보장한다. 여기서는 강화 참조만 본다.
        private static void VerifySkillTree(SkillTreeDefinition tree,
            IReadOnlyList<UpgradeDefinition> upgrades, ICollection<ContentDiagnostic> into)
        {
            if (tree == null) return;
            var upgradeIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (UpgradeDefinition upgrade in upgrades)
                if (upgrade != null) upgradeIds.Add(upgrade.Id);

            var referenced = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (SkillTreeNodeDefinition node in tree.Nodes)
            {
                string at = $"SkillTree.Nodes[{node.Id}].UpgradeId";
                if (!upgradeIds.Contains(node.UpgradeId))
                    into.Add(new ContentDiagnostic(at, $"정의되지 않은 강화 '{node.UpgradeId}'."));
                else if (referenced.TryGetValue(node.UpgradeId, out string other))
                    into.Add(new ContentDiagnostic(at,
                        $"강화 '{node.UpgradeId}'를 노드 '{other}'도 참조한다. 한 강화는 한 노드만 참조한다."));
                else
                    referenced.Add(node.UpgradeId, node.Id);
            }
        }

        private static void VerifySpawn(
            SpawnDefinition spawn,
            Dictionary<string, TargetDefinition> targetsById,
            ICollection<ContentDiagnostic> into)
        {
            if (spawn == null)
            {
                into.Add(new ContentDiagnostic("Spawn", "출현 정의가 없다."));
                return;
            }

            for (int i = 0; i < spawn.TargetOrder.Count; i++)
            {
                if (!targetsById.ContainsKey(spawn.TargetOrder[i]))
                    into.Add(new ContentDiagnostic(
                        $"Spawn.TargetOrder[{i}]", $"정의되지 않은 대상 ID '{spawn.TargetOrder[i]}'."));
            }
        }
    }
}
