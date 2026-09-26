using System;
using System.Collections.Generic;

namespace BlackHole.Core.Tests
{
    // 적: 전투 시작 공급·풀 여과·배치, HQ 공전, 피해와 사망 확정, 생성·파괴 요청, 판 정리(GAME_RULES 7~9·12·13절).
    // 공급된 적은 판의 단계 풀을 거쳐서만 나오므로, 계약은 공급하는 종류를 기본 풀에 넣는다(TestContent.Allow).
    internal static class EnemyContracts
    {
        private static readonly Damage Hit = new Damage(4, TestContent.First);

        public static IEnumerable<Contract> Cases()
        {
            yield return new Contract("Enemy.StartSupplyPlacesEveryRequestInsideBand", StartSupplyPlacesEveryRequestInsideBand);
            yield return new Contract("Enemy.PlacementIsReproducibleBySeed", PlacementIsReproducibleBySeed);
            yield return new Contract("Enemy.OrbitsHqKeepingDistance", OrbitsHqKeepingDistance);
            yield return new Contract("Enemy.DeathIsConfirmedOnceAndLeavesTheBattle", DeathIsConfirmedOnceAndLeavesTheBattle);
            yield return new Contract("Enemy.DeadEnemiesNoLongerMove", DeadEnemiesNoLongerMove);
            yield return new Contract("Enemy.DeathRecordsLastOneAdvance", DeathRecordsLastOneAdvance);
            yield return new Contract("Enemy.BattleCleanupIsNotAKill", BattleCleanupIsNotAKill);
            yield return new Contract("Enemy.DeathEarnsItsGoldForTheBattleAtOnce", DeathEarnsItsGoldForTheBattleAtOnce);
            yield return new Contract("Enemy.EachBattleHasItsOwnEnemies", EachBattleHasItsOwnEnemies);
            yield return new Contract("Enemy.PoolFilterDropsKindsOutsideThePool", PoolFilterDropsKindsOutsideThePool);
            yield return new Contract("Enemy.PoolFilterCapsAliveCountPerKind", PoolFilterCapsAliveCountPerKind);
            yield return new Contract("Enemy.BattleUsesItsStagePool", BattleUsesItsStagePool);
            yield return new Contract("Enemy.SpawnsTakeTheBattleStats", SpawnsTakeTheBattleStats);
            yield return new Contract("Enemy.TierStatsComeFromTheMassLevel", TierStatsComeFromTheMassLevel);
            yield return new Contract("Enemy.TierRatioHoldsAtEveryCount", TierRatioHoldsAtEveryCount);
            yield return new Contract("Enemy.KillsAreTalliedByKind", KillsAreTalliedByKind);
            yield return new Contract("Enemy.SpawnRequestsWaitForTheNextStep", SpawnRequestsWaitForTheNextStep);
            yield return new Contract("Enemy.FilteredSpawnRequestsAreDropped", FilteredSpawnRequestsAreDropped);
            yield return new Contract("Enemy.RejectsRequestsTheBattleCannotTake", RejectsRequestsTheBattleCannotTake);
            yield return new Contract("Enemy.DestroyRequestsConfirmDeathAtTheNextStep", DestroyRequestsConfirmDeathAtTheNextStep);
            yield return new Contract("Enemy.DeathsComeBeforeSupplyInAStep", DeathsComeBeforeSupplyInAStep);
        }

        // 생성 요청은 쌓였다가 다음 Step의 공급 처리 때 적이 된다. 정지 중에는 Step이 없으므로 계속 쌓여 있다.
        private static void SpawnRequestsWaitForTheNextStep()
        {
            GameSession game = TestContent.Session(OneEnemy(health: 10));
            World world = game.World;
            EnemyDefinition kind = world.Enemies[0].Definition;

            world.RequestSpawn(new SupplyRequest(kind, 2));
            Expect.Equal(1, world.Enemies.Count);
            Expect.Equal(1, world.PendingSpawns.Count);

            game.TogglePause();
            game.Advance(0.1f);
            Expect.Equal(1, world.Enemies.Count);
            Expect.Equal(1, world.PendingSpawns.Count);

            game.TogglePause();
            game.Advance(0.1f);
            Expect.Equal(3, world.Enemies.Count);
            Expect.Equal(3, world.CountAlive(kind));
            Expect.Equal(0, world.PendingSpawns.Count);

            foreach (Enemy enemy in world.Enemies)
                Expect.Near(3, TestContent.DistanceToHq(enemy.Position));
        }

