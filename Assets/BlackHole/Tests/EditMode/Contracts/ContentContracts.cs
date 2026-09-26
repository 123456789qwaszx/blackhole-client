using System;
using System.Collections.Generic;
using BlackHole.Sample;

namespace BlackHole.Core.Tests
{
    // Content: 오류는 경로와 함께 모두 모으고, 오류가 하나라도 있으면 판을 만들 콘텐츠를 내지 않는다.
    internal static class ContentContracts
    {
        public static IEnumerable<Contract> Cases()
        {
            yield return new Contract("Content.SampleLoads", SampleLoads);
            yield return new Contract("Content.ReportsEveryErrorWithPath", ReportsEveryErrorWithPath);
            yield return new Contract("Content.ReportsMissingSections", ReportsMissingSections);
            yield return new Contract("Content.ReportsUpgradeErrorsWithPath", ReportsUpgradeErrorsWithPath);
            yield return new Contract("Content.StartSupplyFitsTheEnemyCap", StartSupplyFitsTheEnemyCap);
            yield return new Contract("Content.ReportsEnemyErrorsWithPath", ReportsEnemyErrorsWithPath);
            yield return new Contract("Content.ReportsEnemyReferenceErrorsWithPath", ReportsEnemyReferenceErrorsWithPath);
            yield return new Contract("Content.SupplyNeedsPlacement", SupplyNeedsPlacement);
            yield return new Contract("Content.ReportsPoolAndStageErrorsWithPath", ReportsPoolAndStageErrorsWithPath);
            yield return new Contract("Content.StageTableNumbersStagesFromOne", StageTableNumbersStagesFromOne);
        }

        // 적 종류와 출현 배치의 오류. 행동 종류 이름은 로더가, 수치는 정의 생성자가 경로와 함께 보고한다.
        private static void ReportsEnemyErrorsWithPath()
        {
            ContentData data = TestContent.Data();
            EnemyData chaser = TestContent.Enemy("chaser");
            chaser.Behavior.Kind = "Chase";
            EnemyData still = TestContent.Enemy("still");
            still.Behavior = null;
            // 색 등급은 둘인데 질량 단계의 색 비율은 하나다.
            EnemyData lopsided = TestContent.Tiered("lopsided", 1, false, TestContent.Tier(10, 0.2f, 1), TestContent.Tier(20, 0.3f, 2));
            lopsided.MassLevels.Add(TestContent.MassLevel(1, 1, 1));
            data.Enemies.Add(TestContent.Enemy("fragile", health: 0));
            data.Enemies.Add(chaser);
            data.Enemies.Add(still);
            data.Enemies.Add(lopsided);
            data.EnemyPlacement = new EnemyPlacementData { MinDistance = 5, MaxDistance = 2 };

            ContentLoadResult result = ContentLoader.Load(data);
            Expect.True(!result.Succeeded, "적·배치 정의 오류가 있으면 로드에 실패해야 한다.");
            Expect.Equal(5, result.Diagnostics.Count);
            TestContent.HasDiagnostic(result, "Enemies[fragile].Tiers[0]", "maxHealth");
            TestContent.HasDiagnostic(result, "Enemies[lopsided]", "색 비율 수");
            TestContent.HasDiagnostic(result, "Enemies[chaser].Behavior.Kind", "Chase");
            TestContent.HasDiagnostic(result, "Enemies[still].Behavior", "데이터가 없다");
            TestContent.HasDiagnostic(result, "EnemyPlacement", "minDistance");
        }

