using System;
using System.Collections.Generic;

namespace BlackHole.Core.Tests
{
    // 스킬: 판 안의 참가자(조준점·스킬), Breaker의 주기 공격, 관통 레이저의 예고·발사, 피해가 적 시스템으로 들어가는 길
    // (GAME_RULES 6·8·13절, CONTENT_DEFINITION 2.3, SKILL_SYSTEM_PLAN).
    // 공전은 HQ까지의 거리를 지키므로, HQ에 조준하면 적이 원 안에 있는지가 판 내내 같다. 계약은 이것으로 원 안팎을 정한다.
    // 레이저는 HQ에 조준하면 경로가 HQ를 지나는 지름이 된다. 적은 경계 원 안에 있으므로 경로까지의 거리는 그 직선까지의 거리다.
    internal static class SkillContracts
    {
        private const float Boundary = 10;

        public static IEnumerable<Contract> Cases()
        {
            yield return new Contract("Skill.SkillsAreOptionalButValidatedAtLoad", SkillsAreOptionalButValidatedAtLoad);
            yield return new Contract("Skill.BreakerHitsEveryAliveEnemyInsideAimRadius", BreakerHitsEveryAliveEnemyInsideAimRadius);
            yield return new Contract("Skill.DamageGoesThroughWorldOnce", DamageGoesThroughWorldOnce);
            yield return new Contract("Skill.NothingCarriesIntoNextBattle", NothingCarriesIntoNextBattle);
            yield return new Contract("Skill.LaserAimsAtTelegraphStartAndPiercesOnFire", LaserAimsAtTelegraphStartAndPiercesOnFire);
            yield return new Contract("Skill.LaserSkipsAimOutsideBoundaryAndStopsWithTheBattle", LaserSkipsAimOutsideBoundaryAndStopsWithTheBattle);
            yield return new Contract("Skill.LaserStartIsReproducibleAndSeparateFromSpawns", LaserStartIsReproducibleAndSeparateFromSpawns);
            yield return new Contract("Skill.DisabledSkillDropsItsTimerAndPendingShots", DisabledSkillDropsItsTimerAndPendingShots);
            yield return new Contract("Skill.BreakerCriticalIsOneRollPerTickOnItsOwnStream", BreakerCriticalIsOneRollPerTickOnItsOwnStream);
        }

        // Breaker의 치명타는 Tick마다 한 번 정해지고, 그 Tick에 맞은 적 모두가 같은 결과를 받는다.
        // 치명타 난수는 Breaker만 쓰므로, 치명타 확률이 달라도 같은 seed의 레이저 시작점은 같다.
        private static void BreakerCriticalIsOneRollPerTickOnItsOwnStream()
        {
            GameSession rolling = CriticalArena(critChance: 0.5f);
            GameSession never = CriticalArena(critChance: 0);
            int critical = 0;
            int plain = 0;

            for (int i = 0; i < 10; i++)
            {
                float step = i == 0 ? 0.1f : 1;
                rolling.Advance(step);
                never.Advance(step);

                BreakerTick tick = rolling.World.Players[0].Breaker.Ticks[0];
                float expected = rolling.World.Enemies[0].Health;

                foreach (Enemy enemy in rolling.World.Enemies)
                    Expect.Near(expected, enemy.Health);

                if (tick.IsCritical)
                    critical++;
                else
                    plain++;

                Expect.Equal(
                    never.World.Players[0].Laser.PendingShots[0].Start,
                    rolling.World.Players[0].Laser.PendingShots[0].Start);
            }

            Expect.True(critical > 0 && plain > 0, "확률 0.5면 이 seed의 10 Tick에 치명타와 아닌 Tick이 모두 있어야 한다.");
            Expect.Near(1000 - plain - 2 * critical, rolling.World.Enemies[0].Health);
        }