        // 풀 여과 장치가 거른 생성 요청은 버린다. 나중에 자리가 나도 다시 나오지 않는다.
        private static void FilteredSpawnRequestsAreDropped()
        {
            ContentData data = TestContent.Arena(2, 4, TestContent.Supply(TestContent.EnemyId, 2));
            data.Enemies.Add(TestContent.Enemy(TestContent.EnemyId));
            TestContent.Allow(data, TestContent.EnemyId, maxAlive: 2);
            GameSession game = TestContent.Session(data);
            World world = game.World;

            world.RequestSpawn(new SupplyRequest(world.Enemies[0].Definition, 1));
            game.Advance(0.1f);
            Expect.Equal(2, world.Enemies.Count);
            Expect.Equal(0, world.PendingSpawns.Count);

            world.RequestDestroy(world.Enemies[0]);
            game.Advance(0.1f);
            Expect.Equal(1, world.Enemies.Count);
        }

        // 판이 만들 수 없는 적의 생성 요청(이 판의 종류가 아님, 출현 배치 없음)과 빈 파괴 요청은 요청 때 거부한다.
        private static void RejectsRequestsTheBattleCannotTake()
        {
            World world = TestContent.Session(OneEnemy(health: 10)).World;
            EnemyDefinition stranger = TestContent.Stranger();
            Expect.Throws<ArgumentException>(() => world.RequestSpawn(new SupplyRequest(stranger, 1)));
            Expect.Throws<ArgumentException>(() => world.RequestSpawn(default));
            Expect.Throws<ArgumentNullException>(() => world.RequestDestroy(null));
            Expect.Equal(0, world.PendingSpawns.Count);
            Expect.Equal(0, world.PendingDestroys.Count);

            World bare = TestContent.Session(TestContent.Data()).World;
            EnemyDefinition poolKind = bare.Pool.Entries[0].Enemy;
            Expect.Throws<InvalidOperationException>(() => bare.RequestSpawn(new SupplyRequest(poolKind, 1)));
        }

        // 파괴 요청은 쌓였다가 다음 Step의 사망 처리 때 사망을 확정한다. 피해·HP를 계산하지 않고,
        // 피해로 죽을 때와 같은 사망 절차(목록에서 제외, 사망 기록, 처치 수)를 거친다.
        // 같은 적의 두 번째 요청과 이미 죽은 적의 요청은 아무것도 하지 않는다.
        private static void DestroyRequestsConfirmDeathAtTheNextStep()
        {
            ContentData data = TestContent.Arena(3, 3, TestContent.Supply(TestContent.EnemyId, 2));
            data.Enemies.Add(TestContent.Enemy(TestContent.EnemyId, health: 10));
            TestContent.Allow(data, TestContent.EnemyId);
            GameSession game = TestContent.Session(data);
            World world = game.World;
            Enemy target = world.Enemies[0];

            world.RequestDestroy(target);
            world.RequestDestroy(target);
            Expect.True(target.IsAlive, "요청만으로는 죽지 않는다.");
            Expect.Equal(2, world.Enemies.Count);
            Expect.Equal(2, world.PendingDestroys.Count);

            game.Advance(0.1f);
            Expect.True(!target.IsAlive, "다음 Step에 죽어야 한다.");
            Expect.Equal(1, world.Enemies.Count);
            Expect.Equal(1, world.CountAlive(target.Definition));
            Expect.Equal(1, world.Deaths.Count);
            Expect.Equal(target.Id, world.Deaths[0].EnemyId);
            Expect.Equal(1, world.TotalKills);
            Expect.Equal(0, world.PendingDestroys.Count);

            world.RequestDestroy(target);
            game.Advance(0.1f);
            Expect.Equal(0, world.Deaths.Count);
            Expect.Equal(1, world.TotalKills);
        }

