using System;
using System.Collections.Generic;

namespace BlackHole.Core.Tests
{
    // 스킬: 판 안의 참가자(조준점·스킬), Breaker의 주기 공격, 피해가 적 시스템으로 들어가는 길(GAME_RULES 6·8·13절, SKILL_SYSTEM_PLAN).
    // 공전은 HQ까지의 거리를 지키므로, HQ에 조준하면 적이 원 안에 있는지가 판 내내 같다. 계약은 이것으로 원 안팎을 정한다.
    internal static class SkillContracts
    {
        public static IEnumerable<Contract> Cases()
        {
            yield return new Contract("Skill.BreakerIsOptionalButValidatedAtLoad", BreakerIsOptionalButValidatedAtLoad);
            yield return new Contract("Skill.BreakerHitsEveryAliveEnemyInsideAimRadius", BreakerHitsEveryAliveEnemyInsideAimRadius);
            yield return new Contract("Skill.DamageGoesThroughWorldOnce", DamageGoesThroughWorldOnce);
            yield return new Contract("Skill.NothingCarriesIntoNextBattle", NothingCarriesIntoNextBattle);
        }

        // Breaker 칸이 없으면 판에 Breaker가 없고, 판은 그대로 돈다. 칸이 있으면 수치를 검사해 경로와 함께 보고한다.
        private static void BreakerIsOptionalButValidatedAtLoad()
        {
            GameSession without = TestContent.Session(TestContent.Data());
            Expect.True(without.World.Players[0].Breaker == null, "Breaker 칸이 없으면 참가자에게 Breaker가 없다.");
            without.Advance(1);
            Expect.Near(1, without.Elapsed);

            ContentData data = TestContent.Data();
            data.Breaker = new BreakerData { Damage = 0, Interval = 1, Radius = 1 };
            ContentLoadResult result = ContentLoader.Load(data);
            Expect.True(!result.Succeeded, "피해가 0인 Breaker로는 조립할 수 없다.");
            TestContent.HasDiagnostic(result, "Breaker", "양수");
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

        // HQ 둘레 [2, 4] 띠에 count마리가 나오는 판. 모든 참가자가 이 Breaker를 받는다.
        private static ContentData Arena(float radius, int count, float health, float damage)
        {
            ContentData data = TestContent.Arena(2, 4, TestContent.Supply(TestContent.EnemyId, count));
            data.Enemies.Add(TestContent.Enemy(TestContent.EnemyId, health));
            TestContent.Allow(data, TestContent.EnemyId);
            data.Breaker = new BreakerData { Damage = damage, Interval = 1, Radius = radius };
            return data;
        }
    }
}
