using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 전투 조립과 구매에 필요한 검증된 공유 정의 묶음. 읽기 전용이며 여러 판이 함께 쓴다.
    //
    // 생성자 보장(구현 = ContentInvariants와 아래 검사):
    // [1] 적 종류 ID가 유일하다.
    // [2] 전투 시작 공급이 있으면 출현 배치가 있다. 공급은 적을 정의 객체로 참조한다(ContentLoader가 ID를 해석하며 진단한다).
    // [3] 업그레이드 노드 ID가 유일하고, 선행 노드가 실재하며, 선행을 따라가면 시작 노드에 닿는다.
    // 오류가 있는 콘텐츠의 경로별 보고는 ContentLoader가 맡는다.
    public sealed class GameContent
    {
        private readonly Dictionary<string, EnemyDefinition> _enemiesById;
        private readonly Dictionary<string, UpgradeNodeDefinition> _upgradesById;

        public TimeLimitDefinition TimeLimit { get; }
        public IReadOnlyList<EnemyDefinition> Enemies { get; }
        // 출현 위치. 공급이 없으면 null일 수 있다.
        public EnemyPlacementDefinition EnemyPlacement { get; }
        // 전투 시작 공급. 판 조립 때 한 번 공급한다.
        public IReadOnlyList<SupplyRequest> StartSupply { get; }
        // 업그레이드 노드(콘텐츠 순서).
        public IReadOnlyList<UpgradeNodeDefinition> Upgrades { get; }

        public GameContent(
            TimeLimitDefinition timeLimit,
            IReadOnlyList<EnemyDefinition> enemies,
            EnemyPlacementDefinition enemyPlacement,
            IReadOnlyList<SupplyRequest> startSupply,
            IReadOnlyList<UpgradeNodeDefinition> upgrades)
        {
            TimeLimit = timeLimit ?? throw new ArgumentNullException(nameof(timeLimit));
            Enemies = Copy(enemies);
            EnemyPlacement = enemyPlacement;
            StartSupply = Copy(startSupply);
            Upgrades = Copy(upgrades);

            var diagnostics = new List<ContentDiagnostic>();
            ContentInvariants.CollectEnemies(Enemies, diagnostics, out _enemiesById);
            ContentInvariants.CollectUpgrades(Upgrades, diagnostics, out _upgradesById);

            if (StartSupply.Count > 0 && EnemyPlacement == null)
                diagnostics.Add(new ContentDiagnostic("EnemyPlacement", "공급이 있으면 출현 배치가 필요하다."));

            if (diagnostics.Count > 0)
                throw new ArgumentException(diagnostics[0].ToString());
        }

        public bool TryGetEnemy(string id, out EnemyDefinition enemy)
        {
            enemy = null;
            return id != null && _enemiesById.TryGetValue(id, out enemy);
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