        // 한 Step에서 사망 처리가 공급 처리보다 먼저다(GAME_RULES 13절). 죽어서 비운 자리에 같은 Step의 생성이 들어간다.
        private static void DeathsComeBeforeSupplyInAStep()
        {
            ContentData data = TestContent.Arena(2, 4, TestContent.Supply(TestContent.EnemyId, 1));
            data.Enemies.Add(TestContent.Enemy(TestContent.EnemyId));
            TestContent.Allow(data, TestContent.EnemyId, maxAlive: 1);
            GameSession game = TestContent.Session(data);
            World world = game.World;
            Enemy old = world.Enemies[0];

            world.RequestSpawn(new SupplyRequest(old.Definition, 1));
            world.RequestDestroy(old);
            game.Advance(0.1f);

            Expect.Equal(1, world.Enemies.Count);
            Expect.True(!ReferenceEquals(old, world.Enemies[0]), "비운 자리에 새 적이 나와야 한다.");
            Expect.Equal(1, world.TotalKills);
        }

        // 판의 적 수치는 조립 때 정해지고, 출현하는 적은 그 수치를 받는다. 질량 단계 0(계수 1)이면 색 등급의 기본값과 같다.
        // 판에 없는 종류의 수치는 묻지 않는다.
        private static void SpawnsTakeTheBattleStats()
        {
            GameSession game = TestContent.Session(OneEnemy(health: 12));
            Enemy enemy = game.World.Enemies[0];
            EnemyStats battle = game.World.Stats.Of(enemy.Definition, enemy.Tier);

            Expect.Near(battle.MaxHealth, enemy.Stats.MaxHealth);
            Expect.Near(battle.Size, enemy.Stats.Size);
            Expect.Near(battle.MoveSpeed, enemy.Stats.MoveSpeed);
            Expect.Near(enemy.Definition.Tiers[enemy.Tier].MaxHealth, battle.MaxHealth);

            Expect.Throws<ArgumentException>(() => game.World.Stats.Of(TestContent.Stranger(), 0));
        }

        // 판 조립이 받은 질량 단계가 그 종류의 색 비율과 HP·Gold 계수를 정한다. 같은 판의 같은 색은 수치가 정확히 같다.
        // 질량 단계를 주지 않은 종류는 0단계다. 범위 밖의 단계나 판에 없는 종류의 단계는 조립이 거부한다.
        private static void TierStatsComeFromTheMassLevel()
        {
            ContentData data = TestContent.Arena(3, 3, TestContent.Supply(TestContent.EnemyId, 4));
            EnemyData kindData = TestContent.Tiered(TestContent.EnemyId, 1, false,
                TestContent.Tier(10, 0.2f, 3),
                TestContent.Tier(20, 0.3f, 5));
            kindData.MassLevels.Add(TestContent.MassLevel(1, 1, 1, 0));
            kindData.MassLevels.Add(TestContent.MassLevel(2, 3, 0, 1));
            data.Enemies.Add(kindData);
            TestContent.Allow(data, TestContent.EnemyId);
            GameContent content = TestContent.Load(data);
            content.TryGetEnemy(TestContent.EnemyId, out EnemyDefinition kind);

            GameSession plain = TestContent.Begun(SessionAssembler.CreateBattle(content, new[] { new PlayerState(TestContent.First) }));
            Expect.Equal(0, plain.World.Stats.MassLevelOf(kind));

            foreach (Enemy enemy in plain.World.Enemies)
            {
                Expect.Equal(0, enemy.Tier);
                Expect.Near(10, enemy.Stats.MaxHealth);
                Expect.Equal(3L, enemy.Stats.Gold);
            }

            var levels = new Dictionary<EnemyDefinition, int> { { kind, 1 } };
            GameSession massive = TestContent.Begun(
                SessionAssembler.CreateBattle(content, new[] { new PlayerState(TestContent.First) }, 1, 0, levels));
            Expect.Equal(1, massive.World.Stats.MassLevelOf(kind));
            Expect.Equal(4, massive.World.Enemies.Count);

            foreach (Enemy enemy in massive.World.Enemies)
            {
                Expect.Equal(1, enemy.Tier);
                Expect.Near(40, enemy.Stats.MaxHealth);
                Expect.Near(0.3f, enemy.Stats.Size);
                Expect.Equal(15L, enemy.Stats.Gold);
            }

            var state = new PlayerState(TestContent.First);
            Expect.Throws<ArgumentOutOfRangeException>(() => SessionAssembler.CreateBattle(
                content, new[] { state }, 1, 0, new Dictionary<EnemyDefinition, int> { { kind, 2 } }));
            Expect.Throws<ArgumentException>(() => SessionAssembler.CreateBattle(
                content, new[] { state }, 1, 0, new Dictionary<EnemyDefinition, int> { { TestContent.Stranger(), 0 } }));
            Expect.True(!state.InBattle, "실패한 조립이 PlayerState를 묶으면 안 된다.");
        }

