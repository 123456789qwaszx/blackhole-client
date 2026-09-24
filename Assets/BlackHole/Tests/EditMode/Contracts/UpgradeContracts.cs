using System;
using System.Collections.Generic;
using BlackHole.Core;

namespace BlackHole.Core.Tests
{
    // B1 Player별 진행 상태, B4 생성 시 구매 보정, B8 새 전투와 새 진행, B11 전투 밖 구매.
    internal static class UpgradeContracts
    {
        private static readonly Point2 East = new Point2(3, 0);

        public static IEnumerable<Contract> Cases()
        {
            yield return new Contract("Upgrade.PurchaseNeedsPrerequisiteAndGold", PurchaseNeedsPrerequisiteAndGold);
            yield return new Contract("Upgrade.CannotPurchaseDuringBattle", CannotPurchaseDuringBattle);
            yield return new Contract("Upgrade.NextBattleKeepsProgressAndRenewsBattle", NextBattleKeepsProgressAndRenewsBattle);
            yield return new Contract("Upgrade.PurchasedEffectsChangeNextBattle", PurchasedEffectsChangeNextBattle);
            yield return new Contract("Upgrade.SkillEffectsTargetTheirSkill", SkillEffectsTargetTheirSkill);
            yield return new Contract("Upgrade.SkillStatEffectsTargetOnlyBreaker", SkillStatEffectsTargetOnlyBreaker);
            yield return new Contract("Upgrade.NewProgressionStartsEmpty", NewProgressionStartsEmpty);
            yield return new Contract("Upgrade.PlayerStatesAreIndependent", PlayerStatesAreIndependent);
            yield return new Contract("Upgrade.SharedWorldPurchasesInMultiplayerHaveNoPolicy", SharedWorldPurchasesInMultiplayerHaveNoPolicy);
            yield return new Contract("Upgrade.PurchasedNodeMustExistInContent", PurchasedNodeMustExistInContent);
        }

        // 실패한 구매는 Gold와 구매 상태를 바꾸지 않는다.
        private static void PurchaseNeedsPrerequisiteAndGold()
        {
            GameContent content = TestContent.Load(Market(coins: 2, gold: 10));
            var state = new PlayerState(TestContent.First);
            EarnInBattle(content, state);
            Expect.Equal(20, state.Gold);

            Expect.Equal(PurchaseResult.MissingPrerequisite, Buy(content, state, "child"));
            Expect.Equal(20, state.Gold);
            Expect.True(!state.Owns("child"), "선행 노드 없이 사면 안 된다.");

            Expect.Equal(PurchaseResult.Purchased, Buy(content, state, "root"));
            Expect.Equal(10, state.Gold);

            Expect.Equal(PurchaseResult.AlreadyOwned, Buy(content, state, "root"));
            Expect.Equal(10, state.Gold);

            Expect.Equal(PurchaseResult.NotEnoughGold, Buy(content, state, "child"));
            Expect.Equal(10, state.Gold);
            Expect.True(!state.Owns("child"), "Gold가 모자라면 사면 안 된다.");
            Expect.Equal(1, state.Upgrades.Count);
        }

        // 진행 중인 전투에 들어간 PlayerState는 살 수 없고, 다른 전투에도 들어갈 수 없다. 전투가 끝나면 풀린다.
        private static void CannotPurchaseDuringBattle()
        {
            GameContent content = TestContent.Load(Market(coins: 2, gold: 10));
            var state = new PlayerState(TestContent.First);
            EarnInBattle(content, state);

            GameSession battle = SessionAssembler.CreateBattle(content, new[] { state });
            Expect.True(state.InBattle, "전투에 들어가 있어야 한다.");
            Expect.Equal(PurchaseResult.InBattle, Buy(content, state, "root"));
            Expect.Equal(20, state.Gold);
            Expect.Throws<InvalidOperationException>(() => SessionAssembler.CreateBattle(content, new[] { state }));

            battle.Stop();
            Expect.True(!state.InBattle, "전투가 끝나면 풀려야 한다.");
            Expect.Equal(PurchaseResult.Purchased, Buy(content, state, "root"));
        }

