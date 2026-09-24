using System;
using System.Collections.Generic;
using BlackHole.Core;

namespace BlackHole.Core.Tests
{
    // 구매가 Skill 구성과 Skill별 실행 수치를 바꾼다(CA-004).
    // 구매 기록은 노드 ID로 남고, 다음 전투 조립이 그것을 Skill 구성(시작 구성 + 해금)과 Skill별 실행 수치로 바꾼다.
    internal static class UpgradeSkillContracts
    {
        private const string Unlock = "laser-unlock";
        private const string LaserDamage = "laser-damage";
        private const string LaserSpeed = "laser-speed";
        private const string LaserWidth = "laser-width";
        private const int Seed = 3;

        private static readonly Point2 East = new Point2(3, 0);
        private static readonly Point2 Hq = new Point2(0, 0);

        public static IEnumerable<Contract> Cases()
        {
            yield return new Contract("Upgrade.LaserAbsentBeforeUnlock", LaserAbsentBeforeUnlock);
            yield return new Contract("Upgrade.UnlockAddsLaserFromNextBattle", UnlockAddsLaserFromNextBattle);
            yield return new Contract("Upgrade.UnlockIsNotRetroactive", UnlockIsNotRetroactive);
            yield return new Contract("Upgrade.UnlockingStartingSkillIsRejected", UnlockingStartingSkillIsRejected);
            yield return new Contract("Upgrade.UnlockingUnknownSkillIsRejected", UnlockingUnknownSkillIsRejected);
            yield return new Contract("Upgrade.UnlockTakesNoValue", UnlockTakesNoValue);
            yield return new Contract("Upgrade.OneUnlockNodePerSkill", OneUnlockNodePerSkill);
            yield return new Contract("Upgrade.LaserDamageAddChangesLaserDamage", LaserDamageAddChangesLaserDamage);
            yield return new Contract("Upgrade.LaserIntervalMultiplyChangesFireInterval", LaserIntervalMultiplyChangesFireInterval);
            yield return new Contract("Upgrade.LaserWidthAddChangesHitWidth", LaserWidthAddChangesHitWidth);
            yield return new Contract("Upgrade.LaserEffectsDoNotTouchBreaker", LaserEffectsDoNotTouchBreaker);
            yield return new Contract("Upgrade.LaserUpgradeMustFollowUnlock", LaserUpgradeMustFollowUnlock);
            yield return new Contract("Upgrade.PurchaseByNodeId", PurchaseByNodeId);
            yield return new Contract("Upgrade.UnknownNodeIdChangesNothing", UnknownNodeIdChangesNothing);
        }

        // 해금 전에는 레이저가 없다. Gold를 벌었어도 사지 않으면 없다.
        private static void LaserAbsentBeforeUnlock()
        {
            GameContent content = TestContent.Load(Armory());
            GameSession fresh = SessionAssembler.Create(content, new[] { TestContent.First });
            Expect.Equal(1, fresh.World.Players[0].Skills.Count);
            Expect.True(fresh.World.Players[0].Skills[0] is BreakerSkill, "시작 구성은 Breaker 하나다.");

            GameSession next = Battle(content, Progress(content));
            Expect.Equal(1, next.World.Players[0].Skills.Count);
        }

        // 해금 노드를 사면 다음 전투부터 시작 구성 뒤에 레이저가 붙는다. 수치는 기본 수치다.
        private static void UnlockAddsLaserFromNextBattle()
        {
            GameContent content = TestContent.Load(Armory());
            PlayerState state = Progress(content, Unlock);
            Expect.Equal(90, state.Gold);

            Player player = Battle(content, state).World.Players[0];
            Expect.Equal(2, player.Skills.Count);
            Expect.True(player.Skills[0] is BreakerSkill && player.Skills[1] is PiercingLaserSkill,
                "Skill은 시작 구성, 그 뒤 해금 Skill 순서다.");

            PiercingLaserStats stats = TestContent.LaserSkill(player, 1).Stats;
            Expect.Near(1, stats.Interval);
            Expect.Near(5, stats.Damage);
            Expect.Near(0.8f, stats.Width);
            Expect.Near(0.3f, stats.TelegraphDuration);
        }

