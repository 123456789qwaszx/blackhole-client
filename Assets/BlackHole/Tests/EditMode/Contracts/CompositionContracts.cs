using System;
using System.Collections.Generic;

namespace BlackHole.Core.Tests
{
    // 판 구성: 산 노드 → 업그레이드 표 → 적 종류의 판 구성(질량 단계·황금 비율·황금 배율·더할 공급 수, BATTLE_COMPOSITION_PLAN 4.7).
    // 노드 트리는 수치의 뜻을 모른다. 적 시스템이 공개한 수치 이름(EnemyUpgradeStats)을 판 조립이 읽는다.
    internal static class CompositionContracts
    {
        private const string Rock = "rock";
        private const string Pebble = "pebble";

        public static IEnumerable<Contract> Cases()
        {
            yield return new Contract("Composition.OwnedNodesBecomeTheEnemyComposition", OwnedNodesBecomeTheEnemyComposition);
            yield return new Contract("Composition.SupplyNodesAddToTheStartSupply", SupplyNodesAddToTheStartSupply);
            yield return new Contract("Composition.LoadCheckFindsNodesTheContentCannotTake", LoadCheckFindsNodesTheContentCannotTake);
        }

        // 산 노드의 업그레이드가 종류별 판 구성이 된다. 사지 않은 노드는 아무것도 바꾸지 않고, 산 순서와 결과는 무관하다.
        // 질량 단계는 더하고, 황금 비율은 더한 뒤(황금 소행성 추가) 곱하며(자릿수 올리기) 1을 넘지 않는다. 황금 배율은 종류의 기본값에서 시작한다.
        // 판 조립은 그 판 구성으로 적 수치 표를 만든다.
        private static void OwnedNodesBecomeTheEnemyComposition()
        {
            ContentData data = TestContent.Data();
            EnemyData rock = TestContent.Tiered(Rock, 1, false, TestContent.Tier(10, 0.2f, 1));

            rock.StageColors.Add(TestContent.StageColor(1, 1));

            for (int i = 0; i < 3; i++)
                rock.MassLevels.Add(TestContent.MassLevel(1, 1));

            rock.GoldenMultiplier = 50;
            data.Enemies.Add(rock);
            GameContent content = TestContent.Load(data);
            content.TryGetEnemy(Rock, out EnemyDefinition kind);

            NodeTree tree = Tree(
                Node("mass-1", true, EnemyUpgradeStats.MassLevel(Rock), UpgradeOperation.Add, 1, "mass-2", "golden"),
                Node("mass-2", false, EnemyUpgradeStats.MassLevel(Rock), UpgradeOperation.Add, 1),
                Node("golden", false, EnemyUpgradeStats.GoldenRatio(Rock), UpgradeOperation.Add, 0.01f, "digits-1", "rich"),
                Node("digits-1", false, EnemyUpgradeStats.GoldenRatio(Rock), UpgradeOperation.Multiply, 10, "digits-2"),
                Node("digits-2", false, EnemyUpgradeStats.GoldenRatio(Rock), UpgradeOperation.Multiply, 10),
                Node("rich", false, EnemyUpgradeStats.GoldenMultiplier(Rock), UpgradeOperation.Add, 4150));

            var fresh = new PlayerState(TestContent.First);
            EnemyComposition none = CompositionOf(content, tree, kind, fresh);
            Expect.Equal(0, none.MassLevel);
            Expect.Near(0, none.GoldenRatio);
            Expect.Near(50, none.GoldenMultiplier);

            var forward = new PlayerState(TestContent.First);
            var backward = new PlayerState(TestContent.Second);
            forward.EarnGold(100);
            backward.EarnGold(100);

            foreach (string node in new[] { "mass-1", "mass-2", "golden", "digits-1", "rich" })
                Expect.Equal(PurchaseResult.Purchased, NodePurchase.TryPurchase(forward, tree, node));

            foreach (string node in new[] { "mass-1", "golden", "rich", "digits-1", "mass-2" })
                Expect.Equal(PurchaseResult.Purchased, NodePurchase.TryPurchase(backward, tree, node));

            foreach (PlayerState state in new[] { forward, backward })
            {
                EnemyComposition bought = CompositionOf(content, tree, kind, state);
                Expect.Equal(2, bought.MassLevel);
                Expect.Near(0.1f, bought.GoldenRatio);
                Expect.Near(4200, bought.GoldenMultiplier);
            }

            NodePurchase.TryPurchase(forward, tree, "digits-2");
            Expect.Near(1, CompositionOf(content, tree, kind, forward).GoldenRatio);

            GameSession game = SessionAssembler.CreateBattle(content, backward, 0, tree);
            Expect.Equal(2, game.World.Stats.CompositionOf(kind).MassLevel);
            Expect.Equal(4200L, game.World.Stats.Of(kind, 0, golden: true).Gold);
        }

