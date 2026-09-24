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
            ICollection<ContentDiagnostic> into,
            out Dictionary<string, EnemyDefinition> enemiesById)
        {
            enemiesById = IndexEnemies(enemies, into);
            VerifySpawn(spawn, enemiesById, into);
        }

        private static Dictionary<string, EnemyDefinition> IndexEnemies(
            IReadOnlyList<EnemyDefinition> enemies, ICollection<ContentDiagnostic> into)
        {
            var byId = new Dictionary<string, EnemyDefinition>(StringComparer.Ordinal);
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyDefinition enemy = enemies[i];
                if (enemy == null)
                    into.Add(new ContentDiagnostic($"Enemies[{i}]", "Enemy 정의가 null이다."));
                else if (byId.ContainsKey(enemy.Id))
                    into.Add(new ContentDiagnostic($"Enemies[{i}]", $"Enemy ID '{enemy.Id}'가 중복됐다."));
                else
                    byId.Add(enemy.Id, enemy);
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
    }
}
