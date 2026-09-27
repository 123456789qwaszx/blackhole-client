using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 전투 조립에 필요한 검증된 공유 정의 묶음. 읽기 전용이며 여러 판이 함께 쓴다.
    //
    // 생성자 보장(구현 = ContentInvariants와 아래 검사):
    // [1] 적 종류 ID가 유일하다. 변환 대상·부모 종류가 콘텐츠에 있고, 부모는 특수 종류가 아니며, 변환 사슬이 돌지 않는다.
    // [2] 전투 시작 공급이 있으면 출현 배치가 있다. 공급은 적을 정의 객체로 참조한다(ContentLoader가 ID를 해석하며 진단한다).
    // [3] 출현 배치가 있으면 전체 개체 수 상한이 1 이상이고, 전투 시작 공급이 그 안이다.
    // 업그레이드 노드는 이 묶음에 없다(NodeTree). 노드를 모두 산 경우의 검사는 UpgradeContentCheck가 한다.
    // 오류가 있는 콘텐츠의 경로별 보고는 ContentLoader가 맡는다.
    public sealed class GameContent
    {
        private readonly Dictionary<string, EnemyDefinition> _enemiesById;

        public TimeLimitDefinition TimeLimit { get; }
        // 스킬. 없으면 null이고 판에 그 스킬이 없다.
        public BreakerDefinition Breaker { get; }
        public LaserDefinition Laser { get; }
        public IReadOnlyList<EnemyDefinition> Enemies { get; }
        // 출현 위치. 공급이 없으면 null일 수 있다.
        public EnemyPlacementDefinition EnemyPlacement { get; }
        // 한 판에 동시에 살아 있을 수 있는 적의 전체 최대 수(성능 예산, SYSTEM_CATALOG S08). 넘는 생성 요청은 버린다.
        public int MaxAliveEnemies { get; }
        // 전투 시작 공급. 전투를 시작할 때 한 번 공급한다. 업그레이드가 더하는 공급 수는 판 조립이 더한다.
        public IReadOnlyList<SupplyRequest> StartSupply { get; }
        // 블랙홀 성장의 Level 표. 없으면 HqGrowthDefinition.None(블랙홀이 Level 1에 머문다).
        public HqGrowthDefinition Growth { get; }

        public GameContent(
            TimeLimitDefinition timeLimit,
            BreakerDefinition breaker,
            LaserDefinition laser,
            IReadOnlyList<EnemyDefinition> enemies,
            EnemyPlacementDefinition enemyPlacement,
            int maxAliveEnemies,
            IReadOnlyList<SupplyRequest> startSupply,
            HqGrowthDefinition growth = null)
        {
            Growth = growth ?? HqGrowthDefinition.None;
            TimeLimit = timeLimit ?? throw new ArgumentNullException(nameof(timeLimit));
            Breaker = breaker;
            Laser = laser;
            Enemies = Copy(enemies);
            EnemyPlacement = enemyPlacement;
            MaxAliveEnemies = maxAliveEnemies;
            StartSupply = Copy(startSupply);

            var diagnostics = new List<ContentDiagnostic>();
            ContentInvariants.CollectEnemies(Enemies, diagnostics, out _enemiesById);
            ContentInvariants.CheckKindLinks(Enemies, _enemiesById, diagnostics);

            if (StartSupply.Count > 0 && EnemyPlacement == null)
                diagnostics.Add(new ContentDiagnostic("EnemyPlacement", "공급이 있으면 출현 배치가 필요하다."));

            ContentInvariants.CheckMaxAlive(EnemyPlacement, MaxAliveEnemies, diagnostics);
            ContentInvariants.CheckStartSupplyFits(StartSupply, 0, MaxAliveEnemies, diagnostics);

            if (diagnostics.Count > 0)
                throw new ArgumentException(diagnostics[0].ToString());
        }

        public bool TryGetEnemy(string id, out EnemyDefinition enemy)
        {
            enemy = null;
            return id != null && _enemiesById.TryGetValue(id, out enemy);
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