        // 색 비율은 몫 방식으로 지킨다: 색이 둘이면 몇 마리를 공급한 시점이든 색마다 (비율 × 공급 수)에서 1마리 넘게 벗어나지 않는다.
        // 비율이 0인 색은 나오지 않는다. 같은 seed면 같은 색 순서가 나오고, 색 비율을 바꿔도 출현 위치의 순서는 같다(난수 스트림이 다르다).
        private static void TierRatioHoldsAtEveryCount()
        {
            const int count = 25;
            GameSession mixed = TierSession(count, seed: 7, 0.3f, 0.7f);
            int first = 0;

            for (int i = 0; i < count; i++)
            {
                if (mixed.World.Enemies[i].Tier == 0)
                    first++;

                float expected = 0.3f * (i + 1);
                Expect.True(Math.Abs(first - expected) < 1, $"{i + 1}마리째에 0번 색 {first}마리, 기대 {expected}.");
            }

            GameSession again = TierSession(count, seed: 7, 0.3f, 0.7f);
            GameSession even = TierSession(count, seed: 7, 0.5f, 0.5f);
            GameSession single = TierSession(count, seed: 7, 0f, 1f);

            for (int i = 0; i < count; i++)
            {
                Expect.Equal(mixed.World.Enemies[i].Tier, again.World.Enemies[i].Tier);
                Expect.Equal(mixed.World.Enemies[i].Position, even.World.Enemies[i].Position);
                Expect.Equal(1, single.World.Enemies[i].Tier);
            }
        }

        // 색 등급 둘(비율만 다름)인 종류를 count마리 공급한 판.
        private static GameSession TierSession(int count, int seed, float first, float second)
        {
            ContentData data = TestContent.Arena(1, 3, TestContent.Supply(TestContent.EnemyId, count));
            EnemyData kind = TestContent.Tiered(TestContent.EnemyId, 1, false, TestContent.Tier(10, 0.2f, 1), TestContent.Tier(10, 0.2f, 1));
            kind.MassLevels.Add(TestContent.MassLevel(1, 1, first, second));
            data.Enemies.Add(kind);
            TestContent.Allow(data, TestContent.EnemyId);
            return TestContent.Session(data, seed);
        }

