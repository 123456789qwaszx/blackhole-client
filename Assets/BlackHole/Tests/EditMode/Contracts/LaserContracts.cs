using System;
using System.Collections.Generic;
using BlackHole.Core;

namespace BlackHole.Core.Tests
{
    // 관통 레이저(CONTENT_DEFINITION 2.3, CA-003). 시작점은 전투의 seed로 정해지므로
    // 계약은 PendingShots에서 확정된 경로를 읽고, 그 경로에 대해 피해를 확인한다.
    internal static class LaserContracts
    {
        private static readonly Point2 East = new Point2(3, 0);
        private static readonly Point2 West = new Point2(-3, 0);
        private static readonly Point2 Hq = new Point2(0, 0);

        public static IEnumerable<Contract> Cases()
        {
            yield return new Contract("Laser.AimIsSnapshotAtTelegraphStart", AimIsSnapshotAtTelegraphStart);
            yield return new Contract("Laser.NextShotRereadsAimPoint", NextShotRereadsAimPoint);
            yield return new Contract("Laser.TelegraphDelaysDamage", TelegraphDelaysDamage);
            yield return new Contract("Laser.PiercesEveryEnemyOnPathAndNoOther", PiercesEveryEnemyOnPathAndNoOther);
            yield return new Contract("Laser.OneFireDamagesEachEnemyOnce", OneFireDamagesEachEnemyOnce);
            yield return new Contract("Laser.OverlappingTelegraphsAreBounded", OverlappingTelegraphsAreBounded);
            yield return new Contract("Laser.BattleEndDropsPendingShots", BattleEndDropsPendingShots);
            yield return new Contract("Laser.SameSeedSameStart", SameSeedSameStart);
            yield return new Contract("Laser.FireRecordedOncePerFire", FireRecordedOncePerFire);
            yield return new Contract("Laser.FireRecordMatchesShot", FireRecordMatchesShot);
        }

        // 발사마다 기록이 하나 생긴다. 긴 프레임 하나에 여러 발사가 있어도 모두 남고, 번호는 전투 안에서 계속 는다.
        // 기록은 다음 Advance가 시작할 때 비운다(사망 기록과 같다). 발사는 0.3, 1.3, 2.3, 3.3, 4.3초다.
        private static void FireRecordedOncePerFire()
        {
            GameSession game = TestContent.Session(
                TestContent.LaserArena(enemies: 1, angleStep: 1, interval: 1, damage: 5, width: 0.2f, telegraph: 0.3f));
            game.SetAimPoint(TestContent.First, East);

            game.Advance(3.5f);
            Expect.Equal(4, game.World.LaserFires.Count);
            for (int i = 0; i < 4; i++)
            {
                LaserFireRecord fire = game.World.LaserFires[i];
                Expect.Equal((long)(i + 1), fire.Sequence);
                Expect.Equal(TestContent.First, fire.Owner);
                Expect.Near(0.2f, fire.Width);
                Expect.Equal(1, fire.HitCount);
            }

            game.Advance(0.1f);
            Expect.Equal(0, game.World.LaserFires.Count);

            game.Advance(0.8f);
            Expect.Equal(1, game.World.LaserFires.Count);
            Expect.Equal(5L, game.World.LaserFires[0].Sequence);
        }

        // 발사 기록의 경로는 예고 중이던 경로이고, 맞힌 수는 실제로 피해를 받은 Enemy 수다.
        private static void FireRecordMatchesShot()
        {
            GameSession game = TestContent.Session(
                TestContent.LaserArena(enemies: 36, angleStep: (float)(Math.PI / 18), interval: 10, damage: 5, width: 0.8f, telegraph: 0.5f));
            PiercingLaserSkill laser = TestContent.LaserSkill(game.World.Players[0]);
            game.SetAimPoint(TestContent.First, Hq);
            game.Advance(0.01f);
            LaserShot shot = laser.PendingShots[0];

            game.Advance(0.5f);
            Expect.Equal(1, game.World.LaserFires.Count);
            LaserFireRecord fire = game.World.LaserFires[0];
            Expect.True(fire.Start.Equals(shot.Start) && fire.End.Equals(shot.End), "발사 기록의 경로는 예고 중이던 경로여야 한다.");

            int damaged = 0;
            foreach (Enemy enemy in game.World.Enemies)
                if (enemy.Health < 100) damaged++;
            Expect.Equal(damaged, fire.HitCount);
            Expect.True(damaged >= 2, "관통을 보이려면 둘 이상 맞아야 한다.");
        }