        // 끈 스킬은 공격하지 않고, 돌던 주기와 예고 중인 발사를 버린다(발사하지 않는다).
        // 다시 켜면 처음부터 돈다: 켠 뒤 첫 Step에 Breaker는 Tick하고 레이저는 새로 예고한다. 번호는 이어진다.
        private static void DisabledSkillDropsItsTimerAndPendingShots()
        {
            ContentData data = Arena(radius: 5, count: 3, health: 100, damage: 1);
            data.Laser = new LaserData { Damage = 1, Interval = 1, Width = 20, TelegraphDuration = 0.4f, BoundaryRadius = Boundary };
            GameSession game = TestContent.Session(data);
            BattlePlayer player = game.World.Players[0];
            game.SetAimPoint(TestContent.First, BattleSpace.Origin);

            game.Advance(0.1f);
            Expect.Equal(1, player.Breaker.TickCount);
            Expect.Equal(1, player.Laser.PendingShots.Count);

            player.Breaker.SetEnabled(false);
            player.Laser.SetEnabled(false);
            Expect.True(!player.Breaker.Enabled && !player.Laser.Enabled, "끈 스킬은 꺼져 있다.");
            Expect.Equal(0, player.Laser.PendingShots.Count);

            game.Advance(1.3f);
            Expect.Equal(1, player.Breaker.TickCount);
            Expect.Equal(0, player.Laser.FireCount);
            Expect.Equal(0, player.Laser.PendingShots.Count);

            foreach (Enemy enemy in game.World.Enemies)
                Expect.Near(99, enemy.Health);

            player.Breaker.SetEnabled(true);
            player.Laser.SetEnabled(true);
            game.Advance(0.1f);
            Expect.Equal(2, player.Breaker.TickCount);
            Expect.Equal(1, player.Laser.PendingShots.Count);
            Expect.Equal(2, player.Laser.PendingShots[0].Number);
            Expect.Near(0.3f, player.Laser.PendingShots[0].Remaining);
        }

        // 스킬 칸이 없으면 판에 그 스킬이 없고, 판은 그대로 돈다. 칸이 있으면 수치를 검사해 경로와 함께 보고한다.
        private static void SkillsAreOptionalButValidatedAtLoad()
        {
            GameSession without = TestContent.Session(TestContent.Data());
            Expect.True(without.World.Players[0].Breaker == null, "Breaker 칸이 없으면 참가자에게 Breaker가 없다.");
            Expect.True(without.World.Players[0].Laser == null, "레이저 칸이 없으면 참가자에게 레이저가 없다.");
            without.Advance(1);
            Expect.Near(1, without.Elapsed);

            ContentData data = TestContent.Data();
            data.Breaker = new BreakerData { Damage = 0, Interval = 1, Radius = 1, CritMultiplier = 1 };
            data.Laser = new LaserData { Damage = 1, Interval = 1, Width = 1, TelegraphDuration = 0.4f, BoundaryRadius = -1 };
            ContentLoadResult result = ContentLoader.Load(data);
            Expect.True(!result.Succeeded, "잘못된 수치의 스킬로는 조립할 수 없다.");
            TestContent.HasDiagnostic(result, "Breaker", "양수");
            TestContent.HasDiagnostic(result, "Laser", "양수");

            // 치명타 확률은 0 ~ 1, 치명타 배율은 1 이상이다.
            data.Laser = null;
            data.Breaker = new BreakerData { Damage = 1, Interval = 1, Radius = 1, CritChance = 1.5f, CritMultiplier = 2 };
            TestContent.HasDiagnostic(ContentLoader.Load(data), "Breaker", "0부터 1");
            data.Breaker = new BreakerData { Damage = 1, Interval = 1, Radius = 1, CritChance = 0, CritMultiplier = 0.5f };
            TestContent.HasDiagnostic(ContentLoader.Load(data), "Breaker", "1 이상");
        }