        // 공급이 요청해도 이 판의 단계 풀에 없는 종류는 나오지 않는다. 걸러진 요청은 버린다.
        private static void PoolFilterDropsKindsOutsideThePool()
        {
            ContentData data = TestContent.Arena(2, 4, TestContent.Supply("inside", 2), TestContent.Supply("outside", 3));
            data.Enemies.Add(TestContent.Enemy("inside"));
            data.Enemies.Add(TestContent.Enemy("outside"));
            TestContent.Allow(data, "inside");
            GameSession game = TestContent.Session(data);

            Expect.Equal(2, game.World.Enemies.Count);
            Expect.Equal(2, game.World.CountAlive(game.World.Enemies[0].Definition));
            foreach (Enemy enemy in game.World.Enemies)
                Expect.Equal("inside", enemy.Definition.Id);
        }

        // 한 종류가 동시에 살아 있을 수 있는 수는 풀의 최대 수까지다. 살아 있는 수는 사망 때 준다.
        private static void PoolFilterCapsAliveCountPerKind()
        {
            ContentData data = TestContent.Arena(2, 4, TestContent.Supply(TestContent.EnemyId, 5));
            data.Enemies.Add(TestContent.Enemy(TestContent.EnemyId, health: 1));
            TestContent.Allow(data, TestContent.EnemyId, maxAlive: 3);
            GameSession game = TestContent.Session(data);

            EnemyDefinition kind = game.World.Enemies[0].Definition;
            Expect.Equal(3, game.World.Enemies.Count);
            Expect.Equal(3, game.World.CountAlive(kind));

            game.World.DealDamage(game.World.Enemies[0], Hit);
            Expect.Equal(2, game.World.CountAlive(kind));
            Expect.Equal(2, game.World.Enemies.Count);
        }

        // 판은 자신을 조립한 단계의 풀을 쓴다. 같은 공급이라도 단계가 다르면 나오는 적이 다르다.
        private static void BattleUsesItsStagePool()
        {
            ContentData data = TestContent.Arena(2, 4, TestContent.Supply("small", 2), TestContent.Supply("big", 2));
            data.Enemies.Add(TestContent.Enemy("small"));
            data.Enemies.Add(TestContent.Enemy("big"));
            TestContent.Allow(data, "small");
            data.EnemyPools.Add(TestContent.Pool("late", "big"));
            data.Stages[1].Pool = "late";
            GameContent content = TestContent.Load(data);

            GameSession early = TestContent.Begun(
                SessionAssembler.CreateBattle(content, new[] { new PlayerState(TestContent.First) }, 1, 0));
            GameSession late = TestContent.Begun(
                SessionAssembler.CreateBattle(content, new[] { new PlayerState(TestContent.First) }, 2, 0));

            Expect.Equal(TestContent.PoolId, early.World.Pool.Id);
            Expect.Equal("late", late.World.Pool.Id);
            Expect.Equal("small", early.World.Enemies[0].Definition.Id);
            Expect.Equal(2, early.World.Enemies.Count);
            Expect.Equal("big", late.World.Enemies[0].Definition.Id);
            Expect.Equal(2, late.World.Enemies.Count);
        }

        // 요청마다 정해진 수가 요청 순서대로 나온다. 모두 HQ로부터 띠 [2, 4] 안에 있고, 체력은 가득 차 있다.
        private static void StartSupplyPlacesEveryRequestInsideBand()
        {
            ContentData data = TestContent.Arena(2, 4, TestContent.Supply("a", 3), TestContent.Supply("b", 2));
            data.Enemies.Add(TestContent.Enemy("a", health: 10));
            data.Enemies.Add(TestContent.Enemy("b", health: 25));
            TestContent.Allow(data, "a");
            TestContent.Allow(data, "b");
            GameSession game = TestContent.Session(data);

            IReadOnlyList<Enemy> enemies = game.World.Enemies;
            Expect.Equal(5, enemies.Count);

            for (int i = 0; i < enemies.Count; i++)
            {
                Enemy enemy = enemies[i];
                Expect.Equal(i < 3 ? "a" : "b", enemy.Definition.Id);
                Expect.Equal(i + 1, enemy.Id.Value);
                Expect.True(enemy.IsAlive, "처음에는 살아 있어야 한다.");
                Expect.Near(enemy.Stats.MaxHealth, enemy.Health);

                float distance = TestContent.DistanceToHq(enemy.Position);
                Expect.True(distance >= 2 - 0.0001f && distance <= 4 + 0.0001f, $"띠 밖에 나왔다: {distance}");
            }
        }

