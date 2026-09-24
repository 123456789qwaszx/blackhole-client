using System;
using System.Collections.Generic;

namespace BlackHole.Core.Tests
{
    // Unity EditMode와 .NET 실행기가 같은 계약을 실행한다. 엔진 대역은 사용하지 않는다.
    public static class CoreContracts
    {
        public static IEnumerable<KeyValuePair<string, Action>> Cases()
        {
            yield return Case(nameof(DeathDoesNotRewardUntilAbsorption), DeathDoesNotRewardUntilAbsorption);
            yield return Case(nameof(AbsorptionRewardsExactlyOnce), AbsorptionRewardsExactlyOnce);
            yield return Case(nameof(StrikeSelectsOneAndPulseHitsMany), StrikeSelectsOneAndPulseHitsMany);
            yield return Case(nameof(PulsePullsWithoutOwningReward), PulsePullsWithoutOwningReward);
            yield return Case(nameof(CooldownAndNoTargetHaveDistinctResults), CooldownAndNoTargetHaveDistinctResults);
            yield return Case(nameof(UpgradeFailureIsAtomicAndSuccessChangesDamage), UpgradeFailureIsAtomicAndSuccessChangesDamage);
            yield return Case(nameof(PauseFreezesAllSimulationState), PauseFreezesAllSimulationState);
            yield return Case(nameof(EndRejectsCommandsAndFreezesResult), EndRejectsCommandsAndFreezesResult);
            yield return Case(nameof(RestartHasFreshState), RestartHasFreshState);
            yield return Case(nameof(DefinitionsRejectInvalidData), DefinitionsRejectInvalidData);
            yield return Case(nameof(NewSkillNeedsOnlyComposition), NewSkillNeedsOnlyComposition);
            yield return Case(nameof(TargetCapacityAndTwoDefinitionsAreUsed), TargetCapacityAndTwoDefinitionsAreUsed);
            yield return Case(nameof(ReferenceLoopReachesGrowthUpgradeAndEnd), ReferenceLoopReachesGrowthUpgradeAndEnd);
            yield return Case(nameof(BaselineAtCoarseFixedStep), BaselineAtCoarseFixedStep);
            yield return Case(nameof(BaselineAtFrameFixedStep), BaselineAtFrameFixedStep);
            yield return Case(nameof(FinalFrameIsClippedToTimeLimit), FinalFrameIsClippedToTimeLimit);
            yield return Case(nameof(CooldownExpiresBeforeSameFrameRequest), CooldownExpiresBeforeSameFrameRequest);
            yield return Case(nameof(AbsorptionHappensInStepReachingRadius), AbsorptionHappensInStepReachingRadius);
        }

        public static void DeathDoesNotRewardUntilAbsorption()
        {
            GameSession game = Reference();
            TargetState target = game.Field.Targets[0];
            Equal(CastResult.Cast, game.TryCast(ReferenceGame.StrikeId, target.Position));
            Equal(TargetPhase.Defeated, target.Phase);
            Equal(0, game.Field.Growth.Mass);
            game.Advance(0.1f);
            Equal(0, game.Field.Growth.Mass);
        }

        public static void AbsorptionRewardsExactlyOnce()
        {
            GameSession game = Reference();
            TargetState target = game.Field.Targets[0];
            game.TryCast(ReferenceGame.StrikeId, target.Position);
            game.Advance(2);
            Equal(TargetPhase.Absorbed, target.Phase);
            Equal(2, game.Field.Growth.Mass);
            Equal(2, game.Field.Growth.Credits);
            Equal(1, game.Field.Growth.AbsorbedCount);
            game.Advance(4);
            Equal(2, game.Field.Growth.Mass);
            foreach (TargetState current in game.Field.Targets)
                Check(current.Id != target.Id, "흡수한 대상은 목록에서 제거되어야 한다.");
        }

