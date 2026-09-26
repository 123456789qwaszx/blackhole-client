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
    // 1. 개별 정의: 판 설정, 스킬, 적 종류, 출현 배치.
    // 2. 적 종류를 가리키는 것: 적 ID 유일, 공급, 적 풀.
    // 3. 적 풀을 가리키는 것: 풀 ID 유일, 단계 표.
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

            if (diagnostics.Count > 0)
                return Fail(diagnostics);

            ContentInvariants.CollectEnemies(enemies, diagnostics, out Dictionary<string, EnemyDefinition> enemiesById);
            List<SupplyRequest> startSupply = LoadSupplyList(data.StartSupply, "StartSupply", enemiesById, diagnostics);
            List<EnemyPoolDefinition> pools = LoadPools(data.EnemyPools, enemiesById, diagnostics);

            if (startSupply.Count > 0 && placement == null)
                diagnostics.Add(new ContentDiagnostic("EnemyPlacement", "공급이 있으면 출현 배치가 필요하다."));

            if (diagnostics.Count > 0)
                return Fail(diagnostics);

            ContentInvariants.CollectPools(pools, diagnostics, out Dictionary<string, EnemyPoolDefinition> poolsById);
            List<StageDefinition> stages = LoadStages(data.Stages, poolsById, diagnostics);

            if (diagnostics.Count > 0)
                return Fail(diagnostics);

            return new ContentLoadResult(
                new GameContent(timeLimit, breaker, laser, enemies, placement, startSupply, pools, stages),
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
                EnemyStats? stats = GuardValue(at, into, () => new EnemyStats(item.MaxHealth, item.MoveSpeed, item.Size));

                if (into.Count > errors)
                    continue;

                EnemyDefinition enemy = Guard(at, into, () => new EnemyDefinition(item.Id, stats.Value, behavior, deathEffect));

                if (enemy != null)
                    enemies.Add(enemy);
            }

            return enemies;
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

        // ── 적 풀과 단계 표 ─────────────────────────────────────────────────

        // 없으면 적 풀이 없다. 적 ID는 2단계의 색인으로 정의에 잇는다.
        private static List<EnemyPoolDefinition> LoadPools(
            List<EnemyPoolData> items,
            IReadOnlyDictionary<string, EnemyDefinition> enemies,
            List<ContentDiagnostic> into)
        {
            var pools = new List<EnemyPoolDefinition>();

            if (items == null)
                return pools;

            for (int i = 0; i < items.Count; i++)
            {
                EnemyPoolData item = items[i];
                string at = At("EnemyPools", i, item?.Id);

                if (item == null)
                {
                    into.Add(new ContentDiagnostic(at, "적 풀 데이터가 null이다."));
                    continue;
                }

                int errors = into.Count;
                var entries = new List<EnemyPoolEntry>();

                if (item.Entries != null)
                {
                    for (int j = 0; j < item.Entries.Count; j++)
                    {
                        EnemyPoolEntryData entry = item.Entries[j];
                        string entryAt = $"{at}.Entries[{j}]";

                        if (entry == null)
                        {
                            into.Add(new ContentDiagnostic(entryAt, "풀 항목이 null이다."));
                            continue;
                        }

                        if (entry.Enemy == null || !enemies.TryGetValue(entry.Enemy, out EnemyDefinition enemy))
                        {
                            into.Add(new ContentDiagnostic(entryAt + ".Enemy", $"정의되지 않은 적 ID '{entry.Enemy}'."));
                            continue;
                        }

                        EnemyPoolEntry? loaded = GuardValue(entryAt, into, () => new EnemyPoolEntry(enemy, entry.MaxAlive));

                        if (loaded.HasValue)
                            entries.Add(loaded.Value);
                    }
                }

                if (into.Count > errors)
                    continue;

                EnemyPoolDefinition pool = Guard(at, into, () => new EnemyPoolDefinition(item.Id, entries));

                if (pool != null)
                    pools.Add(pool);
            }

            return pools;
        }

        // Stages[i]가 (i + 1)단계다. 단계는 하나 이상 있어야 한다. 풀 ID는 3단계의 색인으로 정의에 잇는다.
        private static List<StageDefinition> LoadStages(
            List<StageData> items,
            IReadOnlyDictionary<string, EnemyPoolDefinition> pools,
            List<ContentDiagnostic> into)
        {
            var stages = new List<StageDefinition>();

            if (items == null || items.Count == 0)
            {
                into.Add(new ContentDiagnostic("Stages", "단계가 하나 이상 필요하다."));
                return stages;
            }

            for (int i = 0; i < items.Count; i++)
            {
                StageData item = items[i];
                string at = $"Stages[{i}]";

                if (item == null)
                {
                    into.Add(new ContentDiagnostic(at, "단계 데이터가 null이다."));
                    continue;
                }

                if (item.Pool == null || !pools.TryGetValue(item.Pool, out EnemyPoolDefinition pool))
                {
                    into.Add(new ContentDiagnostic(at + ".Pool", $"정의되지 않은 적 풀 ID '{item.Pool}'."));
                    continue;
                }

                stages.Add(new StageDefinition(i + 1, pool));
            }

            return stages;
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