        // 같은 seed의 판은 같은 자리에, 다른 seed의 판은 다른 자리에 적을 둔다.
        private static void PlacementIsReproducibleBySeed()
        {
            ContentData data = TestContent.Arena(1, 5, TestContent.Supply(TestContent.EnemyId, 4));
            data.Enemies.Add(TestContent.Enemy(TestContent.EnemyId));
            TestContent.Allow(data, TestContent.EnemyId);

            IReadOnlyList<Enemy> first = TestContent.Session(data, seed: 7).World.Enemies;
            IReadOnlyList<Enemy> again = TestContent.Session(data, seed: 7).World.Enemies;
            IReadOnlyList<Enemy> other = TestContent.Session(data, seed: 8).World.Enemies;

            bool differs = false;

            for (int i = 0; i < first.Count; i++)
            {
                Expect.Equal(first[i].Position, again[i].Position);
                differs |= !first[i].Position.Equals(other[i].Position);
            }

            Expect.True(differs, "다른 seed인데 배치가 모두 같다.");
        }

        // 원점으로부터의 거리를 유지하며 이동 속도만큼 원을 따라 돈다. 반시계 방향이 기본이다.
        private static void OrbitsHqKeepingDistance()
        {
            ContentData data = TestContent.Arena(3, 3,
                TestContent.Supply("ccw", 1), TestContent.Supply("cw", 1));
            data.Enemies.Add(TestContent.Enemy("ccw", speed: 1.5f));
            data.Enemies.Add(TestContent.Enemy("cw", speed: 1.5f, clockwise: true));
            TestContent.Allow(data, "ccw");
            TestContent.Allow(data, "cw");
            GameSession game = TestContent.Session(data);

            Enemy ccw = game.World.Enemies[0];
            Enemy cw = game.World.Enemies[1];
            Point2 ccwStart = ccw.Position;
            Point2 cwStart = cw.Position;

            game.Advance(0.5f);

            // 반지름 3에서 초당 거리 1.5는 초당 0.5라디안이다. 0.5초면 0.25라디안 돈다.
            Expect.Near(3, TestContent.DistanceToHq(ccw.Position));
            Expect.Near(3, TestContent.DistanceToHq(cw.Position));
            Expect.Near(0.25f, SignedTurn(ccwStart, ccw.Position));
            Expect.Near(-0.25f, SignedTurn(cwStart, cw.Position));
        }

        // HP가 0 이하가 되는 순간 한 번만 죽는다. 그 즉시 판의 적 목록에서 빠지고 사망 기록이 하나 남는다.
        // 죽은 적에게 준 피해는 아무것도 바꾸지 않는다.
        private static void DeathIsConfirmedOnceAndLeavesTheBattle()
        {
            GameSession game = TestContent.Session(OneEnemy(health: 10));
            World world = game.World;
            Enemy enemy = world.Enemies[0];

            Expect.True(!world.DealDamage(enemy, Hit), "HP가 남으면 죽지 않는다.");
            Expect.Near(6, enemy.Health);
            Expect.Equal(TestContent.First, enemy.LastDamageSource.Value);
            Expect.Equal(0, world.Deaths.Count);

            Expect.True(world.DealDamage(enemy, new Damage(6, TestContent.First)), "HP가 0이 되면 죽는다.");
            Expect.True(!enemy.IsAlive, "죽은 상태여야 한다.");
            Expect.Equal(0, world.Enemies.Count);
            Expect.Equal(1, world.Deaths.Count);
            Expect.Equal(enemy.Id, world.Deaths[0].EnemyId);
            Expect.Equal(TestContent.EnemyId, world.Deaths[0].EnemyTypeId);
            Expect.Equal(enemy.Position, world.Deaths[0].Position);

            Expect.True(!world.DealDamage(enemy, Hit), "다시 죽으면 안 된다.");
            Expect.Near(0, enemy.Health);
            Expect.Equal(1, world.Deaths.Count);

            Expect.Throws<ArgumentOutOfRangeException>(() => new Damage(0, TestContent.First));
        }