        public static void StrikeSelectsOneAndPulseHitsMany()
        {
            GameSession game = CreateCrowdedSession();
            game.Advance(0.3f);
            Equal(3, game.Field.Targets.Count);
            Equal(CastResult.Cast, game.TryCast("strike", new Point2(0, 0)));
            int damaged = 0;
            foreach (TargetState target in game.Field.Targets)
                if (target.Health < 100) damaged++;
            Equal(1, damaged);
            Equal(CastResult.Cast, game.TryCast("pulse", new Point2(0, 0)));
            foreach (TargetState target in game.Field.Targets)
                Check(target.Health <= 95, "범위 공격은 범위 내 모든 생존 대상에 적용되어야 한다.");
        }

        public static void PulsePullsWithoutOwningReward()
        {
            GameSession game = Reference();
            TargetState target = game.Field.Targets[0];
            float radius = target.Radius;
            game.TryCast(ReferenceGame.PulseId, target.Position);
            Near(radius - 0.8f, target.Radius);
            Near(3, target.Health);
            Equal(TargetPhase.Orbiting, target.Phase);
            Equal(0, game.Field.Growth.Mass);
        }

        public static void CooldownAndNoTargetHaveDistinctResults()
        {
            GameSession game = Reference();
            Equal(CastResult.UnknownSkill, game.TryCast("missing", new Point2(0, 0)));
            Equal(CastResult.NoTarget, game.TryCast(ReferenceGame.StrikeId, new Point2(100, 100)));
            Near(0, game.Field.Skills[0].RemainingCooldown);
            Equal(CastResult.Cast, game.TryCast(ReferenceGame.StrikeId, game.Field.Targets[0].Position));
            Equal(CastResult.CoolingDown, game.TryCast(ReferenceGame.StrikeId, new Point2(0, 0)));
            game.Advance(0.4f);
            Near(0, game.Field.Skills[0].RemainingCooldown);
        }

        public static void UpgradeFailureIsAtomicAndSuccessChangesDamage()
        {
            GameSession game = new GameSession(new Playfield(
                new SpawnSchedule(new[]
                {
                    new TargetDefinition("reward", 1, 0, 0.01f, 10),
                    new TargetDefinition("durable", 100, 0, 0.01f, 10)
                }, 0.8f, 2, 3),
                new SkillLoadout(new[] { new SkillDefinition("strike", 0.1f, new FocusedStrike(10, 0.5f)) }),
                new GrowthState(new GrowthDefinition(6, 1, 0.5f))), 60);
            Equal(UpgradeResult.InsufficientCredits, game.TryUpgrade());
            Equal(0, game.Field.Growth.PowerLevel);
            Equal(0, game.Field.Growth.Credits);
            game.TryCast("strike", game.Field.Targets[0].Position);
            game.Advance(1);
            Equal(10, game.Field.Growth.Credits);
            Equal(UpgradeResult.Purchased, game.TryUpgrade());
            Equal(4, game.Field.Growth.Credits);
            Equal(1, game.Field.Growth.PowerLevel);
            Near(1.5f, game.Field.Growth.DamageMultiplier);
            Equal(UpgradeResult.MaxLevel, game.TryUpgrade());
            Equal(4, game.Field.Growth.Credits);

            TargetState durable = game.Field.Targets[0];
            Equal("durable", durable.Definition.Id);
            Equal(CastResult.Cast, game.TryCast("strike", durable.Position));
            Near(85, durable.Health);
        }

        public static void PauseFreezesAllSimulationState()
        {
            GameSession game = Reference();
            TargetState target = game.Field.Targets[0];
            game.TryCast(ReferenceGame.PulseId, target.Position);
            float radius = target.Radius;
            float cooldown = game.Field.Skills[1].RemainingCooldown;
            game.TogglePause();
            game.Advance(10);
            Near(0, game.Elapsed);
            Near(radius, target.Radius);
            Near(cooldown, game.Field.Skills[1].RemainingCooldown);
            Equal(1, game.Field.Targets.Count);
            Equal(CastResult.SessionInactive, game.TryCast(ReferenceGame.StrikeId, target.Position));
            Equal(UpgradeResult.SessionInactive, game.TryUpgrade());
            game.TogglePause();
            game.Advance(1);
            Check(game.Elapsed > 0, "재개 후 진행되어야 한다.");
        }

