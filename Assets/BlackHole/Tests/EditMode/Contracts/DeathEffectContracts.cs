using System;
using System.Collections.Generic;
using BlackHole.Core;

namespace BlackHole.Core.Tests
{
    // 적의 사망 효과(GAME_RULES 11절). 첫 효과는 전기 연쇄 번개다.
    internal static class DeathEffectContracts
    {
        private static readonly Point2 East = new Point2(3, 0);

        // 원 위의 배치. n번째 적의 각도가 n × (π - 0.25)라 반경 3의 원 위에서 다음 자리에 놓인다.
        // spark 0, a π-0.25, b -0.5, c π-0.75, d -1.0.
        // 목록 순서는 spark, a, b, c, d이고 거리 순서와 다르다.
        // spark에서 b 1.48, d 2.88, c 5.58, a 5.95. b에서 d 1.48. d에서 b 1.48, a 5.58, c 5.95.
        private const float Scattered = (float)(Math.PI - 0.25);

        public static IEnumerable<Contract> Cases()
        {
            yield return new Contract("DeathEffect.ChainPicksNearestUnstruckUpToLimit", ChainPicksNearestUnstruckUpToLimit);
            yield return new Contract("DeathEffect.ChainStopsWithoutTargetInRange", ChainStopsWithoutTargetInRange);
            yield return new Contract("DeathEffect.SkipsEnemiesWithDeathEffect", SkipsEnemiesWithDeathEffect);
            yield return new Contract("DeathEffect.EffectKillsFollowDeathOnce", EffectKillsFollowDeathOnce);
            yield return new Contract("DeathEffect.EffectKillsCountBeforeGrowthAndTimeEnd", EffectKillsCountBeforeGrowthAndTimeEnd);
            yield return new Contract("DeathEffect.LongAdvanceKeepsAllHitsUntilNextAdvance", LongAdvanceKeepsAllHitsUntilNextAdvance);
            yield return new Contract("DeathEffect.SourceIsTheKiller", SourceIsTheKiller);
        }

        // 가까운 순서로 튄다(목록 순서가 아니다). 이미 맞힌 적은 다시 맞히지 않는다. 최대 Chains번.
        // spark → b → d → a. d에서 가장 가까운 b는 이미 맞았으므로 a로 간다. c는 네 번째라 맞지 않는다.
        private static void ChainPicksNearestUnstruckUpToLimit()
        {
            GameSession game = Aimed(Circle(range: 6, chains: 3));
            Enemy a = Find(game, "a");
            Enemy b = Find(game, "b");
            Enemy c = Find(game, "c");
            Enemy d = Find(game, "d");

            game.Advance(0.1f);

            IReadOnlyList<DeathEffectHit> hits = game.World.DeathEffectHits;
            Expect.Equal(3, hits.Count);
            Expect.Equal(b.Id, hits[0].Target);
            Expect.Equal(d.Id, hits[1].Target);
            Expect.Equal(a.Id, hits[2].Target);
            ExpectPoint(East, hits[0].From);
            ExpectPoint(b.Position, hits[0].To);
            ExpectPoint(b.Position, hits[1].From);
            ExpectPoint(d.Position, hits[2].From);
            Expect.Near(95, a.Health);
            Expect.Near(95, b.Health);
            Expect.Near(100, c.Health);
            Expect.Near(95, d.Health);
        }

        // 거리 안에 맞힐 적이 없으면 Chains가 남아도 멈춘다. spark → b → d. d에서 거리 3 안에는 이미 맞은 b뿐이다.
        private static void ChainStopsWithoutTargetInRange()
        {
            GameSession game = Aimed(Circle(range: 3, chains: 10));

            game.Advance(0.1f);

            Expect.Equal(2, game.World.DeathEffectHits.Count);
            Expect.Near(100, Find(game, "a").Health);
            Expect.Near(100, Find(game, "c").Health);
        }

