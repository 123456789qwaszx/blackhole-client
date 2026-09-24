using System;
using System.Collections.Generic;
using BlackHole.Core;

namespace BlackHole.Core.Tests
{
    // B1 Player 단위, B5 Passive Skill 실행.
    internal static class SkillContracts
    {
        private static readonly Point2 East = new Point2(3, 0);
        private static readonly Point2 West = new Point2(-3, 0);
        private static readonly Point2 Nowhere = new Point2(50, 50);

        public static IEnumerable<Contract> Cases()
        {
            yield return new Contract("Skill.FirstTickAtSessionStart", FirstTickAtSessionStart);
            yield return new Contract("Skill.TicksOnScheduleWithoutInput", TicksOnScheduleWithoutInput);
            yield return new Contract("Skill.HitsEveryEnemyInsideAndNoneOutside", HitsEveryEnemyInsideAndNoneOutside);
            yield return new Contract("Skill.EmptyTickPassesWithoutHolding", EmptyTickPassesWithoutHolding);
            yield return new Contract("Skill.LongFrameCountsEveryTick", LongFrameCountsEveryTick);
            yield return new Contract("Skill.AimPointBelongsToPlayer", AimPointBelongsToPlayer);
            yield return new Contract("Skill.RuntimeStatsLeaveBaseDefinitionUnchanged", RuntimeStatsLeaveBaseDefinitionUnchanged);
            yield return new Contract("Skill.EachPlayerHasOwnSkills", EachPlayerHasOwnSkills);
            yield return new Contract("Skill.LastTickHitCountCountsTargets", LastTickHitCountCountsTargets);
        }

        // 마지막 틱이 피해를 준 Enemy 수. 빈 틱은 0이다(소리가 적중 유무를 읽는다).
        private static void LastTickHitCountCountsTargets()
        {
            GameSession game = TestContent.Session(TestContent.SkillArena(enemies: 2, radius: 5, interval: 0.5f, damage: 1));
            BreakerSkill skill = TestContent.Breaker(game.World.Players[0]);
            Expect.Equal(0, skill.LastTickHitCount);

            game.SetAimPoint(TestContent.First, new Point2(0, 0));
            game.Advance(0.25f);
            Expect.Equal(2, skill.LastTickHitCount);

            game.SetAimPoint(TestContent.First, Nowhere);
            game.Advance(0.5f);
            Expect.Equal(2, skill.TickCount);
            Expect.Equal(0, skill.LastTickHitCount);

            game.SetAimPoint(TestContent.First, East);
            game.Advance(0.5f);
            Expect.Equal(1, skill.LastTickHitCount);

            // 조준점이 없는 틱도 빈 틱이다.
            game.SetAimPoint(TestContent.First, null);
            game.Advance(0.5f);
            Expect.Equal(4, skill.TickCount);
            Expect.Equal(0, skill.LastTickHitCount);
        }

        // 첫 틱은 0초다. 전투 시작 배치가 판 조립 때 나오므로 0초 틱이 그 적을 맞힌다.
        // 조준점이 없어도 틱은 일어나고 세어진다.
        private static void FirstTickAtSessionStart()
        {
            GameSession game = TestContent.Session(TestContent.SkillArena(enemies: 1, radius: 1, interval: 0.5f, damage: 1));
            game.World.TryGetPlayer(TestContent.First, out Player player);
            BreakerSkill skill = TestContent.Breaker(player);
            Expect.Equal(0, skill.TickCount);

            game.SetAimPoint(TestContent.First, East);
            game.Advance(0.01f);
            Expect.Equal(1, skill.TickCount);
            Expect.Near(99, game.World.Enemies[0].Health);

            game.SetAimPoint(TestContent.First, null);
            game.Advance(1);
            Expect.Equal(3, skill.TickCount);
            Expect.Near(99, game.World.Enemies[0].Health);
        }

        // 틱은 0초, 0.5초, 1.0초, …에 키 입력 없이 일어난다.
        // 2.25초까지 맞는 틱: 0, 0.5, 1.0, 1.5, 2.0 → 피해 1씩 5번.
        private static void TicksOnScheduleWithoutInput()
        {
            GameSession game = TestContent.Session(TestContent.SkillArena(enemies: 1, radius: 1, interval: 0.5f, damage: 1));
            game.SetAimPoint(TestContent.First, East);
            game.Advance(2.25f);
            Expect.Near(95, game.World.Enemies[0].Health);
        }

        // 0초 틱 한 번만 보도록 0.25초만 진행한다.
        private static void HitsEveryEnemyInsideAndNoneOutside()
        {
            // 반경 1로 동쪽만 조준: 동쪽 Enemy만 맞는다.
            GameSession narrow = TestContent.Session(TestContent.SkillArena(enemies: 2, radius: 1, interval: 0.5f, damage: 5));
            narrow.SetAimPoint(TestContent.First, East);
            narrow.Advance(0.25f);
            Expect.Equal(2, narrow.World.Enemies.Count);
            Expect.Near(95, EnemyNear(narrow, East).Health);
            Expect.Near(100, EnemyNear(narrow, West).Health);

            // 반경 5로 HQ를 조준: 둘 다 원 안이다.
            GameSession wide = TestContent.Session(TestContent.SkillArena(enemies: 2, radius: 5, interval: 0.5f, damage: 5));
            wide.SetAimPoint(TestContent.First, new Point2(0, 0));
            wide.Advance(0.25f);
            Expect.Near(95, EnemyNear(wide, East).Health);
            Expect.Near(95, EnemyNear(wide, West).Health);
        }