        public static void EndRejectsCommandsAndFreezesResult()
        {
            GameSession game = Reference(0.1f);
            game.Advance(100);
            Equal(SessionPhase.Ended, game.Phase);
            Equal(SessionEndReason.TimeExpired, game.Result.Reason);
            Near(0.1f, game.Elapsed);
            Equal(1, game.Field.Targets.Count);
            SessionResult result = game.Result;
            float angle = game.Field.Targets[0].Angle;
            game.Stop();
            game.TogglePause();
            game.Advance(10);
            Check(ReferenceEquals(result, game.Result), "종료 결과를 다시 만들면 안 된다.");
            Near(angle, game.Field.Targets[0].Angle);
            Equal(CastResult.SessionInactive, game.TryCast(ReferenceGame.StrikeId, new Point2(0, 0)));
            Equal(UpgradeResult.SessionInactive, game.TryUpgrade());
        }

        public static void RestartHasFreshState()
        {
            GameSession old = Reference();
            old.TryCast(ReferenceGame.StrikeId, old.Field.Targets[0].Position);
            old.Advance(2);
            old.Stop();
            GameSession next = Reference();
            Equal(0, next.Field.Growth.Mass);
            Near(0, next.Elapsed);
            Near(0, next.Field.Skills[0].RemainingCooldown);
            Near(12, next.Field.Targets[0].Health);
            Equal(2, old.Result.Mass);
            Check(!ReferenceEquals(old.Field.Targets[0], next.Field.Targets[0]), "개체 상태를 재사용하면 안 된다.");
        }

        public static void DefinitionsRejectInvalidData()
        {
            Throws<ArgumentException>(() => new TargetDefinition("", 1, 1, 1, 1));
            Throws<ArgumentOutOfRangeException>(() => new TargetDefinition("a", float.NaN, 1, 1, 1));
            Throws<ArgumentOutOfRangeException>(() => new GravityPulse(1, -1, 1));
            Throws<ArgumentOutOfRangeException>(() => new Point2(float.PositiveInfinity, 0));
            var skill = new SkillDefinition("same", 1, new FocusedStrike(1, 1));
            Throws<ArgumentException>(() => new SkillLoadout(new[] { skill, skill }));
            var target = new TargetDefinition("same", 1, 1, 1, 1);
            Throws<ArgumentException>(() => new SpawnSchedule(new[] { target, target }, 1, 5, 10));
            Throws<ArgumentOutOfRangeException>(() => Reference().Advance(float.NaN));
            Throws<ArgumentOutOfRangeException>(() => Reference(-1));
        }

        public static void NewSkillNeedsOnlyComposition()
        {
            var effect = new TestSkillEffect();
            var game = new GameSession(new Playfield(
                new SpawnSchedule(new[] { new TargetDefinition("new-target", 50, 0, 1, 3) }, 1, 5, 4),
                new SkillLoadout(new[] { new SkillDefinition("new-skill", 1, effect) }),
                new GrowthState(new GrowthDefinition(1, 1, 1))), 5);
            Equal(CastResult.Cast, game.TryCast("new-skill", new Point2(0, 0)));
            Near(43, game.Field.Targets[0].Health);
            Near(1, game.Field.Skills[0].RemainingCooldown);
        }

        public static void TargetCapacityAndTwoDefinitionsAreUsed()
        {
            GameSession game = Reference();
            game.Advance(50);
            Equal(32, game.Field.Targets.Count);
            var types = new HashSet<string>();
            foreach (TargetState target in game.Field.Targets) types.Add(target.Definition.Id);
            Equal(2, types.Count);
            Equal(0, game.Field.Growth.Mass);
        }

        public static void ReferenceLoopReachesGrowthUpgradeAndEnd()
        {
            GameSession game = Reference(20);
            bool upgraded = RunScriptedLoop(game, 0.1f);
            Check(upgraded, "핵심 흐름에서 강화에 도달해야 한다.");
            Check(game.Result.Mass > 0 && game.Result.AbsorbedCount > 0, "핵심 흐름에서 성장해야 한다.");
            Equal(SessionEndReason.TimeExpired, game.Result.Reason);
        }

