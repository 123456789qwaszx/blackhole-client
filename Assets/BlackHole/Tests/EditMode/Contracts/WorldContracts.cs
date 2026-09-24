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