        // 목록 규칙(ID 유일)과 공급의 참조는 개별 정의가 모두 올바를 때 검사된다.
        private static void ReportsEnemyReferenceErrorsWithPath()
        {
            ContentData data = TestContent.Arena(1, 3,
                TestContent.Supply(TestContent.EnemyId, 1),
                TestContent.Supply("ghost", 1),
                TestContent.Supply(TestContent.EnemyId, 0));
            data.Enemies.Add(TestContent.Enemy(TestContent.EnemyId));
            data.Enemies.Add(TestContent.Enemy(TestContent.EnemyId));
            int duplicate = data.Enemies.Count - 1;

            ContentLoadResult result = ContentLoader.Load(data);
            Expect.Equal(3, result.Diagnostics.Count);
            TestContent.HasDiagnostic(result, $"Enemies[{duplicate}]", TestContent.EnemyId);
            TestContent.HasDiagnostic(result, "StartSupply[1].Enemy", "ghost");
            TestContent.HasDiagnostic(result, "StartSupply[2]", "count");
        }

        // 적을 내보내려면 어디에 둘지가 있어야 한다. 공급이 없으면 배치 없이도 로드된다.
        private static void SupplyNeedsPlacement()
        {
            ContentData data = TestContent.Data();
            data.Enemies.Add(TestContent.Enemy(TestContent.EnemyId));
            TestContent.Load(data);

            data.StartSupply.Add(TestContent.Supply(TestContent.EnemyId, 1));
            ContentLoadResult result = ContentLoader.Load(data);
            Expect.Equal(1, result.Diagnostics.Count);
            TestContent.HasDiagnostic(result, "EnemyPlacement", "배치");
        }

        // 적 풀 하나의 오류는 적 종류가 올바를 때 보고한다: 없는 적, 1 미만의 최대 수, 빈 풀, 같은 적 두 번.
        // 풀 ID의 중복과 단계 표의 풀 참조는 풀이 모두 올바를 때 보고한다.
        private static void ReportsPoolAndStageErrorsWithPath()
        {
            ContentData pools = TestContent.Data();
            pools.EnemyPools.Add(TestContent.Pool("stray", "ghost"));
            var zero = new EnemyPoolData { Id = "zero" };
            zero.Entries.Add(TestContent.Entry(TestContent.PoolEnemyId, 0));
            pools.EnemyPools.Add(zero);
            pools.EnemyPools.Add(TestContent.Pool("empty"));
            pools.EnemyPools.Add(TestContent.Pool("twice", TestContent.PoolEnemyId, TestContent.PoolEnemyId));

            ContentLoadResult result = ContentLoader.Load(pools);
            Expect.Equal(4, result.Diagnostics.Count);
            TestContent.HasDiagnostic(result, "EnemyPools[stray].Entries[0].Enemy", "ghost");
            TestContent.HasDiagnostic(result, "EnemyPools[zero].Entries[0]", "maxAlive");
            TestContent.HasDiagnostic(result, "EnemyPools[empty]", "하나 이상");
            TestContent.HasDiagnostic(result, "EnemyPools[twice]", "두 번");

            ContentData stages = TestContent.Data();
            stages.EnemyPools.Add(TestContent.Pool("again", TestContent.PoolEnemyId));
            stages.EnemyPools.Add(TestContent.Pool("again", TestContent.PoolEnemyId));
            stages.Stages[3].Pool = "nowhere";

            result = ContentLoader.Load(stages);
            Expect.Equal(2, result.Diagnostics.Count);
            TestContent.HasDiagnostic(result, "EnemyPools[2]", "again");
            TestContent.HasDiagnostic(result, "Stages[3].Pool", "nowhere");
        }