        // 다음 전투: Gold·구매는 이어지고, 전투의 실행 상태(World, 적, HQ 성장, 조준점, Skill 타이머, 시간)는 새로 만든다.
        // 끝난 전투의 결과는 뒤의 전투가 바꾸지 않는다.
        private static void NextBattleKeepsProgressAndRenewsBattle()
        {
            ContentData data = Market(coins: 2, gold: 10);
            data.Enemies[0].HqExp = 1;
            data.Growth.Levels.Add(TestContent.Level(1));
            GameContent content = TestContent.Load(data);
            var state = new PlayerState(TestContent.First);

            GameSession first = SessionAssembler.CreateBattle(content, new[] { state });
            first.SetAimPoint(TestContent.First, East);
            first.Advance(0.1f);
            first.Stop();
            SessionResult result = first.Result;
            Expect.Equal(2, first.World.Hq.Level);
            Buy(content, state, "root");

            GameSession next = SessionAssembler.CreateBattle(content, new[] { state });
            Player player = next.World.Players[0];
            Expect.True(ReferenceEquals(state, player.State), "진행 상태는 이어받아야 한다.");
            Expect.Equal(10, player.State.Gold);
            Expect.True(player.State.Owns("root"), "구매는 이어져야 한다.");

            Expect.True(!ReferenceEquals(first.World, next.World), "World를 재사용하면 안 된다.");
            Expect.Equal(HqGrowthDefinition.StartLevel, next.World.Hq.Level);
            Expect.Equal(0, next.World.Hq.Exp);
            Expect.Equal(2, next.World.Enemies.Count);
            Expect.True(player.AimPoint == null, "조준점은 새 전투에서 비어 있어야 한다.");
            Expect.Equal(0, TestContent.Breaker(player).TickCount);
            Expect.Near(0, next.Elapsed);

            next.SetAimPoint(TestContent.First, East);
            next.Advance(1);
            Expect.True(ReferenceEquals(result, first.Result), "끝난 전투의 결과를 다시 만들면 안 된다.");
            Expect.Near(0.1f, first.Result.PlayedSeconds);
        }

        // 산 효과가 다음 전투에서 실제로 일어난다. 기본 정의는 그대로다.
        private static void PurchasedEffectsChangeNextBattle()
        {
            ContentData data = Market(coins: 1, gold: 101);
            data.Enemies[0].HqExp = 3;
            data.Growth.Levels.Add(TestContent.Level(1, 0, TestContent.Supply("coin", 1)));
            data.Upgrades = new List<UpgradeData>
            {
                TestContent.Upgrade("dmg", 10, null, TestContent.Effect("SkillDamageAdd", 2, TestContent.SkillId)),
                TestContent.Upgrade("rad", 10, "dmg", TestContent.Effect("SkillRadiusAdd", 0.5f, TestContent.SkillId)),
                TestContent.Upgrade("spd", 10, "dmg", TestContent.Effect("SkillIntervalMultiply", 0.5f, TestContent.SkillId)),
                TestContent.Upgrade("hp", 10, "dmg", TestContent.Effect("EnemyHealthMultiply", 3)),
                TestContent.Upgrade("gold", 10, "dmg", TestContent.Effect("GoldMultiply", 1.5f, "coin")),
                TestContent.Upgrade("exp", 10, "dmg", TestContent.Effect("HqExpMultiply", 2)),
                TestContent.Upgrade("supply", 10, "dmg", TestContent.Effect("GrowthSupplyAdd", 2, "coin"))
            };
            GameContent content = TestContent.Load(data);
            var state = new PlayerState(TestContent.First);
            EarnInBattle(content, state);

            foreach (UpgradeNodeDefinition node in content.Upgrades)
                Expect.Equal(PurchaseResult.Purchased, UpgradePurchase.TryPurchase(state, node));

            Expect.Equal(31, state.Gold);

            GameSession game = SessionAssembler.CreateBattle(content, new[] { state });
            BreakerStats skill = TestContent.Breaker(game.World.Players[0]).Stats;
            Expect.Near(3, skill.Damage);
            Expect.Near(1.5f, skill.Radius);
            Expect.Near(0.25f, skill.Interval);

            Enemy coin = game.World.Enemies[0];
            Expect.Near(3, coin.Stats.MaxHealth);
            Expect.Equal(152, coin.Reward.Gold);
            Expect.Equal(6, coin.Reward.HqExp);

            // 0초 틱(피해 3)에 처치 → Gold +152, HQ EXP +6 → Level 2 → 성장 공급 coin 1 + 구매 2.
            game.SetAimPoint(TestContent.First, East);
            game.Advance(0.01f);
            Expect.Equal(183, state.Gold);
            Expect.Equal(6, game.World.Hq.Exp);
            Expect.Equal(3, game.World.Enemies.Count);

            content.TryGetSkill(TestContent.SkillId, out PassiveSkillDefinition skillDefinition);
            content.TryGetEnemy("coin", out EnemyDefinition coinDefinition);
            Expect.Near(1, ((BreakerSkillDefinition)skillDefinition).BaseStats.Damage);
            Expect.Near(1, coinDefinition.BaseStats.MaxHealth);
            Expect.Equal(101, coinDefinition.Gold);
        }

