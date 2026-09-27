using System;
using System.Collections.Generic;

namespace BlackHole.Core.Tests
{
    // 블랙홀 성장: 사망 순간의 EXP, Step 5 자리의 Level, 성장 노드를 산 뒤 Level업마다의 시간 연장·공급(BLACKHOLE_GROWTH_PLAN 4·7절),
    // 판을 넘어 이어지는 누적 EXP(BLACKHOLE_LEVEL_PLAN 4.1).
    internal static class GrowthContracts
    {
        private const string Rock = "rock";
        private const string Hidden = "hidden";

        public static IEnumerable<Contract> Cases()
        {
            yield return new Contract("Growth.ExpComesFromConfirmedDeathsOnly", ExpComesFromConfirmedDeathsOnly);
            yield return new Contract("Growth.WithoutGrowthNodesOnlyTheLevelRises", WithoutGrowthNodesOnlyTheLevelRises);
            yield return new Contract("Growth.OneGainCanRaiseSeveralLevels", OneGainCanRaiseSeveralLevels);
            yield return new Contract("Growth.LevelUpExtendsThisBattleAndRequestsSupply", LevelUpExtendsThisBattleAndRequestsSupply);
            yield return new Contract("Growth.GrowthOnTheLastStepKeepsTheBattleGoing", GrowthOnTheLastStepKeepsTheBattleGoing);
            yield return new Contract("Hq.LevelCarriesOverBetweenBattles", LevelCarriesOverBetweenBattles);
            yield return new Contract("Hq.CleanupGivesNoExp", CleanupGivesNoExp);
            yield return new Contract("Milestone.EndsTheBattleAtOnce", MilestoneEndsTheBattleAtOnce);
            yield return new Contract("Milestone.PaysTheFixedRewardInsteadOfEarnedGold", MilestonePaysTheFixedRewardInsteadOfEarnedGold);
            yield return new Contract("Milestone.IsReachedOnce", MilestoneIsReachedOnce);
            yield return new Contract("Hq.BattleCheatAddsExpForTheNextStep", BattleCheatAddsExpForTheNextStep);
            yield return new Contract("Growth.StopsAtTheLastLevel", StopsAtTheLastLevel);
            yield return new Contract("Growth.EndedBattleDoesNotGrow", EndedBattleDoesNotGrow);
        }

        // 사망이 확정되는 순간 그 색 등급의 EXP가 블랙홀에 든다. 황금이어도 EXP는 같다(Gold만 곱한다).
        // 파괴 요청의 사망도 준다. 전투 정리로 치운 적은 주지 않는다. Level은 Step에서만 오른다.
        private static void ExpComesFromConfirmedDeathsOnly()
        {
            ContentData data = Arena(exp: 3, 100);
            data.Enemies[0].GoldenMultiplier = 50;
            data.Enemies[0].Tiers[0].Gold = 2;
            GameContent content = TestContent.Load(data);
            content.TryGetEnemy(Rock, out EnemyDefinition rock);
            GameSession game = Grown(content, new Upgrade(EnemyUpgradeStats.GoldenRatio(Rock), UpgradeOperation.Add, 1));
            World world = game.World;

            Enemy golden = world.Enemies[0];
            Expect.True(golden.IsGolden, "황금 비율 1이면 모두 황금이다.");
            Expect.Equal(100L, golden.Stats.Gold);
            Expect.Equal(3L, golden.Stats.Exp);

            Kill(world, golden);
            Expect.Equal(3L, world.Hq.Exp);
            Expect.Equal(1, world.Hq.Level);

            world.RequestDestroy(world.Enemies[0]);
            game.Advance(0.1f);
            Expect.Equal(6L, world.Hq.Exp);

            game.RequestEnd();
            Expect.Equal(1, game.ClearRemainingEnemies());
            Expect.Equal(6L, world.Hq.Exp);
        }

