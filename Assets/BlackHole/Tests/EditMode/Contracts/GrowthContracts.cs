using System;
using System.Collections.Generic;

namespace BlackHole.Core.Tests
{
    // 블랙홀 성장: 사망 순간의 EXP, Step 5 자리의 Level, 성장 노드를 산 뒤 Level업마다의 시간 연장·공급(BLACKHOLE_GROWTH_PLAN 4·7절).
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
            yield return new Contract("Growth.EachBattleStartsFresh", EachBattleStartsFresh);
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
        // 성장 공급도 생성 요청이라 잠긴 종류는 나오지 않고, 전체 상한에 닿으면 버린다.
        private static void LevelUpExtendsThisBattleAndRequestsSupply()
        {
            ContentData data = Arena(exp: 5, 5);
            EnemyData hidden = Kind(Hidden, exp: 5);
            hidden.StartsLocked = true;
            data.Enemies.Add(hidden);
            data.MaxAliveEnemies = 3;
            GameContent content = TestContent.Load(data);
            content.TryGetEnemy(Rock, out EnemyDefinition rock);
            content.TryGetEnemy(Hidden, out EnemyDefinition locked);
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
            Expect.Equal(0, world.CountAlive(locked));
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

        // 새 판은 Level 1, EXP 0이고 제한 시간은 콘텐츠의 기본값이다. 앞 판에서 늘어난 시간은 이어지지 않는다.
        private static void EachBattleStartsFresh()
        {
            GameContent content = TestContent.Load(Arena(exp: 5, 5));
            var state = new PlayerState(TestContent.First);
            NodeTree tree = TestContent.Owned(state, GrowthTime(3));

            GameSession first = TestContent.Begun(SessionAssembler.CreateBattle(content, state, 0, tree));
            Kill(first.World, first.World.Enemies[0]);
            first.Advance(0.1f);
            Expect.Equal(2, first.World.Hq.Level);
            Expect.Near(content.TimeLimit.Duration + 3, first.TimeLimit.Limit);
            first.RequestEnd();

            GameSession next = TestContent.Begun(SessionAssembler.CreateBattle(content, state, 0, tree));
            Expect.Equal(1, next.World.Hq.Level);
            Expect.Equal(0L, next.World.Hq.Exp);
            Expect.Near(content.TimeLimit.Duration, next.TimeLimit.Limit);
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
