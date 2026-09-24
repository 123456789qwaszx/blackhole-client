using System;
using System.Collections.Generic;
using BlackHole.Core;

namespace BlackHole.Core.Tests
{
    // B2 HQ 기준점, B3 행동 분리, B4 Runtime Stat 분리. 출현과 이동.
    internal static class EnemyContracts
    {
        public static IEnumerable<Contract> Cases()
        {
            yield return new Contract("Enemy.SpawnsAroundHqAtDistance", SpawnsAroundHqAtDistance);
            yield return new Contract("Enemy.OrbitsAroundHq", OrbitsAroundHq);
            yield return new Contract("Enemy.FollowsWhereHqIs", FollowsWhereHqIs);
            yield return new Contract("Enemy.LongFrameIsSteppedLikeShortFrames", LongFrameIsSteppedLikeShortFrames);
            yield return new Contract("Enemy.BehaviorIsSwappableWithoutTouchingEnemy", BehaviorIsSwappableWithoutTouchingEnemy);
            yield return new Contract("Enemy.RuntimeStatsLeaveBaseDefinitionUnchanged", RuntimeStatsLeaveBaseDefinitionUnchanged);
            yield return new Contract("Enemy.SpawnRespectsMaxAliveAndOrder", SpawnRespectsMaxAliveAndOrder);
            yield return new Contract("Enemy.SessionsDoNotShareEnemies", SessionsDoNotShareEnemies);
        }

        private static void SpawnsAroundHqAtDistance()
        {
            GameSession game = TestContent.Session(TestContent.Data(hqX: 2, hqY: 1));
            game.Advance(0.9f);
            Expect.Equal(0, game.World.Enemies.Count);
            game.Advance(0.15f);
            Expect.Equal(1, game.World.Enemies.Count);

            Enemy enemy = game.World.Enemies[0];
            Expect.Near(3, TestContent.DistanceToHq(game, enemy));
            Expect.Equal(TestContent.EnemyId, enemy.Definition.Id);
            Expect.Near(enemy.Stats.MaxHealth, enemy.Health);
        }

        // HQ로부터의 거리를 유지하며 이동 속도만큼 원 둘레를 돈다(속도 1, 반지름 3 → 초당 1/3 라디안).
        private static void OrbitsAroundHq()
        {
            GameSession game = TestContent.Session(TestContent.Data(hqX: -1, hqY: 4));
            game.Advance(1.05f);
            Enemy enemy = game.World.Enemies[0];
            float before = AngleAroundHq(game, enemy);

            game.Advance(1);
            Expect.Near(3, TestContent.DistanceToHq(game, enemy), 0.01f);
            Expect.Near(1f / 3f, AngleAroundHq(game, enemy) - before, 0.01f);
        }

        // HQ를 옮긴 판에서는 출현과 공전이 새 위치를 기준으로 한다. HQ에 대한 상대 위치는 같다.
        private static void FollowsWhereHqIs()
        {
            GameSession atOrigin = TestContent.Session(TestContent.Data());
            GameSession moved = TestContent.Session(TestContent.Data(hqX: 5, hqY: -3));
            atOrigin.Advance(2.5f);
            moved.Advance(2.5f);

            Expect.Equal(atOrigin.World.Enemies.Count, moved.World.Enemies.Count);
            for (int i = 0; i < atOrigin.World.Enemies.Count; i++)
            {
                Point2 a = atOrigin.World.Enemies[i].Position;
                Point2 b = moved.World.Enemies[i].Position;
                Expect.Near(a.X, b.X - 5);
                Expect.Near(a.Y, b.Y + 3);
            }
        }

        // 3.5초짜리 프레임 한 번도 짧은 프레임을 여러 번 진행한 것처럼 단계로 나뉜다.
        // 나뉘지 않으면 1초에 나온 Enemy가 그 프레임 안에서 전혀 움직이지 않는다.
        // (출현 시각과 겹치지 않는 3.5초를 쓴다. 정확히 3초면 부동소수 누적 차이로 출현 수가 갈린다.)
        private static void LongFrameIsSteppedLikeShortFrames()
        {
            GameSession longFrame = TestContent.Session(TestContent.Data());
            GameSession shortFrames = TestContent.Session(TestContent.Data());
            longFrame.Advance(3.5f);
            for (int i = 0; i < 210; i++) shortFrames.Advance(1f / 60f);

            Expect.Equal(shortFrames.World.Enemies.Count, longFrame.World.Enemies.Count);
            float expected = AngleAroundHq(shortFrames, shortFrames.World.Enemies[0]);
            Expect.True(expected > 0.5f, "짧은 프레임에서는 첫 Enemy가 약 2.5초 동안 돌았어야 한다: " + expected);
            // 단계 크기(1/30초) 차이만큼의 오차: 속도 1 / 반지름 3 × 1/30초 ≈ 0.011 라디안.
            Expect.Near(expected, AngleAroundHq(longFrame, longFrame.World.Enemies[0]), 0.02f);
        }