        // 성장 노드를 사기 전: Level은 오르지만 제한 시간과 적 수는 그대로다. 진행 막대는 지금 Level에서 다음 Level까지의 몫이다.
        private static void WithoutGrowthNodesOnlyTheLevelRises()
        {
            GameContent content = TestContent.Load(Arena(exp: 5, 5, 15));
            GameSession game = TestContent.Begun(SessionAssembler.CreateBattle(content, new PlayerState(TestContent.First)));
            World world = game.World;
            float limit = game.TimeLimit.Limit;
            Expect.Near(0, world.Hq.GrowthTime);
            Expect.Near(0, world.Hq.Progress);

            Kill(world, world.Enemies[0]);
            game.Advance(0.1f);

            Expect.Equal(2, world.Hq.Level);
            Expect.Equal(15L, world.Hq.NextLevelExp.Value);
            Expect.Near(0, world.Hq.Progress);
            Expect.Near(limit, game.TimeLimit.Limit);
            Expect.Equal(2, world.Enemies.Count);

            Kill(world, world.Enemies[0]);
            Expect.Near(0.5f, world.Hq.Progress);
        }

        // 한 Step에서 임계값 여럿을 넘으면 그만큼 오르고, 성장 효과는 오른 Level마다 한 번씩이다.
        private static void OneGainCanRaiseSeveralLevels()
        {
            GameContent content = TestContent.Load(Arena(exp: 5, 2, 4, 6));
            GameSession game = Grown(content, GrowthTime(3), GrowthSupply(Rock, 1));
            World world = game.World;
            float limit = game.TimeLimit.Limit;

            Kill(world, world.Enemies[0]);
            Expect.Equal(1, world.Hq.Level);

            game.Advance(0.1f);
            Expect.Equal(3, world.Hq.Level);
            Expect.Near(limit + 2 * 3, game.TimeLimit.Limit);
            Expect.Equal(2 + 2, world.Enemies.Count);
            Expect.Equal(0, world.PendingSpawns.Count);
        }

        // 성장 노드를 사면 Level업마다 이 판의 제한 시간이 늘고, 성장 공급이 같은 Step의 공급 처리에서 나온다.
        // 성장 공급도 생성 요청이라 전체 상한에 닿으면 버린다(여기서는 rock 둘 중 하나만 들어가고 hidden은 모두 버려진다).
        private static void LevelUpExtendsThisBattleAndRequestsSupply()
        {
            ContentData data = Arena(exp: 5, 5);
            EnemyData hidden = Kind(Hidden, exp: 5);
            data.Enemies.Add(hidden);
            data.MaxAliveEnemies = 3;
            GameContent content = TestContent.Load(data);
            content.TryGetEnemy(Rock, out EnemyDefinition rock);
            content.TryGetEnemy(Hidden, out EnemyDefinition hiddenKind);
            GameSession game = Grown(content, GrowthTime(3), GrowthSupply(Rock, 2), GrowthSupply(Hidden, 2));
            World world = game.World;
            float limit = game.TimeLimit.Limit;
            Expect.Near(3, world.Hq.GrowthTime);
            Expect.Equal(2, world.Stats.CompositionOf(rock).GrowthSupply);

            Kill(world, world.Enemies[0]);
            game.Advance(0.1f);

            Expect.Equal(2, world.Hq.Level);
            Expect.Near(limit + 3, game.TimeLimit.Limit);
            Expect.Equal(3, world.CountAlive(rock));
            Expect.Equal(0, world.CountAlive(hiddenKind));
            Expect.Equal(0, world.PendingSpawns.Count);
        }

        // 늘어난 시간은 종료 판정보다 먼저다. 마지막 Step에서 성장하면 판이 이어진다. 성장 노드가 없으면 그 Step에 끝난다.
        private static void GrowthOnTheLastStepKeepsTheBattleGoing()
        {
            ContentData data = Arena(exp: 5, 5);
            data.Session.TimeLimit = 1;
            GameContent content = TestContent.Load(data);

            GameSession grown = Grown(content, GrowthTime(3));
            grown.Advance(0.9f);
            Kill(grown.World, grown.World.Enemies[0]);
            grown.Advance(0.5f);
            Expect.Near(1, grown.Elapsed);
            Expect.Equal(SessionPhase.Running, grown.Phase);
            Expect.Near(3, grown.Remaining);

            GameSession plain = TestContent.Begun(SessionAssembler.CreateBattle(content, new PlayerState(TestContent.First)));
            plain.Advance(0.9f);
            Kill(plain.World, plain.World.Enemies[0]);
            plain.Advance(0.5f);
            Expect.Equal(2, plain.World.Hq.Level);
            Expect.Equal(SessionPhase.Ended, plain.Phase);
        }

