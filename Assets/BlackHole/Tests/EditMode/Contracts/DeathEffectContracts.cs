using System;
using System.Collections.Generic;

namespace BlackHole.Core.Tests
{
    // 사망 효과: 적 종류의 특성, Step의 4. Death Effect 자리, 연쇄 번개와 폭발(GAME_RULES 10·13절, SKILL_SYSTEM_PLAN SK-005).
    // 계약은 반경을 판 전체보다 크게 잡아, 적의 무작위 배치와 관계없이 누가 맞는지 정해지게 한다.
    // 효과 보유 적은 Step 밖에서 World.DealDamage로 죽이고, 그 효과는 다음 Step의 4 자리에서 처리된다.
    internal static class DeathEffectContracts
    {
        private const string Normal = "normal";
        private const string Electric = "electric";
        private const string Explosive = "explosive";
        private static readonly Damage Kill = new Damage(1, TestContent.First);

        public static IEnumerable<Contract> Cases()
        {
            yield return new Contract("Death.EffectsAreValidatedAtLoad", EffectsAreValidatedAtLoad);
            yield return new Contract("Death.EffectDamageSkipsEffectOwners", EffectDamageSkipsEffectOwners);
            yield return new Contract("Death.ChainEndsAtHopLimitWithoutRevisit", ChainEndsAtHopLimitWithoutRevisit);
            yield return new Contract("Death.EffectKillsCountInTheSameStep", EffectKillsCountInTheSameStep);
        }

        // 효과 종류 이름과 수치를 검사해 경로와 함께 보고한다. 종류 이름이 비어 있으면 효과가 없다.
        private static void EffectsAreValidatedAtLoad()
        {
            ContentData data = TestContent.Data();
            data.Enemies.Add(WithEffect(Electric, 1, new DeathEffectData { Kind = "ChainLightning", Damage = 1, Radius = 1, MaxTargets = 0 }));
            data.Enemies.Add(WithEffect("frozen", 1, new DeathEffectData { Kind = "Freeze" }));
            data.Enemies.Add(WithEffect(Normal, 1, new DeathEffectData { Kind = string.Empty }));
            data.Enemies.Add(WithEffect("moon", 1, new DeathEffectData { Kind = "AttackHaste", Duration = 5, IntervalMultiplier = 1 }));
            data.Enemies.Add(WithEffect("comet", 1, new DeathEffectData { Kind = "GuaranteedCritical", Duration = 0 }));
            ContentLoadResult result = ContentLoader.Load(data);
            Expect.True(!result.Succeeded, "잘못된 사망 효과로는 조립할 수 없다.");
            TestContent.HasDiagnostic(result, $"Enemies[{Electric}].DeathEffect", "1부터");
            TestContent.HasDiagnostic(result, "Enemies[frozen].DeathEffect.Kind", "Freeze");
            TestContent.HasDiagnostic(result, "Enemies[moon].DeathEffect", "1보다 작은");
            TestContent.HasDiagnostic(result, "Enemies[comet].DeathEffect", "양수");
            Expect.Equal(4, result.Diagnostics.Count);
        }

        // 폭발과 연쇄 번개는 효과를 가진 적에게 피해를 주지 않는다. 효과 없는 적만 맞는다.
        private static void EffectDamageSkipsEffectOwners()
        {
            GameSession game = TestContent.Session(Arena(normals: 3, electrics: 1, explosives: 2, normalHealth: 100));
            World world = game.World;
            List<Enemy> explosives = Of(world, Explosive);
            Enemy electric = Of(world, Electric)[0];

            world.DealDamage(explosives[0], Kill);
            game.Advance(0.01f);
            Expect.Equal(1, world.DeathEffects.Explosions.Count);
            Expect.Equal(3, world.DeathEffects.Explosions[0].HitCount);
            Expect.True(electric.IsAlive && explosives[1].IsAlive, "효과를 가진 적은 폭발에 맞지 않는다.");

            foreach (Enemy normal in Of(world, Normal))
                Expect.Near(93, normal.Health);

            world.DealDamage(electric, Kill);
            game.Advance(0.01f);
            Expect.Equal(3, world.DeathEffects.LightningHits.Count);
            Expect.True(explosives[1].IsAlive, "효과를 가진 적은 번개에 맞지 않는다.");

            foreach (Enemy normal in Of(world, Normal))
                Expect.Near(88, normal.Health);
        }

        // 번개는 가장 가까운 적부터 옮겨 가고, 한 번 맞힌 적은 다시 맞히지 않으며, 최대 횟수에서 멈춘다.
        // 옮겨 갈 적이 반경 안에 없으면 거기서 끝난다.
        private static void ChainEndsAtHopLimitWithoutRevisit()
        {
            GameSession game = TestContent.Session(Arena(normals: 5, electrics: 2, explosives: 0, normalHealth: 100, chainTargets: 3));
            World world = game.World;
            List<Enemy> electrics = Of(world, Electric);
            Point2 origin = electrics[0].Position;

            world.DealDamage(electrics[0], Kill);
            game.Advance(0.01f);
            IReadOnlyList<LightningHit> hits = world.DeathEffects.LightningHits;
            Expect.Equal(3, hits.Count);

            var struck = new HashSet<Enemy>();

            foreach (LightningHit hit in hits)
            {
                Expect.Equal(origin, hit.From);
                Enemy nearest = Nearest(Of(world, Normal), origin, struck);
                Expect.Equal(nearest.Position, hit.To);
                struck.Add(nearest);
                origin = hit.To;
            }

            foreach (Enemy normal in Of(world, Normal))
                Expect.Near(struck.Contains(normal) ? 95 : 100, normal.Health);

            // 두 번째 번개: 최대 횟수(3)보다 적이 많지 않게, 효과 없는 적을 둘만 남긴다.
            List<Enemy> normals = Of(world, Normal);
            world.DealDamage(normals[0], new Damage(1000, TestContent.First));
            world.DealDamage(normals[1], new Damage(1000, TestContent.First));
            world.DealDamage(normals[2], new Damage(1000, TestContent.First));
            world.DealDamage(electrics[1], Kill);
            game.Advance(0.01f);
            Expect.Equal(2, world.DeathEffects.LightningHits.Count);
        }