        // Skill은 각자 자기 특성을 가진다. Skill 효과는 대상 Skill에만 붙는다.
        private static void SkillEffectsTargetTheirSkill()
        {
            ContentData data = Market(coins: 1, gold: 10);
            data.Skills.Add(TestContent.Skill("second", 1, 0.5f, 1));
            data.StartingSkills.Add("second");
            data.Upgrades = new List<UpgradeData>
            {
                TestContent.Upgrade("second-damage", 10, null, TestContent.Effect("SkillDamageAdd", 5, "second"))
            };
            GameContent content = TestContent.Load(data);
            var state = new PlayerState(TestContent.First);
            EarnInBattle(content, state);
            Buy(content, state, "second-damage");

            GameSession game = SessionAssembler.CreateBattle(content, new[] { state });
            Player player = game.World.Players[0];
            Expect.Near(1, TestContent.Breaker(player, 0).Stats.Damage);
            Expect.Near(6, TestContent.Breaker(player, 1).Stats.Damage);
        }

        // 지금 있는 Skill 수치 효과는 Breaker의 반경·주기·피해만 바꾼다. 레이저를 대상으로 하면
        // 사도 아무 일이 없는 노드가 되므로 콘텐츠 오류로 보고한다(CA-002 W2 흉내가 더는 통과하지 않는다).
        private static void SkillStatEffectsTargetOnlyBreaker()
        {
            ContentData data = Market(coins: 1, gold: 10);
            data.Skills.Add(TestContent.Laser(TestContent.LaserId, interval: 1, damage: 1, width: 0.2f, telegraph: 0.5f));
            data.Upgrades.Add(TestContent.Upgrade("laser-width", 10, null, TestContent.Effect("SkillRadiusAdd", 0.2f, TestContent.LaserId)));
            data.Upgrades.Add(TestContent.Upgrade("laser-damage", 10, null, TestContent.Effect("SkillDamageAdd", 1, TestContent.LaserId)));
            ContentLoadResult result = ContentLoader.Load(data);
            Expect.Equal(2, result.Diagnostics.Count);
            TestContent.HasDiagnostic(result, "Upgrades[laser-width].Effects[0]", "Breaker");
            TestContent.HasDiagnostic(result, "Upgrades[laser-damage].Effects[0]", "Breaker");
        }

        // 새 진행은 빈 PlayerState로 시작한다. 이어 받는 다음 전투와 구분된다.
        private static void NewProgressionStartsEmpty()
        {
            GameContent content = TestContent.Load(Market(coins: 2, gold: 10));
            var state = new PlayerState(TestContent.First);
            EarnInBattle(content, state);
            Buy(content, state, "root");

            GameSession fresh = SessionAssembler.Create(content, new[] { TestContent.First });
            PlayerState freshState = fresh.World.Players[0].State;
            Expect.True(!ReferenceEquals(state, freshState), "새 진행은 새 PlayerState여야 한다.");
            Expect.Equal(0, freshState.Gold);
            Expect.Equal(0, freshState.Upgrades.Count);
            Expect.Equal(10, state.Gold);
            Expect.True(state.Owns("root"), "기존 진행은 그대로여야 한다.");
        }

        // 두 Player의 진행 상태는 서로 영향을 주지 않는다. Skill 효과는 산 Player의 Skill에만 붙는다.
        private static void PlayerStatesAreIndependent()
        {
            GameContent content = TestContent.Load(Market(coins: 2, gold: 10));
            var first = new PlayerState(TestContent.First);
            var second = new PlayerState(TestContent.Second);
            EarnInBattle(content, first);
            Buy(content, first, "root");
            Expect.Equal(0, second.Gold);
            Expect.Equal(0, second.Upgrades.Count);

            // 보상이 없는 같은 모양의 콘텐츠로 2인 전투를 만든다. 구매는 노드 ID로 이어진다.
            ContentData unrewarded = Market(coins: 2, gold: 0);
            GameSession pair = SessionAssembler.CreateBattle(TestContent.Load(unrewarded), new[] { first, second });
            pair.World.TryGetPlayer(TestContent.First, out Player a);
            pair.World.TryGetPlayer(TestContent.Second, out Player b);
            Expect.Near(2, TestContent.Breaker(a).Stats.Damage);
            Expect.Near(1, TestContent.Breaker(b).Stats.Damage);
        }

