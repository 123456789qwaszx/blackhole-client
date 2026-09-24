using System;
using System.Collections.Generic;
using System.Diagnostics;
using BlackHole.Core;

// Core 규모 참고 측정(M8). 목표나 합격선이 아니다. 대상 수가 늘 때 무엇이 먼저 비싸지는지 보는 자료다.
//
// 유지: 적 N마리가 HQ 둘레를 돈다. HP가 커서 죽지 않는다. Skill 하나가 둘레의 한 곳을 0.5초마다 친다.
//       1/60초 진행을 반복해 프레임당 시간과 할당을 잰다.
// 번개: 첫 프레임에 전기 적 한 마리가 죽어 번개가 3번 튀는 판과, 전기 적이 없는 판의 첫 프레임 시간 차이(중앙값).
//       사망 처리(보상, 기록, 목록 제거)와 번개의 대상 찾기가 함께 들어간다.
internal static class Program
{
    private const float Frame = 1f / 60f;
    private const int WarmupFrames = 600;
    private const int MeasuredFrames = 6000;
    private const int FirstFrameSamples = 400;

    private static int Main()
    {
        Console.WriteLine($".NET {Environment.Version}, {Environment.OSVersion}, 논리 프로세서 {Environment.ProcessorCount}");
        Console.WriteLine();
        Console.WriteLine("| 적 수 | 유지 (μs/프레임) | 유지 할당 (B/프레임) | 전기 적 1마리 사망과 번개의 추가 비용 (μs) |");
        Console.WriteLine("|---|---|---|---|");

        foreach (int count in new[] { 32, 256, 1024 })
        {
            (double micros, double bytes) = Steady(count);
            double lightning = FirstFrame(count, spark: true) - FirstFrame(count, spark: false);
            Console.WriteLine($"| {count} | {micros:0.0} | {bytes:0.0} | {lightning:0.0} |");
        }

        return 0;
    }

    private static (double micros, double bytes) Steady(int count)
    {
        GameSession game = Session(count, spark: false);

        for (int i = 0; i < WarmupFrames; i++)
        {
            game.Advance(Frame);
        }

        GC.Collect();
        long allocated = GC.GetAllocatedBytesForCurrentThread();
        var watch = Stopwatch.StartNew();

        for (int i = 0; i < MeasuredFrames; i++)
        {
            game.Advance(Frame);
        }

        watch.Stop();
        allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;

        return (watch.Elapsed.TotalMilliseconds * 1000 / MeasuredFrames, (double)allocated / MeasuredFrames);
    }

    // 첫 프레임만 잰다. 판 조립은 재지 않는다. 여러 판의 중앙값.
    private static double FirstFrame(int count, bool spark)
    {
        var samples = new List<double>();

        for (int i = 0; i < FirstFrameSamples; i++)
        {
            GameSession game = Session(count, spark);
            var watch = Stopwatch.StartNew();
            game.Advance(Frame);
            watch.Stop();

            if (spark && game.World.DeathEffectHits.Count != 3)
                throw new InvalidOperationException("번개가 3번 튀어야 한다.");

            samples.Add(watch.Elapsed.TotalMilliseconds * 1000);
        }

        samples.Sort();
        return samples[samples.Count / 2];
    }

    // 모든 적은 HQ(0, 0)에서 거리 4.5, 황금각 간격. spark가 있으면 첫 번째(각도 0, 조준점 위)로 나온다.
    private static GameSession Session(int count, bool spark)
    {
        var data = new ContentData
        {
            Session = new SessionData { TimeLimit = 100000 },
            Hq = new HqData { X = 0, Y = 0 },
            Spawn = new SpawnData { Distance = 4.5f, AngleStep = 2.399963f },
            Skills = new List<SkillData>
            {
                new SkillData { Id = "aura", Kind = "Breaker", Radius = 1.2f, Interval = 0.5f, Damage = 3 }
            },
            StartingSkills = new List<string> { "aura" }
        };

        data.Enemies.Add(Enemy("rock", 1e9f, null));
        data.Enemies.Add(Enemy("spark", 1, new DeathEffectData { Kind = "ChainLightning", Damage = 5, Range = 2.5f, Chains = 3 }));

        if (spark)
            data.StartSupply.Add(new SupplyData { Enemy = "spark", Count = 1 });

        data.StartSupply.Add(new SupplyData { Enemy = "rock", Count = spark ? count - 1 : count });

        ContentLoadResult result = ContentLoader.Load(data);

        if (!result.Succeeded)
            throw new InvalidOperationException(result.Diagnostics[0].ToString());

        var player = new PlayerId(1);
        GameSession game = SessionAssembler.Create(result.Content, new[] { player });
        game.SetAimPoint(player, new Point2(4.5f, 0));
        return game;
    }

    private static EnemyData Enemy(string id, float health, DeathEffectData deathEffect) =>
        new EnemyData
        {
            Id = id,
            MaxHealth = health,
            MoveSpeed = 1.2f,
            Size = 0.3f,
            Behavior = new EnemyBehaviorData { Kind = "OrbitHq", Clockwise = false },
            DeathEffect = deathEffect
        };
}
