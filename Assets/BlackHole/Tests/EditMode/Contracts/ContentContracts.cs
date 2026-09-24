using System.Collections.Generic;
using BlackHole.Sample;

namespace BlackHole.Core.Tests
{
    // B9 Content: 오류는 경로와 함께 모두 모으고, 오류가 하나라도 있으면 판을 만들 콘텐츠를 내지 않는다.
    internal static class ContentContracts
    {
        public static IEnumerable<Contract> Cases()
        {
            yield return new Contract("Content.SampleLoads", SampleLoads);
            yield return new Contract("Content.ReportsEveryErrorWithPath", ReportsEveryErrorWithPath);
            yield return new Contract("Content.ReportsMissingSections", ReportsMissingSections);
            yield return new Contract("Content.ReportsEnemyAndSpawnErrorsWithPath", ReportsEnemyAndSpawnErrorsWithPath);
            yield return new Contract("Content.ReportsEnemyReferenceErrorsWithPath", ReportsEnemyReferenceErrorsWithPath);
            yield return new Contract("Content.ReportsSkillErrorsWithPath", ReportsSkillErrorsWithPath);
            yield return new Contract("Content.ReportsLaserErrorsWithPath", ReportsLaserErrorsWithPath);
            yield return new Contract("Content.ReportsSupplyAndGrowthErrorsWithPath", ReportsSupplyAndGrowthErrorsWithPath);
            yield return new Contract("Content.ReportsUpgradeErrorsWithPath", ReportsUpgradeErrorsWithPath);
            yield return new Contract("Content.ReportsDeathEffectErrorsWithPath", ReportsDeathEffectErrorsWithPath);
        }

        // 사망 효과의 오류. 종류 이름은 로더가, 수치는 효과 정의 생성자가 경로와 함께 보고한다.
        // 종류 이름이 비어 있으면 사망 효과가 없는 적이다.
        private static void ReportsDeathEffectErrorsWithPath()
        {
            ContentData values = TestContent.Data();
            values.Enemies.Add(WithEffect("bad-kind", new DeathEffectData { Kind = "Explode" }));
            values.Enemies.Add(WithEffect("no-chains", TestContent.ChainLightning(5, 2, 0)));
            values.Enemies.Add(WithEffect("no-damage", TestContent.ChainLightning(0, 2, 1)));
            values.Enemies.Add(WithEffect("no-range", TestContent.ChainLightning(5, -1, 1)));
            ContentLoadResult result = ContentLoader.Load(values);
            Expect.Equal(4, result.Diagnostics.Count);
            TestContent.HasDiagnostic(result, "Enemies[bad-kind].DeathEffect.Kind", "Explode");
            TestContent.HasDiagnostic(result, "Enemies[no-chains].DeathEffect", "chains");
            TestContent.HasDiagnostic(result, "Enemies[no-damage].DeathEffect", "damage");
            TestContent.HasDiagnostic(result, "Enemies[no-range].DeathEffect", "range");

            ContentData empty = TestContent.Data();
            empty.Enemies[0].DeathEffect = new DeathEffectData();
            GameContent content = TestContent.Load(empty);
            content.TryGetEnemy(TestContent.EnemyId, out EnemyDefinition enemy);
            Expect.True(enemy.DeathEffect == null, "종류 이름이 비어 있으면 사망 효과가 없어야 한다.");
        }

        private static EnemyData WithEffect(string id, DeathEffectData effect)
        {
            EnemyData enemy = TestContent.Enemy(id, 10, 1, 0.3f);
            enemy.DeathEffect = effect;
            return enemy;
        }

