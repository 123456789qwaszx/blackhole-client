using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 콘텐츠 전체의 규칙: ID는 유일하고, 참조는 실재하며, 모든 스킬은 실행 규칙으로 해석된다.
    // ContentCatalog 생성자(첫 오류로 생성 실패)와 ContentLoader(경로별 진단 수집)가 함께 쓴다.
    // 개별 정의의 수치 규칙은 각 정의 생성자에 있다 — 여기서 다시 보지 않는다.
    internal static class ContentInvariants
    {
        public static void Collect(
            IReadOnlyList<TargetDefinition> targets,
            IReadOnlyList<SkillDefinition> skills,
            SpawnDefinition spawn,
            ICollection<ContentDiagnostic> into,
            out Dictionary<string, TargetDefinition> targetsById)
        {
            targetsById = IndexTargets(targets, into);
            VerifySkills(skills, into);
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

                string error = SkillEffectFactory.Verify(skill);
                if (error != null)
                    into.Add(new ContentDiagnostic($"Skills[{skill.Id}]", error));
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