        // 기준 수치는 "같은 콘텐츠·같은 입력·같은 Advance 간격"에서만 보장된다.
        // 간격이 다르면 생성·이동·입력 적용 시점이 달라져 결과가 달라질 수 있다(6초 시점 HP 합계).
        // 최종 수치는 생성량에 묶여 두 간격이 같으므로, 강화 비용이 드러나는 6초 시점도 함께 본다.
        // 구조 변경에서 이 값이 바뀌면 의도한 규칙 변경인지 먼저 확인한다.
        public static void BaselineAtCoarseFixedStep()
        {
            // 0.1초 간격은 MaxStep(1/30초)보다 커서 한 번의 Advance가 여러 단계로 나뉜다.
            GameSession game = Reference();
            RunScriptedLoop(game, 0.1f, 60);
            ExpectSnapshot(game, new Snapshot(23, 5, 7, 2, 1, 6.2f));
            RunScriptedLoop(game, 0.1f);
            ExpectFinal(game, new Snapshot(259, 169, 74, 5, 2, 0));
        }

        public static void BaselineAtFrameFixedStep()
        {
            // 1/60초 간격은 분할 없이 한 단계로 진행된다.
            GameSession game = Reference();
            RunScriptedLoop(game, 1f / 60f, 360);
            ExpectSnapshot(game, new Snapshot(23, 5, 7, 2, 1, 0));
            RunScriptedLoop(game, 1f / 60f);
            ExpectFinal(game, new Snapshot(259, 169, 74, 5, 2, 0));
        }

        public static void FinalFrameIsClippedToTimeLimit()
        {
            // 0.8초에 두 번째 대상이 생성된다. 0.75초 제한을 넘겨 진행하면 대상이 2개가 된다.
            GameSession overshoot = Reference(0.75f);
            overshoot.Advance(0.5f);
            overshoot.Advance(0.5f);
            GameSession exact = Reference(0.75f);
            exact.Advance(0.5f);
            exact.Advance(0.25f);

            Equal(SessionPhase.Ended, overshoot.Phase);
            Equal(SessionPhase.Ended, exact.Phase);
            Near(0.75f, overshoot.Elapsed);
            Equal(1, overshoot.Field.Targets.Count);
            Near(exact.Field.Targets[0].Angle, overshoot.Field.Targets[0].Angle);
            Near(exact.Field.Targets[0].Radius, overshoot.Field.Targets[0].Radius);
        }

        public static void CooldownExpiresBeforeSameFrameRequest()
        {
            // 한 프레임은 Advance 뒤에 요청을 적용한다. 쿨다운은 Advance 안에서 먼저 줄어든다.
            GameSession game = Reference();
            Equal(CastResult.Cast, game.TryCast(ReferenceGame.StrikeId, game.Field.Targets[0].Position));
            game.Advance(0.3f);
            Near(0.05f, game.Field.Skills[0].RemainingCooldown);
            Equal(CastResult.CoolingDown, game.TryCast(ReferenceGame.StrikeId, new Point2(100, 100)));
            game.Advance(0.1f);
            // 쿨다운 판정이 대상 선택보다 먼저이므로 NoTarget은 쿨다운이 끝났다는 뜻이다.
            Equal(CastResult.NoTarget, game.TryCast(ReferenceGame.StrikeId, new Point2(100, 100)));
        }

        public static void AbsorptionHappensInStepReachingRadius()
        {
            // 사망한 shard는 반경 5.5에서 초당 4씩 떨어지고, 질량 0의 흡수 반경은 0.65다.
            GameSession game = Reference();
            TargetState target = game.Field.Targets[0];
            Equal(CastResult.Cast, game.TryCast(ReferenceGame.StrikeId, target.Position));
            game.Advance(1.2f);
            Equal(TargetPhase.Defeated, target.Phase);
            Near(0.7f, target.Radius);
            Equal(0, game.Field.Growth.Mass);
            game.Advance(0.05f);
            Equal(TargetPhase.Absorbed, target.Phase);
            Equal(2, game.Field.Growth.Mass);
            foreach (TargetState current in game.Field.Targets)
                Check(current.Id != target.Id, "흡수한 단계에서 목록 제거까지 끝나야 한다.");
        }

