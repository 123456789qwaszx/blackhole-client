using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 콘텐츠 전체의 규칙: ID는 유일하고, 참조는 실재한다.
    // GameContent 생성자(첫 오류로 생성 실패)와 ContentLoader(경로별 진단 수집)가 함께 쓴다.
    // 개별 정의의 수치 규칙은 각 정의 생성자에 있다 — 여기서 다시 보지 않는다.
    internal static class ContentInvariants
    {
        public static void Collect(
            IReadOnlyList<EnemyDefinition> enemies,
            SpawnDefinition spawn,
            IReadOnlyList<PassiveSkillDefinition> skills,
            IReadOnlyList<string> startingSkills,
            ICollection<ContentDiagnostic> into,
            out Dictionary<string, EnemyDefinition> enemiesById,
            out Dictionary<string, PassiveSkillDefinition> skillsById)
        {
            enemiesById = Index(enemies, "Enemies", "Enemy", e => e.Id, into);
            skillsById = Index(skills, "Skills", "Skill", s => s.Id, into);
            VerifySpawn(spawn, enemiesById, into);
            VerifyStartingSkills(startingSkills, skillsById, into);
        }

        private static Dictionary<string, T> Index<T>(IReadOnlyList<T> items, string section, string label,
            Func<T, string> idOf, ICollection<ContentDiagnostic> into) where T : class
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

        private static void VerifySpawn(
            SpawnDefinition spawn, Dictionary<string, EnemyDefinition> enemiesById, ICollection<ContentDiagnostic> into)
        {
            if (spawn == null)
            {
                into.Add(new ContentDiagnostic("Spawn", "출현 정의가 없다."));
                return;
            }
            for (int i = 0; i < spawn.Order.Count; i++)
                if (!enemiesById.ContainsKey(spawn.Order[i]))
                    into.Add(new ContentDiagnostic($"Spawn.Order[{i}]", $"정의되지 않은 Enemy ID '{spawn.Order[i]}'."));
        }

        // 시작 Skill은 실재해야 하고, 같은 Skill을 두 번 가질 수 없다.
        private static void VerifyStartingSkills(IReadOnlyList<string> startingSkills,
            Dictionary<string, PassiveSkillDefinition> skillsById, ICollection<ContentDiagnostic> into)
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