        // 사망이 확정되는 순간 그 적의 Gold가 이 판의 합계에 한 번 더해진다. 피해 사망과 파괴 요청 사망이 같다.
        // 이미 죽은 적은 다시 더하지 않고, 전투 정리로 치운 적은 더하지 않는다(GAME_RULES 9절).
        // 판은 진행 상태의 Gold를 바꾸지 않는다. 번 Gold는 원자료에 남고, 진행 상태에 더하는 결산은 판 바깥이 한다.
        private static void DeathEarnsItsGoldForTheBattleAtOnce()
        {
            ContentData data = TestContent.Arena(3, 3, TestContent.Supply(TestContent.EnemyId, 3));
            data.Enemies.Add(TestContent.Enemy(TestContent.EnemyId, health: 10, gold: 7));
            TestContent.Allow(data, TestContent.EnemyId);
            var state = new PlayerState(TestContent.First);
            state.EarnGold(20);
            GameSession game = TestContent.Begun(SessionAssembler.CreateBattle(TestContent.Load(data), new[] { state }));
            World world = game.World;
            Enemy hit = world.Enemies[0];
            Enemy destroyed = world.Enemies[1];

            world.DealDamage(hit, new Damage(10, TestContent.First));
            Expect.Equal(7L, world.EarnedGold);
            world.DealDamage(hit, Hit);
            Expect.Equal(7L, world.EarnedGold);

            world.RequestDestroy(destroyed);
            game.Advance(0.1f);
            Expect.Equal(14L, world.EarnedGold);

            game.RequestEnd(SessionEndReason.TimeExpired);
            Expect.Equal(1, game.ClearRemainingEnemies());
            Expect.Equal(14L, world.EarnedGold);
            Expect.Equal(14L, game.CreateRawData().EarnedGold);
            Expect.Equal(20L, state.Gold);
        }

        private static void DeadEnemiesNoLongerMove()
        {
            GameSession game = TestContent.Session(OneEnemy(health: 1));
            Enemy enemy = game.World.Enemies[0];
            game.World.DealDamage(enemy, Hit);
            Point2 where = enemy.Position;

            game.Advance(1);
            Expect.Equal(where, enemy.Position);
        }

        // 사망 기록은 그 사망이 일어난 진행 동안만 남는다. 화면·소리는 그 사이에 번호로 한 번씩 읽는다.
        private static void DeathRecordsLastOneAdvance()
        {
            GameSession game = TestContent.Session(OneEnemy(health: 1));
            game.World.DealDamage(game.World.Enemies[0], Hit);
            Expect.Equal(1, game.World.Deaths.Count);

            game.Advance(0.1f);
            Expect.Equal(0, game.World.Deaths.Count);
        }