        // 전투 중에는 살 수 없고, 전투가 끝난 뒤 사도 끝난 전투의 Skill은 바뀌지 않는다.
        private static void UnlockIsNotRetroactive()
        {
            GameContent content = TestContent.Load(Armory());
            var state = new PlayerState(TestContent.First);
            GameSession battle = SessionAssembler.CreateBattle(content, new[] { state });
            battle.SetAimPoint(state.Id, East);
            battle.Advance(0.1f);
            Expect.Equal(100, state.Gold);

            Expect.Equal(PurchaseResult.InBattle, UpgradePurchase.TryPurchase(state, content, Unlock));
            Expect.Equal(100, state.Gold);
            Expect.True(!state.Owns(Unlock), "전투 중 구매는 기록되지 않아야 한다.");

            battle.Stop();
            Expect.Equal(PurchaseResult.Purchased, UpgradePurchase.TryPurchase(state, content, Unlock));
            Expect.Equal(1, battle.World.Players[0].Skills.Count);
            Expect.Equal(2, Battle(content, state).World.Players[0].Skills.Count);
        }

        private static void UnlockingStartingSkillIsRejected()
        {
            ContentData data = Armory();
            data.Upgrades.Add(TestContent.Upgrade("breaker-unlock", 10, null, UnlockEffect(TestContent.SkillId)));
            ContentLoadResult result = ContentLoader.Load(data);
            Expect.Equal(1, result.Diagnostics.Count);
            TestContent.HasDiagnostic(result, "Upgrades[breaker-unlock].Effects[0]", "시작 구성에 있는");
        }

        private static void UnlockingUnknownSkillIsRejected()
        {
            ContentData data = Armory();
            data.Upgrades.Add(TestContent.Upgrade("ghost-unlock", 10, null, UnlockEffect("ghost")));
            ContentLoadResult result = ContentLoader.Load(data);
            Expect.Equal(1, result.Diagnostics.Count);
            TestContent.HasDiagnostic(result, "Upgrades[ghost-unlock].Effects[0].Target", "ghost");
        }

        // 해금은 값이 없는 효과다. 더미 값을 적으면 콘텐츠 오류다(CA-002 U3).
        private static void UnlockTakesNoValue()
        {
            ContentData data = Armory();
            data.Upgrades[0].Effects[0].Value = 1;
            ContentLoadResult result = ContentLoader.Load(data);
            Expect.True(!result.Succeeded, "값이 있는 해금은 로드되지 않아야 한다.");
            TestContent.HasDiagnostic(result, "Upgrades[laser-unlock].Effects[0]", "값을 받지 않는다");
        }

        // [임시] 한 Skill의 해금 노드는 하나다.
        private static void OneUnlockNodePerSkill()
        {
            ContentData data = Armory();
            data.Upgrades.Add(TestContent.Upgrade("laser-unlock-again", 10, null, UnlockEffect(TestContent.LaserId)));
            ContentLoadResult result = ContentLoader.Load(data);
            Expect.Equal(1, result.Diagnostics.Count);
            TestContent.HasDiagnostic(result, "Upgrades[laser-unlock-again].Effects[0]", Unlock);
        }