        // ── 공통 준비 ───────────────────────────────────────────────────────

        private static GameSession Reference(float duration = 60) =>
            ReferenceGame.CreateSession(duration);

        // 매 프레임: 첫 번째 궤도 대상에 두 스킬 요청 → 가능하면 강화 → 진행.
        // 판이 끝나거나 frames만큼 진행하면 멈춘다.
        private static bool RunScriptedLoop(GameSession game, float step, int frames = int.MaxValue)
        {
            bool upgraded = false;
            for (int frame = 0; frame < frames && game.Phase != SessionPhase.Ended; frame++)
            {
                foreach (TargetState target in game.Field.Targets)
                {
                    if (target.Phase != TargetPhase.Orbiting) continue;
                    game.TryCast(ReferenceGame.StrikeId, target.Position);
                    game.TryCast(ReferenceGame.PulseId, target.Position);
                    break;
                }
                if (game.TryUpgrade() == UpgradeResult.Purchased) upgraded = true;
                game.Advance(step);
            }
            return upgraded;
        }

        // 80549cb 구현을 실행해 기록한 값.
        private readonly struct Snapshot
        {
            public readonly int Mass, Credits, Absorbed, PowerLevel, Targets;
            public readonly float TotalHealth;

            public Snapshot(int mass, int credits, int absorbed, int powerLevel, int targets, float totalHealth)
            {
                Mass = mass;
                Credits = credits;
                Absorbed = absorbed;
                PowerLevel = powerLevel;
                Targets = targets;
                TotalHealth = totalHealth;
            }
        }

        private static void ExpectSnapshot(GameSession game, Snapshot expected)
        {
            Near(6, game.Elapsed);
            Equal(expected.Mass, game.Field.Growth.Mass);
            Equal(expected.Credits, game.Field.Growth.Credits);
            Equal(expected.Absorbed, game.Field.Growth.AbsorbedCount);
            Equal(expected.PowerLevel, game.Field.Growth.PowerLevel);
            Equal(expected.Targets, game.Field.Targets.Count);
            float health = 0;
            foreach (TargetState target in game.Field.Targets) health += target.Health;
            Near(expected.TotalHealth, health);
        }

        private static void ExpectFinal(GameSession game, Snapshot expected)
        {
            Equal(SessionEndReason.TimeExpired, game.Result.Reason);
            Near(60, game.Result.PlayedSeconds);
            Equal(expected.Mass, game.Result.Mass);
            Equal(expected.Absorbed, game.Result.AbsorbedCount);
            Equal(expected.Credits, game.Field.Growth.Credits);
            Equal(expected.PowerLevel, game.Field.Growth.PowerLevel);
            Equal(expected.Targets, game.Field.Targets.Count);
        }

        private static GameSession CreateCrowdedSession(float health = 100)
        {
            return new GameSession(new Playfield(
                new SpawnSchedule(new[] { new TargetDefinition("test", health, 0, 0.01f, 10) }, 0.1f, 2, 3),
                new SkillLoadout(new[]
                {
                    new SkillDefinition("strike", 0.1f, new FocusedStrike(10, 10)),
                    new SkillDefinition("pulse", 0.1f, new GravityPulse(5, 10, 0.5f))
                }),
                new GrowthState(new GrowthDefinition(6, 1, 0.5f))), 60);
        }

        private sealed class TestSkillEffect : ISkillEffect
        {
            public int Execute(Point2 aim, float multiplier, IReadOnlyList<TargetState> targets, CombatResolver combat) =>
                combat.Hit(targets[0].Id, 7 * multiplier) ? 1 : 0;
        }

        private static KeyValuePair<string, Action> Case(string name, Action action) =>
            new KeyValuePair<string, Action>(name, action);

        private static void Check(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }

        private static void Equal<T>(T expected, T actual) =>
            Check(EqualityComparer<T>.Default.Equals(expected, actual), $"Expected {expected}, got {actual}");

        private static void Near(float expected, float actual) =>
            Check(Math.Abs(expected - actual) < 0.001f, $"Expected {expected}, got {actual}");

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException("Expected exception: " + typeof(T).Name);
        }
    }
}