        // 사망 효과는 사망 효과를 가진 적에게 피해를 주지 않는다. 가까운 twin을 건너뛰고 rock을 맞힌다.
        // rock에서 가장 가까운 twin도 대상이 아니라 번개가 끝난다.
        private static void SkipsEnemiesWithDeathEffect()
        {
            EnemyData twin = Still("twin", 100);
            twin.DeathEffect = TestContent.ChainLightning(5, 3, 3);
            ContentData data = Arena(0.5f, Spark("spark", 5, 3, 3), twin, Still("rock", 100));
            GameSession game = Aimed(data);
            Enemy twinEnemy = Find(game, "twin");
            Enemy rock = Find(game, "rock");

            game.Advance(0.1f);

            Expect.Equal(1, game.World.DeathEffectHits.Count);
            Expect.Equal(rock.Id, game.World.DeathEffectHits[0].Target);
            Expect.Near(100, twinEnemy.Health);
            Expect.Near(95, rock.Health);
        }

        // 효과로 죽은 적도 같은 사망 절차를 거친다: 보상 1회, 사망 기록, 목록에서 제외.
        // 죽은 적은 다시 고르지 않고, 번개는 죽은 자리에서 다음으로 튄다.
        // 두 Skill이 같은 단계에 spark를 노려도 사망과 효과는 한 번이다.
        private static void EffectKillsFollowDeathOnce()
        {
            EnemyData spark = Spark("spark", 5, 2, 5);
            spark.Gold = 1;
            spark.HqExp = 1;
            ContentData data = Arena(0.5f, spark, Rewarded("a"), Rewarded("b"), Rewarded("c"));
            data.Skills.Add(TestContent.Skill("second", 0.5f, 0.5f, 1));
            data.StartingSkills.Add("second");
            GameSession game = Aimed(data);

            game.Advance(0.1f);

            game.World.TryGetPlayer(TestContent.First, out Player player);
            Expect.Equal(3, game.World.DeathEffectHits.Count);
            Expect.Equal(4, game.World.Deaths.Count);
            Expect.Equal("spark", game.World.Deaths[0].EnemyTypeId);
            Expect.Equal("a", game.World.Deaths[1].EnemyTypeId);
            Expect.Equal("b", game.World.Deaths[2].EnemyTypeId);
            Expect.Equal("c", game.World.Deaths[3].EnemyTypeId);
            Expect.Equal(7, player.State.Gold);
            Expect.Equal(10, game.World.Hq.Exp);
            Expect.Equal(0, game.World.Enemies.Count);
        }

        // 효과 처치는 같은 단계의 처치다. 성장 진행과 종료 판정보다 먼저 처리되어,
        // 그 EXP로 도달한 성장의 공급과 시간 연장이 그 단계에 들어간다. spark 자체는 EXP가 없다.
        private static void EffectKillsCountBeforeGrowthAndTimeEnd()
        {
            EnemyData rock = Still("rock", 5);
            rock.HqExp = 1;
            ContentData data = Arena(0.5f, Spark("spark", 5, 2, 1), rock, Still("x", 100));
            data.StartSupply.RemoveAt(2);
            data.Session.TimeLimit = 0.02f;
            data.Growth.Levels.Add(TestContent.Level(1, 2, TestContent.Supply("x", 1)));
            GameSession game = Aimed(data);

            game.Advance(0.02f);

            Expect.Equal(SessionPhase.Running, game.Phase);
            Expect.Equal(2, game.World.Hq.Level);
            Expect.Near(2.02f, game.TimeLimit.Limit);
            Expect.Equal(1, game.World.Enemies.Count);
            Expect.Equal("x", game.World.Enemies[0].Definition.Id);
        }

