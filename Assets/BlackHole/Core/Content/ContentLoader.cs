using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // ContentData(저작 형식) → GameContent(검증된 정의).
    //
    // 오류가 하나라도 있으면 Content 없이 모든 진단을 돌려준다(부분 통과 금지).
    // 여기서 새로 두는 규칙은 데이터 모양에 관한 것뿐이다(빠진 칸, 알 수 없는 종류 이름, 정의되지 않은 참조).
    // 수치 규칙은 정의 생성자를, 콘텐츠 전체 규칙은 ContentInvariants를 그대로 호출해 경로를 붙인다.
    //
    // 두 단계로 읽는다.
    // 1. 독립 정의: 판 설정, HQ, Enemy, 출현 위치, Skill.
    // 2. 다른 정의를 참조하는 정의: 전투 시작 배치, HQ 성장 노드, 업그레이드 노드.
    //    1단계의 Enemy·Skill 색인으로 대상 ID를 정의로 해석한다.
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

            if (diagnostics.Count > 0)
                return Fail(diagnostics);

            ContentInvariants.Collect(
                enemies,
                skills,
                startingSkills,
                diagnostics,
                out Dictionary<string, EnemyDefinition> enemiesById,
                out Dictionary<string, PassiveSkillDefinition> skillsById);

            List<SupplyRequest> startSupply = LoadSupplyList(data.StartSupply, "StartSupply", enemiesById, diagnostics);
            HqGrowthDefinition growth = LoadGrowth(data.Growth, enemiesById, diagnostics);
            List<UpgradeNodeDefinition> upgrades = LoadUpgrades(data.Upgrades, enemiesById, skillsById, diagnostics);

            if (diagnostics.Count > 0)
                return Fail(diagnostics);

            // 노드 사이의 규칙(ID 유일, 선행 노드의 실재, 순환 없음)은 노드가 모두 올바를 때 본다.
            ContentInvariants.CollectUpgrades(upgrades, diagnostics, out _);

            if (diagnostics.Count > 0)
                return Fail(diagnostics);

            // 해금 규칙은 선행 사슬이 올바를 때 본다.
            ContentInvariants.CollectSkillUnlocks(upgrades, startingSkills, diagnostics);

            if (diagnostics.Count > 0)
                return Fail(diagnostics);

            var content = new GameContent(
                timeLimit,
                hq,
                enemies,
                spawn,
                startSupply,
                growth,
                skills,
                startingSkills,
                upgrades);

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
                DeathEffectDefinition deathEffect = LoadDeathEffect(item.DeathEffect, at + ".DeathEffect", into);
                EnemyStats? stats = GuardValue(at, into, () => new EnemyStats(item.MaxHealth, item.MoveSpeed, item.Size));
                if (into.Count > errors) continue;

                EnemyDefinition enemy = Guard(at, into, () =>
                    new EnemyDefinition(item.Id, stats.Value, behavior, item.Gold, item.HqExp, deathEffect));
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

        // 비어 있으면 사망 효과가 없다(null을 돌려준다). 수치 규칙은 효과 정의 생성자가 가진다.
        private static DeathEffectDefinition LoadDeathEffect(DeathEffectData item, string at, List<ContentDiagnostic> into)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Kind))
                return null;

            switch (item.Kind)
            {
                case "ChainLightning":
                    return Guard(at, into, () => new ChainLightningDefinition(item.Damage, item.Range, item.Chains));
                default:
                    into.Add(new ContentDiagnostic(at + ".Kind", $"알 수 없는 사망 효과 종류 '{item.Kind}'. 가능한 값: ChainLightning."));
                    return null;
            }
        }

        // ── 출현·공급 ───────────────────────────────────────────────────────

        private static SpawnDefinition LoadSpawn(SpawnData item, List<ContentDiagnostic> into)
        {
            if (item == null)
                return Missing<SpawnDefinition>("Spawn", into);

            return Guard("Spawn", into, () => new SpawnDefinition(item.Distance, item.AngleStep));
        }

        // 없으면 공급이 없다.
        private static List<SupplyRequest> LoadSupplyList(
            List<SupplyData> items,
            string section,
            IReadOnlyDictionary<string, EnemyDefinition> enemies,
            List<ContentDiagnostic> into)
        {
            var requests = new List<SupplyRequest>();

            if (items == null)
                return requests;

            for (int i = 0; i < items.Count; i++)
            {
                SupplyData item = items[i];
                string at = $"{section}[{i}]";

                if (item == null)
                {
                    into.Add(new ContentDiagnostic(at, "공급 데이터가 null이다."));
                    continue;
                }

                if (item.Enemy == null || !enemies.TryGetValue(item.Enemy, out EnemyDefinition enemy))
                {
                    into.Add(new ContentDiagnostic(at + ".Enemy", $"정의되지 않은 Enemy ID '{item.Enemy}'."));
                    continue;
                }

                SupplyRequest? request = GuardValue(at, into, () => new SupplyRequest(enemy, item.Count));

                if (request.HasValue)
                    requests.Add(request.Value);
            }

            return requests;
        }

        // ── HQ 성장 ─────────────────────────────────────────────────────────

        // 없으면 성장 노드가 없다(HQ는 시작 Level에 머문다).
        private static HqGrowthDefinition LoadGrowth(
            GrowthData item,
            IReadOnlyDictionary<string, EnemyDefinition> enemies,
            List<ContentDiagnostic> into)
        {
            if (item?.Levels == null)
                return HqGrowthDefinition.None;

            int errors = into.Count;
            var levels = new List<HqLevelDefinition>();

            for (int i = 0; i < item.Levels.Count; i++)
            {
                GrowthLevelData level = item.Levels[i];
                string at = $"Growth.Levels[{i}]";

                if (level == null)
                {
                    into.Add(new ContentDiagnostic(at, "성장 노드 데이터가 null이다."));
                    continue;
                }

                int levelErrors = into.Count;
                List<SupplyRequest> supply = LoadSupplyList(level.Supply, at + ".Supply", enemies, into);

                if (into.Count > levelErrors)
                    continue;

                HqLevelDefinition definition = Guard(at, into, () =>
                    new HqLevelDefinition(level.Exp, level.ExtraTime, supply));

                if (definition != null)
                    levels.Add(definition);
            }

            if (into.Count > errors)
                return null;

            // 노드 사이의 규칙(임계값이 앞 노드보다 큼)은 성장 정의 생성자가 본다.
            return Guard("Growth", into, () => new HqGrowthDefinition(levels));
        }

        // ── 업그레이드 ──────────────────────────────────────────────────────

        // 없으면 업그레이드 노드가 없다.
        private static List<UpgradeNodeDefinition> LoadUpgrades(
            List<UpgradeData> items,
            IReadOnlyDictionary<string, EnemyDefinition> enemies,
            IReadOnlyDictionary<string, PassiveSkillDefinition> skills,
            List<ContentDiagnostic> into)
        {
            var upgrades = new List<UpgradeNodeDefinition>();

            if (items == null)
                return upgrades;

            for (int i = 0; i < items.Count; i++)
            {
                UpgradeData item = items[i];
                string at = At("Upgrades", i, item?.Id);

                if (item == null)
                {
                    into.Add(new ContentDiagnostic(at, "업그레이드 데이터가 null이다."));
                    continue;
                }

                int errors = into.Count;
                var effects = new List<UpgradeEffect>();

                if (item.Effects != null)
                {
                    for (int j = 0; j < item.Effects.Count; j++)
                    {
                        UpgradeEffect effect = LoadUpgradeEffect(item.Effects[j], $"{at}.Effects[{j}]", enemies, skills, into);

                        if (effect != null)
                            effects.Add(effect);
                    }
                }

                if (into.Count > errors)
                    continue;

                UpgradeNodeDefinition node = Guard(at, into, () =>
                    new UpgradeNodeDefinition(item.Id, item.Price, item.Requires, effects));

                if (node != null)
                    upgrades.Add(node);
            }

            return upgrades;
        }

        // 효과 종류 이름을 UpgradeEffectKind로, 대상 ID를 정의로 바꾼다. 종류 목록은 열거형 하나에만 있다.
        private static UpgradeEffect LoadUpgradeEffect(
            UpgradeEffectData item,
            string at,
            IReadOnlyDictionary<string, EnemyDefinition> enemies,
            IReadOnlyDictionary<string, PassiveSkillDefinition> skills,
            List<ContentDiagnostic> into)
        {
            if (item == null)
                return Missing<UpgradeEffect>(at, into);

            if (!TryParseUpgradeKind(item.Kind, out UpgradeEffectKind kind))
            {
                into.Add(new ContentDiagnostic(
                    at + ".Kind",
                    $"알 수 없는 업그레이드 효과 종류 '{item.Kind}'. 가능한 값: {string.Join(", ", Enum.GetNames(typeof(UpgradeEffectKind)))}."));
                return null;
            }

            PassiveSkillDefinition skill = null;
            EnemyDefinition enemy = null;
            bool hasTarget = !string.IsNullOrWhiteSpace(item.Target);

            if (UpgradeEffect.TargetsSkill(kind))
            {
                if (!hasTarget || !skills.TryGetValue(item.Target, out skill))
                {
                    into.Add(new ContentDiagnostic(at + ".Target", $"대상 Skill ID '{item.Target}'가 정의되지 않았다."));
                    return null;
                }
            }
            else if (hasTarget || UpgradeEffect.RequiresEnemy(kind))
            {
                if (!hasTarget || !enemies.TryGetValue(item.Target, out enemy))
                {
                    into.Add(new ContentDiagnostic(at + ".Target", $"대상 Enemy ID '{item.Target}'가 정의되지 않았다."));
                    return null;
                }
            }

            return Guard(at, into, () => new UpgradeEffect(kind, item.Value, skill, enemy));
        }

        // Enum.TryParse는 숫자 문자열도 통과시킨다. 이름이 정확히 같을 때만 받는다.
        private static bool TryParseUpgradeKind(string name, out UpgradeEffectKind kind)
        {
            foreach (UpgradeEffectKind candidate in (UpgradeEffectKind[])Enum.GetValues(typeof(UpgradeEffectKind)))
            {
                if (candidate.ToString() == name)
                {
                    kind = candidate;
                    return true;
                }
            }

            kind = default;
            return false;
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

                PassiveSkillDefinition skill = LoadSkill(item, at, into);
                if (skill != null) skills.Add(skill);
            }
            return skills;
        }

        // 종류 이름을 하위 정의로 바꾼다. 가능한 값을 진단에 그대로 싣는다. 수치 규칙은 정의 생성자가 가진다.
        private static PassiveSkillDefinition LoadSkill(SkillData item, string at, List<ContentDiagnostic> into)
        {
            switch (item.Kind)
            {
                case "Breaker":
                {
                    BreakerStats? stats = GuardValue(at, into, () => new BreakerStats(item.Radius, item.Interval, item.Damage));
                    if (stats == null) return null;
                    return Guard(at, into, () => new BreakerSkillDefinition(item.Id, stats.Value));
                }
                case "PiercingLaser":
                {
                    PiercingLaserStats? stats = GuardValue(at, into, () =>
                        new PiercingLaserStats(item.Interval, item.Damage, item.Width, item.TelegraphDuration));
                    if (stats == null) return null;
                    return Guard(at, into, () => new PiercingLaserDefinition(item.Id, stats.Value, item.BoundaryRadius));
                }
                default:
                    into.Add(new ContentDiagnostic(at + ".Kind", $"알 수 없는 Skill 종류 '{item.Kind}'. 가능한 값: Breaker, PiercingLaser."));
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
