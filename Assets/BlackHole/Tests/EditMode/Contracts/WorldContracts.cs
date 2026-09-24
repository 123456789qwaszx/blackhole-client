using System;
using System.Collections.Generic;

namespace BlackHole.Core.Tests
{
    // B1 Player 단위, B2 HQ 기준점.
    internal static class WorldContracts
    {
        public static IEnumerable<Contract> Cases()
        {
            yield return new Contract("World.HqPositionComesFromContent", HqPositionComesFromContent);
            yield return new Contract("World.PlayersAreAListNotASingleton", PlayersAreAListNotASingleton);
            yield return new Contract("World.ParticipantsMustBeValid", ParticipantsMustBeValid);
            yield return new Contract("World.ReadingRecordsDoesNotChangeGameplay", ReadingRecordsDoesNotChangeGameplay);
        }

        // 표현은 기록과 실행 상태를 읽기만 한다(CA-005). 매 프레임 모두 읽는 전투와 전혀 읽지 않는 전투의 결과가 같다.
        // 읽기가 기록을 소비하거나 상태를 바꾸면 이 계약이 깨진다.
        private static void ReadingRecordsDoesNotChangeGameplay()
        {
            ContentData data = TestContent.LaserArena(enemies: 30, angleStep: (float)(Math.PI / 15), interval: 0.4f, damage: 4, width: 0.8f, telegraph: 0.3f);
            data.Enemies[0].MaxHealth = 10;
            data.Enemies[0].HqExp = 1;
            EnemyData spark = TestContent.Enemy("spark", 10, 0.0001f, 0.3f);
            spark.HqExp = 1;
            spark.DeathEffect = TestContent.ChainLightning(damage: 6, range: 2, chains: 3);
            data.Enemies.Add(spark);
            data.StartSupply.Add(TestContent.Supply("spark", 6));
            data.Skills.Add(TestContent.Skill(TestContent.SkillId, radius: 1.5f, interval: 0.5f, damage: 3));
            data.StartingSkills.Add(TestContent.SkillId);

            GameSession watched = TestContent.Session(data, seed: 5);
            GameSession unwatched = TestContent.Session(data, seed: 5);
            var aim = new Point2(3, 0);
            watched.SetAimPoint(TestContent.First, aim);
            unwatched.SetAimPoint(TestContent.First, aim);

            long seen = 0;
            for (int frame = 0; frame < 240; frame++)
            {
                watched.Advance(1f / 60f);
                unwatched.Advance(1f / 60f);

                World world = watched.World;
                seen += world.Deaths.Count + world.DeathEffectHits.Count + world.LaserFires.Count;
                foreach (DeathRecord death in world.Deaths) seen += death.Sequence;
                foreach (DeathEffectHit hit in world.DeathEffectHits) seen += hit.Sequence;
                foreach (LaserFireRecord fire in world.LaserFires) seen += fire.HitCount;
                foreach (PassiveSkill skill in world.Players[0].Skills)
                {
                    if (skill is BreakerSkill breaker) seen += breaker.TickCount + breaker.LastTickHitCount;
                    if (skill is PiercingLaserSkill laser) seen += laser.PendingShots.Count;
                }
            }

            Expect.True(seen > 0, "읽은 기록이 있어야 이 계약이 뜻을 가진다.");
            Expect.Equal(unwatched.World.Hq.Exp, watched.World.Hq.Exp);
            Expect.Equal(unwatched.World.Enemies.Count, watched.World.Enemies.Count);
            for (int i = 0; i < watched.World.Enemies.Count; i++)
            {
                Expect.Equal(unwatched.World.Enemies[i].Id, watched.World.Enemies[i].Id);
                Expect.Near(unwatched.World.Enemies[i].Health, watched.World.Enemies[i].Health);
            }
            Expect.True(watched.World.Hq.Exp > 0, "처치가 있어야 이 계약이 뜻을 가진다.");
        }

        // HQ는 원점이 아니라 콘텐츠가 정한 위치에 있다. 이후 출현·행동은 이 위치를 읽는다.
        private static void HqPositionComesFromContent()
        {
            GameSession game = TestContent.Session(TestContent.Data(hqX: 3, hqY: -2));
            Expect.Equal(new Point2(3, -2), game.World.Hq.Position);
        }

        // 게임 콘텐츠는 1명이지만 Player는 목록이다. 2명 판도 만들어지고, 각자 자기 상태를 가진다.
        private static void PlayersAreAListNotASingleton()
        {
            GameSession single = TestContent.Session(TestContent.Data());
            Expect.Equal(1, single.World.Players.Count);
            Expect.True(single.World.TryGetPlayer(TestContent.First, out Player first), "참가한 Player를 찾아야 한다.");
            Expect.Equal(TestContent.First, first.Id);
            Expect.True(!single.World.TryGetPlayer(TestContent.Second, out _), "참가하지 않은 Player는 없어야 한다.");

            GameSession pair = TestContent.Session(TestContent.Data(), TestContent.First, TestContent.Second);
            Expect.Equal(2, pair.World.Players.Count);
            pair.World.TryGetPlayer(TestContent.First, out Player a);
            pair.World.TryGetPlayer(TestContent.Second, out Player b);
            Expect.True(!ReferenceEquals(a, b), "Player는 서로 다른 개체여야 한다.");
            Expect.True(!ReferenceEquals(a.State, b.State), "PlayerState는 Player마다 따로 있어야 한다.");
        }

        // 누가 참가하는지는 호스트가 정한다. 비었거나 중복된 참가자는 판 조립 오류다.
        private static void ParticipantsMustBeValid()
        {
            GameContent content = TestContent.Load(TestContent.Data());
            Expect.Throws<ArgumentException>(() => SessionAssembler.Create(content, new PlayerId[0]));
            Expect.Throws<ArgumentException>(() => SessionAssembler.Create(content, null));
            Expect.Throws<ArgumentException>(() =>
                SessionAssembler.Create(content, new[] { TestContent.First, TestContent.First }));
        }
    }
}
