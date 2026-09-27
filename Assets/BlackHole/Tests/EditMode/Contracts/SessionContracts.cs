using System;
using System.Collections.Generic;

namespace BlackHole.Core.Tests
{
    // Session: 조립(준비) → 시작 → 진행 → 종료 판정 → 결과 확정 → 원자료. 재시작은 새로 조립한다.
    internal static class SessionContracts
    {
        public static IEnumerable<Contract> Cases()
        {
            yield return new Contract("Session.PreparesThenBeginsOnce", PreparesThenBeginsOnce);
            yield return new Contract("Session.NeverAdvancesPastTimeLimit", NeverAdvancesPastTimeLimit);
            yield return new Contract("Session.EndsOnceAndIgnoresLaterRequests", EndsOnceAndIgnoresLaterRequests);
            yield return new Contract("Session.PauseFreezesTime", PauseFreezesTime);
            yield return new Contract("Session.RestartIsANewAssembly", RestartIsANewAssembly);
            yield return new Contract("Session.RejectsInvalidAdvance", RejectsInvalidAdvance);
            yield return new Contract("Session.RemembersSeed", RemembersSeed);
            yield return new Contract("Session.RawDataRecordsTheEndedBattle", RawDataRecordsTheEndedBattle);
            yield return new Contract("Session.UpgradesAreFixedAtAssembly", UpgradesAreFixedAtAssembly);
        }

        // 판 조립 때 방장의 산 노드로 업그레이드 표가 만들어진다. 표는 그 판의 것이라, 판이 끝난 뒤 산 노드가 바뀌어도 그대로다.
        // 다음 판은 그때의 산 노드로 새 표를 받는다. 노드 트리 없이 조립하면 빈 표다.
        private static void UpgradesAreFixedAtAssembly()
        {
            GameContent content = TestContent.Load(TestContent.Data());
            var data = new NodeTreeData();
            data.Nodes.Add(new NodeData
            {
                Id = "s",
                Price = 1,
                Start = true,
                Upgrades = { new UpgradeData { Stat = "damage", Operation = UpgradeOperation.Add, Value = 2 } },
            });
            NodeTree tree = NodeTreeLoader.Load(data).Tree;
            var buyer = new PlayerState(TestContent.First);
            buyer.EarnGold(1);
            NodePurchase.TryPurchase(buyer, tree, "s");

            GameSession battle = SessionAssembler.CreateBattle(content, buyer, 0, tree);
            Expect.Equal(12f, battle.Upgrades.Apply("damage", 10));

            battle.RequestEnd();
            ProgressCheats.LockAllNodes(buyer);
            Expect.Equal(12f, battle.Upgrades.Apply("damage", 10));

            GameSession next = SessionAssembler.CreateBattle(content, buyer, 0, tree);
            Expect.Equal(10f, next.Upgrades.Apply("damage", 10));
            next.RequestEnd();

            buyer.EarnGold(1);
            NodePurchase.TryPurchase(buyer, tree, "s");
            GameSession withoutTree = SessionAssembler.CreateBattle(content, buyer);
            Expect.Equal(10f, withoutTree.Upgrades.Apply("damage", 10));
        }

        // 조립한 판은 준비 단계다: 적이 없고 시간이 흐르지 않는다. 시작하면 전투 시작 공급이 나오고 시간이 흐른다.
        // 시작은 한 번뿐이다. 준비 단계에서도 판을 끝낼 수 있다.
        private static void PreparesThenBeginsOnce()
        {
            ContentData data = TestContent.Arena(2, 4, TestContent.Supply(TestContent.EnemyId, 2));
            data.Enemies.Add(TestContent.Enemy(TestContent.EnemyId));
            GameContent content = TestContent.Load(data);

            GameSession game = SessionAssembler.CreateBattle(content, new PlayerState(TestContent.First));
            Expect.Equal(SessionPhase.Preparing, game.Phase);
            Expect.Equal(0, game.World.Enemies.Count);
            game.Advance(1);
            game.TogglePause();
            Expect.Near(0, game.Elapsed);
            Expect.Equal(SessionPhase.Preparing, game.Phase);

            game.Begin();
            Expect.Equal(SessionPhase.Running, game.Phase);
            Expect.Equal(2, game.World.Enemies.Count);
            game.Advance(1);
            Expect.Near(1, game.Elapsed);
            Expect.Throws<InvalidOperationException>(() => game.Begin());

            var state = new PlayerState(TestContent.First);
            GameSession unused = SessionAssembler.CreateBattle(content, state);
            unused.RequestEnd();
            Expect.Equal(SessionPhase.Ended, unused.Phase);
            Expect.True(!state.InBattle, "준비 단계에서 끝나도 진행 상태를 풀어 줘야 한다.");
            Expect.Throws<InvalidOperationException>(() => unused.Begin());
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
            Expect.Near(0.75f, split.Result.PlayedSeconds);

            GameSession longFrame = TestContent.Session(TestContent.Data(timeLimit: 0.1f));
            longFrame.Advance(100);
            Expect.Near(0.1f, longFrame.Elapsed);
        }

        // 결과는 한 번 확정되고, 이후 진행·일시정지·종료 요청은 판을 바꾸지 않는다.
        private static void EndsOnceAndIgnoresLaterRequests()
        {
            GameSession game = TestContent.Session(TestContent.Data());
            game.Advance(1);
            game.RequestEnd();
            SessionResult result = game.Result;
            Expect.Near(1, result.PlayedSeconds);

            game.Advance(5);
            game.TogglePause();
            game.RequestEnd();
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
            GameSession first = TestContent.Begun(SessionAssembler.CreateBattle(content, state));
            first.Advance(10);
            first.RequestEnd();
            SessionResult result = first.Result;

            GameSession next = TestContent.Begun(SessionAssembler.CreateBattle(content, state));
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

        // 판은 자신을 조립한 seed를 기억한다. seed를 주지 않으면 기본 seed다.
        private static void RemembersSeed()
        {
            GameContent content = TestContent.Load(TestContent.Data());
            GameSession game = SessionAssembler.CreateBattle(content, new PlayerState(TestContent.First), 42);
            Expect.Equal(42, game.Seed);

            GameSession plain = SessionAssembler.CreateBattle(content, new PlayerState(TestContent.First));
            Expect.Equal(SessionAssembler.DefaultSeed, plain.Seed);
        }

        // 원자료는 끝난 판에서만 만든다: 조립 조건(seed), 진행 시간, 종류별 처치 수.
        private static void RawDataRecordsTheEndedBattle()
        {
            GameContent content = TestContent.Load(TestContent.Data());
            GameSession game = TestContent.Begun(
                SessionAssembler.CreateBattle(content, new PlayerState(TestContent.First), 99));
            game.Advance(2);
            Expect.Throws<InvalidOperationException>(() => game.CreateRawData());

            game.RequestEnd();
            BattleRawData raw = game.CreateRawData();
            Expect.Equal(99, raw.Seed);
            Expect.Near(2, raw.PlayedSeconds);
            Expect.Equal(0, raw.TotalKills);
            Expect.Equal(0, raw.Kills.Count);
        }
    }
}
