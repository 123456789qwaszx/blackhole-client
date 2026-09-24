using System;
using System.Collections.Generic;
using BlackHole.Core;

namespace BlackHole.Core.Tests
{
    // B2 HQ 성장 상태, B8 판 시간, B10 성장 진행과 공급·생성의 분리.
    internal static class GrowthContracts
    {
        private static readonly Point2 East = new Point2(3, 0);

        public static IEnumerable<Contract> Cases()
        {
            yield return new Contract("Growth.StartPlacementOnceWithoutTimer", StartPlacementOnceWithoutTimer);
            yield return new Contract("Growth.LevelRisesAtThreshold", LevelRisesAtThreshold);
            yield return new Contract("Growth.OneGainCrossesSeveralLevelsOnce", OneGainCrossesSeveralLevelsOnce);
            yield return new Contract("Growth.SuppliedEnemiesJoinFromNextStep", SuppliedEnemiesJoinFromNextStep);
            yield return new Contract("Growth.ExtensionCountsBeforeTimeEnds", ExtensionCountsBeforeTimeEnds);
            yield return new Contract("Growth.LongFrameMatchesShortFrames", LongFrameMatchesShortFrames);
            yield return new Contract("Growth.SessionsAreIndependent", SessionsAreIndependent);
        }

        // 전투 시작 배치는 판 조립 때 한 번이다. 처치나 성장이 없으면 적이 늘지 않는다(주기 생성 없음).
        private static void StartPlacementOnceWithoutTimer()
        {
            GameSession game = TestContent.Session(TestContent.Data());
            Expect.Equal(1, game.World.Enemies.Count);

            game.Advance(10);
            Expect.Equal(1, game.World.Enemies.Count);
            Expect.Equal(HqGrowthDefinition.StartLevel, game.World.Hq.Level);
        }

        // 임계값 전·같음·후. 서로 다른 틱에 죽는 세 적으로 EXP를 2 → 3 → 6으로 올린다.
        private static void LevelRisesAtThreshold()
        {
            ContentData data = Arena(
                Target("a", 1, 2),
                Target("b", 2, 1),
                Target("c", 3, 3));
            data.StartSupply = new List<SupplyData>
            {
                TestContent.Supply("a", 1),
                TestContent.Supply("b", 1),
                TestContent.Supply("c", 1)
            };
            data.Growth.Levels.Add(TestContent.Level(3));
            data.Growth.Levels.Add(TestContent.Level(5));
            GameSession game = Aimed(data);
            Hq hq = game.World.Hq;

            game.Advance(0.1f);
            Expect.Equal(2, hq.Exp);
            Expect.Equal(1, hq.Level);
            Expect.Equal(3, hq.NextLevelExp.Value);

            game.Advance(0.5f);
            Expect.Equal(3, hq.Exp);
            Expect.Equal(2, hq.Level);

            game.Advance(0.5f);
            Expect.Equal(6, hq.Exp);
            Expect.Equal(3, hq.Level);
            Expect.True(hq.NextLevelExp == null, "마지막 Level이면 다음 임계값이 없다.");
        }

        // 한 번의 처치로 여러 Level을 넘으면 각 Level의 효과를 낮은 것부터 한 번씩 실행한다.
        // 효과가 없는 Level은 아무것도 하지 않는다. 공급된 적은 Level과 무관하게 콘텐츠의 출현 거리에 나온다.
        private static void OneGainCrossesSeveralLevelsOnce()
        {
            ContentData data = Arena(
                Target("big", 1, 10),
                Target("x", 100, 0),
                Target("y", 100, 0));
            data.StartSupply = new List<SupplyData> { TestContent.Supply("big", 1) };
            data.Growth.Levels.Add(TestContent.Level(2, 5, TestContent.Supply("x", 1)));
            data.Growth.Levels.Add(TestContent.Level(4));
            data.Growth.Levels.Add(TestContent.Level(8, 7, TestContent.Supply("y", 2)));
            data.Growth.Levels.Add(TestContent.Level(20, 100, TestContent.Supply("x", 9)));
            GameSession game = Aimed(data);

            game.Advance(0.1f);
            Expect.Equal(4, game.World.Hq.Level);
            Expect.Near(72, game.TimeLimit.Limit);
            Expect.Equal(3, game.World.Enemies.Count);
            Expect.Equal("x", game.World.Enemies[0].Definition.Id);
            Expect.Equal("y", game.World.Enemies[1].Definition.Id);
            Expect.Equal("y", game.World.Enemies[2].Definition.Id);

            foreach (Enemy enemy in game.World.Enemies)
                Expect.Near(3, TestContent.DistanceToHq(game, enemy));

            game.Advance(1);
            Expect.Equal(3, game.World.Enemies.Count);
            Expect.Near(72, game.TimeLimit.Limit);
        }

        // 공급된 적은 그 단계 끝에 나온다. 같은 단계의 뒤 Skill은 맞히지 못하고, 다음 틱부터 맞는다.
        private static void SuppliedEnemiesJoinFromNextStep()
        {
            ContentData data = Arena(
                Target("a", 1, 1),
                Target("b", 5, 0));
            data.StartSupply = new List<SupplyData> { TestContent.Supply("a", 1) };
            data.Growth.Levels.Add(TestContent.Level(1, 0, TestContent.Supply("b", 1)));
            data.Skills.Add(TestContent.Skill("second", 1, 0.5f, 1));
            data.StartingSkills.Add("second");
            GameSession game = Aimed(data);

            game.Advance(0.01f);
            Expect.Equal(2, game.World.Hq.Level);
            Expect.Equal(1, game.World.Enemies.Count);
            Enemy supplied = game.World.Enemies[0];
            Expect.Near(5, supplied.Health);

            game.Advance(0.5f);
            Expect.Near(3, supplied.Health);
        }