        // 첫 Step에 첫 Tick. 조준점이 없으면 빈 Tick이고 주기는 소비된다(미뤄 두지 않는다).
        // Tick마다 그때의 조준점 원 안에 있는 살아 있는 적 전부가 맞고, 밖의 적은 맞지 않는다.
        private static void BreakerHitsEveryAliveEnemyInsideAimRadius()
        {
            GameSession game = TestContent.Session(Arena(radius: 3, count: 12, health: 10, damage: 3));
            World world = game.World;
            BreakerSkill breaker = world.Players[0].Breaker;

            game.Advance(0.1f);
            Expect.Equal(1, breaker.TickCount);
            Expect.Equal(0, breaker.LastTickHitCount);
            Expect.Equal(1, breaker.Ticks.Count);
            Expect.True(!breaker.Ticks[0].Center.HasValue, "조준점이 없던 Tick은 중심이 없다.");

            foreach (Enemy enemy in world.Enemies)
                Expect.Near(10, enemy.Health);

            game.SetAimPoint(TestContent.First, BattleSpace.Origin);
            game.Advance(0.5f);
            Expect.Equal(1, breaker.TickCount);
            Expect.Equal(0, breaker.Ticks.Count);

            game.Advance(0.4f);
            Expect.Equal(2, breaker.TickCount);
            int inside = 0;

            foreach (Enemy enemy in world.Enemies)
            {
                bool isInside = TestContent.DistanceToHq(enemy.Position) <= 3;
                Expect.Near(isInside ? 7 : 10, enemy.Health);

                if (isInside)
                    inside++;
            }

            Expect.True(inside > 0 && inside < world.Enemies.Count, "이 seed의 배치에는 원 안과 밖의 적이 모두 있어야 한다.");
            Expect.Equal(inside, breaker.LastTickHitCount);

            BreakerTick tick = breaker.Ticks[0];
            Expect.Equal(2, tick.Number);
            Expect.Equal(BattleSpace.Origin, tick.Center.Value);
            Expect.Near(3, tick.Radius);
            Expect.Equal(inside, tick.HitCount);

            Expect.Throws<ArgumentException>(() => game.SetAimPoint(new PlayerId(99), null));
        }

        // 스킬의 피해는 World.DealDamage로 들어간다. 죽은 적은 그 순간 판에서 빠지고, 사망 기록과 처치 수는 적마다 한 번이다.
        // 두 참가자의 Breaker가 같은 Step에 같은 적을 겨누면, 먼저 공격한 참가자(판 조립 순서)의 Tick만 맞힌다.
        private static void DamageGoesThroughWorldOnce()
        {
            GameContent content = TestContent.Load(Arena(radius: 5, count: 6, health: 3, damage: 3));
            GameSession game = TestContent.Begun(SessionAssembler.CreateBattle(
                content, new[] { new PlayerState(TestContent.First), new PlayerState(TestContent.Second) }));
            game.SetAimPoint(TestContent.First, BattleSpace.Origin);
            game.SetAimPoint(TestContent.Second, BattleSpace.Origin);

            game.Advance(0.1f);
            World world = game.World;
            Expect.Equal(0, world.Enemies.Count);
            Expect.Equal(6, world.Deaths.Count);
            Expect.Equal(6, world.TotalKills);
            Expect.Equal(6, world.PlayerOf(TestContent.First).Breaker.LastTickHitCount);
            Expect.Equal(1, world.PlayerOf(TestContent.Second).Breaker.TickCount);
            Expect.Equal(0, world.PlayerOf(TestContent.Second).Breaker.LastTickHitCount);
        }

        // 끝난 판은 공격하지 않는다. 새 판은 새 참가자로 시작한다: 조준점이 없고 Tick은 처음부터다.
        private static void NothingCarriesIntoNextBattle()
        {
            GameContent content = TestContent.Load(Arena(radius: 5, count: 3, health: 10, damage: 1));
            var state = new PlayerState(TestContent.First);

            GameSession first = TestContent.Begun(SessionAssembler.CreateBattle(content, new[] { state }));
            first.SetAimPoint(TestContent.First, BattleSpace.Origin);
            first.Advance(0.1f);
            BreakerSkill ended = first.World.Players[0].Breaker;
            Expect.Equal(1, ended.TickCount);

            first.RequestEnd(SessionEndReason.TimeExpired);
            first.Advance(2);
            Expect.Equal(1, ended.TickCount);

            GameSession second = TestContent.Begun(SessionAssembler.CreateBattle(content, new[] { state }));
            BattlePlayer player = second.World.PlayerOf(TestContent.First);
            Expect.True(!player.AimPoint.HasValue, "새 판의 참가자는 조준점이 없다.");
            Expect.True(player.Breaker != ended, "새 판은 Breaker 실행 상태를 새로 만든다.");
            Expect.Equal(0, player.Breaker.TickCount);

            second.Advance(0.1f);
            Expect.Equal(1, player.Breaker.TickCount);
        }