        // 같은 seed·조준이면 경로가 같다. 경로 위의 과녁은 강화한 피해만큼 맞는다.
        private static void LaserDamageAddChangesLaserDamage()
        {
            GameContent content = TestContent.Load(Armory());
            GameSession plain = Battle(content, Progress(content, Unlock));
            GameSession boosted = Battle(content, Progress(content, Unlock, LaserDamage));
            Expect.Near(9, TestContent.LaserSkill(boosted.World.Players[0], 1).Stats.Damage);

            LaserShot shot = FirstShot(plain);
            Expect.True(FirstShot(boosted).Start.Equals(shot.Start), "같은 seed면 경로가 같아야 한다.");
            plain.Advance(0.3f);
            boosted.Advance(0.3f);

            int hit = 0;
            foreach (Enemy target in Targets(plain))
            {
                if (DistanceToPath(target.Position, shot) > 0.4f) continue;
                hit++;
                Expect.Near(95, target.Health);
            }

            foreach (Enemy target in Targets(boosted))
                if (DistanceToPath(target.Position, shot) <= 0.4f)
                    Expect.Near(91, target.Health);

            Expect.True(hit >= 1, $"seed {Seed}의 첫 경로에 과녁이 있어야 이 계약이 뜻을 가진다.");
        }

        // 주기 × 0.5: 예고가 1초 대신 0.5초마다 시작한다. 예고 시간(0.3초)은 그대로다.
        private static void LaserIntervalMultiplyChangesFireInterval()
        {
            GameContent content = TestContent.Load(Armory());
            GameSession plain = Battle(content, Progress(content, Unlock));
            GameSession faster = Battle(content, Progress(content, Unlock, LaserSpeed));
            PiercingLaserSkill plainLaser = TestContent.LaserSkill(plain.World.Players[0], 1);
            PiercingLaserSkill fasterLaser = TestContent.LaserSkill(faster.World.Players[0], 1);
            Expect.Near(0.5f, fasterLaser.Stats.Interval);
            Expect.Near(0.3f, fasterLaser.Stats.TelegraphDuration);

            foreach (GameSession game in new[] { plain, faster })
            {
                game.SetAimPoint(TestContent.First, Hq);
                game.Advance(0.52f);
            }

            // 0.3초에 첫 발사. 0.5초 주기는 0.5초에 둘째 예고를 시작했고, 1초 주기는 아직 없다.
            Expect.Equal(0, plainLaser.PendingShots.Count);
            Expect.Equal(1, fasterLaser.PendingShots.Count);
        }

        // 굵기 + 1.6: 같은 경로에서 더 넓은 폭 안의 과녁까지 맞는다.
        private static void LaserWidthAddChangesHitWidth()
        {
            GameContent content = TestContent.Load(Armory());
            GameSession plain = Battle(content, Progress(content, Unlock));
            GameSession wide = Battle(content, Progress(content, Unlock, LaserWidth));
            Expect.Near(2.4f, TestContent.LaserSkill(wide.World.Players[0], 1).Stats.Width);

            LaserShot shot = FirstShot(plain);
            Expect.True(FirstShot(wide).Start.Equals(shot.Start), "같은 seed면 경로가 같아야 한다.");
            plain.Advance(0.3f);
            wide.Advance(0.3f);

            int plainHits = 0;
            int wideHits = 0;
            foreach (Enemy target in Targets(plain))
                if (target.Health < 100) plainHits++;

            foreach (Enemy target in Targets(wide))
            {
                bool inside = DistanceToPath(target.Position, shot) <= 1.2f;
                Expect.Near(inside ? 95 : 100, target.Health);
                if (inside) wideHits++;
            }

            Expect.True(wideHits > plainHits, $"넓어진 폭이 더 많이 맞혀야 한다. 기본 {plainHits}, 강화 {wideHits}.");
        }

        // 레이저 효과는 Breaker를 대상으로 받지 않고, 레이저 강화를 모두 사도 Breaker 수치는 그대로다.
        private static void LaserEffectsDoNotTouchBreaker()
        {
            ContentData data = Armory();
            data.Upgrades.Add(TestContent.Upgrade("breaker-by-laser", 10, null, TestContent.Effect("LaserDamageAdd", 1, TestContent.SkillId)));
            ContentLoadResult result = ContentLoader.Load(data);
            Expect.Equal(1, result.Diagnostics.Count);
            TestContent.HasDiagnostic(result, "Upgrades[breaker-by-laser].Effects[0]", "관통 레이저");

            GameContent content = TestContent.Load(Armory());
            Player player = Battle(content, Progress(content, Unlock, LaserDamage, LaserSpeed, LaserWidth)).World.Players[0];
            BreakerStats breaker = TestContent.Breaker(player).Stats;
            Expect.Near(0.3f, breaker.Radius);
            Expect.Near(0.5f, breaker.Interval);
            Expect.Near(1, breaker.Damage);
        }

