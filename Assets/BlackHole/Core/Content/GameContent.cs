using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 전투 조립과 구매에 필요한 검증된 공유 정의 묶음. 읽기 전용이며 여러 판이 함께 쓴다.
    //
    // 생성자 보장(구현 = ContentInvariants):
    // 업그레이드 노드 ID가 유일하고, 선행 노드가 실재하며, 선행을 따라가면 시작 노드에 닿는다.
    // 오류가 있는 콘텐츠의 경로별 보고는 ContentLoader가 맡는다.
    public sealed class GameContent
    {
        private readonly Dictionary<string, UpgradeNodeDefinition> _upgradesById;

        public TimeLimitDefinition TimeLimit { get; }
        // 업그레이드 노드(콘텐츠 순서).
        public IReadOnlyList<UpgradeNodeDefinition> Upgrades { get; }

        public GameContent(
            TimeLimitDefinition timeLimit,
            IReadOnlyList<UpgradeNodeDefinition> upgrades)
        {
            TimeLimit = timeLimit ?? throw new ArgumentNullException(nameof(timeLimit));
            Upgrades = Copy(upgrades);

            var diagnostics = new List<ContentDiagnostic>();
            ContentInvariants.CollectUpgrades(Upgrades, diagnostics, out _upgradesById);

            if (diagnostics.Count > 0)
                throw new ArgumentException(diagnostics[0].ToString());
        }

        public bool TryGetUpgrade(string id, out UpgradeNodeDefinition upgrade)
        {
            upgrade = null;
            return id != null && _upgradesById.TryGetValue(id, out upgrade);
        }

        // 호출자가 원본 목록을 나중에 바꿔도 따라 바뀌지 않게 한다.
        private static IReadOnlyList<T> Copy<T>(IReadOnlyList<T> source)
        {
            if (source == null)
                return Array.Empty<T>();

            var copy = new T[source.Count];

            for (int i = 0; i < copy.Length; i++)
            {
                copy[i] = source[i];
            }

            return Array.AsReadOnly(copy);
        }
    }
}
