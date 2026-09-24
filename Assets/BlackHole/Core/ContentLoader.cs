using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // ContentData(저작 형식) → ContentCatalog(검증된 정의).
    //
    // 오류가 하나라도 있으면 Catalog 없이 모든 진단을 돌려준다(부분 통과 금지).
    // 여기서 새로 두는 규칙은 데이터 모양에 관한 것뿐이다 — 빈 칸, 알 수 없는 종류 이름.
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

            List<TargetDefinition> targets = LoadTargets(data.Targets, diagnostics);
            List<SkillDefinition> skills = LoadSkills(data.Skills, diagnostics);
            GrowthDefinition growth = LoadGrowth(data.Growth, diagnostics);
            SpawnDefinition spawn = LoadSpawn(data.Spawn, diagnostics);
            Check("SessionDuration", diagnostics,
                () => DefinitionGuard.Positive(data.SessionDuration, nameof(data.SessionDuration)));
            if (diagnostics.Count > 0) return Fail(diagnostics);

            ContentInvariants.Collect(targets, skills, spawn, diagnostics, out _);
            if (diagnostics.Count > 0) return Fail(diagnostics);

            var catalog = new ContentCatalog(data.SessionDuration, targets, skills, growth, spawn);
            return new ContentLoadResult(catalog, diagnostics);
        }

        // ── 대상 ────────────────────────────────────────────────────────────

        private static List<TargetDefinition> LoadTargets(List<TargetData> items, List<ContentDiagnostic> into)
        {
            var targets = new List<TargetDefinition>();
            if (items == null) return targets;

            for (int i = 0; i < items.Count; i++)
            {
                TargetData item = items[i];
                string at = At("Targets", i, item?.Id);
                if (item == null)
                {
                    into.Add(new ContentDiagnostic(at, "대상 데이터가 null이다."));
                    continue;
                }

                TargetDefinition target = Guard(at, into, () => new TargetDefinition(
                    item.Id, item.MaxHealth, item.AngularSpeed, item.InwardSpeed, item.Reward));
                if (target != null) targets.Add(target);
            }
            return targets;
        }

        // ── 스킬 ────────────────────────────────────────────────────────────

        private static List<SkillDefinition> LoadSkills(List<SkillData> items, List<ContentDiagnostic> into)
        {
            var skills = new List<SkillDefinition>();
            if (items == null) return skills;

            for (int i = 0; i < items.Count; i++)
            {
                SkillData item = items[i];
                string at = At("Skills", i, item?.Id);
                if (item == null)
                {
                    into.Add(new ContentDiagnostic(at, "스킬 데이터가 null이다."));
                    continue;
                }
                if (!TryParseSkillKind(item.Kind, out SkillKind kind))
                {
                    into.Add(new ContentDiagnostic(at + ".Kind",
                        $"알 수 없는 스킬 종류 '{item.Kind}'. 가능한 값: FocusedStrike, GravityPulse."));
                    continue;
                }

                SkillDefinition skill = Guard(at, into, () => new SkillDefinition(
                    item.Id, kind, item.Cooldown, item.Damage, item.Radius, item.PullDistance));
                if (skill != null) skills.Add(skill);
            }
            return skills;
        }

        // Enum.TryParse는 숫자 문자열("0")도 통과시킨다. 명시 목록이면 가능한 값을 진단에 그대로 싣는다.
        private static bool TryParseSkillKind(string name, out SkillKind kind)
        {
            switch (name)
            {
                case "FocusedStrike": kind = SkillKind.FocusedStrike; return true;
                case "GravityPulse": kind = SkillKind.GravityPulse; return true;
                default: kind = default; return false;
            }
        }

        // ── 성장·출현 ───────────────────────────────────────────────────────

        private static GrowthDefinition LoadGrowth(GrowthData item, List<ContentDiagnostic> into)
        {
            if (item == null)
            {
                into.Add(new ContentDiagnostic("Growth", "성장 데이터가 없다."));
                return null;
            }
            return Guard("Growth", into, () =>
                new GrowthDefinition(item.UpgradeCost, item.MaxPowerLevel, item.PowerPerLevel));
        }

        private static SpawnDefinition LoadSpawn(SpawnData item, List<ContentDiagnostic> into)
        {
            if (item == null)
            {
                into.Add(new ContentDiagnostic("Spawn", "출현 데이터가 없다."));
                return null;
            }
            return Guard("Spawn", into, () =>
                new SpawnDefinition(item.TargetOrder, item.Interval, item.Radius, item.Capacity));
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

        private static void Check(string at, List<ContentDiagnostic> into, Action check)
        {
            try { check(); }
            catch (ArgumentException error) { into.Add(new ContentDiagnostic(at, error.Message)); }
        }

        private static string At(string section, int index, string id) =>
            string.IsNullOrWhiteSpace(id) ? $"{section}[{index}]" : $"{section}[{id}]";

        private static ContentLoadResult Fail(List<ContentDiagnostic> diagnostics) =>
            new ContentLoadResult(null, diagnostics);
    }
}