        // 타이머는 범위가 비어도 돈다. 틱 직후에 조준을 옮겨도 즉시 공격하지 않고 다음 틱을 기다린다.
        private static void EmptyTickPassesWithoutHolding()
        {
            GameSession game = TestContent.Session(TestContent.SkillArena(enemies: 1, radius: 1, interval: 1, damage: 1));
            game.SetAimPoint(TestContent.First, Nowhere);
            game.Advance(1.05f);
            Enemy enemy = game.World.Enemies[0];
            Expect.Near(100, enemy.Health);
            game.World.TryGetPlayer(TestContent.First, out Player player);
            Expect.Equal(2, TestContent.Breaker(player).TickCount);

            game.SetAimPoint(TestContent.First, East);
            game.Advance(0.5f);
            Expect.Near(100, enemy.Health);
            game.Advance(0.5f);
            Expect.Near(99, enemy.Health);
        }

        // 긴 프레임 한 번과 짧은 프레임 여러 번의 틱 수가 같다(0 ~ 3.0초 틱 7번).
        private static void LongFrameCountsEveryTick()
        {
            ContentData data = TestContent.SkillArena(enemies: 1, radius: 1, interval: 0.5f, damage: 1);
            GameSession longFrame = TestContent.Session(data);
            GameSession shortFrames = TestContent.Session(data);
            longFrame.SetAimPoint(TestContent.First, East);
            shortFrames.SetAimPoint(TestContent.First, East);

            longFrame.Advance(3.1f);
            for (int i = 0; i < 186; i++) shortFrames.Advance(1f / 60f);
            Expect.Near(93, longFrame.World.Enemies[0].Health);
            Expect.Near(93, shortFrames.World.Enemies[0].Health);
        }

        // 조준점은 Player의 값이다. 각 Player의 Skill은 자기 Player의 조준점만 쓴다.
        // 피해에는 출처 Player가 기록된다(보상 귀속에는 쓰지 않는다).
        private static void AimPointBelongsToPlayer()
        {
            GameSession game = TestContent.Session(
                TestContent.SkillArena(enemies: 1, radius: 1, interval: 0.5f, damage: 1),
                TestContent.First, TestContent.Second);
            Expect.True(!game.SetAimPoint(new PlayerId(9), East), "참가하지 않은 Player의 조준점은 받지 않는다.");

            game.SetAimPoint(TestContent.First, East);
            game.SetAimPoint(TestContent.Second, Nowhere);
            // 0초와 0.5초 틱: 첫째 Player만 맞힌다.
            game.Advance(0.75f);
            Enemy enemy = game.World.Enemies[0];
            Expect.Near(98, enemy.Health);
            Expect.Equal(TestContent.First, enemy.LastDamageSource.Value);

            // 1.0초 틱: 둘 다 맞힌다. 뒤 순서인 둘째 Player가 마지막 출처다.
            game.SetAimPoint(TestContent.Second, East);
            game.Advance(0.5f);
            Expect.Near(96, enemy.Health);
            Expect.Equal(TestContent.Second, enemy.LastDamageSource.Value);

            // 조준점이 없으면 그 Player의 Skill은 아무것도 공격하지 않는다.
            game.SetAimPoint(TestContent.First, null);
            game.SetAimPoint(TestContent.Second, null);
            game.Advance(1);
            Expect.Near(96, enemy.Health);
        }

        // 실행 수치 = 기본 수치 + 보정. 기본 정의는 바뀌지 않는다. 게임에서는 보정의 출처가 미정이라 보정이 없다.
        private static void RuntimeStatsLeaveBaseDefinitionUnchanged()
        {
            GameContent content = TestContent.Load(TestContent.Data());
            content.TryGetSkill(TestContent.SkillId, out PassiveSkillDefinition skill);
            var definition = (BreakerSkillDefinition)skill;

            BreakerStats boosted = BreakerStatCalculator.Compute(definition, new IBreakerStatModifier[] { new Overdrive() });
            Expect.Near(2, boosted.Radius);
            Expect.Near(0.25f, boosted.Interval);
            Expect.Near(4, boosted.Damage);
            Expect.Near(1, definition.BaseStats.Radius);
            Expect.Near(0.5f, definition.BaseStats.Interval);
            Expect.Near(1, definition.BaseStats.Damage);

            GameSession game = TestContent.Session(TestContent.Data());
            game.World.TryGetPlayer(TestContent.First, out Player player);
            Expect.Near(definition.BaseStats.Radius, TestContent.Breaker(player).Stats.Radius);
        }

        private static void EachPlayerHasOwnSkills()
        {
            GameSession game = TestContent.Session(TestContent.Data(), TestContent.First, TestContent.Second);
            game.World.TryGetPlayer(TestContent.First, out Player first);
            game.World.TryGetPlayer(TestContent.Second, out Player second);
            Expect.Equal(1, first.Skills.Count);
            Expect.Equal(1, second.Skills.Count);
            Expect.True(!ReferenceEquals(first.Skills[0], second.Skills[0]), "Skill 실행 상태는 Player마다 따로 있어야 한다.");
            Expect.True(ReferenceEquals(first.Skills[0].Definition, second.Skills[0].Definition), "정의는 공유한다.");
        }

        private static Enemy EnemyNear(GameSession game, Point2 point)
        {
            foreach (Enemy enemy in game.World.Enemies)
                if (enemy.Position.DistanceSquared(point) < 0.01f) return enemy;
            throw new InvalidOperationException($"{point} 근처에 Enemy가 없다.");
        }

        // 테스트 전용 보정: 반경 2배, 주기 절반, 피해 +3.
        private sealed class Overdrive : IBreakerStatModifier
        {
            public BreakerStats Apply(BreakerSkillDefinition definition, BreakerStats current) =>
                new BreakerStats(current.Radius * 2, current.Interval / 2, current.Damage + 3);
        }
    }
}
