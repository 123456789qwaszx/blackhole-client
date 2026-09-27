using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 노드 트리와 판 조립 콘텐츠를 함께 보는 로드 검사. 두 데이터는 따로 불러오고(NodeTreeLoader, ContentLoader) 서로를 모른다.
    // 노드를 모두 산 경우의 업그레이드 표로 판을 조립할 값을 계산해 본다. 여기서 실패하는 노드는 언젠가 전투 시작을 막는다.
    // - Breaker 수치(BreakerDefinition.Upgraded): 피해·공격 속도·반지름이 0보다 큰가, 치명타 확률이 음수가 아닌가.
    // - 적 종류마다 판 구성(EnemyComposition.From): 질량 단계가 표 안인가, 황금이 되지 않는 종류의 황금 비율을 올리지 않는가, 음수가 없는가.
    // - 공급 수 노드를 모두 산 전투 시작 공급이 전체 개체 수 상한 안인가(전투 시작에는 살아 있는 적이 없으므로 넘으면 매 판 버려진다).
    // 업그레이드 표는 더하기·비율·곱하기의 합성이라, 모두 산 경우가 늘 가장 큰 값은 아니다(1보다 작은 곱하기). 지금 노드에는 그런 것이 없다.
    public static class UpgradeContentCheck
    {
        public static IReadOnlyList<ContentDiagnostic> Check(GameContent content, NodeTree nodes)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            if (nodes == null)
                throw new ArgumentNullException(nameof(nodes));

            var diagnostics = new List<ContentDiagnostic>();
            var everything = new List<Upgrade>();

            foreach (NodeDefinition node in nodes.Nodes)
                everything.AddRange(node.Upgrades);

            var table = new UpgradeTable(everything);
            long extraSupply = 0;

            if (content.Breaker != null)
            {
                try
                {
                    content.Breaker.Upgraded(table);
                }
                catch (ArgumentException error)
                {
                    diagnostics.Add(new ContentDiagnostic("Nodes(모두 산 경우).Breaker", error.Message));
                }
            }

            foreach (EnemyDefinition kind in content.Enemies)
            {
                try
                {
                    EnemyComposition composition = EnemyComposition.From(kind, table);
                    extraSupply += composition.StartSupplyBonus;
                }
                catch (ArgumentException error)
                {
                    diagnostics.Add(new ContentDiagnostic($"Nodes(모두 산 경우).Enemies[{kind.Id}]", error.Message));
                }
            }

            ContentInvariants.CheckStartSupplyFits(content.StartSupply, extraSupply, content.MaxAliveEnemies, diagnostics);
            return diagnostics.AsReadOnly();
        }
    }
}