        // 블랙홀 Level은 판을 넘어 이어진다: 결산이 누적 EXP를 진행 상태에 돌려놓고, 다음 판은 그 EXP·Level에서 시작한다.
        // 이어받은 Level의 성장 효과는 다시 오지 않는다. 제한 시간은 다시 기본값이다(늘어난 시간은 그 판에만). 새 진행은 Level 1이다.
        private static void LevelCarriesOverBetweenBattles()
        {
            GameContent content = TestContent.Load(Arena(exp: 5, 5, 15));
            var state = new PlayerState(TestContent.First);
            NodeTree tree = TestContent.Owned(state, GrowthTime(3));

            GameSession fresh = SessionAssembler.CreateBattle(content, new PlayerState(TestContent.Second));
            Expect.Equal(1, fresh.World.Hq.Level);
            Expect.Equal(0L, fresh.World.Hq.Exp);

            GameSession first = TestContent.Begun(SessionAssembler.CreateBattle(content, state, 0, tree));
            Kill(first.World, first.World.Enemies[0]);
            Kill(first.World, first.World.Enemies[0]);
            first.Advance(0.1f);
            Expect.Equal(2, first.World.Hq.Level);
            Expect.Near(content.TimeLimit.Duration + 3, first.TimeLimit.Limit);
            first.RequestEnd();
            Expect.Equal(0L, state.HqExp);
            first.Settle();
            Expect.Equal(10L, state.HqExp);

            GameSession next = TestContent.Begun(SessionAssembler.CreateBattle(content, state, 0, tree));
            Expect.Equal(2, next.World.Hq.Level);
            Expect.Equal(2, next.World.Hq.StartLevel);
            Expect.Equal(10L, next.World.Hq.Exp);
            Expect.Near(0.5f, next.World.Hq.Progress);
            Expect.Near(content.TimeLimit.Duration, next.TimeLimit.Limit);

            next.Advance(0.1f);
            Expect.Near(content.TimeLimit.Duration, next.TimeLimit.Limit);
            Expect.Equal(3, next.World.Enemies.Count);
        }

        // 전투 정리로 치운 적은 EXP를 주지 않는다. 결산이 돌려놓는 것은 확정된 사망의 EXP뿐이다(시간 종료·요청 종료 모두).
        private static void CleanupGivesNoExp()
        {
            ContentData data = Arena(exp: 5, 100);
            data.Session.TimeLimit = 1;
            GameContent content = TestContent.Load(data);

            var timedOut = new PlayerState(TestContent.First);
            GameSession expired = TestContent.Begun(SessionAssembler.CreateBattle(content, timedOut));
            Kill(expired.World, expired.World.Enemies[0]);
            expired.Advance(2);
            Expect.Equal(SessionPhase.Ended, expired.Phase);
            Expect.Equal(2, expired.ClearRemainingEnemies());
            expired.Settle();
            Expect.Equal(5L, timedOut.HqExp);

            var requested = new PlayerState(TestContent.First);
            GameSession ended = TestContent.Begun(SessionAssembler.CreateBattle(content, requested));
            ended.RequestEnd();
            ended.ClearRemainingEnemies();
            ended.Settle();
            Expect.Equal(0L, requested.HqExp);
        }

