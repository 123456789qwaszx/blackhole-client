using System;
using System.Collections.Generic;

namespace BlackHole.Core.Tests
{
    // Session: 시작 → 진행 → 종료 판정 → 결과 확정 → 재시작(새로 조립).
    internal static class SessionContracts
    {
        public static IEnumerable<Contract> Cases()
        {
            yield return new Contract("Session.NeverAdvancesPastTimeLimit", NeverAdvancesPastTimeLimit);
            yield return new Contract("Session.EndsOnceAndIgnoresLaterRequests", EndsOnceAndIgnoresLaterRequests);
            yield return new Contract("Session.PauseFreezesTime", PauseFreezesTime);
            yield return new Contract("Session.RestartIsANewAssembly", RestartIsANewAssembly);
            yield return new Contract("Session.RejectsInvalidAdvance", RejectsInvalidAdvance);
            yield return new Contract("Session.RemembersStageAndSeed", RemembersStageAndSeed);
            yield return new Contract("Session.RejectsStageOutsideContent", RejectsStageOutsideContent);
        }

        // 판은 자신을 조립한 진행도(적의 강도 단계)와 seed를 기억한다. 단계를 주지 않으면 첫 단계다.
        private static void RemembersStageAndSeed()
        {
            GameContent content = TestContent.Load(TestContent.Data());
            GameSession game = SessionAssembler.CreateBattle(content, new[] { new PlayerState(TestContent.First) }, 7, 42);
            Expect.Equal(7, game.Stage);
            Expect.Equal(42, game.Seed);

            GameSession plain = SessionAssembler.CreateBattle(content, new[] { new PlayerState(TestContent.First) });
            Expect.Equal(SessionAssembler.FirstStage, plain.Stage);
            Expect.Equal(SessionAssembler.DefaultSeed, plain.Seed);
        }

        // 단계는 1부터 콘텐츠의 단계 수까지다. 범위 밖이면 조립하지 않고, 진행 상태를 전투에 묶지 않는다.
        private static void RejectsStageOutsideContent()
        {
            GameContent content = TestContent.Load(TestContent.Data());
            var state = new PlayerState(TestContent.First);

            Expect.Throws<ArgumentOutOfRangeException>(() => SessionAssembler.CreateBattle(content, new[] { state }, 0, 1));
            Expect.Throws<ArgumentOutOfRangeException>(() =>
                SessionAssembler.CreateBattle(content, new[] { state }, TestContent.StageCount + 1, 1));
            Expect.True(!state.InBattle, "실패한 조립이 PlayerState를 묶으면 안 된다.");

            GameSession last = SessionAssembler.CreateBattle(content, new[] { state }, TestContent.StageCount, 1);
            Expect.Equal(TestContent.StageCount, last.Stage);
        }

        // 진행 전 제한: 마지막 프레임이 제한 시간을 넘으면 넘는 만큼 진행하지 않는다.
        private static void NeverAdvancesPastTimeLimit()
        {
            GameSession split = TestContent.Session(TestContent.Data(timeLimit: 0.75f));
            split.Advance(0.5f);
            Expect.Equal(SessionPhase.Running, split.Phase);
            split.Advance(0.5f);
            Expect.Equal(SessionPhase.Ended, split.Phase);
            Expect.Near(0.75f, split.Elapsed);
            Expect.Near(0, split.Remaining);
            Expect.Equal(SessionEndReason.TimeExpired, split.Result.Reason);
            Expect.Near(0.75f, split.Result.PlayedSeconds);

            GameSession longFrame = TestContent.Session(TestContent.Data(timeLimit: 0.1f));
            longFrame.Advance(100);
            Expect.Near(0.1f, longFrame.Elapsed);
        }

        // 결과는 한 번 확정되고, 이후 진행·일시정지·정지 요청은 판을 바꾸지 않는다.
        private static void EndsOnceAndIgnoresLaterRequests()
        {
            GameSession game = TestContent.Session(TestContent.Data());
            game.Advance(1);
            game.Stop();
            SessionResult result = game.Result;
            Expect.Equal(SessionEndReason.Stopped, result.Reason);
            Expect.Near(1, result.PlayedSeconds);

            game.Advance(5);
            game.TogglePause();
            game.Stop();
            Expect.Equal(SessionPhase.Ended, game.Phase);
            Expect.Near(1, game.Elapsed);
            Expect.True(ReferenceEquals(result, game.Result), "결과를 다시 만들면 안 된다.");
        }

        private static void PauseFreezesTime()
        {
            GameSession game = TestContent.Session(TestContent.Data(timeLimit: 10));
            game.TogglePause();
            Expect.Equal(SessionPhase.Paused, game.Phase);
            game.Advance(5);
            Expect.Near(0, game.Elapsed);
            Expect.Near(10, game.Remaining);

            game.TogglePause();
            game.Advance(1);
            Expect.Near(1, game.Elapsed);
        }

        // 같은 진행 상태로 만든 다음 전투는 앞 전투의 시간·결과를 이어받지 않는다. 끝난 전투의 결과도 그대로다.
        private static void RestartIsANewAssembly()
        {
            GameContent content = TestContent.Load(TestContent.Data());
            var state = new PlayerState(TestContent.First);
            GameSession first = SessionAssembler.CreateBattle(content, new[] { state });
            first.Advance(10);
            first.Stop();
            SessionResult result = first.Result;

            GameSession next = SessionAssembler.CreateBattle(content, new[] { state });
            Expect.Equal(SessionPhase.Running, next.Phase);
            Expect.Near(0, next.Elapsed);
            Expect.True(next.Result == null, "새 판에는 결과가 없어야 한다.");

            next.Advance(1);
            Expect.True(ReferenceEquals(result, first.Result), "끝난 전투의 결과를 다시 만들면 안 된다.");
            Expect.Near(10, first.Result.PlayedSeconds);
        }

        private static void RejectsInvalidAdvance()
        {
            GameSession game = TestContent.Session(TestContent.Data());
            Expect.Throws<ArgumentOutOfRangeException>(() => game.Advance(float.NaN));
            Expect.Throws<ArgumentOutOfRangeException>(() => game.Advance(-1));
        }
    }
}
