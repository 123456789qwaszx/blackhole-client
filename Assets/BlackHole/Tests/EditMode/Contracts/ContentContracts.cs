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
            yield return new Contract("Content.UnlockDefaultsComeFromTheKind", UnlockDefaultsComeFromTheKind);
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

        // 해금 기본값은 종류가 정한다: 잠긴 채 시작하지 않는 종류(기본)는 노드 없이 나오고, 잠긴 채 시작하는 종류는
        // 해금 수치(enemy.<id>.unlock)를 1 이상으로 올리는 노드를 사야 나온다.
        private static void UnlockDefaultsComeFromTheKind()
        {
            ContentData data = TestContent.Data();
            data.Enemies.Add(TestContent.Enemy("open"));
            EnemyData lockedData = TestContent.Enemy("locked");
            lockedData.StartsLocked = true;
            data.Enemies.Add(lockedData);
            GameContent content = TestContent.Load(data);
            content.TryGetEnemy("open", out EnemyDefinition open);
            content.TryGetEnemy("locked", out EnemyDefinition locked);
            Expect.True(!open.StartsLocked && locked.StartsLocked, "잠긴 채 시작하는가는 저작 데이터 그대로다.");

            var state = new PlayerState(TestContent.First);
            IReadOnlyDictionary<EnemyDefinition, EnemyComposition> plain = SessionAssembler.PreviewCompositions(content, state, null);
            Expect.True(plain[open].Unlocked, "기본 종류는 노드 없이 해금돼 있다.");
            Expect.True(!plain[locked].Unlocked, "잠긴 채 시작하는 종류는 노드 없이 잠겨 있다.");

            NodeTree tree = TestContent.Owned(state, new Upgrade(EnemyUpgradeStats.Unlock(locked.Id), UpgradeOperation.Add, 1));
            Expect.True(SessionAssembler.PreviewCompositions(content, state, tree)[locked].Unlocked, "해금 노드를 사면 해금된다.");
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