        // 마지막 단계의 처치로 도달한 성장도 종료 판정 전에 시간을 늘린다.
        // 이후 진행 제한, 종료 판정, 남은 시간이 모두 늘어난 같은 시간을 쓴다. 공유 정의는 바뀌지 않는다.
        private static void ExtensionCountsBeforeTimeEnds()
        {
            ContentData data = Arena(Target("a", 1, 1));
            data.Session.TimeLimit = 0.02f;
            data.StartSupply = new List<SupplyData> { TestContent.Supply("a", 1) };
            data.Growth.Levels.Add(TestContent.Level(1, 2));
            GameSession game = Aimed(data);

            game.Advance(0.02f);
            Expect.Equal(SessionPhase.Running, game.Phase);
            Expect.Near(2.02f, game.TimeLimit.Limit);
            Expect.Near(2, game.Remaining);
            Expect.Near(0.02f, game.TimeLimit.Definition.Duration);

            game.Advance(5);
            Expect.Equal(SessionPhase.Ended, game.Phase);
            Expect.Near(2.02f, game.Elapsed);
            Expect.Near(2.02f, game.Result.PlayedSeconds);
            Expect.Near(0, game.Remaining);
        }

        // 긴 프레임 한 번과 짧은 프레임 여러 번의 성장·공급·시간 결과가 같다.
        // 시간 분할이 없으면 한 번의 긴 단계 안에서 공급이 틱 뒤로 밀려, 연쇄 성장이 멈춘다.
        private static void LongFrameMatchesShortFrames()
        {
            GameSession longFrame = Aimed(ChainedGrowth());
            GameSession shortFrames = Aimed(ChainedGrowth());
            longFrame.Advance(3.1f);

            for (int i = 0; i < 186; i++)
            {
                shortFrames.Advance(1f / 60f);
            }

            foreach (GameSession game in new[] { longFrame, shortFrames })
            {
                Expect.Equal(4, game.World.Hq.Level);
                Expect.Equal(7, game.World.Hq.Exp);
                Expect.Equal(0, game.World.Enemies.Count);
                Expect.Near(63, game.TimeLimit.Limit);
            }
        }

        // 같은 콘텐츠로 만든 두 판의 시간·HQ·공급 상태는 서로 영향을 주지 않는다.
        private static void SessionsAreIndependent()
        {
            ContentData data = Arena(Target("a", 1, 1));
            data.StartSupply = new List<SupplyData> { TestContent.Supply("a", 1) };
            data.Growth.Levels.Add(TestContent.Level(1, 3, TestContent.Supply("a", 1)));
            GameContent content = TestContent.Load(data);
            GameSession first = SessionAssembler.Create(content, new[] { TestContent.First });
            GameSession second = SessionAssembler.Create(content, new[] { TestContent.First });
            first.SetAimPoint(TestContent.First, East);

            first.Advance(0.1f);
            Expect.Equal(2, first.World.Hq.Level);
            Expect.Near(63, first.TimeLimit.Limit);

            Expect.Equal(1, second.World.Hq.Level);
            Expect.Equal(0, second.World.Hq.Exp);
            Expect.Near(60, second.TimeLimit.Limit);
            Expect.Equal(1, second.World.Enemies.Count);
            Expect.True(!ReferenceEquals(first.World.Enemies[0], second.World.Enemies[0]), "Enemy를 공유하면 안 된다.");
        }

        // 처치가 다음 성장을 부르는 사슬: EXP 1 → Lv2(적 2, +1초) → 3 → Lv3(적 3, +1초) → 6 → Lv4(적 1, +1초) → 7.
        private static ContentData ChainedGrowth()
        {
            ContentData data = Arena(Target("a", 1, 1));
            data.StartSupply = new List<SupplyData> { TestContent.Supply("a", 1) };
            data.Growth.Levels.Add(TestContent.Level(1, 1, TestContent.Supply("a", 2)));
            data.Growth.Levels.Add(TestContent.Level(3, 1, TestContent.Supply("a", 3)));
            data.Growth.Levels.Add(TestContent.Level(6, 1, TestContent.Supply("a", 1)));
            data.Growth.Levels.Add(TestContent.Level(10));
            return data;
        }

        // 모든 적이 HQ(0, 0)에서 거리 3, 각도 0 = (3, 0)에 나온다. 기본 Skill은 반경 1, 0.5초마다 피해 1이다.
        private static ContentData Arena(params EnemyData[] enemies)
        {
            ContentData data = TestContent.Data();
            data.Enemies = new List<EnemyData>(enemies);
            data.Spawn = new SpawnData { Distance = 3, AngleStep = 0 };
            data.StartSupply = new List<SupplyData>();
            return data;
        }

        // 거의 움직이지 않는 적. 성장 EXP만 준다.
        private static EnemyData Target(string id, float health, int hqExp)
        {
            EnemyData enemy = TestContent.Enemy(id, health, 0.0001f, 0.3f);
            enemy.HqExp = hqExp;
            return enemy;
        }

        private static GameSession Aimed(ContentData data)
        {
            GameSession game = TestContent.Session(data);
            game.SetAimPoint(TestContent.First, East);
            return game;
        }
    }
}
