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

            TimeLimitDefinition timeLimit = Guard("TimeLimit", diagnostics,
                () => new TimeLimitDefinition(data.TimeLimit));
            TargetRulesDefinition targetRules = LoadTargetRules(data.TargetRules, diagnostics);
            BlackHoleDefinition blackHole = LoadBlackHole(data.BlackHole, diagnostics);
            List<TargetDefinition> targets = LoadTargets(data.Targets, diagnostics);
            List<SkillDefinition> skills = LoadSkills(data.Skills, diagnostics);
            List<UpgradeDefinition> upgrades = LoadUpgrades(data.Upgrades, diagnostics);
            SkillTreeDefinition skillTree = LoadSkillTree(data.SkillTree, diagnostics);
            SpawnDefinition spawn = LoadSpawn(data.Spawn, diagnostics);
            if (diagnostics.Count > 0) return Fail(diagnostics);

            ContentInvariants.Collect(targets, skills, upgrades, skillTree, spawn, diagnostics, out _);
            if (diagnostics.Count > 0) return Fail(diagnostics);

            var catalog = new ContentCatalog(
                timeLimit, targetRules, blackHole, targets, skills, upgrades, skillTree, spawn);
            return new ContentLoadResult(catalog, diagnostics);
        }

        // ── 대상 ────────────────────────────────────────────────────────────

        private static TargetRulesDefinition LoadTargetRules(TargetRulesData item, List<ContentDiagnostic> into)
        {
            if (item == null)
            {
                into.Add(new ContentDiagnostic("TargetRules", "대상 공통 규칙 데이터가 없다."));
                return null;
            }
            return Guard("TargetRules", into, () => new TargetRulesDefinition(item.AliveMargin, item.FallSpeed));
        }

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

                int errors = into.Count;
                MovementDefinition movement = LoadMovement(item.Movement, at + ".Movement", into);
                RewardDefinition reward = LoadReward(item.Reward, at + ".Reward", into);
                if (into.Count > errors) continue;

                TargetDefinition target = Guard(at, into, () => new TargetDefinition(
                    item.Id, item.MaxHealth, movement, reward));
                if (target != null) targets.Add(target);
            }
            return targets;
        }

        // 종류 이름을 하위 정의로 바꾼다. 종류마다 쓰지 않는 칸이 0인지도 여기서 본다 —
        // 하위 정의에는 그 칸이 없으므로 이것은 데이터 모양의 규칙이다.
        private static MovementDefinition LoadMovement(MovementData item, string at, List<ContentDiagnostic> into)
        {
            if (item == null)
            {
                into.Add(new ContentDiagnostic(at, "이동 데이터가 없다."));
                return null;
            }

            switch (item.Kind)
            {
                case "Orbit":
                    if (item.InitialSpeed != 0 || item.Acceleration != 0)
                        return Unused(at, into, "Orbit", "InitialSpeed, Acceleration");
                    return Guard(at, into, () => new OrbitMovementDefinition(item.AngularSpeed, item.InwardSpeed));
                case "Dive":
                    if (item.AngularSpeed != 0 || item.InwardSpeed != 0)
                        return Unused(at, into, "Dive", "AngularSpeed, InwardSpeed");
                    return Guard(at, into, () => new DiveMovementDefinition(item.InitialSpeed, item.Acceleration));
                default:
                    into.Add(new ContentDiagnostic(at + ".Kind",
                        $"알 수 없는 이동 종류 '{item.Kind}'. 가능한 값: Orbit, Dive."));
                    return null;
            }
        }

        private static MovementDefinition Unused(string at, List<ContentDiagnostic> into, string kind, string fields)
        {
            into.Add(new ContentDiagnostic(at, $"{kind}는 {fields}을(를) 쓰지 않는다. 0으로 둘 것."));
            return null;
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
                int errors = into.Count;
                TargetSelectionDefinition selection = LoadSelection(item.Selection, at + ".Selection", into);
                List<SkillEffectDefinition> effects = LoadEffects(item.Effects, at + ".Effects", into);
                if (into.Count > errors) continue;

                SkillDefinition skill = Guard(at, into, () =>
                    new SkillDefinition(item.Id, item.Cooldown, selection, effects));
                if (skill != null) skills.Add(skill);
            }
            return skills;
        }

        // 종류 이름은 명시 목록으로 해석한다. 가능한 값을 진단에 그대로 싣는다.
        private static TargetSelectionDefinition LoadSelection(SelectionData item, string at,
            List<ContentDiagnostic> into)
        {
            if (item == null)
            {
                into.Add(new ContentDiagnostic(at, "대상 선택 데이터가 없다."));
                return null;
            }

            switch (item.Kind)
            {
                case "NearestInRadius":
                    return Guard(at, into, () => new NearestInRadiusDefinition(item.Radius));
                case "AllInRadius":
                    return Guard(at, into, () => new AllInRadiusDefinition(item.Radius));
                default:
                    into.Add(new ContentDiagnostic(at + ".Kind",
                        $"알 수 없는 대상 선택 종류 '{item.Kind}'. 가능한 값: NearestInRadius, AllInRadius."));
                    return null;
            }
        }

        private static List<SkillEffectDefinition> LoadEffects(List<EffectData> items, string at,
            List<ContentDiagnostic> into)
        {
            var effects = new List<SkillEffectDefinition>();
            if (items == null) return effects;

            for (int i = 0; i < items.Count; i++)
            {
                EffectData item = items[i];
                string where = $"{at}[{i}]";
                if (item == null)
                {
                    into.Add(new ContentDiagnostic(where, "효과 데이터가 null이다."));
                    continue;
                }

                SkillEffectDefinition effect;
                switch (item.Kind)
                {
                    case "Damage":
                        effect = Guard(where, into, () => new DamageEffectDefinition(item.Amount));
                        break;
                    case "Pull":
                        effect = Guard(where, into, () => new PullEffectDefinition(item.Amount));
                        break;
                    default:
                        into.Add(new ContentDiagnostic(where + ".Kind",
                            $"알 수 없는 효과 종류 '{item.Kind}'. 가능한 값: Damage, Pull."));
                        continue;
                }
                if (effect != null) effects.Add(effect);
            }
            return effects;
        }

        // ── 블랙홀·보상·강화 ─────────────────────────────────────────────────

        private static BlackHoleDefinition LoadBlackHole(BlackHoleData item, List<ContentDiagnostic> into)
        {
            if (item == null)
            {
                into.Add(new ContentDiagnostic("BlackHole", "블랙홀 데이터가 없다."));
                return null;
            }
            return Guard("BlackHole", into, () =>
                new BlackHoleDefinition(item.BaseAbsorptionRadius, item.RadiusPerMass, item.MassRadiusCap));
        }

        private static RewardDefinition LoadReward(RewardData item, string at, List<ContentDiagnostic> into)
        {
            if (item == null)
            {
                into.Add(new ContentDiagnostic(at, "보상 데이터가 없다."));
                return null;
            }
            return Guard(at, into, () => new RewardDefinition(item.Mass, item.Credits));
        }

        private static List<UpgradeDefinition> LoadUpgrades(List<UpgradeData> items, List<ContentDiagnostic> into)
        {
            var upgrades = new List<UpgradeDefinition>();
            if (items == null) return upgrades;

            for (int i = 0; i < items.Count; i++)
            {
                UpgradeData item = items[i];
                string at = At("Upgrades", i, item?.Id);
                if (item == null)
                {
                    into.Add(new ContentDiagnostic(at, "강화 데이터가 null이다."));
                    continue;
                }
                if (!TryParseUpgradeStat(item.Stat, out UpgradeStat stat))
                {
                    into.Add(new ContentDiagnostic(at + ".Stat",
                        $"알 수 없는 강화 대상 '{item.Stat}'. 가능한 값: DamageMultiplier, AbsorptionRadius."));
                    continue;
                }

                UpgradeDefinition upgrade = Guard(at, into, () =>
                    new UpgradeDefinition(item.Id, item.BaseCost, item.MaxLevel, stat, item.PerLevel));
                if (upgrade != null) upgrades.Add(upgrade);
            }
            return upgrades;
        }

        // Enum.TryParse는 숫자 문자열("0")도 통과시킨다. 명시 목록이면 가능한 값을 진단에 그대로 싣는다.
        private static bool TryParseUpgradeStat(string name, out UpgradeStat stat)
        {
            switch (name)
            {
                case "DamageMultiplier": stat = UpgradeStat.DamageMultiplier; return true;
                case "AbsorptionRadius": stat = UpgradeStat.AbsorptionRadius; return true;
                default: stat = default; return false;
            }
        }

        // ── 스킬 트리 ───────────────────────────────────────────────────────

        // 노드마다 생성자 규칙을 모으고, 그래프 규칙(중복·누락·순환)은 SkillTreeInvariants로 모은다.
        private static SkillTreeDefinition LoadSkillTree(SkillTreeData item, List<ContentDiagnostic> into)
        {
            if (item == null || item.Nodes == null) return SkillTreeDefinition.Empty;

            int errors = into.Count;
            var nodes = new List<SkillTreeNodeDefinition>();
            for (int i = 0; i < item.Nodes.Count; i++)
            {
                SkillTreeNodeData node = item.Nodes[i];
                string at = At("SkillTree.Nodes", i, node?.Id);
                if (node == null)
                {
                    into.Add(new ContentDiagnostic(at, "트리 노드 데이터가 null이다."));
                    continue;
                }
                SkillTreeNodeDefinition definition = Guard(at, into, () =>
                    new SkillTreeNodeDefinition(node.Id, node.UpgradeId, node.Requires));
                if (definition != null) nodes.Add(definition);
            }
            if (into.Count > errors) return null;

            SkillTreeInvariants.Collect(nodes, into);
            return into.Count > errors ? null : new SkillTreeDefinition(nodes);
        }

        // ── 출현 ────────────────────────────────────────────────────────────

        private static SpawnDefinition LoadSpawn(SpawnData item, List<ContentDiagnostic> into)
        {
            if (item == null)
            {
                into.Add(new ContentDiagnostic("Spawn", "출현 데이터가 없다."));
                return null;
            }
            return Guard("Spawn", into, () =>
                new SpawnDefinition(item.TargetOrder, item.Interval, item.Radius, item.AngleStep, item.Capacity));
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

        private static string At(string section, int index, string id) =>
            string.IsNullOrWhiteSpace(id) ? $"{section}[{index}]" : $"{section}[{id}]";

        private static ContentLoadResult Fail(List<ContentDiagnostic> diagnostics) =>
            new ContentLoadResult(null, diagnostics);
    }
}