        // Level 표의 끝에서는 EXP만 쌓이고 성장 효과도 없다. 진행 막대는 가득 찬 것으로 보인다.
        private static void StopsAtTheLastLevel()
        {
            GameContent content = TestContent.Load(Arena(exp: 5, 5));
            GameSession game = Grown(content, GrowthTime(3));
            World world = game.World;
            float limit = game.TimeLimit.Limit;

            Kill(world, world.Enemies[0]);
            game.Advance(0.1f);
            Kill(world, world.Enemies[0]);
            game.Advance(0.1f);

            Expect.Equal(2, world.Hq.Level);
            Expect.True(world.Hq.IsMaxLevel, "표 끝이다.");
            Expect.True(!world.Hq.NextLevelExp.HasValue, "다음 Level이 없다.");
            Expect.Near(1, world.Hq.Progress);
            Expect.Equal(10L, world.Hq.Exp);
            Expect.Near(limit + 3, game.TimeLimit.Limit);
        }

        // 끝난 판은 Step이 없으므로 Level이 오르지 않고 시간도 늘지 않는다.
        private static void EndedBattleDoesNotGrow()
        {
            GameContent content = TestContent.Load(Arena(exp: 5, 5));
            GameSession game = Grown(content, GrowthTime(3));
            float limit = game.TimeLimit.Limit;

            Kill(game.World, game.World.Enemies[0]);
            game.RequestEnd();
            game.Advance(0.1f);

            Expect.Equal(1, game.World.Hq.Level);
            Expect.Near(limit, game.TimeLimit.Limit);
        }

        // 이정표 Level에 닿은 Step에서 판이 끝난다: 남은 시간이 있어도, 그 Step의 성장 효과(시간 연장·성장 공급) 없이.
        // 이정표가 아닌 Level업에서는 판이 이어진다.
        private static void MilestoneEndsTheBattleAtOnce()
        {
            GameContent content = TestContent.Load(MilestoneArena());
            GameSession game = Grown(content, GrowthTime(3), GrowthSupply(Rock, 2));
            World world = game.World;
            float limit = game.TimeLimit.Limit;

            Kill(world, world.Enemies[0]);
            game.Advance(0.1f);
            Expect.Equal(2, world.Hq.Level);
            Expect.Equal(SessionPhase.Running, game.Phase);
            Expect.True(!world.Hq.ReachedMilestone, "Level 2는 이정표가 아니다.");
            Expect.Equal(2 + 2, world.Enemies.Count);

            Kill(world, world.Enemies[0]);
            game.Advance(0.1f);
            Expect.Equal(3, world.Hq.Level);
            Expect.Equal(SessionPhase.Ended, game.Phase);
            Expect.True(game.Remaining > 0, "남은 시간이 있어도 끝난다.");
            Expect.Near(limit + 3, game.TimeLimit.Limit);
            Expect.Equal(3, world.Enemies.Count);
            Expect.Equal(0, world.PendingSpawns.Count);
            Expect.Equal(1, world.Hq.ReachedMilestones.Count);
            Expect.Equal(3, world.Hq.ReachedMilestones[0].Level);
        }

        // 이정표로 끝난 판의 결산은 번 Gold 대신 이정표의 고정 보상을 더한다. 확정된 사망의 EXP는 남는다.
        private static void MilestonePaysTheFixedRewardInsteadOfEarnedGold()
        {
            GameContent content = TestContent.Load(MilestoneArena());
            var state = new PlayerState(TestContent.First);
            GameSession game = TestContent.Begun(SessionAssembler.CreateBattle(content, state));

            Kill(game.World, game.World.Enemies[0]);
            Kill(game.World, game.World.Enemies[0]);
            game.Advance(0.1f);
            Expect.Equal(SessionPhase.Ended, game.Phase);
            Expect.Equal(14L, game.World.EarnedGold);
            Expect.Equal(1000L, game.SettledGold);

            game.ClearRemainingEnemies();
            BattleRawData raw = game.CreateRawData();
            game.Settle();
            game.Settle();

            Expect.Equal(1000L, state.Gold);
            Expect.Equal(10L, state.HqExp);
            Expect.Equal(14L, raw.EarnedGold);
            Expect.Equal(1000L, raw.SettledGold);
            Expect.Equal(1, raw.Milestones.Count);
            Expect.Equal(1, content.Growth.MilestonesReachedBy(raw.ReachedLevel));
        }

