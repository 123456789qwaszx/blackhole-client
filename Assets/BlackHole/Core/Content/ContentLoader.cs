using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // ContentData(저작 형식) → GameContent(검증된 정의).
    //
    // 오류가 하나라도 있으면 Content 없이 모든 진단을 돌려준다(부분 통과 금지).
    // 여기서 새로 두는 규칙은 데이터 모양에 관한 것뿐이다(빠진 칸, 알 수 없는 종류 이름).
    // 수치 규칙은 정의 생성자를, 콘텐츠 전체 규칙은 ContentInvariants를 그대로 호출해 경로를 붙인다.
    public static class ContentLoader
    {
        public static ContentLoadResult Load(ContentData data)
        {
            var diagnostics = new List<ContentDiagnostic>();
            if (data == null)
            {
                diagnostics.Add(new ContentDiagnostic(string.Empty, "콘텐츠 데이터가 null이다."));
                return Fail(diagnostics);
            }

            TimeLimitDefinition timeLimit = LoadSession(data.Session, diagnostics);
            HqDefinition hq = LoadHq(data.Hq, diagnostics);
            List<EnemyDefinition> enemies = LoadEnemies(data.Enemies, diagnostics);
            SpawnDefinition spawn = LoadSpawn(data.Spawn, diagnostics);
            List<PassiveSkillDefinition> skills = LoadSkills(data.Skills, diagnostics);
            IReadOnlyList<string> startingSkills = (IReadOnlyList<string>)data.StartingSkills ?? Array.Empty<string>();
            if (diagnostics.Count > 0) return Fail(diagnostics);

            ContentInvariants.Collect(enemies, spawn, skills, startingSkills, diagnostics, out _, out _);
            if (diagnostics.Count > 0) return Fail(diagnostics);

            var content = new GameContent(timeLimit, hq, enemies, spawn, skills, startingSkills);
            return new ContentLoadResult(content, diagnostics);
        }

        // ── 판 설정 ─────────────────────────────────────────────────────────

        private static TimeLimitDefinition LoadSession(SessionData item, List<ContentDiagnostic> into)
        {
            if (item == null) return Missing<TimeLimitDefinition>("Session", into);
            return Guard("Session.TimeLimit", into, () => new TimeLimitDefinition(item.TimeLimit));
        }

        private static HqDefinition LoadHq(HqData item, List<ContentDiagnostic> into)
        {
            if (item == null) return Missing<HqDefinition>("Hq", into);
            return Guard("Hq", into, () => new HqDefinition(new Point2(item.X, item.Y)));
        }

        // ── Enemy ───────────────────────────────────────────────────────────

        private static List<EnemyDefinition> LoadEnemies(List<EnemyData> items, List<ContentDiagnostic> into)
        {
            var enemies = new List<EnemyDefinition>();
            if (items == null) return enemies;

            for (int i = 0; i < items.Count; i++)
            {
                EnemyData item = items[i];
                string at = At("Enemies", i, item?.Id);
                if (item == null)
                {
                    into.Add(new ContentDiagnostic(at, "Enemy 데이터가 null이다."));
                    continue;
                }

                int errors = into.Count;
                EnemyBehaviorDefinition behavior = LoadBehavior(item.Behavior, at + ".Behavior", into);
                EnemyStats? stats = GuardValue(at, into, () => new EnemyStats(item.MaxHealth, item.MoveSpeed, item.Size));
                if (into.Count > errors) continue;

                EnemyDefinition enemy = Guard(at, into, () =>
                    new EnemyDefinition(item.Id, stats.Value, behavior, item.Gold, item.HqExp));
                if (enemy != null) enemies.Add(enemy);
            }
            return enemies;
        }

        // 종류 이름을 하위 정의로 바꾼다. 가능한 값을 진단에 그대로 싣는다.
        private static EnemyBehaviorDefinition LoadBehavior(EnemyBehaviorData item, string at, List<ContentDiagnostic> into)
        {
            if (item == null) return Missing<EnemyBehaviorDefinition>(at, into);
            switch (item.Kind)
            {
                case "OrbitHq":
                    return new OrbitHqBehaviorDefinition(item.Clockwise);
                default:
                    into.Add(new ContentDiagnostic(at + ".Kind", $"알 수 없는 행동 종류 '{item.Kind}'. 가능한 값: OrbitHq."));
                    return null;
            }
        }

        // ── 출현 ────────────────────────────────────────────────────────────

        private static SpawnDefinition LoadSpawn(SpawnData item, List<ContentDiagnostic> into)
        {
            if (item == null) return Missing<SpawnDefinition>("Spawn", into);
            return Guard("Spawn", into, () =>
                new SpawnDefinition(item.Interval, item.MaxAlive, item.Distance, item.AngleStep, item.Order));
        }

        // ── Passive Skill ───────────────────────────────────────────────────

        private static List<PassiveSkillDefinition> LoadSkills(List<SkillData> items, List<ContentDiagnostic> into)
        {
            var skills = new List<PassiveSkillDefinition>();
            if (items == null) return skills;

            for (int i = 0; i < items.Count; i++)
            {
                SkillData item = items[i];
                string at = At("Skills", i, item?.Id);
                if (item == null)
                {
                    into.Add(new ContentDiagnostic(at, "Skill 데이터가 null이다."));
                    continue;
                }

                int errors = into.Count;
                SkillOrigin? origin = ParseOrigin(item.Origin, at + ".Origin", into);
                PassiveSkillStats? stats = GuardValue(at, into, () => new PassiveSkillStats(item.Radius, item.Interval, item.Damage));
                if (into.Count > errors) continue;

                PassiveSkillDefinition skill = Guard(at, into, () => new PassiveSkillDefinition(item.Id, origin.Value, stats.Value));
                if (skill != null) skills.Add(skill);
            }
            return skills;
        }

        // Enum.TryParse는 숫자 문자열도 통과시킨다. 명시 목록이면 가능한 값을 진단에 그대로 싣는다.
        private static SkillOrigin? ParseOrigin(string name, string at, List<ContentDiagnostic> into)
        {
            switch (name)
            {
                case "OwnerAimPoint": return SkillOrigin.OwnerAimPoint;
                default:
                    into.Add(new ContentDiagnostic(at, $"알 수 없는 기준점 종류 '{name}'. 가능한 값: OwnerAimPoint."));
                    return null;
            }
        }

        // ── 공통 ────────────────────────────────────────────────────────────

        // 정의 생성자의 규칙 위반을 그 자리의 진단으로 바꾼다.
        private static T Guard<T>(string at, List<ContentDiagnostic> into, Func<T> create) where T : class
        {
            try { return create(); }
            catch (ArgumentException error)
            {
                into.Add(new ContentDiagnostic(at, error.Message));
                return null;
            }
        }

        private static T? GuardValue<T>(string at, List<ContentDiagnostic> into, Func<T> create) where T : struct
        {
            try { return create(); }
            catch (ArgumentException error)
            {
                into.Add(new ContentDiagnostic(at, error.Message));
                return null;
            }
        }

        private static T Missing<T>(string at, List<ContentDiagnostic> into) where T : class
        {
            into.Add(new ContentDiagnostic(at, "데이터가 없다."));
            return null;
        }

        private static string At(string section, int index, string id) =>
            string.IsNullOrWhiteSpace(id) ? $"{section}[{index}]" : $"{section}[{id}]";

        private static ContentLoadResult Fail(List<ContentDiagnostic> diagnostics) =>
            new ContentLoadResult(null, diagnostics);
    }
}