        // 단계 표의 i번째 줄이 (i + 1)단계다. 줄 수가 단계의 수이고, 여러 단계가 같은 풀을 쓸 수 있다.
        private static void StageTableNumbersStagesFromOne()
        {
            ContentData data = TestContent.Data();
            data.Enemies.Add(TestContent.Enemy("big"));
            data.EnemyPools.Add(TestContent.Pool("late", "big", TestContent.PoolEnemyId));
            data.Stages.Clear();
            TestContent.AddStages(data, TestContent.PoolId, 2);
            TestContent.AddStages(data, "late", 1);

            GameContent content = TestContent.Load(data);
            Expect.Equal(3, content.StageCount);
            Expect.Equal(1, content.GetStage(1).Number);
            Expect.True(ReferenceEquals(content.GetStage(1).Pool, content.GetStage(2).Pool), "같은 풀을 가리켜야 한다.");

            EnemyPoolDefinition late = content.GetStage(3).Pool;
            Expect.Equal("late", late.Id);
            Expect.Equal(2, late.Entries.Count);
            Expect.Equal("big", late.Entries[0].Enemy.Id);
            Expect.Equal(TestContent.RoomyMax, late.Entries[0].MaxAlive);
            Expect.Equal(TestContent.PoolEnemyId, late.Entries[1].Enemy.Id);

            Expect.Throws<ArgumentOutOfRangeException>(() => content.GetStage(0));
            Expect.Throws<ArgumentOutOfRangeException>(() => content.GetStage(4));
        }

        // 샘플의 C# 부분(판 설정)은 [임시] 값이라 값 자체는 검사하지 않는다.
        // 적 종류와 단계 표는 Unity 에셋이 채우므로, 최소 단계 표를 붙여 C# 부분이 로드되는지만 본다.
        private static void SampleLoads()
        {
            ContentData data = SampleContent.Create();
            data.Enemies.Add(TestContent.Enemy(TestContent.PoolEnemyId));
            data.EnemyPools.Add(TestContent.Pool(TestContent.PoolId, TestContent.PoolEnemyId));
            TestContent.AddStages(data, TestContent.PoolId, 1);

            ContentLoadResult result = ContentLoader.Load(data);
            Expect.True(result.Succeeded, "샘플 콘텐츠가 로드되어야 한다: " + string.Join(" | ", result.Diagnostics));
        }

        // 노드와 Grant의 오류. 개별 노드·Grant는 적 종류를 해석하는 단계에서, 노드끼리의 규칙(선행 노드, 순환,
        // 질량 단계 범위)은 모든 노드가 올바를 때 보고한다.
        private static void ReportsUpgradeErrorsWithPath()
        {
            ContentData values = TestContent.Data();
            values.Enemies.Add(TestContent.Enemy("rock"));
            values.Upgrades.Add(TestContent.Upgrade("bad-price", 0, null));
            values.Upgrades.Add(TestContent.Upgrade("self", 5, "self"));
            values.Upgrades.Add(TestContent.Upgrade("haunt", 5, null, TestContent.Grant("ghost", "MassLevel", "Add", 1)));
            values.Upgrades.Add(TestContent.Upgrade("fast", 5, null, TestContent.Grant("rock", "Speed", "Add", 1)));
            values.Upgrades.Add(TestContent.Upgrade("set-mass", 5, null, TestContent.Grant("rock", "MassLevel", "Set", 1)));
            values.Upgrades.Add(TestContent.Upgrade("gild", 5, null, TestContent.Grant("rock", "GoldenRatio", "Set", 0.1f)));
            values.Upgrades.Add(TestContent.Upgrade("set-supply", 5, null, TestContent.Grant("rock", "StartSupply", "Set", 3)));

            ContentLoadResult result = ContentLoader.Load(values);
            Expect.Equal(7, result.Diagnostics.Count);
            TestContent.HasDiagnostic(result, "Upgrades[set-supply].Grants[0]", "시작 공급 수");
            TestContent.HasDiagnostic(result, "Upgrades[bad-price]", "price");
            TestContent.HasDiagnostic(result, "Upgrades[self]", "requires");
            TestContent.HasDiagnostic(result, "Upgrades[haunt].Grants[0].Enemy", "ghost");
            TestContent.HasDiagnostic(result, "Upgrades[fast].Grants[0].Stat", "Speed");
            TestContent.HasDiagnostic(result, "Upgrades[set-mass].Grants[0]", "질량 단계");
            TestContent.HasDiagnostic(result, "Upgrades[gild].Grants[0]", "황금");

            ContentData links = TestContent.Data();
            links.Enemies.Add(TestContent.Enemy("rock"));
            links.Upgrades.Add(TestContent.Upgrade("orphan", 5, "missing"));
            links.Upgrades.Add(TestContent.Upgrade("a", 5, "b"));
            links.Upgrades.Add(TestContent.Upgrade("b", 5, "a"));
            links.Upgrades.Add(TestContent.Upgrade("heavy", 5, null, TestContent.Grant("rock", "MassLevel", "Add", 1)));

            result = ContentLoader.Load(links);
            Expect.Equal(4, result.Diagnostics.Count);
            TestContent.HasDiagnostic(result, "Upgrades[0].Requires", "missing");
            TestContent.HasDiagnostic(result, "Upgrades[1].Requires", "순환");
            TestContent.HasDiagnostic(result, "Upgrades[2].Requires", "순환");
            TestContent.HasDiagnostic(result, "Upgrades", "질량 단계 표는 0까지");
        }

