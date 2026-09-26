using System.Collections.Generic;

namespace BlackHole.Core.Tests
{
    // 처치 버프: 달(공격 주기 감소)·혜성(확정 치명타)의 사망 효과. Breaker에만 붙는다(SKILL_SYSTEM_PLAN D2·D3, SK-006).
    // Breaker와 레이저의 원·굵기를 판 전체보다 크게 잡아, 적의 무작위 배치와 관계없이 누가 맞는지 정해지게 한다.
    internal static class BuffContracts
    {
        private const string Normal = "normal";
        private const string Haste = "haste";
        private const string Critical = "critical";

        public static IEnumerable<Contract> Cases()
        {
            yield return new Contract("Buff.StartsNextStepAndExpires", StartsNextStepAndExpires);
            yield return new Contract("Buff.HasteKeepsTimerProgress", HasteKeepsTimerProgress);
            yield return new Contract("Buff.KillBuffsApplyOnlyToBreaker", KillBuffsApplyOnlyToBreaker);
            yield return new Contract("Buff.GoesToTheSingleParticipantOnly", GoesToTheSingleParticipantOnly);
        }

        // 버프는 그 처치가 난 Step의 공격에는 들지 않고(사망 효과는 공격 뒤 4 자리), 다음 Step부터 적용된다.
        // 시간이 끝나면 원래 주기로 돌아간다. 한 Step의 공격은 Step을 시작할 때의 버프로 한다.
        private static void StartsNextStepAndExpires()
        {
            GameSession game = TestContent.Session(Arena(haste: true, critical: false, hasteDuration: 2));
            BreakerSkill breaker = game.World.Players[0].Breaker;
            game.SetAimPoint(TestContent.First, BattleSpace.Origin);

            game.Advance(0.1f);
            Expect.Equal(1, breaker.TickCount);
            Expect.Near(2, breaker.HasteRemaining);
            Expect.Near(0.5f, breaker.HasteMultiplier);

            // 다음 Tick까지 남은 0.9초분이 두 배 빠르게 찬다.
            game.Advance(0.45f);
            Expect.Equal(2, breaker.TickCount);

            game.Advance(1.6f);
            Expect.Equal(5, breaker.TickCount);
            Expect.Near(0, breaker.HasteRemaining);
            Expect.Near(1, breaker.HasteMultiplier);

            // 버프가 끝났으니 남은 0.8초분이 제 속도로 찬다.
            game.Advance(0.4f);
            Expect.Equal(5, breaker.TickCount);
            game.Advance(0.4f);
            Expect.Equal(6, breaker.TickCount);
        }

        // 버프를 얻는 순간 돌던 주기의 진행률은 그대로이고, 남은 부분만 빨리 찬다.
        private static void HasteKeepsTimerProgress()
        {
            GameSession game = TestContent.Session(Arena(haste: true, critical: false, hasteDuration: 2, buffHealth: 100));
            World world = game.World;
            BreakerSkill breaker = world.Players[0].Breaker;

            game.Advance(0.1f);
            game.Advance(0.4f);
            Expect.Equal(1, breaker.TickCount);

            // 주기의 절반이 지난 때에 달을 부순다. 효과는 다음 Step의 4 자리에서 버프가 된다.
            world.DealDamage(Of(world, Haste)[0], new Damage(1000, TestContent.First));
            game.Advance(0.0001f);
            Expect.Near(0.5f, breaker.HasteMultiplier);
            Expect.Equal(1, breaker.TickCount);

            game.Advance(0.25f);
            Expect.Equal(2, breaker.TickCount);
        }

