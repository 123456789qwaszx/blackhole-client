using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 콘텐츠 전체의 규칙: ID는 유일하고, ID로 남는 참조(시작 Skill)는 실재한다.
    // GameContent 생성자(첫 오류로 생성 실패)와 ContentLoader(경로별 진단 수집)가 함께 쓴다.
    // 개별 정의의 수치 규칙은 각 정의 생성자에 있다 — 여기서 다시 보지 않는다.
    // 정의 객체로 해석되는 참조(공급·성장 노드의 Enemy)는 ContentLoader가 이 색인으로 해석하며 진단한다.
    internal static class ContentInvariants
    {
        public static void Collect(
            IReadOnlyList<EnemyDefinition> enemies,
            IReadOnlyList<PassiveSkillDefinition> skills,
            IReadOnlyList<string> startingSkills,
            ICollection<ContentDiagnostic> into,
            out Dictionary<string, EnemyDefinition> enemiesById,
            out Dictionary<string, PassiveSkillDefinition> skillsById)
        {
            enemiesById = Index(enemies, "Enemies", "Enemy", e => e.Id, into);
            skillsById = Index(skills, "Skills", "Skill", s => s.Id, into);
            VerifyStartingSkills(startingSkills, skillsById, into);
        }

        private static Dictionary<string, T> Index<T>(
            IReadOnlyList<T> items,
            string section,
            string label,
            Func<T, string> idOf,
            ICollection<ContentDiagnostic> into) where T : class
        {
            var byId = new Dictionary<string, T>(StringComparer.Ordinal);

            for (int i = 0; i < items.Count; i++)
            {
                T item = items[i];

                if (item == null)
                    into.Add(new ContentDiagnostic($"{section}[{i}]", $"{label} 정의가 null이다."));
                else if (byId.ContainsKey(idOf(item)))
                    into.Add(new ContentDiagnostic($"{section}[{i}]", $"{label} ID '{idOf(item)}'가 중복됐다."));
                else
                    byId.Add(idOf(item), item);
            }

            return byId;
        }

        // 업그레이드 노드 사이의 규칙: ID 유일, 선행 노드의 실재, 선행을 따라가면 시작 노드에 닿는다(순환 없음).
        public static void CollectUpgrades(
            IReadOnlyList<UpgradeNodeDefinition> upgrades,
            ICollection<ContentDiagnostic> into,
            out Dictionary<string, UpgradeNodeDefinition> upgradesById)
        {
            upgradesById = Index(upgrades, "Upgrades", "업그레이드", u => u.Id, into);

            for (int i = 0; i < upgrades.Count; i++)
            {
                UpgradeNodeDefinition node = upgrades[i];

                if (node?.Requires == null)
                    continue;

                string at = $"Upgrades[{i}].Requires";

                if (!upgradesById.ContainsKey(node.Requires))
                    into.Add(new ContentDiagnostic(at, $"정의되지 않은 선행 노드 ID '{node.Requires}'."));
                else if (!ReachesRoot(node, upgradesById))
                    into.Add(new ContentDiagnostic(at, $"선행 노드를 따라가면 시작 노드에 닿지 않는다(순환): '{node.Id}'."));
            }
        }

        // Skill 해금의 규칙(CONTENT_DEFINITION 3.3). 노드 사이의 규칙(선행 실재, 순환 없음)이 통과한 뒤에 본다.
        // - 시작 구성에 있는 Skill은 해금하지 않는다.
        // - 한 Skill의 해금 노드는 하나다([임시]: 여러 경로로 여는 해금은 아직 없다).
        // - 시작 구성에 없는 Skill을 바꾸는 효과는, 그 노드에서 선행을 따라가면(자기 자신 포함) 그 Skill의 해금 노드에 닿아야 한다.
        //   닿지 않으면 사도 효과가 없는 노드가 된다.
        public static void CollectSkillUnlocks(
            IReadOnlyList<UpgradeNodeDefinition> upgrades,
            IReadOnlyList<string> startingSkills,
            ICollection<ContentDiagnostic> into)
        {
            var starting = new HashSet<string>(startingSkills, StringComparer.Ordinal);
            var unlockNodeBySkill = new Dictionary<string, string>(StringComparer.Ordinal);
            var byId = new Dictionary<string, UpgradeNodeDefinition>(StringComparer.Ordinal);

            foreach (UpgradeNodeDefinition node in upgrades)
                byId[node.Id] = node;

            foreach (UpgradeNodeDefinition node in upgrades)
            {
                for (int j = 0; j < node.Effects.Count; j++)
                {
                    UpgradeEffect effect = node.Effects[j];

                    if (effect.Kind != UpgradeEffectKind.SkillUnlock)
                        continue;

                    string at = $"Upgrades[{node.Id}].Effects[{j}]";
                    string skill = effect.Skill.Id;

                    if (starting.Contains(skill))
                        into.Add(new ContentDiagnostic(at, $"시작 구성에 있는 Skill '{skill}'는 해금하지 않는다."));
                    else if (unlockNodeBySkill.TryGetValue(skill, out string other))
                        into.Add(new ContentDiagnostic(at, $"Skill '{skill}'의 해금 노드가 '{other}'와 둘이다."));
                    else
                        unlockNodeBySkill.Add(skill, node.Id);
                }
            }

            foreach (UpgradeNodeDefinition node in upgrades)
            {
                for (int j = 0; j < node.Effects.Count; j++)
                {
                    UpgradeEffect effect = node.Effects[j];

                    if (!UpgradeEffect.TargetsSkill(effect.Kind)
                        || effect.Kind == UpgradeEffectKind.SkillUnlock
                        || starting.Contains(effect.Skill.Id))
                        continue;

                    string skill = effect.Skill.Id;

                    if (!unlockNodeBySkill.TryGetValue(skill, out string unlock) || !ChainContains(node, unlock, byId))
                        into.Add(new ContentDiagnostic(
                            $"Upgrades[{node.Id}].Effects[{j}]",
                            $"시작 구성에 없는 Skill '{skill}'를 바꾼다. 선행을 따라가면 이 Skill의 해금 노드에 닿아야 한다."));
                }
            }
        }

        // node에서 선행을 따라가며(자기 자신 포함) targetId 노드를 만나는가. 순환이 없음은 먼저 확인됐다.
        private static bool ChainContains(
            UpgradeNodeDefinition node,
            string targetId,
            Dictionary<string, UpgradeNodeDefinition> byId)
        {
            UpgradeNodeDefinition current = node;

            for (int steps = 0; steps <= byId.Count; steps++)
            {
                if (current.Id == targetId)
                    return true;

                if (current.Requires == null || !byId.TryGetValue(current.Requires, out current))
                    return false;
            }

            return false;
        }

        private static bool ReachesRoot(UpgradeNodeDefinition node, Dictionary<string, UpgradeNodeDefinition> byId)
        {
            UpgradeNodeDefinition current = node;

            for (int steps = 0; steps <= byId.Count; steps++)
            {
                if (current.Requires == null)
                    return true;

                if (!byId.TryGetValue(current.Requires, out current))
                    return false;
            }

            return false;
        }

        // 시작 Skill은 실재해야 하고, 같은 Skill을 두 번 가질 수 없다.
        private static void VerifyStartingSkills(
            IReadOnlyList<string> startingSkills,
            Dictionary<string, PassiveSkillDefinition> skillsById,
            ICollection<ContentDiagnostic> into)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < startingSkills.Count; i++)
            {
                string id = startingSkills[i];
                string at = $"StartingSkills[{i}]";

                if (id == null || !skillsById.ContainsKey(id))
                    into.Add(new ContentDiagnostic(at, $"정의되지 않은 Skill ID '{id}'."));
                else if (!seen.Add(id))
                    into.Add(new ContentDiagnostic(at, $"시작 Skill '{id}'가 중복됐다."));
            }
        }
    }
}