        // D3: 게임 콘텐츠와 종류 해석에는 Orbit뿐이다. 테스트가 행동 경계에 Fake를 꽂아도
        // Enemy·출현·Session은 그대로 동작한다.
        private static void BehaviorIsSwappableWithoutTouchingEnemy()
        {
            GameContent content = TestContent.Load(TestContent.Data());
            var seen = new List<EnemyBehaviorDefinition>();
            GameSession game = SessionAssembler.Create(content, new[] { TestContent.First }, definition =>
            {
                seen.Add(definition);
                return new SlideRight();
            });

            game.Advance(1.05f);
            Enemy enemy = game.World.Enemies[0];
            Point2 start = enemy.Position;
            game.Advance(1);
            Expect.Near(start.X + 1, enemy.Position.X, 0.01f);
            Expect.Near(start.Y, enemy.Position.Y);
            Expect.Equal(game.World.Enemies.Count, seen.Count);
            foreach (EnemyBehaviorDefinition definition in seen)
                Expect.True(definition is OrbitHqBehaviorDefinition, "콘텐츠의 행동 정의는 그대로 Orbit이어야 한다.");

            // 같은 콘텐츠를 표준 해석기로 조립하면 공전한다.
            GameSession standard = TestContent.Session(TestContent.Data());
            standard.Advance(2.05f);
            Expect.Near(3, TestContent.DistanceToHq(standard, standard.World.Enemies[0]), 0.01f);
        }

        // 실행 수치 = 기본 수치 + 보정(순서대로). 기본 정의는 바뀌지 않는다.
        // 게임에서는 보정의 출처가 미정이라 보정이 없다.
        private static void RuntimeStatsLeaveBaseDefinitionUnchanged()
        {
            GameContent content = TestContent.Load(TestContent.Data());
            content.TryGetEnemy(TestContent.EnemyId, out EnemyDefinition definition);

            EnemyStats plain = EnemyStatCalculator.Compute(definition, Array.Empty<IEnemyStatModifier>());
            Expect.Near(10, plain.MaxHealth);

            var addThenDouble = new IEnemyStatModifier[] { new AddHealth(5), new DoubleHealth() };
            var doubleThenAdd = new IEnemyStatModifier[] { new DoubleHealth(), new AddHealth(5) };
            Expect.Near(30, EnemyStatCalculator.Compute(definition, addThenDouble).MaxHealth);
            Expect.Near(25, EnemyStatCalculator.Compute(definition, doubleThenAdd).MaxHealth);
            Expect.Near(10, definition.BaseStats.MaxHealth);

            GameSession game = TestContent.Session(TestContent.Data());
            game.Advance(1.05f);
            Enemy enemy = game.World.Enemies[0];
            Expect.Near(definition.BaseStats.MaxHealth, enemy.Stats.MaxHealth);
            Expect.Near(enemy.Stats.MaxHealth, enemy.Health);
        }

        private static void SpawnRespectsMaxAliveAndOrder()
        {
            ContentData data = TestContent.Data();
            data.Enemies.Add(TestContent.Enemy("other", 20, 1, 0.5f));
            data.Spawn.Order = new List<string> { TestContent.EnemyId, "other" };
            data.Spawn.MaxAlive = 3;

            GameSession game = TestContent.Session(data);
            game.Advance(10.5f);
            Expect.Equal(3, game.World.Enemies.Count);
            Expect.Equal(TestContent.EnemyId, game.World.Enemies[0].Definition.Id);
            Expect.Equal("other", game.World.Enemies[1].Definition.Id);
            Expect.Equal(TestContent.EnemyId, game.World.Enemies[2].Definition.Id);
        }

        private static void SessionsDoNotShareEnemies()
        {
            GameContent content = TestContent.Load(TestContent.Data());
            GameSession first = SessionAssembler.Create(content, new[] { TestContent.First });
            GameSession second = SessionAssembler.Create(content, new[] { TestContent.First });
            first.Advance(3.5f);
            Expect.Equal(3, first.World.Enemies.Count);
            Expect.Equal(0, second.World.Enemies.Count);

            second.Advance(1.05f);
            Expect.Equal(new EnemyId(1), second.World.Enemies[0].Id);
            Expect.True(!ReferenceEquals(first.World.Enemies[0], second.World.Enemies[0]), "Enemy를 공유하면 안 된다.");
        }

        private static float AngleAroundHq(GameSession game, Enemy enemy)
        {
            Point2 hq = game.World.Hq.Position;
            return (float)Math.Atan2(enemy.Position.Y - hq.Y, enemy.Position.X - hq.X);
        }

        // 테스트 전용 행동: 초당 1씩 오른쪽으로 민다. 게임 콘텐츠에는 없다.
        private sealed class SlideRight : IEnemyBehavior
        {
            public Point2 NextPosition(in EnemyBehaviorInput input, float delta) =>
                new Point2(input.Position.X + delta, input.Position.Y);
        }

        // 테스트 전용 보정.
        private sealed class AddHealth : IEnemyStatModifier
        {
            private readonly float _amount;
            public AddHealth(float amount) { _amount = amount; }
            public EnemyStats Apply(EnemyDefinition definition, EnemyStats current) =>
                new EnemyStats(current.MaxHealth + _amount, current.MoveSpeed, current.Size);
        }

        private sealed class DoubleHealth : IEnemyStatModifier
        {
            public EnemyStats Apply(EnemyDefinition definition, EnemyStats current) =>
                new EnemyStats(current.MaxHealth * 2, current.MoveSpeed, current.Size);
        }
    }
}
