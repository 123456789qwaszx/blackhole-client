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
    // 세 단계로 읽는다. 앞 단계에 오류가 있으면 뒤 단계를 보지 않는다(잘못된 정의가 거짓 참조 오류를 만들지 않게).
    // 1. 개별 정의: 판 설정, 스킬, 적 종류(색 등급·Level별 색 비율·질량 단계·사망 효과), 출현 배치, 블랙홀 성장의 Level 표.
    // 2. 적 종류를 가리키는 것: 적 ID 유일, 종류 사이 연결(변환 대상·부모), 공급, 전체 개체 수 상한.
    // 3. 전체: 전투 시작 공급이 상한 안인가.
    // 업그레이드 노드는 여기서 읽지 않는다(NodeTreeLoader). 노드와 콘텐츠를 함께 보는 검사는 UpgradeContentCheck가 한다.
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
            BreakerDefinition breaker = LoadBreaker(data.Breaker, diagnostics);
            LaserDefinition laser = LoadLaser(data.Laser, diagnostics);
            List<EnemyDefinition> enemies = LoadEnemies(data.Enemies, diagnostics);
            EnemyPlacementDefinition placement = LoadPlacement(data.EnemyPlacement, diagnostics);
            HqGrowthDefinition growth = LoadGrowth(data.Growth, diagnostics);

            if (diagnostics.Count > 0)
                return Fail(diagnostics);

            ContentInvariants.CollectEnemies(enemies, diagnostics, out Dictionary<string, EnemyDefinition> enemiesById);
            ContentInvariants.CheckKindLinks(enemies, enemiesById, diagnostics);
            List<SupplyRequest> startSupply = LoadSupplyList(data.StartSupply, "StartSupply", enemiesById, diagnostics);

            if (startSupply.Count > 0 && placement == null)
                diagnostics.Add(new ContentDiagnostic("EnemyPlacement", "공급이 있으면 출현 배치가 필요하다."));

            ContentInvariants.CheckMaxAlive(placement, data.MaxAliveEnemies, diagnostics);

            if (diagnostics.Count > 0)
                return Fail(diagnostics);

            ContentInvariants.CheckStartSupplyFits(startSupply, 0, data.MaxAliveEnemies, diagnostics);

            if (diagnostics.Count > 0)
                return Fail(diagnostics);

            return new ContentLoadResult(
                new GameContent(timeLimit, breaker, laser, enemies, placement, data.MaxAliveEnemies, startSupply, growth),
                diagnostics);
        }

        private static TimeLimitDefinition LoadSession(SessionData item, List<ContentDiagnostic> into)
        {
            if (item == null)
                return Missing<TimeLimitDefinition>("Session", into);

            return Guard("Session.TimeLimit", into, () => new TimeLimitDefinition(item.TimeLimit));
        }

        // ── 스킬 ────────────────────────────────────────────────────────────

        // 없으면 판에 Breaker가 없다.
        private static BreakerDefinition LoadBreaker(BreakerData item, List<ContentDiagnostic> into)
        {
            if (item == null)
                return null;

            return Guard("Breaker", into, () =>
                new BreakerDefinition(item.Damage, item.Interval, item.Radius, item.CritChance, item.CritMultiplier));
        }

        // 없으면 판에 레이저가 없다.
        private static LaserDefinition LoadLaser(LaserData item, List<ContentDiagnostic> into)
        {
            if (item == null)
                return null;

            return Guard("Laser", into, () =>
                new LaserDefinition(item.Damage, item.Interval, item.Width, item.TelegraphDuration, item.BoundaryRadius));
        }

        // ── 적 ──────────────────────────────────────────────────────────────

        private static List<EnemyDefinition> LoadEnemies(List<EnemyData> items, List<ContentDiagnostic> into)
        {
            var enemies = new List<EnemyDefinition>();

            if (items == null)
                return enemies;

            for (int i = 0; i < items.Count; i++)
            {
                EnemyData item = items[i];
                string at = At("Enemies", i, item?.Id);

                if (item == null)
                {
                    into.Add(new ContentDiagnostic(at, "적 데이터가 null이다."));
                    continue;
                }

                int errors = into.Count;
                EnemyBehaviorDefinition behavior = LoadBehavior(item.Behavior, at + ".Behavior", into);
                DeathEffectDefinition deathEffect = LoadDeathEffect(item.DeathEffect, at + ".DeathEffect", into);
                List<EnemyTier> tiers = LoadTiers(item.Tiers, at + ".Tiers", into);
                List<LevelColorDefinition> levelColors = LoadLevelColors(item.LevelColors, at + ".LevelColors", into);
                List<MassLevelDefinition> massLevels = LoadMassLevels(item.MassLevels, at + ".MassLevels", into);

                if (into.Count > errors)
                    continue;

                EnemyDefinition enemy = Guard(at, into, () =>
                    new EnemyDefinition(item.Id, item.MoveSpeed, tiers, levelColors, massLevels, item.GoldenMultiplier, behavior, deathEffect, item.UpgradesTo, item.SpecialOf));

                if (enemy != null)
                    enemies.Add(enemy);
            }

            return enemies;
        }

        // 줄마다 수치를 검사한다. 줄 수(하나 이상)와 Level별 색 비율과의 길이 맞춤은 EnemyDefinition이 검사한다.
        private static List<EnemyTier> LoadTiers(List<EnemyTierData> items, string at, List<ContentDiagnostic> into)
        {
            var tiers = new List<EnemyTier>();

            for (int i = 0; items != null && i < items.Count; i++)
            {
                EnemyTierData item = items[i];

                if (item == null)
                {
                    into.Add(new ContentDiagnostic($"{at}[{i}]", "데이터가 없다."));
                    continue;
                }

                EnemyTier? tier = GuardValue($"{at}[{i}]", into, () => new EnemyTier(item.MaxHealth, item.Size, item.Gold, item.Exp));

                if (tier.HasValue)
                    tiers.Add(tier.Value);
            }

            return tiers;
        }

        private static List<MassLevelDefinition> LoadMassLevels(List<MassLevelData> items, string at, List<ContentDiagnostic> into)
        {
            var levels = new List<MassLevelDefinition>();

            for (int i = 0; items != null && i < items.Count; i++)
            {
                MassLevelData item = items[i];

                if (item == null)
                {
                    into.Add(new ContentDiagnostic($"{at}[{i}]", "데이터가 없다."));
                    continue;
                }

                MassLevelDefinition level = Guard($"{at}[{i}]", into,
                    () => new MassLevelDefinition(item.HealthMultiplier, item.GoldMultiplier));

                if (level != null)
                    levels.Add(level);
            }

            return levels;
        }

        // 줄마다 시작 Level과 색 비율을 검사한다. 줄 수·순서·색 등급과의 길이 맞춤은 EnemyDefinition이 검사한다.
        private static List<LevelColorDefinition> LoadLevelColors(List<LevelColorData> items, string at, List<ContentDiagnostic> into)
        {
            var rows = new List<LevelColorDefinition>();

            for (int i = 0; items != null && i < items.Count; i++)
            {
                LevelColorData item = items[i];

                if (item == null)
                {
                    into.Add(new ContentDiagnostic($"{at}[{i}]", "데이터가 없다."));
                    continue;
                }

                LevelColorDefinition row = Guard($"{at}[{i}]", into, () => new LevelColorDefinition(item.FromLevel, item.TierRatios));

                if (row != null)
                    rows.Add(row);
            }

            return rows;
        }

        // 종류 이름을 하위 정의로 바꾼다. 가능한 값을 진단에 그대로 싣는다.
        private static EnemyBehaviorDefinition LoadBehavior(EnemyBehaviorData item, string at, List<ContentDiagnostic> into)
        {
            if (item == null)
                return Missing<EnemyBehaviorDefinition>(at, into);

            switch (item.Kind)
            {
                case "Orbit":
                    return new OrbitBehaviorDefinition(item.Clockwise);
                default:
                    into.Add(new ContentDiagnostic(at + ".Kind", $"알 수 없는 행동 종류 '{item.Kind}'. 가능한 값: Orbit."));
                    return null;
            }
        }

        // 없거나 종류 이름이 비어 있으면 효과가 없다(null). 종류 이름을 하위 정의로 바꾸고, 가능한 값을 진단에 싣는다.
        private static DeathEffectDefinition LoadDeathEffect(DeathEffectData item, string at, List<ContentDiagnostic> into)
        {
            if (item == null || string.IsNullOrEmpty(item.Kind))
                return null;

            switch (item.Kind)
            {
                case "ChainLightning":
                    return Guard(at, into, () => new ChainLightningDefinition(item.Damage, item.Radius, item.MaxTargets));
                case "Explosion":
                    return Guard(at, into, () => new ExplosionDefinition(item.Damage, item.Radius));
                case "AttackHaste":
                    return Guard(at, into, () => new AttackHasteDefinition(item.Duration, item.IntervalMultiplier));
                case "GuaranteedCritical":
                    return Guard(at, into, () => new GuaranteedCriticalDefinition(item.Duration));
                default:
                    into.Add(new ContentDiagnostic(at + ".Kind",
                        $"알 수 없는 사망 효과 종류 '{item.Kind}'. 가능한 값: ChainLightning, Explosion, AttackHaste, GuaranteedCritical."));
                    return null;
            }
        }

        // 없으면 null이다. 공급이 있을 때만 필요하다(Load에서 본다).
        private static EnemyPlacementDefinition LoadPlacement(EnemyPlacementData item, List<ContentDiagnostic> into)
        {
            if (item == null)
                return null;

            return Guard("EnemyPlacement", into, () => new EnemyPlacementDefinition(item.MinDistance, item.MaxDistance));
        }

        // 없으면 블랙홀이 Level 1에 머문다.
        private static HqGrowthDefinition LoadGrowth(HqGrowthData item, List<ContentDiagnostic> into)
        {
            if (item == null)
                return HqGrowthDefinition.None;

            var milestones = new List<HqMilestone>();
            int errors = into.Count;

            for (int i = 0; item.Milestones != null && i < item.Milestones.Count; i++)
            {
                HqMilestoneData mark = item.Milestones[i];

                if (mark == null)
                {
                    into.Add(new ContentDiagnostic($"Growth.Milestones[{i}]", "데이터가 없다."));
                    continue;
                }

                HqMilestone milestone = Guard($"Growth.Milestones[{i}]", into, () => new HqMilestone(mark.Level, mark.Reward));

                if (milestone != null)
                    milestones.Add(milestone);
            }

            if (into.Count > errors)
                return null;

            // Level 표의 오류는 LevelExp에, 이정표가 표 밖·순서가 틀린 것은 Milestones에 붙인다.
            try
            {
                return new HqGrowthDefinition(item.LevelExp ?? new List<long>(), milestones);
            }
            catch (ArgumentException error)
            {
                into.Add(new ContentDiagnostic(error.ParamName == "milestones" ? "Growth.Milestones" : "Growth.LevelExp", error.Message));
                return null;
            }
        }

        // 없으면 공급이 없다. 적 ID는 1단계의 색인으로 정의에 잇는다.
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
                    into.Add(new ContentDiagnostic(at + ".Enemy", $"정의되지 않은 적 ID '{item.Enemy}'."));
                    continue;
                }

                SupplyRequest? request = GuardValue(at, into, () => new SupplyRequest(enemy, item.Count));

                if (request.HasValue)
                    requests.Add(request.Value);
            }

            return requests;
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
