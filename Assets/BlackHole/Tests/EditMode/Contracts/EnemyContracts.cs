using System;
using System.Collections.Generic;

namespace BlackHole.Core.Tests
{
    // 적: 전투 시작 공급·배치, HQ 공전, 피해와 사망 확정, 판 정리(GAME_RULES 7~9·12절).
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
            yield return new Contract("Enemy.EachBattleHasItsOwnEnemies", EachBattleHasItsOwnEnemies);
        }

        // 요청마다 정해진 수가 요청 순서대로 나온다. 모두 HQ로부터 띠 [2, 4] 안에 있고, 체력은 가득 차 있다.
        private static void StartSupplyPlacesEveryRequestInsideBand()
        {
            ContentData data = TestContent.Arena(2, 4, TestContent.Supply("a", 3), TestContent.Supply("b", 2));
            data.Enemies.Add(TestContent.Enemy("a", health: 10));
            data.Enemies.Add(TestContent.Enemy("b", health: 25));
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

        // 전투가 끝나 남은 적이 사라지는 것은 처치가 아니다. 사망 기록을 만들지 않는다.
        private static void BattleCleanupIsNotAKill()
        {
            GameSession stopped = TestContent.Session(OneEnemy(health: 10));
            stopped.Stop();
            Expect.Equal(0, stopped.World.Deaths.Count);
            Expect.True(stopped.World.Enemies[0].IsAlive, "정리된 적은 죽은 것이 아니다.");

            ContentData timed = OneEnemy(health: 10);
            timed.Session.TimeLimit = 1;
            GameSession expired = TestContent.Session(timed);
            expired.Advance(2);
            Expect.Equal(SessionPhase.Ended, expired.Phase);
            Expect.Equal(0, expired.World.Deaths.Count);
            Expect.True(expired.World.Enemies[0].IsAlive, "시간이 끝나도 적은 죽지 않는다.");
        }

        // 같은 콘텐츠로 만든 두 판은 적을 공유하지 않는다. 한 판의 피해가 다른 판에 닿지 않는다.
        private static void EachBattleHasItsOwnEnemies()
        {
            GameContent content = TestContent.Load(OneEnemy(health: 10));
            GameSession first = SessionAssembler.CreateBattle(content, new[] { new PlayerState(TestContent.First) });
            GameSession second = SessionAssembler.CreateBattle(content, new[] { new PlayerState(TestContent.First) });

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
            return data;
        }

        // 원점에서 본 from → to의 회전각(라디안). 반시계가 양수다.
        private static float SignedTurn(Point2 from, Point2 to) =>
            (float)Math.Atan2(from.X * to.Y - from.Y * to.X, from.X * to.X + from.Y * to.Y);
    }
}