        // 공격 주기 감소와 확정 치명타는 Breaker에만 붙는다. 레이저의 주기와 피해는 그대로다.
        // 확정 치명타 Tick은 Breaker의 치명타 배율을 한 번 곱한다.
        private static void KillBuffsApplyOnlyToBreaker()
        {
            ContentData data = Arena(haste: true, critical: true, hasteDuration: 10, breakerDamage: 2, critMultiplier: 3);
            data.Laser = new LaserData { Damage = 2, Interval = 1, Width = 100, TelegraphDuration = 0.4f, BoundaryRadius = 10 };
            GameSession game = TestContent.Session(data);
            World world = game.World;
            BattlePlayer player = world.Players[0];
            game.SetAimPoint(TestContent.First, BattleSpace.Origin);

            // 첫 Tick: 모두를 2씩 친다. 달·혜성이 죽어 두 버프가 붙는다. 이 Tick은 치명타가 아니다.
            game.Advance(0.1f);
            Expect.True(!player.Breaker.Ticks[0].IsCritical, "버프를 준 처치의 Tick은 치명타가 아니다.");
            Expect.Near(10, player.Breaker.GuaranteedCriticalRemaining);
            ExpectNormals(world, 998);

            // 레이저 발사(0.4초): 피해 2 그대로.
            game.Advance(0.3f);
            Expect.Equal(1, player.Laser.FireCount);
            ExpectNormals(world, 996);

            // Breaker 두 번째 Tick(0.55초, 주기 절반): 치명타 2 × 3.
            game.Advance(0.15f);
            Expect.Equal(2, player.Breaker.TickCount);
            Expect.True(player.Breaker.Ticks[0].IsCritical, "확정 치명타 중의 Tick은 치명타다.");
            ExpectNormals(world, 990);
            Expect.Equal(0, player.Laser.PendingShots.Count);

            // 레이저의 두 번째 예고는 제 주기(1초)에 온다.
            game.Advance(0.45f);
            Expect.Equal(1, player.Laser.PendingShots.Count);
            Expect.Equal(2, player.Laser.PendingShots[0].Number);
        }

        // 버프는 참가자가 한 명일 때만 그 참가자가 받는다. 둘 이상이면 귀속이 미정이라 아무도 받지 않는다.
        private static void GoesToTheSingleParticipantOnly()
        {
            GameContent content = TestContent.Load(Arena(haste: true, critical: true, hasteDuration: 5, buffHealth: 100));
            GameSession game = TestContent.Begun(SessionAssembler.CreateBattle(
                content, new[] { new PlayerState(TestContent.First), new PlayerState(TestContent.Second) }));
            World world = game.World;

            world.DealDamage(Of(world, Haste)[0], new Damage(1000, TestContent.First));
            world.DealDamage(Of(world, Critical)[0], new Damage(1000, TestContent.First));
            game.Advance(0.1f);

            foreach (BattlePlayer player in world.Players)
            {
                Expect.Near(0, player.Breaker.HasteRemaining);
                Expect.Near(0, player.Breaker.GuaranteedCriticalRemaining);
            }
        }

        // 일반 적 셋(HP 1000)과 버프 적이 HQ 둘레 [2, 4] 띠에 나오는 판. Breaker: 주기 1초, 원은 판 전체보다 크다.
        private static ContentData Arena(bool haste, bool critical, float hasteDuration,
            float buffHealth = 1, float breakerDamage = 1, float critMultiplier = 1)
        {
            var supply = new List<SupplyData> { TestContent.Supply(Normal, 3) };

            if (haste)
                supply.Add(TestContent.Supply(Haste, 1));

            if (critical)
                supply.Add(TestContent.Supply(Critical, 1));

            ContentData data = TestContent.Arena(2, 4, supply.ToArray());
            data.Enemies.Add(TestContent.Enemy(Normal, 1000));
            data.Enemies.Add(WithEffect(Haste, buffHealth,
                new DeathEffectData { Kind = "AttackHaste", Duration = hasteDuration, IntervalMultiplier = 0.5f }));
            data.Enemies.Add(WithEffect(Critical, buffHealth, new DeathEffectData { Kind = "GuaranteedCritical", Duration = 10 }));
            TestContent.Allow(data, Normal);
            TestContent.Allow(data, Haste);
            TestContent.Allow(data, Critical);
            data.Breaker = new BreakerData { Damage = breakerDamage, Interval = 1, Radius = 100, CritMultiplier = critMultiplier };
            return data;
        }

        private static EnemyData WithEffect(string id, float health, DeathEffectData effect)
        {
            EnemyData enemy = TestContent.Enemy(id, health);
            enemy.DeathEffect = effect;
            return enemy;
        }

        private static void ExpectNormals(World world, float health)
        {
            foreach (Enemy enemy in Of(world, Normal))
                Expect.Near(health, enemy.Health);
        }

        private static List<Enemy> Of(World world, string kind)
        {
            var found = new List<Enemy>();

            foreach (Enemy enemy in world.Enemies)
            {
                if (enemy.Definition.Id == kind)
                    found.Add(enemy);
            }

            return found;
        }
    }
}
