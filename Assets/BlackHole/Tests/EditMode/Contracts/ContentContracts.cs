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
            yield return new Contract("Content.StartSupplyFitsTheEnemyCap", StartSupplyFitsTheEnemyCap);
            yield return new Contract("Content.ReportsEnemyErrorsWithPath", ReportsEnemyErrorsWithPath);
            yield return new Contract("Content.ReportsEnemyReferenceErrorsWithPath", ReportsEnemyReferenceErrorsWithPath);
            yield return new Contract("Content.SupplyNeedsPlacement", SupplyNeedsPlacement);
            yield return new Contract("Content.ReportsGrowthErrorsWithPath", ReportsGrowthErrorsWithPath);
            yield return new Contract("Content.ReportsLevelErrorsWithPath", ReportsLevelErrorsWithPath);
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
            lopsided.LevelColors.Add(TestContent.LevelColor(1, 1));
            lopsided.MassLevels.Add(TestContent.MassLevel(1, 1));
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

        // 샘플의 C# 부분(판 설정)은 [임시] 값이라 값 자체는 검사하지 않는다.
        // 적 종류는 Unity 에셋이 채우므로, C# 부분이 로드되는지만 본다.
        private static void SampleLoads()
        {
            ContentData data = SampleContent.Create();

            ContentLoadResult result = ContentLoader.Load(data);
            Expect.True(result.Succeeded, "샘플 콘텐츠가 로드되어야 한다: " + string.Join(" | ", result.Diagnostics));
        }

        // 출현 배치가 있으면 전체 개체 수 상한이 필요하다. 전투 시작 공급이 상한 안이어야 한다
        // (전투 시작에는 살아 있는 적이 없으므로, 넘으면 약속한 적이 매 판 시작부터 버려진다).
        // 공급 수 노드를 모두 산 경우는 노드 트리와 함께 본다(Composition.LoadCheckFindsNodesTheContentCannotTake).
        private static void StartSupplyFitsTheEnemyCap()
        {
            ContentData uncapped = TestContent.Arena(1, 3, TestContent.Supply(TestContent.EnemyId, 1));
            uncapped.Enemies.Add(TestContent.Enemy(TestContent.EnemyId));
            uncapped.MaxAliveEnemies = 0;
            ContentLoadResult result = ContentLoader.Load(uncapped);
            Expect.Equal(1, result.Diagnostics.Count);
            TestContent.HasDiagnostic(result, "MaxAliveEnemies", "상한");

            ContentData crowded = TestContent.Arena(1, 3, TestContent.Supply(TestContent.EnemyId, 3), TestContent.Supply(TestContent.EnemyId, 2));
            crowded.Enemies.Add(TestContent.Enemy(TestContent.EnemyId));
            crowded.MaxAliveEnemies = 5;
            TestContent.Load(crowded);

            crowded.StartSupply.Add(TestContent.Supply(TestContent.EnemyId, 1));
            result = ContentLoader.Load(crowded);
            Expect.Equal(1, result.Diagnostics.Count);
            TestContent.HasDiagnostic(result, "MaxAliveEnemies", "6마리");
        }

        // 블랙홀 성장의 Level 표·이정표와 색 등급 EXP의 오류. Level 표는 누적 EXP라 양수이고 앞 줄보다 커야 한다.
        // 표가 없으면 블랙홀이 Level 1에 머문다.
        private static void ReportsGrowthErrorsWithPath()
        {
            ContentData data = TestContent.Data();
            EnemyData kind = TestContent.Enemy(TestContent.EnemyId);
            kind.Tiers[0].Exp = -1;
            data.Enemies.Add(kind);
            data.Growth = new HqGrowthData { LevelExp = new List<long> { 5, 5 } };

            ContentLoadResult result = ContentLoader.Load(data);
            Expect.Equal(2, result.Diagnostics.Count);
            TestContent.HasDiagnostic(result, $"Enemies[{TestContent.EnemyId}].Tiers[0]", "exp");
            TestContent.HasDiagnostic(result, "Growth.LevelExp", "앞 줄");

            data.Growth.LevelExp = new List<long> { 0 };
            kind.Tiers[0].Exp = 0;
            result = ContentLoader.Load(data);
            Expect.Equal(1, result.Diagnostics.Count);
            TestContent.HasDiagnostic(result, "Growth.LevelExp", "양수");

            // 이정표: 보상은 0 이상, Level은 Level 표 안(2 ~ MaxLevel)이고 앞 이정표보다 커야 한다.
            data.Growth = new HqGrowthData { LevelExp = new List<long> { 5, 10 } };
            data.Growth.Milestones.Add(new HqMilestoneData { Level = 2, Reward = -1 });
            result = ContentLoader.Load(data);
            Expect.Equal(1, result.Diagnostics.Count);
            TestContent.HasDiagnostic(result, "Growth.Milestones[0]", "reward");

            data.Growth.Milestones[0].Reward = 10;
            data.Growth.Milestones.Add(new HqMilestoneData { Level = 2, Reward = 10 });
            result = ContentLoader.Load(data);
            Expect.Equal(1, result.Diagnostics.Count);
            TestContent.HasDiagnostic(result, "Growth.Milestones", "앞 이정표");

            data.Growth.Milestones.RemoveAt(1);
            data.Growth.Milestones[0].Level = 4;
            result = ContentLoader.Load(data);
            TestContent.HasDiagnostic(result, "Growth.Milestones", "Level 표 안");

            data.Growth = null;
            Expect.Equal(HqGrowthDefinition.StartLevel, TestContent.Load(data).Growth.MaxLevel);
        }

        // Level별 색 비율과 종류 사이 연결의 오류: 시작 Level은 1 이상이고 앞 줄보다 커야 하며, 줄이 하나 이상 있어야 한다.
        // 색 등급과의 길이 맞춤은 ReportsEnemyErrorsWithPath가 본다.
        private static void ReportsLevelErrorsWithPath()
        {
            ContentData data = TestContent.Data();
            EnemyData zero = TestContent.Enemy("zero");
            zero.LevelColors[0].FromLevel = 0;
            EnemyData backward = TestContent.Enemy("backward");
            backward.LevelColors[0].FromLevel = 5;
            backward.LevelColors.Add(TestContent.LevelColor(3, 1));
            EnemyData colorless = TestContent.Enemy("colorless");
            colorless.LevelColors.Clear();
            data.Enemies.Add(zero);
            data.Enemies.Add(backward);
            data.Enemies.Add(colorless);

            ContentLoadResult result = ContentLoader.Load(data);
            Expect.Equal(3, result.Diagnostics.Count);
            TestContent.HasDiagnostic(result, "Enemies[zero].LevelColors[0]", "fromLevel");
            TestContent.HasDiagnostic(result, "Enemies[backward]", "앞 줄");
            TestContent.HasDiagnostic(result, "Enemies[colorless]", "하나 이상");

            // 종류 사이 연결: 변환 대상·부모는 콘텐츠에 있어야 하고, 부모는 특수 종류가 아니며, 변환 사슬은 돌지 않는다.
            ContentData links = TestContent.Data();
            EnemyData lost = TestContent.Enemy("lost");
            lost.UpgradesTo = "ghost";
            EnemyData parent = TestContent.Enemy("parent");
            EnemyData child = TestContent.Enemy("child");
            child.SpecialOf = "parent";
            EnemyData grandchild = TestContent.Enemy("grandchild");
            grandchild.SpecialOf = "child";
            EnemyData ping = TestContent.Enemy("ping");
            ping.UpgradesTo = "pong";
            EnemyData pong = TestContent.Enemy("pong");
            pong.UpgradesTo = "ping";
            links.Enemies.AddRange(new[] { lost, parent, child, grandchild, ping, pong });

            result = ContentLoader.Load(links);
            Expect.Equal(4, result.Diagnostics.Count);
            TestContent.HasDiagnostic(result, "Enemies[lost].UpgradesTo", "ghost");
            TestContent.HasDiagnostic(result, "Enemies[grandchild].SpecialOf", "특수 종류");
            TestContent.HasDiagnostic(result, "Enemies[ping].UpgradesTo", "다시 돈다");
            TestContent.HasDiagnostic(result, "Enemies[pong].UpgradesTo", "다시 돈다");
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

        // 판 설정이 없으면 개별 정의 단계에서 멈춘다.
        private static void ReportsMissingSections()
        {
            ContentLoadResult result = ContentLoader.Load(new ContentData());
            Expect.Equal(1, result.Diagnostics.Count);
            TestContent.HasDiagnostic(result, "Session", string.Empty);

            Expect.True(!ContentLoader.Load(null).Succeeded, "null 데이터는 실패해야 한다.");
        }
    }
}