        // 효과로 죽은 적도 같은 Step의 사망이다: 사망 기록과 처치 수에 들고, 그 Step이 끝나면 대기열이 비어 있다.
        // 파괴 요청으로 죽은 효과 보유 적은 효과를 내지 않는다. 판이 끝나면 처리되지 않은 효과는 버린다.
        private static void EffectKillsCountInTheSameStep()
        {
            ContentData data = Arena(normals: 3, electrics: 0, explosives: 3, normalHealth: 5);
            data.Breaker = new BreakerData { Damage = 1, Interval = 100, Radius = 100, CritMultiplier = 1 };
            GameSession game = TestContent.Session(data);
            World world = game.World;
            game.SetAimPoint(TestContent.First, BattleSpace.Origin);

            // Breaker가 모두를 1씩 친다: 효과 보유 적 셋은 죽고, 그 폭발(7)이 남은 HP 4의 일반 적을 죽인다.
            game.Advance(0.01f);
            Expect.Equal(0, world.Enemies.Count);
            Expect.Equal(6, world.Deaths.Count);
            Expect.Equal(6, world.TotalKills);
            Expect.Equal(3, world.DeathEffects.Explosions.Count);
            Expect.Equal(Explosive, world.Deaths[0].EnemyTypeId);
            Expect.Equal(Normal, world.Deaths[5].EnemyTypeId);
            Expect.True(!world.HasPendingDeathProcessing, "Step이 끝나면 사망 효과 대기열이 비어 있다.");

            GameSession destroyed = TestContent.Session(Arena(normals: 2, electrics: 0, explosives: 2, normalHealth: 5));
            List<Enemy> explosives = Of(destroyed.World, Explosive);
            destroyed.World.RequestDestroy(explosives[0]);
            destroyed.Advance(0.01f);
            Expect.True(!explosives[0].IsAlive, "파괴 요청으로 죽는다.");
            Expect.Equal(0, destroyed.World.DeathEffects.Explosions.Count);

            destroyed.World.DealDamage(explosives[1], Kill);
            Expect.True(destroyed.World.HasPendingDeathProcessing, "Step 밖에서 죽은 효과 보유 적의 효과는 다음 Step까지 남는다.");
            destroyed.RequestEnd(SessionEndReason.TimeExpired);
            destroyed.ClearRemainingEnemies();
            Expect.True(!destroyed.World.HasPendingDeathProcessing, "판 정리가 처리되지 않은 효과를 버린다.");
        }

        // 일반 적, 전기(연쇄 번개), 폭발 적이 HQ 둘레 [2, 4] 띠에 나오는 판. 효과의 반경은 판 전체보다 크다.
        private static ContentData Arena(int normals, int electrics, int explosives, float normalHealth, int chainTargets = 10)
        {
            var supply = new List<SupplyData> { TestContent.Supply(Normal, normals) };

            if (electrics > 0)
                supply.Add(TestContent.Supply(Electric, electrics));

            if (explosives > 0)
                supply.Add(TestContent.Supply(Explosive, explosives));

            ContentData data = TestContent.Arena(2, 4, supply.ToArray());
            data.Enemies.Add(TestContent.Enemy(Normal, normalHealth));
            data.Enemies.Add(WithEffect(Electric, 1,
                new DeathEffectData { Kind = "ChainLightning", Damage = 5, Radius = 100, MaxTargets = chainTargets }));
            data.Enemies.Add(WithEffect(Explosive, 1, new DeathEffectData { Kind = "Explosion", Damage = 7, Radius = 100 }));
            TestContent.Allow(data, Normal);
            TestContent.Allow(data, Electric);
            TestContent.Allow(data, Explosive);
            return data;
        }

        private static EnemyData WithEffect(string id, float health, DeathEffectData effect)
        {
            EnemyData enemy = TestContent.Enemy(id, health);
            enemy.DeathEffect = effect;
            return enemy;
        }

        // 살아 있는 그 종류의 적(출현 순서).
        private static List<Enemy> Of(World world, string kind)
        {
            var found = new List<Enemy>();

            foreach (Enemy enemy in world.Enemies)
            {
                if (enemy.Definition.Id == kind)
                    found.Add(enemy);
            }

            return found;
        }

        private static Enemy Nearest(List<Enemy> candidates, Point2 origin, HashSet<Enemy> skip)
        {
            Enemy nearest = null;

            foreach (Enemy enemy in candidates)
            {
                if (skip.Contains(enemy))
                    continue;

                if (nearest == null || origin.DistanceSquared(enemy.Position) < origin.DistanceSquared(nearest.Position))
                    nearest = enemy;
            }

            return nearest ?? throw new InvalidOperationException("후보가 없다.");
        }
    }
}