        // 적·보상·공급은 모든 Player가 공유한다. 그런 구매가 있는 다인 전투는 합성 정책이 없어 조립되지 않는다.
        // 조립이 실패하면 PlayerState는 전투에 묶이지 않는다.
        private static void SharedWorldPurchasesInMultiplayerHaveNoPolicy()
        {
            ContentData data = Market(coins: 2, gold: 10);
            data.Upgrades.Add(TestContent.Upgrade("tough", 10, null, TestContent.Effect("EnemyHealthMultiply", 2)));
            GameContent content = TestContent.Load(data);
            var first = new PlayerState(TestContent.First);
            var second = new PlayerState(TestContent.Second);
            EarnInBattle(content, first);
            Buy(content, first, "tough");

            ContentData unrewarded = Market(coins: 2, gold: 0);
            unrewarded.Upgrades.Add(TestContent.Upgrade("tough", 10, null, TestContent.Effect("EnemyHealthMultiply", 2)));
            GameContent shared = TestContent.Load(unrewarded);
            Expect.Throws<InvalidOperationException>(() => SessionAssembler.CreateBattle(shared, new[] { first, second }));
            Expect.True(!first.InBattle && !second.InBattle, "실패한 조립이 PlayerState를 묶으면 안 된다.");

            // 혼자라면 같은 구매로 전투가 조립된다.
            GameSession single = SessionAssembler.CreateBattle(shared, new[] { first });
            Expect.Near(2, single.World.Enemies[0].Stats.MaxHealth);
        }

        // 구매는 노드 ID로 기록된다. 이어 받을 콘텐츠에 그 노드가 없으면 조립 오류다.
        private static void PurchasedNodeMustExistInContent()
        {
            GameContent content = TestContent.Load(Market(coins: 2, gold: 10));
            var state = new PlayerState(TestContent.First);
            EarnInBattle(content, state);
            Buy(content, state, "root");

            ContentData withoutUpgrades = Market(coins: 2, gold: 10);
            withoutUpgrades.Upgrades.Clear();
            GameContent other = TestContent.Load(withoutUpgrades);
            Expect.Throws<InvalidOperationException>(() => SessionAssembler.CreateBattle(other, new[] { state }));
            Expect.True(!state.InBattle, "실패한 조립이 PlayerState를 묶으면 안 된다.");
        }

        // 판 시작에 (3, 0)에 모여 있는 거의 움직이지 않는 적(HP 1). 조준점 (3, 0)이면 0초 틱에 모두 죽는다.
        // 노드: root(10G, 피해 +1), child(15G, root 다음, 반경 +1).
        private static ContentData Market(int coins, int gold)
        {
            ContentData data = TestContent.Data();
            EnemyData coin = TestContent.Enemy("coin", 1, 0.0001f, 0.3f);
            coin.Gold = gold;
            data.Enemies = new List<EnemyData> { coin };
            data.Spawn = new SpawnData { Distance = 3, AngleStep = 0 };
            data.StartSupply = new List<SupplyData> { TestContent.Supply("coin", coins) };
            data.Upgrades = new List<UpgradeData>
            {
                TestContent.Upgrade("root", 10, null, TestContent.Effect("SkillDamageAdd", 1, TestContent.SkillId)),
                TestContent.Upgrade("child", 15, "root", TestContent.Effect("SkillRadiusAdd", 1, TestContent.SkillId))
            };
            return data;
        }

        // 한 전투를 치러 Gold를 번다: 0초 틱에 시작 배치를 모두 처치하고 전투를 끝낸다.
        private static void EarnInBattle(GameContent content, PlayerState state)
        {
            GameSession battle = SessionAssembler.CreateBattle(content, new[] { state });
            battle.SetAimPoint(state.Id, East);
            battle.Advance(0.1f);
            battle.Stop();
        }

        private static PurchaseResult Buy(GameContent content, PlayerState state, string id)
        {
            Expect.True(content.TryGetUpgrade(id, out UpgradeNodeDefinition node), "노드가 있어야 한다: " + id);
            return UpgradePurchase.TryPurchase(state, node);
        }
    }
}
