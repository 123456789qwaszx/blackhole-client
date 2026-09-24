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
            yield return new Contract("Enemy.StartPlacementAroundHqAtDistance", StartPlacementAroundHqAtDistance);
            yield return new Contract("Enemy.OrbitsAroundHq", OrbitsAroundHq);
            yield return new Contract("Enemy.FollowsWhereHqIs", FollowsWhereHqIs);
            yield return new Contract("Enemy.LongFrameMovesLikeShortFrames", LongFrameMovesLikeShortFrames);
            yield return new Contract("Enemy.BehaviorIsSwappableWithoutTouchingEnemy", BehaviorIsSwappableWithoutTouchingEnemy);
            yield return new Contract("Enemy.RuntimeStatsLeaveBaseDefinitionUnchanged", RuntimeStatsLeaveBaseDefinitionUnchanged);
            yield return new Contract("Enemy.SupplyCreatesKindsInRequestOrder", SupplyCreatesKindsInRequestOrder);
            yield return new Contract("Enemy.SessionsDoNotShareEnemies", SessionsDoNotShareEnemies);
        }

        // 전투 시작 배치는 판 조립 때 HQ 기준 거리에 나온다.
        private static void StartPlacementAroundHqAtDistance()
        {
            GameSession game = TestContent.Session(TestContent.Data(hqX: 2, hqY: 1));
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

        // 3.5초짜리 프레임 한 번과 짧은 프레임 여러 번의 이동 결과가 같다.
        // 공전은 각도 계산이라 단계 크기와 무관하다. 시간 분할 자체는 Growth.LongFrameMatchesShortFrames가 확인한다.
        private static void LongFrameMovesLikeShortFrames()
        {
            GameSession longFrame = TestContent.Session(TestContent.Data());
            GameSession shortFrames = TestContent.Session(TestContent.Data());
            longFrame.Advance(3.5f);

            for (int i = 0; i < 210; i++)
            {
                shortFrames.Advance(1f / 60f);
            }

            float expected = AngleAroundHq(shortFrames, shortFrames.World.Enemies[0]);
            Expect.True(expected > 1, "짧은 프레임에서는 Enemy가 약 3.5초 동안 돌았어야 한다: " + expected);
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
        // 게임에서는 보정(구매)이 아직 연결되지 않아 보정이 없다(M6).
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
            Enemy enemy = game.World.Enemies[0];
            Expect.Near(definition.BaseStats.MaxHealth, enemy.Stats.MaxHealth);
            Expect.Near(enemy.Stats.MaxHealth, enemy.Health);
        }

        // 공급은 요청 순서대로 생성한다. n번째 Enemy는 각도 n × AngleStep에 놓인다.
        private static void SupplyCreatesKindsInRequestOrder()
        {
            ContentData data = TestContent.Data();
            data.Enemies.Add(TestContent.Enemy("other", 20, 1, 0.5f));
            data.StartSupply = new List<SupplyData>
            {
                TestContent.Supply(TestContent.EnemyId, 2),
                TestContent.Supply("other", 1)
            };

            GameSession game = TestContent.Session(data);
            Expect.Equal(3, game.World.Enemies.Count);
            Expect.Equal(TestContent.EnemyId, game.World.Enemies[0].Definition.Id);
            Expect.Equal(TestContent.EnemyId, game.World.Enemies[1].Definition.Id);
            Expect.Equal("other", game.World.Enemies[2].Definition.Id);

            for (int i = 0; i < 3; i++)
            {
                Expect.Near(i, AngleAroundHq(game, game.World.Enemies[i]));
                Expect.Near(3, TestContent.DistanceToHq(game, game.World.Enemies[i]));
            }
        }

        private static void SessionsDoNotShareEnemies()
        {
            GameContent content = TestContent.Load(TestContent.Data());
            GameSession first = SessionAssembler.Create(content, new[] { TestContent.First });
            GameSession second = SessionAssembler.Create(content, new[] { TestContent.First });
            Point2 before = second.World.Enemies[0].Position;

            first.Advance(1);
            Expect.Equal(new EnemyId(1), second.World.Enemies[0].Id);
            Expect.True(!ReferenceEquals(first.World.Enemies[0], second.World.Enemies[0]), "Enemy를 공유하면 안 된다.");
            Expect.Equal(before, second.World.Enemies[0].Position);
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
