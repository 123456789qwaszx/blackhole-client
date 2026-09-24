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
            yield return Case(nameof(NewSkillNeedsOnlyDefinition), NewSkillNeedsOnlyDefinition);
            yield return Case(nameof(TargetCapacityAndTwoDefinitionsAreUsed), TargetCapacityAndTwoDefinitionsAreUsed);
            yield return Case(nameof(ReferenceLoopReachesGrowthUpgradeAndEnd), ReferenceLoopReachesGrowthUpgradeAndEnd);
            yield return Case(nameof(BaselineAtCoarseFixedStep), BaselineAtCoarseFixedStep);
            yield return Case(nameof(BaselineAtFrameFixedStep), BaselineAtFrameFixedStep);
            yield return Case(nameof(FinalFrameIsClippedToTimeLimit), FinalFrameIsClippedToTimeLimit);
            yield return Case(nameof(CooldownExpiresBeforeSameFrameRequest), CooldownExpiresBeforeSameFrameRequest);
            yield return Case(nameof(AbsorptionHappensInStepReachingRadius), AbsorptionHappensInStepReachingRadius);
            yield return Case(nameof(SharedCatalogKeepsSessionStateIsolated), SharedCatalogKeepsSessionStateIsolated);
            yield return Case(nameof(LoaderReportsEveryDefinitionErrorWithPath), LoaderReportsEveryDefinitionErrorWithPath);
            yield return Case(nameof(LoaderReportsReferenceErrorsWithPath), LoaderReportsReferenceErrorsWithPath);
            yield return Case(nameof(CatalogConstructorEnforcesSameInvariants), CatalogConstructorEnforcesSameInvariants);
            yield return Case(nameof(EndingFrameRejectsSameFrameRequests), EndingFrameRejectsSameFrameRequests);
            yield return Case(nameof(TargetLifecycleRulesComeFromDefinition), TargetLifecycleRulesComeFromDefinition);
            yield return Case(nameof(LoaderReportsMovementAndTargetRuleErrors), LoaderReportsMovementAndTargetRuleErrors);
            yield return Case(nameof(NewMovementRuleUsesSharedLifecycle), NewMovementRuleUsesSharedLifecycle);
            yield return Case(nameof(LoaderRejectsFieldsUnusedByMovementKind), LoaderRejectsFieldsUnusedByMovementKind);
            yield return Case(nameof(DamagelessAreaPullIsOnlyComposition), DamagelessAreaPullIsOnlyComposition);
            yield return Case(nameof(CastReportCarriesAppliedArea), CastReportCarriesAppliedArea);
            yield return Case(nameof(AreaSkipsDefeatedAndPullsWhatItKills), AreaSkipsDefeatedAndPullsWhatItKills);
            yield return Case(nameof(LoaderReportsSkillCompositionErrors), LoaderReportsSkillCompositionErrors);
            yield return Case(nameof(RewardMassAndCreditsAreIndependent), RewardMassAndCreditsAreIndependent);
            yield return Case(nameof(RejectedPurchaseChangesNothing), RejectedPurchaseChangesNothing);
            yield return Case(nameof(LoaderReportsGrowthAndUpgradeErrors), LoaderReportsGrowthAndUpgradeErrors);
            yield return Case(nameof(AbsorptionRadiusUpgradeAppliesToAbsorption), AbsorptionRadiusUpgradeAppliesToAbsorption);
            yield return Case(nameof(SkillTreeBranchAndMergeRequireAll), SkillTreeBranchAndMergeRequireAll);
            yield return Case(nameof(RejectedTreeRequestsPreserveState), RejectedTreeRequestsPreserveState);
            yield return Case(nameof(SkillTreeRejectsBrokenGraph), SkillTreeRejectsBrokenGraph);
        }

        // 루트 → 두 갈래 → 합류를 데이터로 표현한다. 화면 없이 획득 가능성을 판정한다.
        public static void SkillTreeBranchAndMergeRequireAll()
        {
            GameSession game = TreeSession(20);
            SkillTree tree = game.Field.SkillTree;
            Equal(NodeStatus.Available, tree.Status("root"));
            Equal(NodeStatus.Locked, tree.Status("left"));
            Equal(NodeStatus.Locked, tree.Status("merge"));

            Equal(UpgradeResult.Purchased, game.TryAcquireNode("root"));
            Equal(NodeStatus.Acquired, tree.Status("root"));
            Equal(NodeStatus.Available, tree.Status("left"));
            Equal(NodeStatus.Available, tree.Status("right"));

            // AND: 한 갈래만으로는 합류 노드가 열리지 않는다.
            Equal(UpgradeResult.Purchased, game.TryAcquireNode("left"));
            Equal(NodeStatus.Locked, tree.Status("merge"));
            Equal(UpgradeResult.Purchased, game.TryAcquireNode("right"));
            Equal(NodeStatus.Available, tree.Status("merge"));
            Equal(UpgradeResult.Purchased, game.TryAcquireNode("merge"));
            Equal(NodeStatus.Acquired, tree.Status("merge"));

            // 획득 원본은 강화 단계다. 보정은 실제 계산에 반영된다.
            Equal(20 - 1 - 2 - 2 - 4, game.Field.Wallet.Credits);
            Equal(1, game.Field.Upgrades.Level("merge-up"));
            Near(1 + 0.1f + 0.1f + 0.5f, game.Field.DamageMultiplier);
            Near(0.65f + 0.1f, game.Field.BlackHole.AbsorptionRadius);
        }

        // 잠긴 노드·직접 구매 우회·중복 획득·재화 부족·알 수 없는 노드는 모두 상태를 바꾸지 않는다.
        public static void RejectedTreeRequestsPreserveState()
        {
            GameSession game = TreeSession(3);
            Equal(UpgradeResult.Locked, game.TryAcquireNode("merge"));
            Equal(UpgradeResult.Locked, game.TryPurchaseUpgrade("merge-up"));
            Equal(UpgradeResult.UnknownNode, game.TryAcquireNode("missing"));
            Equal(3, game.Field.Wallet.Credits);
            Equal(0, game.Field.Upgrades.Level("merge-up"));

            Equal(UpgradeResult.Purchased, game.TryAcquireNode("root"));
            Equal(UpgradeResult.MaxLevel, game.TryAcquireNode("root"));
            Equal(UpgradeResult.MaxLevel, game.TryPurchaseUpgrade("root-up"));
            Equal(2, game.Field.Wallet.Credits);

            Equal(UpgradeResult.Purchased, game.TryAcquireNode("left"));
            Equal(0, game.Field.Wallet.Credits);
            Equal(UpgradeResult.InsufficientCredits, game.TryAcquireNode("right"));
            Equal(0, game.Field.Wallet.Credits);
            Equal(0, game.Field.Upgrades.Level("right-up"));
            Equal(NodeStatus.Available, game.Field.SkillTree.Status("right"));
        }

        public static void SkillTreeRejectsBrokenGraph()
        {
            // 그래프 모양: 중복 노드, 없는 선행 노드, 중복 선행.
            ContentData shape = TreeContent(20);
            shape.SkillTree.Nodes.Add(Node("left", "extra-up"));
            shape.SkillTree.Nodes[3].Requires.Add("ghost");
            shape.SkillTree.Nodes[1].Requires.Add("root");
            ContentLoadResult result = ContentLoader.Load(shape);
            Check(!result.Succeeded, "그래프 오류가 있으면 로드에 실패해야 한다.");
            Equal(3, result.Diagnostics.Count);
            HasDiagnostic(result, "SkillTree.Nodes[4]", "left");
            HasDiagnostic(result, "SkillTree.Nodes[merge].Requires[2]", "ghost");
            HasDiagnostic(result, "SkillTree.Nodes[left].Requires[1]", "중복");

            // 순환: root → left → root. 트리 정의 생성자도 같은 규칙으로 거부한다.
            ContentData cycle = TreeContent(20);
            cycle.SkillTree.Nodes[0].Requires.Add("left");
            result = ContentLoader.Load(cycle);
            Equal(1, result.Diagnostics.Count);
            Check(result.Diagnostics[0].Message.Contains("순환"), "순환을 보고해야 한다: " + result.Diagnostics[0]);
            Throws<ArgumentException>(() => new SkillTreeDefinition(new[]
            {
                new SkillTreeNodeDefinition("a", "a-up", new[] { "b" }),
                new SkillTreeNodeDefinition("b", "b-up", new[] { "a" })
            }));

            // 강화 참조: 없는 강화, 한 강화를 두 노드가 참조.
            ContentData references = TreeContent(20);
            references.SkillTree.Nodes[1].UpgradeId = "missing-up";
            references.SkillTree.Nodes[2].UpgradeId = "root-up";
            result = ContentLoader.Load(references);
            Equal(2, result.Diagnostics.Count);
            HasDiagnostic(result, "SkillTree.Nodes[left].UpgradeId", "missing-up");
            HasDiagnostic(result, "SkillTree.Nodes[right].UpgradeId", "root");
        }

        // 두 번째 강화 종류: 흡수 반경 보정. 정의 추가만으로 구매·표시·실제 흡수 판정에 연결된다.
        public static void AbsorptionRadiusUpgradeAppliesToAbsorption()
        {
            ContentData data = ReferenceGame.CreateContent();
            data.Targets[0].Reward = Reward(0, 20);
            data.Targets[1].MaxHealth = 1;
            data.Upgrades[1].PerLevel = 0.5f;
            ContentCatalog catalog = Load(data);
            GameSession upgraded = SessionAssembler.Create(catalog);
            GameSession plain = SessionAssembler.Create(catalog);

            foreach (GameSession game in new[] { upgraded, plain })
            {
                game.TryCast(ReferenceGame.StrikeId, game.Field.Targets[0].Position);
                game.Advance(2);
                Equal(20, game.Field.Wallet.Credits);
            }

            Equal(UpgradeResult.Purchased, upgraded.TryPurchaseUpgrade(ReferenceGame.ReachUpgradeId));
            Equal(12, upgraded.Field.Wallet.Credits);
            Near(1.15f, upgraded.Field.BlackHole.AbsorptionRadius);
            Near(0.65f, plain.Field.BlackHole.AbsorptionRadius);
            Near(1, upgraded.Field.DamageMultiplier);

            // 같은 위치에서 사망한 heavy가 떨어진다: 강화한 판만 1.1초 안에 흡수 반경에 닿는다.
            TargetState upgradedHeavy = upgraded.Field.Targets[0];
            TargetState plainHeavy = plain.Field.Targets[0];
            Near(plainHeavy.Radius, upgradedHeavy.Radius);
            Equal(CastResult.Cast, upgraded.TryCast(ReferenceGame.StrikeId, upgradedHeavy.Position));
            Equal(CastResult.Cast, plain.TryCast(ReferenceGame.StrikeId, plainHeavy.Position));
            upgraded.Advance(1.1f);
            plain.Advance(1.1f);
            Equal(TargetPhase.Absorbed, upgradedHeavy.Phase);
            Equal(TargetPhase.Defeated, plainHeavy.Phase);
        }

        // 질량 증가와 재화 지급은 서로 다른 수치다. 흡수가 확정될 때 한 번만 받는다.
        public static void RewardMassAndCreditsAreIndependent()
        {
            ContentData data = ReferenceGame.CreateContent();
            data.Targets[0].Reward = Reward(3, 7);
            GameSession game = Assemble(data);
            TargetState shard = game.Field.Targets[0];

            game.TryCast(ReferenceGame.StrikeId, shard.Position);
            game.Advance(2);
            Equal(TargetPhase.Absorbed, shard.Phase);
            Equal(3, game.Field.BlackHole.Mass);
            Equal(7, game.Field.Wallet.Credits);
            Equal(1, game.Field.BlackHole.AbsorbedCount);
            // 흡수 반경은 질량에서 계산한다(재화는 반경에 영향이 없다).
            Near(0.65f + 3 * 0.012f, game.Field.BlackHole.AbsorptionRadius);

            game.Advance(0.5f);
            Equal(3, game.Field.BlackHole.Mass);
            Equal(7, game.Field.Wallet.Credits);
        }

        // 거절 조건(참조·자격·비용)은 상태 변경 전에 모두 판정된다.
        public static void RejectedPurchaseChangesNothing()
        {
            ContentData data = ReferenceGame.CreateContent();
            data.Targets[0].Reward = Reward(0, 7);
            GameSession game = Assemble(data);
            game.TryCast(ReferenceGame.StrikeId, game.Field.Targets[0].Position);
            game.Advance(2);
            Equal(7, game.Field.Wallet.Credits);

            Equal(UpgradeResult.UnknownUpgrade, game.TryPurchaseUpgrade("missing"));
            Equal(UpgradeResult.UnknownUpgrade, game.TryPurchaseUpgrade(null));
            Equal(7, game.Field.Wallet.Credits);
            Equal(0, game.Field.Upgrades.Level(ReferenceGame.PowerUpgradeId));

            // 1단계 비용 6 → 성공. 2단계 비용 12 → 잔액 1로 거절.
            Equal(UpgradeResult.Purchased, game.TryPurchaseUpgrade(ReferenceGame.PowerUpgradeId));
            Equal(1, game.Field.Wallet.Credits);
            Equal(12, game.Field.Upgrades.NextCost(ReferenceGame.PowerUpgradeId));
            Equal(UpgradeResult.InsufficientCredits, game.TryPurchaseUpgrade(ReferenceGame.PowerUpgradeId));
            Equal(1, game.Field.Wallet.Credits);
            Equal(1, game.Field.Upgrades.Level(ReferenceGame.PowerUpgradeId));
            Near(1.35f, game.Field.DamageMultiplier);
        }

        public static void LoaderReportsGrowthAndUpgradeErrors()
        {
            ContentData data = ReferenceGame.CreateContent();
            data.BlackHole = null;
            data.Targets[0].Reward = Reward(-1, 2);
            data.Targets[1].Reward = null;
            data.Upgrades.Add(new UpgradeData { Id = "luck", BaseCost = 1, MaxLevel = 1, Stat = "Luck", PerLevel = 1 });
            data.Upgrades.Add(new UpgradeData { Id = "free", BaseCost = 0, MaxLevel = 1, Stat = "DamageMultiplier", PerLevel = 1 });

            ContentLoadResult result = ContentLoader.Load(data);
            Check(!result.Succeeded, "성장·강화 정의 오류가 있으면 로드에 실패해야 한다.");
            Equal(5, result.Diagnostics.Count);
            HasDiagnostic(result, "BlackHole", string.Empty);
            HasDiagnostic(result, "Targets[shard].Reward", "mass");
            HasDiagnostic(result, "Targets[heavy].Reward", string.Empty);
            HasDiagnostic(result, "Upgrades[luck].Stat", "Luck");
            HasDiagnostic(result, "Upgrades[free]", "baseCost");

            ContentData duplicate = ReferenceGame.CreateContent();
            duplicate.Upgrades.Add(Power(1, 1, 1));
            Fails(duplicate, $"Upgrades[{duplicate.Upgrades.Count - 1}]");
        }

        // 피해 없는 범위 당김은 새 코드 없이 기존 선택(AllInRadius)과 효과(Pull)의 조합이다.
        public static void DamagelessAreaPullIsOnlyComposition()
        {
            GameSession game = Assemble(Content(
                new[] { Target("test", 100, 0, 0.01f, 10) },
                new[] { Skill("tractor", 1, "AllInRadius", 10, Effect("Pull", 1)) },
                Power(6, 1, 0.5f),
                Spawn(0.1f, 2, 3, "test")));
            game.Advance(0.3f);
            Equal(3, game.Field.Targets.Count);
            var before = new float[3];
            for (int i = 0; i < 3; i++) before[i] = game.Field.Targets[i].Radius;

            Equal(CastResult.Cast, game.TryCast("tractor", new Point2(0, 0), out CastReport report));
            Equal(3, report.AffectedCount);
            for (int i = 0; i < 3; i++)
            {
                Near(100, game.Field.Targets[i].Health);
                Near(before[i] - 1, game.Field.Targets[i].Radius);
            }
            Near(1, game.Field.Skills[0].RemainingCooldown);
        }

        // 화면은 발동 결과로 연출한다. 결과에는 실제 선택 범위와 적용 대상 수가 있다.
        public static void CastReportCarriesAppliedArea()
        {
            GameSession game = Reference();
            Point2 aim = game.Field.Targets[0].Position;

            Equal(CastResult.NoTarget, game.TryCast(ReferenceGame.PulseId, new Point2(100, 100), out CastReport miss));
            Equal(0, miss.AffectedCount);

            Equal(CastResult.Cast, game.TryCast(ReferenceGame.PulseId, aim, out CastReport pulse));
            Near(2.4f, pulse.AreaRadius);
            Equal(1, pulse.AffectedCount);
            Near(aim.X, pulse.Aim.X);
            Near(aim.Y, pulse.Aim.Y);

            Equal(CastResult.Cast, game.TryCast(ReferenceGame.StrikeId, game.Field.Targets[0].Position, out CastReport strike));
            Near(0.9f, strike.AreaRadius);
            Equal(1, strike.AffectedCount);
        }

        // 범위 선택은 살아 있는 대상만 고른다(이미 사망한 대상은 당기지 않는다).
        // 고른 대상에는 정의 순서대로 효과를 적용하므로, 이번 피해로 사망한 대상에도 당김이 적용된다.
        public static void AreaSkipsDefeatedAndPullsWhatItKills()
        {
            GameSession game = Assemble(Content(
                new[] { Target("weak", 5, 0, 0.01f, 1) },
                new[] { Strike("strike", 0.1f, 10, 10), Pulse("pulse", 0.1f, 9, 10, 0.8f) },
                Power(6, 1, 0.5f),
                Spawn(0.1f, 2, 2, "weak")));
            game.Advance(0.1f);
            Equal(2, game.Field.Targets.Count);
            TargetState first = game.Field.Targets[0];
            TargetState second = game.Field.Targets[1];

            Equal(CastResult.Cast, game.TryCast("strike", first.Position));
            Equal(TargetPhase.Defeated, first.Phase);
            float firstRadius = first.Radius;
            float secondRadius = second.Radius;

            Equal(CastResult.Cast, game.TryCast("pulse", new Point2(0, 0), out CastReport report));
            Equal(1, report.AffectedCount);
            Near(firstRadius, first.Radius);
            Equal(TargetPhase.Defeated, second.Phase);
            Near(secondRadius - 0.8f, second.Radius);
        }

        public static void LoaderReportsSkillCompositionErrors()
        {
            ContentData data = ReferenceGame.CreateContent();
            data.Skills[0].Selection = null;
            data.Skills[1].Selection.Kind = "Cone";
            data.Skills[1].Effects[1].Amount = -1;
            data.Skills.Add(Skill("empty", 1, "AllInRadius", 1));

            ContentLoadResult result = ContentLoader.Load(data);
            Check(!result.Succeeded, "스킬 조합 오류가 있으면 로드에 실패해야 한다.");
            Equal(4, result.Diagnostics.Count);
            HasDiagnostic(result, "Skills[focused-strike].Selection", string.Empty);
            HasDiagnostic(result, "Skills[gravity-pulse].Selection.Kind", "Cone");
            HasDiagnostic(result, "Skills[gravity-pulse].Effects[1]", "distance");
            HasDiagnostic(result, "Skills[empty]", "효과");
        }

        // 검증용 콘텐츠: Dive는 Orbit의 수치 변형이 아닌 두 번째 이동 규칙이다.
        // TargetState·TargetWorld·SpawnSchedule·화면을 고치지 않고 정의 + 규칙 + factory 분기로 연결된다.
        public static void NewMovementRuleUsesSharedLifecycle()
        {
            GameSession game = Assemble(Content(
                new[] { Diver("diver", 20, 1, 2, 4) },
                new[] { Strike("strike", 0.1f, 30, 1) },
                Power(6, 1, 0.5f),
                Spawn(10, 5, 4, "diver")));
            TargetState diver = game.Field.Targets[0];
            float angle = diver.Angle;

            game.Advance(0.5f);
            float firstHalf = 5 - diver.Radius;
            game.Advance(0.5f);
            float secondHalf = 5 - firstHalf - diver.Radius;
            Check(secondHalf > firstHalf, "Dive는 시간이 갈수록 빨라져야 한다.");
            // 1/30초 단계마다 단계 시작 속도(1 + 2 × 나이)로 진행: 5 - 1.9667.
            Near(3.0333f, diver.Radius);
            Near(angle, diver.Angle);

            // 공통 하한: 질량 0의 흡수 반경 0.65 + 여유 0.8.
            game.Advance(2);
            Near(1.45f, diver.Radius);
            Equal(TargetPhase.Alive, diver.Phase);

            // 사망·낙하·흡수·보상은 이동 종류와 무관한 공통 흐름이다.
            Equal(CastResult.Cast, game.TryCast("strike", diver.Position));
            Equal(TargetPhase.Defeated, diver.Phase);
            game.Advance(0.5f);
            Equal(TargetPhase.Absorbed, diver.Phase);
            Near(angle, diver.Angle);
            Equal(4, game.Field.BlackHole.Mass);
            Equal(0, game.Field.Targets.Count);
        }

        public static void LoaderRejectsFieldsUnusedByMovementKind()
        {
            ContentData data = ReferenceGame.CreateContent();
            data.Targets[0].Movement.Acceleration = 1;
            data.Targets.Add(new TargetData
            {
                Id = "diver", MaxHealth = 1, Reward = Reward(1, 1),
                Movement = new MovementData { Kind = "Dive", InitialSpeed = 1, AngularSpeed = 0.5f }
            });

            ContentLoadResult result = ContentLoader.Load(data);
            Check(!result.Succeeded, "종류가 쓰지 않는 칸이 채워지면 로드에 실패해야 한다.");
            Equal(2, result.Diagnostics.Count);
            HasDiagnostic(result, "Targets[shard].Movement", "Acceleration");
            HasDiagnostic(result, "Targets[diver].Movement", "AngularSpeed");
        }

        // 하한 여유와 낙하 속도는 모든 대상에 공통인 정의다. 사망한 대상은 이동 규칙의 각도 변화를 유지한다.
        public static void TargetLifecycleRulesComeFromDefinition()
        {
            ContentData data = ReferenceGame.CreateContent();
            data.TargetRules.AliveMargin = 2;
            data.TargetRules.FallSpeed = 2;
            GameSession game = Assemble(data);

            TargetState alive = game.Field.Targets[0];
            game.Advance(30);
            // 질량 0의 흡수 반경 0.65 + 여유 2.
            Near(2.65f, alive.Radius);
            Equal(TargetPhase.Alive, alive.Phase);

            float angle = alive.Angle;
            Equal(CastResult.Cast, game.TryCast(ReferenceGame.StrikeId, alive.Position));
            Equal(TargetPhase.Defeated, alive.Phase);
            game.Advance(0.5f);
            Near(1.65f, alive.Radius);
            Near(angle + 0.6f * 0.5f, alive.Angle);
        }

        public static void LoaderReportsMovementAndTargetRuleErrors()
        {
            ContentData data = ReferenceGame.CreateContent();
            data.TargetRules = null;
            data.Targets[0].Movement = null;
            data.Targets[1].Movement.Kind = "Teleport";
            data.Targets.Add(new TargetData
            {
                Id = "still", MaxHealth = 1, Reward = Reward(1, 1),
                Movement = new MovementData { Kind = "Orbit", AngularSpeed = 0, InwardSpeed = 0 }
            });

            ContentLoadResult result = ContentLoader.Load(data);
            Check(!result.Succeeded, "이동 정의 오류가 있으면 로드에 실패해야 한다.");
            Equal(4, result.Diagnostics.Count);
            HasDiagnostic(result, "TargetRules", string.Empty);
            HasDiagnostic(result, "Targets[shard].Movement", string.Empty);
            HasDiagnostic(result, "Targets[heavy].Movement.Kind", "Teleport");
            HasDiagnostic(result, "Targets[still].Movement", "inwardSpeed");
        }

        // 호스트는 Advance 뒤에 요청을 적용한다. 그 Advance에서 판이 끝났으면 요청은 판을 바꾸지 않는다.
        public static void EndingFrameRejectsSameFrameRequests()
        {
            GameSession game = Reference(0.5f);
            TargetState target = game.Field.Targets[0];
            game.Advance(0.6f);
            Equal(SessionPhase.Ended, game.Phase);
            Equal(TargetPhase.Alive, target.Phase);
            Near(0, game.Remaining);

            Equal(CastResult.SessionInactive, game.TryCast(ReferenceGame.StrikeId, target.Position));
            Equal(CastResult.SessionInactive, game.TryCast(ReferenceGame.PulseId, target.Position));
            Near(12, target.Health);
            Near(0, game.Field.Skills[0].RemainingCooldown);
        }

        public static void DeathDoesNotRewardUntilAbsorption()
        {
            GameSession game = Reference();
            TargetState target = game.Field.Targets[0];
            Equal(CastResult.Cast, game.TryCast(ReferenceGame.StrikeId, target.Position));
            Equal(TargetPhase.Defeated, target.Phase);
            Equal(0, game.Field.BlackHole.Mass);
            game.Advance(0.1f);
            Equal(0, game.Field.BlackHole.Mass);
        }

        public static void AbsorptionRewardsExactlyOnce()
        {
            GameSession game = Reference();
            TargetState target = game.Field.Targets[0];
            game.TryCast(ReferenceGame.StrikeId, target.Position);
            game.Advance(2);
            Equal(TargetPhase.Absorbed, target.Phase);
            Equal(2, game.Field.BlackHole.Mass);
            Equal(2, game.Field.Wallet.Credits);
            Equal(1, game.Field.BlackHole.AbsorbedCount);
            game.Advance(4);
            Equal(2, game.Field.BlackHole.Mass);
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
            Equal(TargetPhase.Alive, target.Phase);
            Equal(0, game.Field.BlackHole.Mass);
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
            GameSession game = Assemble(Content(
                new[] { Target("reward", 1, 0, 0.01f, 10), Target("durable", 100, 0, 0.01f, 10) },
                new[] { Strike("strike", 0.1f, 10, 0.5f) },
                Power(6, 1, 0.5f),
                Spawn(0.8f, 2, 3, "reward", "durable")));
            Equal(UpgradeResult.InsufficientCredits, game.TryPurchaseUpgrade(ReferenceGame.PowerUpgradeId));
            Equal(0, game.Field.Upgrades.Level(ReferenceGame.PowerUpgradeId));
            Equal(0, game.Field.Wallet.Credits);
            game.TryCast("strike", game.Field.Targets[0].Position);
            game.Advance(1);
            Equal(10, game.Field.Wallet.Credits);
            Equal(UpgradeResult.Purchased, game.TryPurchaseUpgrade(ReferenceGame.PowerUpgradeId));
            Equal(4, game.Field.Wallet.Credits);
            Equal(1, game.Field.Upgrades.Level(ReferenceGame.PowerUpgradeId));
            Near(1.5f, game.Field.DamageMultiplier);
            Equal(UpgradeResult.MaxLevel, game.TryPurchaseUpgrade(ReferenceGame.PowerUpgradeId));
            Equal(4, game.Field.Wallet.Credits);

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
            Equal(UpgradeResult.SessionInactive, game.TryPurchaseUpgrade(ReferenceGame.PowerUpgradeId));
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
            Equal(UpgradeResult.SessionInactive, game.TryPurchaseUpgrade(ReferenceGame.PowerUpgradeId));
        }

        public static void RestartHasFreshState()
        {
            GameSession old = Reference();
            old.TryCast(ReferenceGame.StrikeId, old.Field.Targets[0].Position);
            old.Advance(2);
            old.Stop();
            GameSession next = Reference();
            Equal(0, next.Field.BlackHole.Mass);
            Near(0, next.Elapsed);
            Near(0, next.Field.Skills[0].RemainingCooldown);
            Near(12, next.Field.Targets[0].Health);
            Equal(2, old.Result.Mass);
            Check(!ReferenceEquals(old.Field.Targets[0], next.Field.Targets[0]), "개체 상태를 재사용하면 안 된다.");
        }

        public static void DefinitionsRejectInvalidData()
        {
            Throws<ArgumentException>(() => new TargetDefinition("", 1, new OrbitMovementDefinition(1, 1), new RewardDefinition(1, 1)));
            Throws<ArgumentOutOfRangeException>(() => new TargetDefinition("a", float.NaN, new OrbitMovementDefinition(1, 1), new RewardDefinition(1, 1)));
            Throws<ArgumentOutOfRangeException>(() => new AllInRadiusDefinition(-1));
            Throws<ArgumentException>(() => new SkillDefinition("a", 1, new AllInRadiusDefinition(1), new SkillEffectDefinition[0]));
            Throws<ArgumentOutOfRangeException>(() => new Point2(float.PositiveInfinity, 0));

            ContentData duplicateSkill = ReferenceGame.CreateContent();
            duplicateSkill.Skills[1].Id = ReferenceGame.StrikeId;
            Fails(duplicateSkill, "Skills[1]");

            ContentData duplicateTarget = ReferenceGame.CreateContent();
            duplicateTarget.Targets[1].Id = "shard";
            Fails(duplicateTarget, "Targets[1]");

            Throws<ArgumentOutOfRangeException>(() => Reference().Advance(float.NaN));
            ContentData negativeDuration = ReferenceGame.CreateContent();
            negativeDuration.TimeLimit = -1;
            Fails(negativeDuration, "TimeLimit");
        }

        // 수치만 다른 새 스킬은 정의 추가만으로 동작한다. Session·Loadout·호스트를 고치지 않는다.
        // 새 선택·효과의 의미를 추가하는 자리는 SkillRuleFactory 한 곳이다(하위 정의 + 분기 + 실행 규칙).
        public static void NewSkillNeedsOnlyDefinition()
        {
            GameSession game = Assemble(Content(
                new[] { Target("new-target", 50, 0, 1, 3) },
                new[] { Strike("new-skill", 1, 7, 10) },
                Power(1, 1, 1),
                Spawn(1, 5, 4, "new-target"),
                duration: 5));
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
            Equal(0, game.Field.BlackHole.Mass);
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
            Equal(0, game.Field.BlackHole.Mass);
            game.Advance(0.05f);
            Equal(TargetPhase.Absorbed, target.Phase);
            Equal(2, game.Field.BlackHole.Mass);
            foreach (TargetState current in game.Field.Targets)
                Check(current.Id != target.Id, "흡수한 단계에서 목록 제거까지 끝나야 한다.");
        }

        public static void SharedCatalogKeepsSessionStateIsolated()
        {
            ContentData data = ReferenceGame.CreateContent();
            ContentCatalog catalog = Load(data);
            GameSession first = SessionAssembler.Create(catalog);
            GameSession second = SessionAssembler.Create(catalog);

            RunScriptedLoop(first, 0.1f, 60);
            Check(first.Field.Upgrades.Level(ReferenceGame.PowerUpgradeId) > 0, "첫 판은 강화까지 진행해야 한다.");
            // 로드 뒤 저작 데이터를 바꿔도 이미 만든 카탈로그와 판에는 영향이 없다.
            data.Targets[0].MaxHealth = 999;

            foreach (GameSession fresh in new[] { second, SessionAssembler.Create(catalog) })
            {
                Near(0, fresh.Elapsed);
                Equal(0, fresh.Field.BlackHole.Mass);
                Equal(0, fresh.Field.Wallet.Credits);
                Equal(0, fresh.Field.Upgrades.Level(ReferenceGame.PowerUpgradeId));
                Near(0, fresh.Field.Skills[0].RemainingCooldown);
                Equal(1, fresh.Field.Targets.Count);
                Near(12, fresh.Field.Targets[0].Health);
                Check(!ReferenceEquals(first.Field.Skills[0], fresh.Field.Skills[0]), "스킬 상태는 판마다 새로 만든다.");
                Check(ReferenceEquals(first.Field.Skills[0].Definition, fresh.Field.Skills[0].Definition),
                    "정의는 판 사이에 공유한다.");
            }
        }

        public static void LoaderReportsEveryDefinitionErrorWithPath()
        {
            ContentData data = ReferenceGame.CreateContent();
            data.Targets[1].MaxHealth = float.NaN;
            data.Skills[0].Cooldown = 0;
            data.Skills[1].Effects[0].Kind = "Laser";
            data.Spawn.Interval = -1;
            data.TimeLimit = 0;

            ContentLoadResult result = ContentLoader.Load(data);
            Check(!result.Succeeded && result.Catalog == null, "오류가 있으면 카탈로그를 만들지 않는다.");
            Equal(5, result.Diagnostics.Count);
            HasDiagnostic(result, "Targets[heavy]", "maxHealth");
            HasDiagnostic(result, "Skills[focused-strike]", "cooldown");
            HasDiagnostic(result, "Skills[gravity-pulse].Effects[0].Kind", "Laser");
            HasDiagnostic(result, "Spawn", "interval");
            HasDiagnostic(result, "TimeLimit", "duration");
        }

        public static void LoaderReportsReferenceErrorsWithPath()
        {
            ContentData data = ReferenceGame.CreateContent();
            data.Targets.Add(Target("shard", 1, 0, 1, 1));
            data.Skills[1].Id = ReferenceGame.StrikeId;
            data.Spawn.TargetOrder.Add("ghost");

            ContentLoadResult result = ContentLoader.Load(data);
            Check(!result.Succeeded, "참조 오류가 있으면 로드에 실패해야 한다.");
            Equal(3, result.Diagnostics.Count);
            HasDiagnostic(result, "Targets[2]", "shard");
            HasDiagnostic(result, "Skills[1]", ReferenceGame.StrikeId);
            HasDiagnostic(result, "Spawn.TargetOrder[2]", "ghost");
        }

        public static void CatalogConstructorEnforcesSameInvariants()
        {
            var target = new TargetDefinition("shard", 12, new OrbitMovementDefinition(0, 1), new RewardDefinition(2, 2));
            var skill = new SkillDefinition("strike", 1, new NearestInRadiusDefinition(1),
                new SkillEffectDefinition[] { new DamageEffectDefinition(1) });
            var blackHole = new BlackHoleDefinition(0.65f, 0.012f, 50);
            var upgrades = new[] { new UpgradeDefinition("power", 1, 1, UpgradeStat.DamageMultiplier, 1) };
            var spawn = new SpawnDefinition(new[] { "shard" }, 1, 5, 0, 4);
            var limit = new TimeLimitDefinition(10);
            var rules = new TargetRulesDefinition(0.8f, 4);
            new ContentCatalog(limit, rules, blackHole, new[] { target }, new[] { skill }, upgrades, SkillTreeDefinition.Empty, spawn);

            Throws<ArgumentException>(() => new ContentCatalog(
                limit, rules, blackHole, new[] { target }, new[] { skill, skill }, upgrades, SkillTreeDefinition.Empty, spawn));
            Throws<ArgumentException>(() => new ContentCatalog(
                limit, rules, blackHole, new[] { target }, new[] { skill }, upgrades, SkillTreeDefinition.Empty,
                new SpawnDefinition(new[] { "ghost" }, 1, 5, 0, 4)));
            Throws<ArgumentException>(() => new ContentCatalog(
                limit, rules, blackHole, new[] { target }, new[] { skill }, new[] { upgrades[0], upgrades[0] }, SkillTreeDefinition.Empty, spawn));
        }

        // ── 공통 준비 ───────────────────────────────────────────────────────

        private static GameSession Reference(float duration = 60)
        {
            ContentData data = ReferenceGame.CreateContent();
            data.TimeLimit = duration;
            return Assemble(data);
        }

        private static GameSession Assemble(ContentData data) => SessionAssembler.Create(Load(data));

        private static ContentCatalog Load(ContentData data)
        {
            ContentLoadResult result = ContentLoader.Load(data);
            Check(result.Succeeded, result.Diagnostics.Count > 0 ? result.Diagnostics[0].ToString() : "로드 실패");
            return result.Catalog;
        }

        private static ContentData Content(TargetData[] targets, SkillData[] skills,
            UpgradeData power, SpawnData spawn, float duration = 60) => new ContentData
        {
            TimeLimit = duration,
            TargetRules = new TargetRulesData { AliveMargin = 0.8f, FallSpeed = 4 },
            Targets = new List<TargetData>(targets),
            Skills = new List<SkillData>(skills),
            BlackHole = new BlackHoleData { BaseAbsorptionRadius = 0.65f, RadiusPerMass = 0.012f, MassRadiusCap = 50 },
            Upgrades = new List<UpgradeData> { power },
            Spawn = spawn
        };

        private static TargetData Target(string id, float health, float angular, float inward, int reward) =>
            new TargetData { Id = id, MaxHealth = health, Reward = Reward(reward, reward), Movement = Orbit(angular, inward) };

        private static MovementData Orbit(float angular, float inward) =>
            new MovementData { Kind = "Orbit", AngularSpeed = angular, InwardSpeed = inward };

        private static TargetData Diver(string id, float health, float initialSpeed, float acceleration, int reward) =>
            new TargetData
            {
                Id = id, MaxHealth = health, Reward = Reward(reward, reward),
                Movement = new MovementData { Kind = "Dive", InitialSpeed = initialSpeed, Acceleration = acceleration }
            };

        // 집중 공격 모양: 단일 선택 + 피해.
        private static SkillData Strike(string id, float cooldown, float damage, float aimRadius) =>
            Skill(id, cooldown, "NearestInRadius", aimRadius, Effect("Damage", damage));

        // 중력파 모양: 범위 선택 + 피해 + 당김.
        private static SkillData Pulse(string id, float cooldown, float damage, float radius, float pull) =>
            Skill(id, cooldown, "AllInRadius", radius, Effect("Damage", damage), Effect("Pull", pull));

        private static SkillData Skill(string id, float cooldown, string selection, float radius,
            params EffectData[] effects) => new SkillData
        {
            Id = id,
            Cooldown = cooldown,
            Selection = new SelectionData { Kind = selection, Radius = radius },
            Effects = new List<EffectData>(effects)
        };

        private static EffectData Effect(string kind, float amount) => new EffectData { Kind = kind, Amount = amount };

        // 공격 배율 강화(샘플과 같은 ID).
        private static UpgradeData Power(int cost, int maxLevel, float perLevel) =>
            new UpgradeData
            {
                Id = ReferenceGame.PowerUpgradeId, BaseCost = cost, MaxLevel = maxLevel,
                Stat = "DamageMultiplier", PerLevel = perLevel
            };

        private static RewardData Reward(int mass, int credits) => new RewardData { Mass = mass, Credits = credits };

        // 검증용 트리: root → left, right → merge. 강화는 모두 1단계, 비용 1·2·2·4.
        // 첫 shard 흡수로 credits를 받는다(질량 0이라 흡수 반경은 기본값 그대로).
        private static ContentData TreeContent(int credits)
        {
            ContentData data = ReferenceGame.CreateContent();
            data.Targets[0].Reward = Reward(0, credits);
            data.Upgrades.Add(TreeUpgrade("root-up", 1, "DamageMultiplier", 0.1f));
            data.Upgrades.Add(TreeUpgrade("left-up", 2, "DamageMultiplier", 0.1f));
            data.Upgrades.Add(TreeUpgrade("right-up", 2, "AbsorptionRadius", 0.1f));
            data.Upgrades.Add(TreeUpgrade("merge-up", 4, "DamageMultiplier", 0.5f));
            data.Upgrades.Add(TreeUpgrade("extra-up", 1, "DamageMultiplier", 0.1f));
            data.SkillTree = new SkillTreeData
            {
                Nodes =
                {
                    Node("root", "root-up"),
                    Node("left", "left-up", "root"),
                    Node("right", "right-up", "root"),
                    Node("merge", "merge-up", "left", "right")
                }
            };
            return data;
        }

        private static GameSession TreeSession(int credits)
        {
            GameSession game = Assemble(TreeContent(credits));
            game.TryCast(ReferenceGame.StrikeId, game.Field.Targets[0].Position);
            game.Advance(2);
            Equal(credits, game.Field.Wallet.Credits);
            return game;
        }

        private static UpgradeData TreeUpgrade(string id, int cost, string stat, float perLevel) =>
            new UpgradeData { Id = id, BaseCost = cost, MaxLevel = 1, Stat = stat, PerLevel = perLevel };

        private static SkillTreeNodeData Node(string id, string upgradeId, params string[] requires) =>
            new SkillTreeNodeData { Id = id, UpgradeId = upgradeId, Requires = new List<string>(requires) };

        private static SpawnData Spawn(float interval, float radius, int capacity, params string[] order) =>
            new SpawnData
            {
                TargetOrder = new List<string>(order), Interval = interval, Radius = radius,
                AngleStep = 2.399963f, Capacity = capacity
            };

        private static void Fails(ContentData data, string path)
        {
            ContentLoadResult result = ContentLoader.Load(data);
            Check(!result.Succeeded, "로드에 실패해야 한다: " + path);
            HasDiagnostic(result, path, string.Empty);
        }

        private static void HasDiagnostic(ContentLoadResult result, string path, string reason)
        {
            foreach (ContentDiagnostic diagnostic in result.Diagnostics)
                if (diagnostic.Path == path && diagnostic.Message.Contains(reason)) return;
            throw new InvalidOperationException(
                $"진단 없음: {path} ({reason}). 받은 진단: {string.Join(" | ", result.Diagnostics)}");
        }

        // 매 프레임: 첫 번째 궤도 대상에 두 스킬 요청 → 가능하면 강화 → 진행.
        // 판이 끝나거나 frames만큼 진행하면 멈춘다.
        private static bool RunScriptedLoop(GameSession game, float step, int frames = int.MaxValue)
        {
            bool upgraded = false;
            for (int frame = 0; frame < frames && game.Phase != SessionPhase.Ended; frame++)
            {
                foreach (TargetState target in game.Field.Targets)
                {
                    if (target.Phase != TargetPhase.Alive) continue;
                    game.TryCast(ReferenceGame.StrikeId, target.Position);
                    game.TryCast(ReferenceGame.PulseId, target.Position);
                    break;
                }
                if (game.TryPurchaseUpgrade(ReferenceGame.PowerUpgradeId) == UpgradeResult.Purchased) upgraded = true;
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
            Equal(expected.Mass, game.Field.BlackHole.Mass);
            Equal(expected.Credits, game.Field.Wallet.Credits);
            Equal(expected.Absorbed, game.Field.BlackHole.AbsorbedCount);
            Equal(expected.PowerLevel, game.Field.Upgrades.Level(ReferenceGame.PowerUpgradeId));
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
            Equal(expected.Credits, game.Field.Wallet.Credits);
            Equal(expected.PowerLevel, game.Field.Upgrades.Level(ReferenceGame.PowerUpgradeId));
            Equal(expected.Targets, game.Field.Targets.Count);
        }

        private static GameSession CreateCrowdedSession(float health = 100) => Assemble(Content(
            new[] { Target("test", health, 0, 0.01f, 10) },
            new[] { Strike("strike", 0.1f, 10, 10), Pulse("pulse", 0.1f, 5, 10, 0.5f) },
            Power(6, 1, 0.5f),
            Spawn(0.1f, 2, 3, "test")));

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