        // 시작 구성에 없는 Skill을 바꾸는 노드는 선행을 따라가면(자기 자신 포함) 해금 노드에 닿아야 한다.
        private static void LaserUpgradeMustFollowUnlock()
        {
            ContentData root = Armory();
            root.Upgrades[3].Requires = null;
            ContentLoadResult result = ContentLoader.Load(root);
            Expect.Equal(1, result.Diagnostics.Count);
            TestContent.HasDiagnostic(result, "Upgrades[laser-width].Effects[0]", "해금 노드");

            ContentData sibling = Armory();
            sibling.Upgrades.Add(TestContent.Upgrade("breaker-root", 10, null, TestContent.Effect("SkillDamageAdd", 1, TestContent.SkillId)));
            sibling.Upgrades[3].Requires = "breaker-root";
            result = ContentLoader.Load(sibling);
            Expect.Equal(1, result.Diagnostics.Count);
            TestContent.HasDiagnostic(result, "Upgrades[laser-width].Effects[0]", "해금 노드");

            ContentData combined = Armory();
            combined.Upgrades = new List<UpgradeData>
            {
                TestContent.Upgrade("laser-all", 10, null,
                    UnlockEffect(TestContent.LaserId),
                    TestContent.Effect("LaserDamageAdd", 1, TestContent.LaserId))
            };
            Expect.True(ContentLoader.Load(combined).Succeeded, "해금과 강화를 함께 가진 노드는 올바르다.");
        }

        // 구매는 노드 ID로 요청한다. 정의 조회·조건 검사·기록은 구매 규칙이 한다.
        private static void PurchaseByNodeId()
        {
            GameContent content = TestContent.Load(Armory());
            PlayerState state = Progress(content);

            Expect.Equal(PurchaseResult.MissingPrerequisite, UpgradePurchase.Check(state, content, LaserDamage));
            Expect.Equal(PurchaseResult.MissingPrerequisite, UpgradePurchase.TryPurchase(state, content, LaserDamage));
            Expect.Equal(100, state.Gold);

            Expect.Equal(PurchaseResult.Purchased, UpgradePurchase.Check(state, content, Unlock));
            Expect.Equal(PurchaseResult.Purchased, UpgradePurchase.TryPurchase(state, content, Unlock));
            Expect.Equal(90, state.Gold);
            Expect.Equal(1, state.Upgrades.Count);
            Expect.Equal(Unlock, state.Upgrades[0]);
            Expect.Equal(PurchaseResult.AlreadyOwned, UpgradePurchase.Check(state, content, Unlock));
        }

        // 콘텐츠에 없는 노드 ID는 UnknownNode이고 Gold와 구매 기록을 바꾸지 않는다.
        private static void UnknownNodeIdChangesNothing()
        {
            GameContent content = TestContent.Load(Armory());
            PlayerState state = Progress(content);

            Expect.Equal(PurchaseResult.UnknownNode, UpgradePurchase.Check(state, content, "ghost"));
            Expect.Equal(PurchaseResult.UnknownNode, UpgradePurchase.TryPurchase(state, content, "ghost"));
            Expect.Equal(PurchaseResult.UnknownNode, UpgradePurchase.TryPurchase(state, content, null));
            Expect.Equal(100, state.Gold);
            Expect.Equal(0, state.Upgrades.Count);
        }