        // 출현 배치가 있으면 전체 개체 수 상한이 필요하다. 공급 수 노드를 모두 산 전투 시작 공급이 상한 안이어야 한다
        // (전투 시작에는 살아 있는 적이 없으므로, 넘으면 노드가 약속한 적이 매 판 시작부터 버려진다).
        private static void StartSupplyFitsTheEnemyCap()
        {
            ContentData uncapped = TestContent.Arena(1, 3, TestContent.Supply(TestContent.EnemyId, 1));
            uncapped.Enemies.Add(TestContent.Enemy(TestContent.EnemyId));
            uncapped.MaxAliveEnemies = 0;
            ContentLoadResult result = ContentLoader.Load(uncapped);
            Expect.Equal(1, result.Diagnostics.Count);
            TestContent.HasDiagnostic(result, "MaxAliveEnemies", "상한");

            ContentData crowded = TestContent.Arena(1, 3, TestContent.Supply(TestContent.EnemyId, 3));
            crowded.Enemies.Add(TestContent.Enemy(TestContent.EnemyId));
            crowded.MaxAliveEnemies = 5;
            crowded.Upgrades.Add(TestContent.Upgrade("more", 5, null, TestContent.Grant(TestContent.EnemyId, "StartSupply", "Add", 2)));
            TestContent.Load(crowded);

            crowded.Upgrades.Add(TestContent.Upgrade("even-more", 5, "more", TestContent.Grant(TestContent.EnemyId, "StartSupply", "Add", 1)));
            result = ContentLoader.Load(crowded);
            Expect.Equal(1, result.Diagnostics.Count);
            TestContent.HasDiagnostic(result, "MaxAliveEnemies", "6마리");
        }

        private static void ReportsEveryErrorWithPath()
        {
            ContentData data = TestContent.Data(timeLimit: 0);
            data.Enemies.Add(TestContent.Enemy("slow", speed: 0));

            ContentLoadResult result = ContentLoader.Load(data);
            Expect.True(!result.Succeeded && result.Content == null, "오류가 있으면 콘텐츠를 만들지 않는다.");
            Expect.Equal(2, result.Diagnostics.Count);
            TestContent.HasDiagnostic(result, "Session.TimeLimit", "duration");
            TestContent.HasDiagnostic(result, "Enemies[slow]", "moveSpeed");
        }

        // 판 설정이 없으면 개별 정의 단계에서 멈춘다. 단계 표가 비어 있으면 마지막 단계에서 보고한다.
        private static void ReportsMissingSections()
        {
            ContentLoadResult result = ContentLoader.Load(new ContentData());
            Expect.Equal(1, result.Diagnostics.Count);
            TestContent.HasDiagnostic(result, "Session", string.Empty);

            ContentData noStages = TestContent.Data();
            noStages.Stages.Clear();
            result = ContentLoader.Load(noStages);
            Expect.Equal(1, result.Diagnostics.Count);
            TestContent.HasDiagnostic(result, "Stages", "하나 이상");

            Expect.True(!ContentLoader.Load(null).Succeeded, "null 데이터는 실패해야 한다.");
        }
    }
}