        // 예고를 시작할 때 조준점을 저장해 경로를 정한다: 경계 원 위의 시작점에서 조준점을 지나 반대편까지.
        // 예고 중에는 피해가 없고, 조준점이 움직여도 경로는 그대로다. 예고가 끝나는 순간 경로의 굵기 안에 있는 적 전부가 맞는다.
        private static void LaserAimsAtTelegraphStartAndPiercesOnFire()
        {
            GameSession game = TestContent.Session(LaserArena(count: 12, health: 10, damage: 3, width: 2));
            World world = game.World;
            LaserSkill laser = world.Players[0].Laser;
            game.SetAimPoint(TestContent.First, BattleSpace.Origin);

            game.Advance(0.1f);
            Expect.Equal(1, laser.PendingShots.Count);
            LaserShot shot = laser.PendingShots[0];
            Expect.Near(Boundary, TestContent.DistanceToHq(shot.Start));
            Expect.Near(-shot.Start.X, shot.End.X);
            Expect.Near(-shot.Start.Y, shot.End.Y);
            Expect.Near(0.3f, shot.Remaining);
            Expect.Equal(0, laser.FireCount);

            foreach (Enemy enemy in world.Enemies)
                Expect.Near(10, enemy.Health);

            game.SetAimPoint(TestContent.First, new Point2(3, 3));
            game.Advance(0.2f);
            Expect.Equal(0, laser.FireCount);

            game.Advance(0.1f);
            Expect.Equal(1, laser.FireCount);
            Expect.Equal(0, laser.PendingShots.Count);
            LaserFire fire = laser.Fires[0];
            Expect.Equal(shot.Number, fire.Number);
            Expect.Equal(shot.Start, fire.Start);
            Expect.Equal(shot.End, fire.End);
            int hit = 0;

            foreach (Enemy enemy in world.Enemies)
            {
                bool onPath = DistanceToDiameter(enemy.Position, shot.Start) <= 1;
                Expect.Near(onPath ? 7 : 10, enemy.Health);

                if (onPath)
                    hit++;
            }

            Expect.True(hit > 0 && hit < world.Enemies.Count, "이 seed의 배치에는 경로 안과 밖의 적이 모두 있어야 한다.");
            Expect.Equal(hit, fire.HitCount);
        }

        // 조준점이 없거나 경계 원 밖이면 그 주기는 예고 없이 지나간다. 조준점이 원 안에 들어와도 다음 주기까지 기다린다.
        // 판이 끝나면 예고 중인 발사는 발사하지 않는다.
        private static void LaserSkipsAimOutsideBoundaryAndStopsWithTheBattle()
        {
            GameSession game = TestContent.Session(LaserArena(count: 3, health: 10, damage: 3, width: 20));
            LaserSkill laser = game.World.Players[0].Laser;

            game.Advance(0.1f);
            Expect.Equal(0, laser.PendingShots.Count);

            game.SetAimPoint(TestContent.First, new Point2(Boundary * 2, 0));
            game.Advance(1);
            Expect.Equal(0, laser.PendingShots.Count);

            game.SetAimPoint(TestContent.First, BattleSpace.Origin);
            game.Advance(0.5f);
            Expect.Equal(0, laser.PendingShots.Count);
            game.Advance(0.5f);
            Expect.Equal(1, laser.PendingShots.Count);

            game.RequestEnd(SessionEndReason.TimeExpired);
            game.Advance(1);
            Expect.Equal(0, laser.FireCount);

            foreach (Enemy enemy in game.World.Enemies)
                Expect.Near(10, enemy.Health);
        }