        // 업그레이드 노드의 오류. 노드와 효과의 값·종류·대상은 경로와 함께 모두 보고한다.
        // 노드 사이의 규칙(ID 유일, 선행 노드 실재, 순환)은 노드가 모두 올바를 때 본다.
        private static void ReportsUpgradeErrorsWithPath()
        {
            ContentData values = TestContent.Data();
            values.Upgrades.Add(TestContent.Upgrade("bad-price", 0, null, TestContent.Effect("GoldMultiply", 2)));
            values.Upgrades.Add(TestContent.Upgrade("bad-kind", 5, null, TestContent.Effect("Teleport", 1)));
            values.Upgrades.Add(TestContent.Upgrade("no-skill", 5, null, TestContent.Effect("SkillDamageAdd", 1, "ghost")));
            values.Upgrades.Add(TestContent.Upgrade("zero-gold", 5, null, TestContent.Effect("GoldMultiply", 0)));
            values.Upgrades.Add(TestContent.Upgrade("half-supply", 5, null, TestContent.Effect("GrowthSupplyAdd", 1.5f, TestContent.EnemyId)));
            values.Upgrades.Add(TestContent.Upgrade("empty", 5, null));
            ContentLoadResult result = ContentLoader.Load(values);
            Expect.Equal(6, result.Diagnostics.Count);
            TestContent.HasDiagnostic(result, "Upgrades[bad-price]", "price");
            TestContent.HasDiagnostic(result, "Upgrades[bad-kind].Effects[0].Kind", "Teleport");
            TestContent.HasDiagnostic(result, "Upgrades[no-skill].Effects[0].Target", "ghost");
            TestContent.HasDiagnostic(result, "Upgrades[zero-gold].Effects[0]", "value");
            TestContent.HasDiagnostic(result, "Upgrades[half-supply].Effects[0]", "value");
            TestContent.HasDiagnostic(result, "Upgrades[empty]", "효과");

            ContentData links = TestContent.Data();
            links.Upgrades.Add(TestContent.Upgrade("a", 5, "b", TestContent.Effect("GoldMultiply", 2)));
            links.Upgrades.Add(TestContent.Upgrade("b", 5, "a", TestContent.Effect("GoldMultiply", 2)));
            links.Upgrades.Add(TestContent.Upgrade("c", 5, "ghost", TestContent.Effect("GoldMultiply", 2)));
            links.Upgrades.Add(TestContent.Upgrade("c", 5, null, TestContent.Effect("GoldMultiply", 2)));
            result = ContentLoader.Load(links);
            Expect.Equal(4, result.Diagnostics.Count);
            TestContent.HasDiagnostic(result, "Upgrades[3]", "c");
            TestContent.HasDiagnostic(result, "Upgrades[2].Requires", "ghost");
            TestContent.HasDiagnostic(result, "Upgrades[0].Requires", "순환");
            TestContent.HasDiagnostic(result, "Upgrades[1].Requires", "순환");
        }

        // 전투 시작 배치와 성장 노드의 오류. 개별 값은 정의 생성자가, Enemy 참조는 로더가 경로와 함께 보고한다.
        private static void ReportsSupplyAndGrowthErrorsWithPath()
        {
            ContentData values = TestContent.Data();
            values.StartSupply[0].Count = 0;
            values.Growth.Levels.Add(TestContent.Level(0));
            values.Growth.Levels.Add(TestContent.Level(5, -1));
            values.Growth.Levels.Add(TestContent.Level(8, 0, TestContent.Supply("ghost", 1), TestContent.Supply(TestContent.EnemyId, 0)));
            ContentLoadResult result = ContentLoader.Load(values);
            Expect.Equal(5, result.Diagnostics.Count);
            TestContent.HasDiagnostic(result, "StartSupply[0]", "count");
            TestContent.HasDiagnostic(result, "Growth.Levels[0]", "exp");
            TestContent.HasDiagnostic(result, "Growth.Levels[1]", "extraTime");
            TestContent.HasDiagnostic(result, "Growth.Levels[2].Supply[0].Enemy", "ghost");
            TestContent.HasDiagnostic(result, "Growth.Levels[2].Supply[1]", "count");

            // 노드 사이의 규칙은 노드가 모두 올바를 때 성장 정의 전체에 보고된다.
            ContentData order = TestContent.Data();
            order.Growth.Levels.Add(TestContent.Level(5));
            order.Growth.Levels.Add(TestContent.Level(5));
            result = ContentLoader.Load(order);
            Expect.Equal(1, result.Diagnostics.Count);
            TestContent.HasDiagnostic(result, "Growth", "Levels[1]");
        }

        private static void ReportsSkillErrorsWithPath()
        {
            ContentData shape = TestContent.Data();
            shape.Skills[0].Radius = 0;
            shape.Skills.Add(TestContent.Skill("character-aura", 1, 1, 1));
            shape.Skills[1].Kind = "CharacterCenter";
            ContentLoadResult result = ContentLoader.Load(shape);
            Expect.Equal(2, result.Diagnostics.Count);
            TestContent.HasDiagnostic(result, "Skills[test-skill]", "radius");
            TestContent.HasDiagnostic(result, "Skills[character-aura].Kind", "CharacterCenter");

            ContentData references = TestContent.Data();
            references.Skills.Add(TestContent.Skill(TestContent.SkillId, 1, 1, 1));
            references.StartingSkills.Add("ghost");
            references.StartingSkills.Add(TestContent.SkillId);
            result = ContentLoader.Load(references);
            Expect.Equal(3, result.Diagnostics.Count);
            TestContent.HasDiagnostic(result, "Skills[1]", TestContent.SkillId);
            TestContent.HasDiagnostic(result, "StartingSkills[1]", "ghost");
            TestContent.HasDiagnostic(result, "StartingSkills[2]", "중복");
        }