        // 한 Advance의 서로 다른 하위 단계에서 생긴 적중이 모두 남는다. 일시정지 중에는 유지하고 다음 Advance에 비운다.
        // 세 적이 모두 East에 있다. spark는 0초 틱에, tough는 0.5초 틱에 죽고 각 번개가 rock을 맞힌다.
        private static void LongAdvanceKeepsAllHitsUntilNextAdvance()
        {
            EnemyData tough = Spark("tough", 5, 1, 1);
            tough.MaxHealth = 2;
            GameSession game = Aimed(Arena(0, Spark("spark", 5, 1, 1), tough, Still("rock", 100)));

            game.Advance(1.08f);
            IReadOnlyList<DeathEffectHit> hits = game.World.DeathEffectHits;
            Expect.Equal(2, hits.Count);
            Expect.True(hits[1].Sequence > hits[0].Sequence, "적중 기록의 순번은 증가해야 한다.");

            game.TogglePause();
            game.Advance(1);
            Expect.Equal(2, game.World.DeathEffectHits.Count);
            game.TogglePause();
            game.Advance(0.01f);
            Expect.Equal(0, game.World.DeathEffectHits.Count);
        }

        // 번개 피해의 출처는 spark를 죽인 Player다([임시]). 기록일 뿐이라 보상이 없는 판에서 두 Player로 본다.
        private static void SourceIsTheKiller()
        {
            ContentData data = Arena(0.5f, Spark("spark", 5, 2, 1), Still("rock", 100));
            GameSession game = TestContent.Session(data, TestContent.First, TestContent.Second);
            game.SetAimPoint(TestContent.First, new Point2(-3, 0));
            game.SetAimPoint(TestContent.Second, East);
            Enemy rock = Find(game, "rock");

            game.Advance(0.1f);

            Expect.Equal(1, game.World.DeathEffectHits.Count);
            Expect.True(rock.LastDamageSource.HasValue, "번개가 rock을 맞혀야 한다.");
            Expect.Equal(TestContent.Second, rock.LastDamageSource.Value);
        }

        // 원 위의 spark와 네 적(HP 100). 번개 피해는 5다.
        private static ContentData Circle(float range, int chains) =>
            Arena(
                Scattered,
                Spark("spark", 5, range, chains),
                Still("a", 100),
                Still("b", 100),
                Still("c", 100),
                Still("d", 100));

        // 모든 적은 HQ(0, 0)에서 거리 3, n번째 적은 각도 n × angleStep에 한 마리씩 나오고 거의 움직이지 않는다.
        // Skill은 East 반경 0.5, 0.5초마다 피해 1이라 각도 0의 적만 맞는다. 보상은 따로 정하지 않으면 없다.
        private static ContentData Arena(float angleStep, params EnemyData[] enemies)
        {
            ContentData data = TestContent.Data();
            data.Enemies = new List<EnemyData>(enemies);
            data.Spawn = new SpawnData { Distance = 3, AngleStep = angleStep };
            data.StartSupply = new List<SupplyData>();
            data.Skills = new List<SkillData> { TestContent.Skill(TestContent.SkillId, 0.5f, 0.5f, 1) };

            foreach (EnemyData enemy in enemies)
            {
                data.StartSupply.Add(TestContent.Supply(enemy.Id, 1));
            }

            return data;
        }

        // Skill 한 번에 죽는 연쇄 번개 적.
        private static EnemyData Spark(string id, float damage, float range, int chains)
        {
            EnemyData enemy = Still(id, 1);
            enemy.DeathEffect = TestContent.ChainLightning(damage, range, chains);
            return enemy;
        }

        // 번개 한 번에 죽는 적. Gold 2, HQ EXP 3.
        private static EnemyData Rewarded(string id)
        {
            EnemyData enemy = Still(id, 5);
            enemy.Gold = 2;
            enemy.HqExp = 3;
            return enemy;
        }

        private static EnemyData Still(string id, float health) =>
            TestContent.Enemy(id, health, 0.0001f, 0.3f);

        private static GameSession Aimed(ContentData data)
        {
            GameSession game = TestContent.Session(data);
            game.SetAimPoint(TestContent.First, East);
            return game;
        }

        private static Enemy Find(GameSession game, string id)
        {
            foreach (Enemy enemy in game.World.Enemies)
            {
                if (enemy.Definition.Id == id)
                    return enemy;
            }

            throw new InvalidOperationException($"적 '{id}'가 없다.");
        }

        private static void ExpectPoint(Point2 expected, Point2 actual)
        {
            Expect.Near(expected.X, actual.X);
            Expect.Near(expected.Y, actual.Y);
        }
    }
}