        // 같은 판 seed면 같은 시작점이 같은 순서로 나온다. 레이저는 자기만 쓰는 난수라, 출현 배치가 난수를 더 뽑아도 순서가 바뀌지 않는다.
        private static void LaserStartIsReproducibleAndSeparateFromSpawns()
        {
            GameContent content = TestContent.Load(LaserArena(count: 3, health: 100, damage: 1, width: 1));
            GameSession plain = Laser(content, seed: 7);
            GameSession spawning = Laser(content, seed: 7);
            GameSession other = Laser(content, seed: 8);
            spawning.World.RequestSpawn(new SupplyRequest(spawning.World.Enemies[0].Definition, 3));

            foreach (GameSession game in new[] { plain, spawning, other })
                game.Advance(0.1f);

            Point2 first = plain.World.Players[0].Laser.PendingShots[0].Start;
            Expect.Equal(first, spawning.World.Players[0].Laser.PendingShots[0].Start);
            Expect.True(!first.Equals(other.World.Players[0].Laser.PendingShots[0].Start), "다른 seed면 시작점이 다르다.");
            Expect.Equal(plain.World.Enemies.Count + 3, spawning.World.Enemies.Count);

            // 첫 발사(0.4초) 뒤, 1초의 두 번째 예고.
            plain.Advance(1);
            spawning.Advance(1);
            LaserShot second = plain.World.Players[0].Laser.PendingShots[0];
            Expect.Equal(2, second.Number);
            Expect.Equal(second.Start, spawning.World.Players[0].Laser.PendingShots[0].Start);
        }

        // HQ 둘레 [2, 4] 띠에 count마리가 나오는 판. 모든 참가자가 이 Breaker를 받는다.
        private static ContentData Arena(float radius, int count, float health, float damage)
        {
            ContentData data = TestContent.Arena(2, 4, TestContent.Supply(TestContent.EnemyId, count));
            data.Enemies.Add(TestContent.Enemy(TestContent.EnemyId, health));
            TestContent.Allow(data, TestContent.EnemyId);
            data.Breaker = new BreakerData { Damage = damage, Interval = 1, Radius = radius, CritMultiplier = 1 };
            return data;
        }

        // 같은 띠에 레이저만 있는 판: 주기 1초, 예고 0.4초, 경계 반지름 Boundary.
        private static ContentData LaserArena(int count, float health, float damage, float width)
        {
            ContentData data = TestContent.Arena(2, 4, TestContent.Supply(TestContent.EnemyId, count));
            data.Enemies.Add(TestContent.Enemy(TestContent.EnemyId, health));
            TestContent.Allow(data, TestContent.EnemyId);
            data.Laser = new LaserData
            {
                Damage = damage, Interval = 1, Width = width, TelegraphDuration = 0.4f, BoundaryRadius = Boundary,
            };
            return data;
        }

        // Breaker(피해 1, 배율 2, 원은 판 전체)와 아무도 맞히지 않는 가는 레이저가 있는 판. seed 3, HQ에 조준한다.
        private static GameSession CriticalArena(float critChance)
        {
            ContentData data = Arena(radius: 100, count: 6, health: 1000, damage: 1);
            data.Breaker.CritChance = critChance;
            data.Breaker.CritMultiplier = 2;
            data.Laser = new LaserData { Damage = 1, Interval = 1, Width = 0.001f, TelegraphDuration = 0.4f, BoundaryRadius = Boundary };
            return Laser(TestContent.Load(data), seed: 3);
        }

        // 레이저 판을 seed로 조립해 시작하고, HQ에 조준한다.
        private static GameSession Laser(GameContent content, int seed)
        {
            GameSession game = TestContent.Begun(SessionAssembler.CreateBattle(
                content, new[] { new PlayerState(TestContent.First) }, SessionAssembler.FirstStage, seed));
            game.SetAimPoint(TestContent.First, BattleSpace.Origin);
            return game;
        }

        // HQ와 start를 지나는 직선(HQ에 조준한 레이저의 경로)까지의 거리.
        private static float DistanceToDiameter(Point2 point, Point2 start)
        {
            float length = (float)Math.Sqrt(start.X * start.X + start.Y * start.Y);
            return Math.Abs(start.X * point.Y - start.Y * point.X) / length;
        }
    }
}