        // 관통 레이저의 수치와 공간 값도 경로와 함께 보고한다. 올바른 레이저는 Breaker와 함께 로드된다.
        private static void ReportsLaserErrorsWithPath()
        {
            ContentData data = TestContent.Data();
            data.Skills.Add(TestContent.Laser("thin", interval: 1, damage: 1, width: 0, telegraph: 0.5f));
            data.Skills.Add(TestContent.Laser("instant", interval: 1, damage: 1, width: 0.2f, telegraph: 0));
            data.Skills.Add(TestContent.Laser("nowhere", interval: 1, damage: 1, width: 0.2f, telegraph: 0.5f, boundaryRadius: 0));
            ContentLoadResult result = ContentLoader.Load(data);
            Expect.Equal(3, result.Diagnostics.Count);
            TestContent.HasDiagnostic(result, "Skills[thin]", "width");
            TestContent.HasDiagnostic(result, "Skills[instant]", "telegraphDuration");
            TestContent.HasDiagnostic(result, "Skills[nowhere]", "boundaryRadius");

            ContentData valid = TestContent.Data();
            valid.Skills.Add(TestContent.Laser(TestContent.LaserId, interval: 1, damage: 1, width: 0.2f, telegraph: 0.5f));
            valid.StartingSkills.Add(TestContent.LaserId);
            GameSession game = TestContent.Session(valid);
            Player player = game.World.Players[0];
            Expect.Equal(2, player.Skills.Count);
            Expect.True(player.Skills[0] is BreakerSkill && player.Skills[1] is PiercingLaserSkill,
                "Skill은 시작 구성의 순서로 종류에 맞는 실행 상태가 되어야 한다.");
        }

        private static void ReportsEnemyAndSpawnErrorsWithPath()
        {
            ContentData data = TestContent.Data();
            data.Enemies[0].MaxHealth = 0;
            data.Enemies.Add(TestContent.Enemy("chaser", 5, 1, 0.3f));
            data.Enemies[1].Behavior.Kind = "Chase";
            data.Spawn.Distance = 0;

            ContentLoadResult result = ContentLoader.Load(data);
            Expect.True(!result.Succeeded, "Enemy·출현 정의 오류가 있으면 로드에 실패해야 한다.");
            Expect.Equal(3, result.Diagnostics.Count);
            TestContent.HasDiagnostic(result, "Enemies[test-enemy]", "maxHealth");
            TestContent.HasDiagnostic(result, "Enemies[chaser].Behavior.Kind", "Chase");
            TestContent.HasDiagnostic(result, "Spawn", "distance");
        }

        // 참조 규칙(ID 유일, 공급 대상의 실재)은 개별 정의가 모두 올바를 때 검사된다.
        private static void ReportsEnemyReferenceErrorsWithPath()
        {
            ContentData data = TestContent.Data();
            data.Enemies.Add(TestContent.Enemy(TestContent.EnemyId, 5, 1, 0.3f));
            data.StartSupply.Add(TestContent.Supply("ghost", 1));

            ContentLoadResult result = ContentLoader.Load(data);
            Expect.Equal(2, result.Diagnostics.Count);
            TestContent.HasDiagnostic(result, "Enemies[1]", TestContent.EnemyId);
            TestContent.HasDiagnostic(result, "StartSupply[1].Enemy", "ghost");
        }

        // 샘플 값 자체는 [임시]라서 검사하지 않는다. 샘플이 로드된다는 것만 본다.
        private static void SampleLoads()
        {
            ContentLoadResult result = ContentLoader.Load(SampleContent.Create());
            Expect.True(result.Succeeded, "샘플 콘텐츠가 로드되어야 한다: " + string.Join(" | ", result.Diagnostics));
        }

        private static void ReportsEveryErrorWithPath()
        {
            ContentData data = TestContent.Data(timeLimit: 0);
            data.Hq.X = float.NaN;

            ContentLoadResult result = ContentLoader.Load(data);
            Expect.True(!result.Succeeded && result.Content == null, "오류가 있으면 콘텐츠를 만들지 않는다.");
            Expect.Equal(2, result.Diagnostics.Count);
            TestContent.HasDiagnostic(result, "Session.TimeLimit", "duration");
            TestContent.HasDiagnostic(result, "Hq", "유한");
        }

        private static void ReportsMissingSections()
        {
            ContentLoadResult result = ContentLoader.Load(new ContentData());
            Expect.Equal(3, result.Diagnostics.Count);
            TestContent.HasDiagnostic(result, "Session", string.Empty);
            TestContent.HasDiagnostic(result, "Hq", string.Empty);
            TestContent.HasDiagnostic(result, "Spawn", string.Empty);

            Expect.True(!ContentLoader.Load(null).Succeeded, "null 데이터는 실패해야 한다.");
        }
    }
}