        // 이정표마다 한 번이다: Level은 줄지 않으므로, 다음 판은 이미 지난 이정표로 끝나거나 보상받지 않는다.
        private static void MilestoneIsReachedOnce()
        {
            GameContent content = TestContent.Load(MilestoneArena());
            var state = new PlayerState(TestContent.First);
            TestContent.GrowHq(content, state, 10);

            GameSession next = TestContent.Begun(SessionAssembler.CreateBattle(content, state));
            Expect.Equal(3, next.World.Hq.Level);
            Kill(next.World, next.World.Enemies[0]);
            next.Advance(0.1f);
            Expect.Equal(SessionPhase.Running, next.Phase);
            Expect.True(!next.World.Hq.ReachedMilestone, "이미 지난 이정표다.");

            next.RequestEnd();
            next.Settle();
            Expect.Equal(7L, state.Gold);
        }

        // 개발용 전투 치트: 판의 블랙홀에 EXP를 더하면 다음 Step에서 평소처럼 Level이 오르고 이정표도 판정된다. 끝난 판은 거부한다.
        private static void BattleCheatAddsExpForTheNextStep()
        {
            GameContent content = TestContent.Load(MilestoneArena());
            var state = new PlayerState(TestContent.First);
            GameSession game = TestContent.Begun(SessionAssembler.CreateBattle(content, state));

            BattleCheats.AddHqExp(game, 10);
            Expect.Equal(1, game.World.Hq.Level);
            game.Advance(0.1f);
            Expect.Equal(3, game.World.Hq.Level);
            Expect.Equal(SessionPhase.Ended, game.Phase);
            Expect.Throws<InvalidOperationException>(() => BattleCheats.AddHqExp(game, 1));

            game.Settle();
            Expect.Equal(10L, state.HqExp);
            Expect.Equal(1000L, state.Gold);
        }

        // Level 표 5·10·20(Level 2·3·4), Level 3이 이정표(보상 1000). 적은 EXP 5, Gold 7.
        private static ContentData MilestoneArena()
        {
            ContentData data = Arena(exp: 5, 5, 10, 20);
            data.Enemies[0].Tiers[0].Gold = 7;
            data.Growth.Milestones.Add(new HqMilestoneData { Level = 3, Reward = 1000 });
            return data;
        }

        // 소행성 격인 종류(체력 1, 색 하나, 이 EXP) 셋이 나오는 판과 Level 표.
        private static ContentData Arena(long exp, params long[] levelExp)
        {
            ContentData data = TestContent.Arena(2, 4, TestContent.Supply(Rock, 3));
            data.Enemies.Add(Kind(Rock, exp));
            data.Growth = new HqGrowthData { LevelExp = new List<long>(levelExp) };
            return data;
        }

        private static EnemyData Kind(string id, long exp)
        {
            EnemyData kind = TestContent.Enemy(id, health: 1);
            kind.Tiers[0].Exp = exp;
            return kind;
        }

        // 이 업그레이드들을 산 새 진행 상태로 판을 조립하고 시작한다.
        private static GameSession Grown(GameContent content, params Upgrade[] upgrades)
        {
            var state = new PlayerState(TestContent.First);
            NodeTree tree = TestContent.Owned(state, upgrades);
            return TestContent.Begun(SessionAssembler.CreateBattle(content, state, 0, tree));
        }

        private static Upgrade GrowthTime(float seconds) =>
            new Upgrade(HqUpgradeStats.GrowthTime, UpgradeOperation.Add, seconds);

        private static Upgrade GrowthSupply(string kindId, int count) =>
            new Upgrade(EnemyUpgradeStats.GrowthSupply(kindId), UpgradeOperation.Add, count);

        private static void Kill(World world, Enemy enemy) =>
            Expect.True(world.DealDamage(enemy, new Damage(10, TestContent.First)), "살아 있는 적이 죽어야 한다.");
    }
}
