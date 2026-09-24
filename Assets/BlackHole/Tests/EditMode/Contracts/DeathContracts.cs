using System;
using System.Collections.Generic;

namespace BlackHole.Core.Tests
{
    internal static class DeathContracts
    {
        public static IEnumerable<Contract> Cases()
        {
            yield return new Contract("Death.RewardsAndRemovesInDamageStep", RewardsAndRemovesInDamageStep);
            yield return new Contract("Death.MultipleSkillsRewardOnlyOnce", MultipleSkillsRewardOnlyOnce);
            yield return new Contract("Death.LongAdvanceKeepsAllDeathsUntilNextAdvance", LongAdvanceKeepsAllDeathsUntilNextAdvance);
            yield return new Contract("Death.RewardedMultiplayerHasNoPolicy", RewardedMultiplayerHasNoPolicy);
            yield return new Contract("Death.ContentRejectsNegativeRewards", ContentRejectsNegativeRewards);
            yield return new Contract("Death.StopAndRestartDoNotGrantRewards", StopAndRestartDoNotGrantRewards);
        }

        private static ContentData Arena()
        {
            ContentData data = TestContent.SkillArena(enemies: 1, radius: 1, interval: 0.5f, damage: 3);
            data.Enemies[0].MaxHealth = 3;
            data.Enemies[0].Gold = 7;
            data.Enemies[0].HqExp = 11;
            return data;
        }

        private static void RewardsAndRemovesInDamageStep()
        {
            GameSession game = TestContent.Session(Arena());
            game.SetAimPoint(TestContent.First, new Point2(3, 0));
            game.Advance(0.55f);
            game.World.TryGetPlayer(TestContent.First, out Player player);
            Expect.Equal(7, player.State.Gold);
            Expect.Equal(11, game.World.Hq.Exp);
            Expect.Equal(0, game.World.Enemies.Count);
            Expect.Equal(1, game.World.Deaths.Count);
            DeathRecord death = game.World.Deaths[0];
            Expect.Equal(TestContent.EnemyId, death.EnemyTypeId);
            Expect.Near(3, death.Position.X);
            Expect.Near(0.3f, death.Size);
        }

        private static void MultipleSkillsRewardOnlyOnce()
        {
            ContentData data = Arena();
            data.Skills.Add(TestContent.Skill("second", 1, 0.5f, 3));
            data.StartingSkills.Add("second");
            GameSession game = TestContent.Session(data);
            game.SetAimPoint(TestContent.First, new Point2(3, 0));
            game.Advance(0.55f);
            game.World.TryGetPlayer(TestContent.First, out Player player);
            Expect.Equal(7, player.State.Gold);
            Expect.Equal(11, game.World.Hq.Exp);
            Expect.Equal(1, game.World.Deaths.Count);
        }

        private static void LongAdvanceKeepsAllDeathsUntilNextAdvance()
        {
            // 서로 다른 단계에서 죽는 두 적을 조준점 (3, 0)에 함께 둔다(각도 간격 0).
            // HP 3은 0초 틱에, HP 6은 0.5초 틱에 죽는다.
            ContentData data = Arena();
            EnemyData tough = TestContent.Enemy("tough", 6, 0.0001f, 0.3f);
            tough.Gold = 7;
            tough.HqExp = 11;
            data.Enemies.Add(tough);
            data.StartSupply.Add(TestContent.Supply("tough", 1));
            data.Spawn.AngleStep = 0;
            GameSession game = TestContent.Session(data);
            game.SetAimPoint(TestContent.First, new Point2(3, 0));
            game.Advance(1.08f);
            game.World.TryGetPlayer(TestContent.First, out Player player);
            Expect.Equal(2, game.World.Deaths.Count);
            Expect.True(game.World.Deaths[1].Sequence > game.World.Deaths[0].Sequence,
                "사망 기록의 순번은 증가해야 한다.");
            Expect.Equal(14, player.State.Gold);
            Expect.Equal(22, game.World.Hq.Exp);

            game.TogglePause();
            game.Advance(1);
            Expect.Equal(2, game.World.Deaths.Count);
            game.TogglePause();
            game.Advance(0.01f);
            Expect.Equal(0, game.World.Deaths.Count);
        }

        private static void RewardedMultiplayerHasNoPolicy()
        {
            GameContent content = TestContent.Load(Arena());
            Expect.Throws<InvalidOperationException>(() =>
                SessionAssembler.Create(content, new[] { TestContent.First, TestContent.Second }));
            // 보상이 없는 판에서는 두 Player의 AimPoint·Skill 경계를 계속 검증할 수 있다.
            GameSession noReward = TestContent.Session(TestContent.SkillArena(1, 1, 0.5f, 1),
                TestContent.First, TestContent.Second);
            Expect.Equal(2, noReward.World.Players.Count);
        }

        private static void ContentRejectsNegativeRewards()
        {
            ContentData data = Arena();
            data.Enemies[0].Gold = -1;
            ContentLoadResult result = ContentLoader.Load(data);
            Expect.True(!result.Succeeded, "음수 보상은 콘텐츠 오류여야 한다.");
            TestContent.HasDiagnostic(result, "Enemies[test-enemy]", "gold");
            data.Enemies[0].Gold = 1;
            data.Enemies[0].HqExp = -1;
            result = ContentLoader.Load(data);
            TestContent.HasDiagnostic(result, "Enemies[test-enemy]", "hqExp");
        }

        private static void StopAndRestartDoNotGrantRewards()
        {
            ContentData data = Arena();
            GameContent content = TestContent.Load(data);
            GameSession game = SessionAssembler.Create(content, new[] { TestContent.First });
            game.Advance(0.2f); // 살아 있는 적 하나. 조준점이 없으므로 처치는 없다.
            game.Stop();
            game.World.TryGetPlayer(TestContent.First, out Player player);
            Expect.Equal(0, player.State.Gold);
            Expect.Equal(0, game.World.Hq.Exp);
            Expect.Equal(0, game.World.Deaths.Count);
            GameSession next = SessionAssembler.Create(content, new[] { TestContent.First });
            Expect.Equal(0, next.World.Hq.Exp);
            Expect.Equal(0, next.World.Deaths.Count);
        }
    }
}