        // 예고 시작 순간의 Aim Point로 경로가 확정된다. 예고 중에 조준을 옮겨도 이번 발사는 바뀌지 않는다.
        private static void AimIsSnapshotAtTelegraphStart()
        {
            GameSession game = TestContent.Session(
                TestContent.LaserArena(enemies: 2, angleStep: (float)Math.PI, interval: 1, damage: 5, width: 0.2f, telegraph: 0.5f));
            PiercingLaserSkill laser = TestContent.LaserSkill(game.World.Players[0]);

            game.SetAimPoint(TestContent.First, East);
            game.Advance(0.01f);
            Expect.Equal(1, laser.PendingShots.Count);
            LaserShot shot = laser.PendingShots[0];
            Expect.Near(0, DistanceToPath(East, shot));
            Expect.True(DistanceToPath(West, shot) > 0.1f, "seed 0의 첫 경로가 서쪽 Enemy를 지나지 않아야 이 계약이 뜻을 가진다.");

            game.SetAimPoint(TestContent.First, West);
            game.Advance(0.2f);
            Expect.True(laser.PendingShots[0].Start.Equals(shot.Start) && laser.PendingShots[0].End.Equals(shot.End),
                "예고 중에는 경로가 바뀌지 않아야 한다.");

            // 0.5초 발사: 확정된 경로 위의 동쪽만 맞는다.
            game.Advance(0.3f);
            Expect.Near(95, EnemyNear(game, East).Health);
            Expect.Near(100, EnemyNear(game, West).Health);
        }

        // 다음 예고(1초)는 그 순간의 Aim Point를 새로 읽는다.
        private static void NextShotRereadsAimPoint()
        {
            GameSession game = TestContent.Session(
                TestContent.LaserArena(enemies: 2, angleStep: (float)Math.PI, interval: 1, damage: 5, width: 0.2f, telegraph: 0.5f));
            PiercingLaserSkill laser = TestContent.LaserSkill(game.World.Players[0]);

            game.SetAimPoint(TestContent.First, East);
            game.Advance(0.51f);
            Expect.Near(95, EnemyNear(game, East).Health);

            game.SetAimPoint(TestContent.First, West);
            game.Advance(0.5f);
            Expect.Equal(1, laser.PendingShots.Count);
            LaserShot second = laser.PendingShots[0];
            Expect.Near(0, DistanceToPath(West, second));
            Expect.True(DistanceToPath(East, second) > 0.1f, "seed 0의 둘째 경로가 동쪽 Enemy를 지나지 않아야 이 계약이 뜻을 가진다.");

            game.Advance(0.5f);
            Expect.Near(95, EnemyNear(game, West).Health);
            Expect.Near(95, EnemyNear(game, East).Health);
        }

        // 예고 동안에는 피해가 없고, 예고 시간이 지난 발사 순간에만 피해가 난다.
        private static void TelegraphDelaysDamage()
        {
            GameSession game = TestContent.Session(
                TestContent.LaserArena(enemies: 1, angleStep: 1, interval: 10, damage: 5, width: 0.2f, telegraph: 0.5f));
            PiercingLaserSkill laser = TestContent.LaserSkill(game.World.Players[0]);
            Enemy enemy = game.World.Enemies[0];

            game.SetAimPoint(TestContent.First, East);
            game.Advance(0.01f);
            Expect.Equal(1, laser.PendingShots.Count);
            Expect.Near(100, enemy.Health);

            game.Advance(0.47f);
            Expect.Near(100, enemy.Health);

            game.Advance(0.04f);
            Expect.Near(95, enemy.Health);
            Expect.Equal(0, laser.PendingShots.Count);
        }

        // 경로의 폭 안(Enemy 중심 판정, [임시])에 있는 Enemy는 모두 맞고, 밖의 Enemy는 맞지 않는다.
        private static void PiercesEveryEnemyOnPathAndNoOther()
        {
            GameSession game = TestContent.Session(
                TestContent.LaserArena(enemies: 36, angleStep: (float)(Math.PI / 18), interval: 10, damage: 5, width: 0.8f, telegraph: 0.5f));
            PiercingLaserSkill laser = TestContent.LaserSkill(game.World.Players[0]);

            game.SetAimPoint(TestContent.First, Hq);
            game.Advance(0.01f);
            LaserShot shot = laser.PendingShots[0];
            game.Advance(0.5f);

            int hit = 0;
            int missed = 0;
            foreach (Enemy enemy in game.World.Enemies)
            {
                bool onPath = DistanceToPath(enemy.Position, shot) <= 0.4f;
                Expect.Near(onPath ? 95 : 100, enemy.Health);
                if (onPath) hit++;
                else missed++;
            }

            Expect.True(hit >= 2, $"관통을 보이려면 경로 위 Enemy가 둘 이상이어야 한다. 맞은 수 {hit}.");
            Expect.True(missed >= 1, "경로 밖 Enemy가 있어야 이 계약이 뜻을 가진다.");
        }

        // 한 발은 경로 안의 Enemy에게 한 번씩만 피해를 준다. 폭이 전투 영역 전체를 덮어도 두 번 맞지 않는다.
        private static void OneFireDamagesEachEnemyOnce()
        {
            GameSession game = TestContent.Session(
                TestContent.LaserArena(enemies: 4, angleStep: (float)(Math.PI / 2), interval: 10, damage: 5, width: 20, telegraph: 0.5f));

            game.SetAimPoint(TestContent.First, East);
            game.Advance(0.51f);

            Expect.Equal(4, game.World.Enemies.Count);
            foreach (Enemy enemy in game.World.Enemies)
                Expect.Near(95, enemy.Health);
        }