        // 산 공급 수 노드는 그 종류의 전투 시작 공급에 더해진다. 콘텐츠 공급에 없는 종류는 공급이 새로 붙는다.
        private static void SupplyNodesAddToTheStartSupply()
        {
            ContentData data = TestContent.Arena(1, 3, TestContent.Supply(Rock, 2));
            data.Enemies.Add(TestContent.Enemy(Rock));
            data.Enemies.Add(TestContent.Enemy(Pebble));
            GameContent content = TestContent.Load(data);
            content.TryGetEnemy(Rock, out EnemyDefinition rock);
            content.TryGetEnemy(Pebble, out EnemyDefinition pebble);
            NodeTree tree = Tree(
                Node("more-rocks", true, EnemyUpgradeStats.StartSupply(Rock), UpgradeOperation.Add, 3),
                Node("pebbles", true, EnemyUpgradeStats.StartSupply(Pebble), UpgradeOperation.Add, 2));
            var state = new PlayerState(TestContent.First);

            GameSession plain = TestContent.Begun(SessionAssembler.CreateBattle(content, state, 0, tree));
            Expect.Equal(2, plain.World.CountAlive(rock));
            Expect.Equal(0, plain.World.CountAlive(pebble));
            plain.RequestEnd();

            state.EarnGold(2);
            NodePurchase.TryPurchase(state, tree, "more-rocks");
            NodePurchase.TryPurchase(state, tree, "pebbles");

            GameSession supplied = TestContent.Begun(SessionAssembler.CreateBattle(content, state, 0, tree));
            Expect.Equal(5, supplied.World.CountAlive(rock));
            Expect.Equal(2, supplied.World.CountAlive(pebble));
        }

        // 노드를 모두 산 경우를 로드 때 계산해 본다: 질량 단계 표 밖, 황금이 되지 않는 종류의 황금 비율,
        // 전체 개체 수 상한을 넘는 전투 시작 공급, 음수인 성장 시간, 출현 배치 없는 공급 수 노드를 경로와 함께 보고한다. 받아들일 수 있는 트리는 진단이 없다.
        private static void LoadCheckFindsNodesTheContentCannotTake()
        {
            ContentData data = TestContent.Arena(1, 3, TestContent.Supply(Rock, 2));
            data.MaxAliveEnemies = 4;
            data.Enemies.Add(TestContent.Enemy(Rock));
            data.Enemies.Add(TestContent.Enemy(Pebble));
            GameContent content = TestContent.Load(data);

            NodeTree fine = Tree(Node("supply", true, EnemyUpgradeStats.StartSupply(Rock), UpgradeOperation.Add, 2));
            Expect.Equal(0, UpgradeContentCheck.Check(content, fine).Count);

            NodeTree broken = Tree(
                Node("heavy", true, EnemyUpgradeStats.MassLevel(Rock), UpgradeOperation.Add, 1),
                Node("golden-pebble", true, EnemyUpgradeStats.GoldenRatio(Pebble), UpgradeOperation.Add, 0.5f));
            IReadOnlyList<ContentDiagnostic> diagnostics = UpgradeContentCheck.Check(content, broken);
            Expect.Equal(2, diagnostics.Count);
            Has(diagnostics, "Enemies[rock]", "질량 단계");
            Has(diagnostics, "Enemies[pebble]", "황금");

            NodeTree crowd = Tree(Node("crowd", true, EnemyUpgradeStats.StartSupply(Rock), UpgradeOperation.Add, 3));
            diagnostics = UpgradeContentCheck.Check(content, crowd);
            Expect.Equal(1, diagnostics.Count);
            Has(diagnostics, "MaxAliveEnemies", "5마리");

            // 성장: Level업마다의 시간은 음수가 아니다. 성장 공급은 판 중에 나오므로 전체 상한과 합을 비교하지 않는다.
            NodeTree growth = Tree(
                Node("shrink", true, HqUpgradeStats.GrowthTime, UpgradeOperation.Add, -1),
                Node("swarm", true, EnemyUpgradeStats.GrowthSupply(Rock), UpgradeOperation.Add, 100));
            diagnostics = UpgradeContentCheck.Check(content, growth);
            Expect.Equal(1, diagnostics.Count);
            Has(diagnostics, "Hq", "시간");

            // 공급 수 노드가 있으면 출현 배치가 필요하다.
            ContentData bare = TestContent.Data();
            bare.Enemies.Add(TestContent.Enemy(Rock));
            NodeTree grow = Tree(Node("grow", true, EnemyUpgradeStats.GrowthSupply(Rock), UpgradeOperation.Add, 1));
            diagnostics = UpgradeContentCheck.Check(TestContent.Load(bare), grow);
            Expect.Equal(1, diagnostics.Count);
            Has(diagnostics, "EnemyPlacement", "출현 배치");
        }

        // 진행 상태로 판을 조립해 그 종류의 판 구성을 읽고, 판을 끝낸다.
        private static EnemyComposition CompositionOf(GameContent content, NodeTree tree, EnemyDefinition kind, PlayerState state)
        {
            GameSession game = SessionAssembler.CreateBattle(content, state, 0, tree);
            EnemyComposition composition = game.World.Stats.CompositionOf(kind);
            game.RequestEnd();
            return composition;
        }

        private static NodeData Node(string id, bool start, string stat, UpgradeOperation operation, float value, params string[] links) =>
            new NodeData
            {
                Id = id,
                Price = 1,
                Start = start,
                Links = new List<string>(links),
                Upgrades = { new UpgradeData { Stat = stat, Operation = operation, Value = value } },
            };

        private static NodeTree Tree(params NodeData[] nodes)
        {
            var data = new NodeTreeData();

            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i].X = i;
                data.Nodes.Add(nodes[i]);
            }

            NodeTreeLoadResult result = NodeTreeLoader.Load(data);
            Expect.True(result.Succeeded, result.Diagnostics.Count > 0 ? result.Diagnostics[0].ToString() : "노드 트리 로드 실패");
            return result.Tree;
        }

        private static void Has(IReadOnlyList<ContentDiagnostic> diagnostics, string pathPart, string reason)
        {
            foreach (ContentDiagnostic diagnostic in diagnostics)
                if (diagnostic.Path.Contains(pathPart) && diagnostic.Message.Contains(reason)) return;

            throw new InvalidOperationException($"진단 없음: {pathPart} ({reason}). 받은 진단: {string.Join(" | ", diagnostics)}");
        }
    }
}