        // 동전 1개(HP 1, Gold 100)가 동쪽(3, 0)에, 과녁 35개(HP 100)가 HQ에서 거리 3에 10도 간격으로 있다.
        // 시작 Skill은 Breaker(반경 0.3)이고, 레이저는 정의만 있다. 해금 노드 아래에 레이저 강화 셋이 있다.
        private static ContentData Armory()
        {
            ContentData data = TestContent.Data();
            EnemyData coin = TestContent.Enemy("coin", 1, 0.0001f, 0.3f);
            coin.Gold = 100;
            data.Enemies = new List<EnemyData> { coin, TestContent.Enemy("target", 100, 0.0001f, 0.3f) };
            data.Spawn = new SpawnData { Distance = 3, AngleStep = (float)(Math.PI / 18) };
            data.StartSupply = new List<SupplyData> { TestContent.Supply("coin", 1), TestContent.Supply("target", 35) };
            data.Skills = new List<SkillData>
            {
                TestContent.Skill(TestContent.SkillId, radius: 0.3f, interval: 0.5f, damage: 1),
                TestContent.Laser(TestContent.LaserId, interval: 1, damage: 5, width: 0.8f, telegraph: 0.3f)
            };
            data.StartingSkills = new List<string> { TestContent.SkillId };
            data.Upgrades = new List<UpgradeData>
            {
                TestContent.Upgrade(Unlock, 10, null, UnlockEffect(TestContent.LaserId)),
                TestContent.Upgrade(LaserDamage, 10, Unlock, TestContent.Effect("LaserDamageAdd", 4, TestContent.LaserId)),
                TestContent.Upgrade(LaserSpeed, 10, Unlock, TestContent.Effect("LaserIntervalMultiply", 0.5f, TestContent.LaserId)),
                TestContent.Upgrade(LaserWidth, 10, Unlock, TestContent.Effect("LaserWidthAdd", 1.6f, TestContent.LaserId))
            };
            return data;
        }

        private static UpgradeEffectData UnlockEffect(string skill) =>
            new UpgradeEffectData { Kind = "SkillUnlock", Target = skill };

        // 한 전투에서 0초 Breaker로 동전을 부숴 Gold 100을 벌고, 주어진 노드를 ID로 산 진행 상태.
        private static PlayerState Progress(GameContent content, params string[] nodes)
        {
            var state = new PlayerState(TestContent.First);
            GameSession earn = SessionAssembler.CreateBattle(content, new[] { state });
            earn.SetAimPoint(state.Id, East);
            earn.Advance(0.1f);
            earn.Stop();
            Expect.Equal(100, state.Gold);

            foreach (string id in nodes)
                Expect.Equal(PurchaseResult.Purchased, UpgradePurchase.TryPurchase(state, content, id));

            return state;
        }

        private static GameSession Battle(GameContent content, PlayerState state) =>
            SessionAssembler.CreateBattle(content, new[] { state }, Seed);

        // HQ를 조준하고(Breaker 원 안에는 적이 없다) 첫 예고의 경로를 읽는다. 첫 발사는 0.3초다.
        private static LaserShot FirstShot(GameSession game)
        {
            game.SetAimPoint(TestContent.First, Hq);
            game.Advance(0.01f);
            return TestContent.LaserSkill(game.World.Players[0], 1).PendingShots[0];
        }

        private static IEnumerable<Enemy> Targets(GameSession game)
        {
            foreach (Enemy enemy in game.World.Enemies)
                if (enemy.Definition.Id == "target") yield return enemy;
        }

        private static float DistanceToPath(Point2 point, LaserShot shot)
        {
            float sx = shot.End.X - shot.Start.X;
            float sy = shot.End.Y - shot.Start.Y;
            float t = ((point.X - shot.Start.X) * sx + (point.Y - shot.Start.Y) * sy) / (sx * sx + sy * sy);
            t = Math.Max(0, Math.Min(1, t));
            var nearest = new Point2(shot.Start.X + t * sx, shot.Start.Y + t * sy);
            return (float)Math.Sqrt(point.DistanceSquared(nearest));
        }
    }
}