        // 전투가 끝나 남은 적을 치우는 것은 처치가 아니다. 사망 기록도 처치 수도 만들지 않는다.
        // 처리되지 않은 생성·파괴 요청은 처리되지 않고 함께 버려진다. 남은 적은 끝난 판에서만 치울 수 있다.
        private static void BattleCleanupIsNotAKill()
        {
            GameSession ended = TestContent.Session(OneEnemy(health: 10));
            Enemy survivor = ended.World.Enemies[0];
            Expect.Throws<InvalidOperationException>(() => ended.ClearRemainingEnemies());

            ended.World.RequestSpawn(new SupplyRequest(survivor.Definition, 1));
            ended.World.RequestDestroy(survivor);
            ended.RequestEnd(SessionEndReason.TimeExpired);
            ended.Advance(0.1f);
            Expect.Equal(1, ended.ClearRemainingEnemies());
            Expect.Equal(0, ended.World.Enemies.Count);
            Expect.Equal(0, ended.World.PendingSpawns.Count);
            Expect.Equal(0, ended.World.PendingDestroys.Count);
            Expect.Equal(0, ended.World.CountAlive(survivor.Definition));
            Expect.Equal(0, ended.World.Deaths.Count);
            Expect.Equal(0, ended.World.TotalKills);
            Expect.True(survivor.IsAlive, "정리된 적은 죽은 것이 아니다.");

            ContentData timed = OneEnemy(health: 10);
            timed.Session.TimeLimit = 1;
            GameSession expired = TestContent.Session(timed);
            expired.Advance(2);
            Expect.Equal(SessionPhase.Ended, expired.Phase);
            Expect.Equal(0, expired.World.Deaths.Count);
            Expect.True(expired.World.Enemies[0].IsAlive, "시간이 끝나도 적은 죽지 않는다.");
        }

        // 확정된 사망은 종류별 처치 수가 되고, 끝난 판의 원자료에 처음 처치한 순서로 남는다.
        private static void KillsAreTalliedByKind()
        {
            ContentData data = TestContent.Arena(2, 4, TestContent.Supply("a", 3), TestContent.Supply("b", 1));
            data.Enemies.Add(TestContent.Enemy("a", health: 1));
            data.Enemies.Add(TestContent.Enemy("b", health: 1));
            TestContent.Allow(data, "a");
            TestContent.Allow(data, "b");
            GameSession game = TestContent.Session(data);

            World world = game.World;
            world.DealDamage(world.Enemies[3], Hit);
            world.DealDamage(world.Enemies[0], Hit);
            world.DealDamage(world.Enemies[0], Hit);
            Expect.Equal(3, world.TotalKills);
            Expect.True(!world.HasPendingDeathProcessing, "사망 처리는 확정 순간 끝나야 한다.");

            game.RequestEnd(SessionEndReason.TimeExpired);
            game.ClearRemainingEnemies();
            BattleRawData raw = game.CreateRawData();
            Expect.Equal(3, raw.TotalKills);
            Expect.Equal(2, raw.Kills.Count);
            Expect.Equal("b", raw.Kills[0].Enemy.Id);
            Expect.Equal(1, raw.Kills[0].Count);
            Expect.Equal("a", raw.Kills[1].Enemy.Id);
            Expect.Equal(2, raw.Kills[1].Count);
        }

        // 같은 콘텐츠로 만든 두 판은 적을 공유하지 않는다. 한 판의 피해가 다른 판에 닿지 않는다.
        private static void EachBattleHasItsOwnEnemies()
        {
            GameContent content = TestContent.Load(OneEnemy(health: 10));
            GameSession first = TestContent.Begun(SessionAssembler.CreateBattle(content, new[] { new PlayerState(TestContent.First) }));
            GameSession second = TestContent.Begun(SessionAssembler.CreateBattle(content, new[] { new PlayerState(TestContent.First) }));

            Expect.True(!ReferenceEquals(first.World, second.World), "World를 재사용하면 안 된다.");
            first.World.DealDamage(first.World.Enemies[0], Hit);
            Expect.Near(6, first.World.Enemies[0].Health);
            Expect.Near(10, second.World.Enemies[0].Health);
        }

        // HQ로부터 거리 3에 적 하나.
        private static ContentData OneEnemy(float health)
        {
            ContentData data = TestContent.Arena(3, 3, TestContent.Supply(TestContent.EnemyId, 1));
            data.Enemies.Add(TestContent.Enemy(TestContent.EnemyId, health: health));
            TestContent.Allow(data, TestContent.EnemyId);
            return data;
        }

        // 원점에서 본 from → to의 회전각(라디안). 반시계가 양수다.
        private static float SignedTurn(Point2 from, Point2 to) =>
            (float)Math.Atan2(from.X * to.Y - from.Y * to.X, from.X * to.X + from.Y * to.Y);
    }
}