        // 예고 시간 2.5초, 주기 1초: 예고는 동시에 최대 ⌈2.5 / 1⌉ = 3개다.
        // 7.9초까지 발사는 2.5, 3.5, …, 7.5초의 6번이고, 조준점 위의 Enemy는 매번 맞는다.
        private static void OverlappingTelegraphsAreBounded()
        {
            GameSession game = TestContent.Session(
                TestContent.LaserArena(enemies: 1, angleStep: 1, interval: 1, damage: 1, width: 0.2f, telegraph: 2.5f));
            PiercingLaserSkill laser = TestContent.LaserSkill(game.World.Players[0]);
            Enemy enemy = game.World.Enemies[0];
            game.SetAimPoint(TestContent.First, East);

            int most = 0;
            for (int i = 0; i < 474; i++)
            {
                game.Advance(1f / 60f);
                most = Math.Max(most, laser.PendingShots.Count);
            }

            Expect.Equal(3, most);
            Expect.Near(94, enemy.Health);
        }

        // 전투가 끝나면 예고 중이던 발사는 피해를 만들지 않는다. 새 전투는 예고 없이 시작한다.
        // 제한 시간 1초, 주기 0.5초, 예고 0.8초: 0초 예고는 0.8초에 발사하고, 0.5초 예고(1.3초 발사)는 남는다.
        private static void BattleEndDropsPendingShots()
        {
            ContentData data = TestContent.LaserArena(enemies: 1, angleStep: 1, interval: 0.5f, damage: 5, width: 0.2f, telegraph: 0.8f);
            data.Session.TimeLimit = 1;
            GameSession game = TestContent.Session(data);
            PiercingLaserSkill laser = TestContent.LaserSkill(game.World.Players[0]);
            Enemy enemy = game.World.Enemies[0];

            game.SetAimPoint(TestContent.First, East);
            game.Advance(2);
            Expect.Equal(SessionPhase.Ended, game.Phase);
            Expect.Near(95, enemy.Health);
            Expect.True(laser.PendingShots.Count > 0, "종료 때 예고 중인 발사가 있어야 이 계약이 뜻을 가진다.");

            game.Advance(2);
            Expect.Near(95, enemy.Health);

            GameSession next = TestContent.Session(data);
            Expect.Equal(0, TestContent.LaserSkill(next.World.Players[0]).PendingShots.Count);
        }

        // 같은 seed, 같은 조준, 같은 진행 시간이면 시작점과 피해 결과가 같다. seed가 다르면 시작점이 다르다.
        private static void SameSeedSameStart()
        {
            ContentData data = TestContent.LaserArena(enemies: 36, angleStep: (float)(Math.PI / 18), interval: 0.5f, damage: 5, width: 0.8f, telegraph: 0.3f);
            GameSession a = TestContent.Session(data, seed: 7);
            GameSession b = TestContent.Session(data, seed: 7);
            GameSession other = TestContent.Session(data, seed: 8);

            foreach (GameSession game in new[] { a, b, other })
            {
                game.SetAimPoint(TestContent.First, Hq);
                game.Advance(0.01f);
            }

            Point2 start = TestContent.LaserSkill(a.World.Players[0]).PendingShots[0].Start;
            Expect.True(start.Equals(TestContent.LaserSkill(b.World.Players[0]).PendingShots[0].Start), "같은 seed면 시작점이 같아야 한다.");
            Expect.True(!start.Equals(TestContent.LaserSkill(other.World.Players[0]).PendingShots[0].Start), "seed가 다르면 시작점이 달라야 한다.");

            a.Advance(3);
            b.Advance(3);
            Expect.Equal(a.World.Enemies.Count, b.World.Enemies.Count);
            for (int i = 0; i < a.World.Enemies.Count; i++)
                Expect.Near(a.World.Enemies[i].Health, b.World.Enemies[i].Health);
        }

        private static float DistanceToPath(Point2 point, LaserShot shot)
        {
            float sx = shot.End.X - shot.Start.X;
            float sy = shot.End.Y - shot.Start.Y;
            float t = ((point.X - shot.Start.X) * sx + (point.Y - shot.Start.Y) * sy) / (sx * sx + sy * sy);
            t = Math.Max(0, Math.Min(1, t));
            var nearest = new Point2(shot.Start.X + t * sx, shot.Start.Y + t * sy);
            return (float)Math.Sqrt(point.DistanceSquared(nearest));
        }

        private static Enemy EnemyNear(GameSession game, Point2 point)
        {
            foreach (Enemy enemy in game.World.Enemies)
                if (enemy.Position.DistanceSquared(point) < 0.01f) return enemy;
            throw new InvalidOperationException($"{point} 근처에 Enemy가 없다.");
        }
    }
}
