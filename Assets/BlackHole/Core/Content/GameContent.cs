using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 전투 조립에 필요한 검증된 공유 정의 묶음. 읽기 전용이며 여러 판이 함께 쓴다.
    //
    // 생성자 보장(구현 = ContentInvariants와 아래 검사):
    // [1] 적 종류 ID가 유일하다.
    // [2] 전투 시작 공급이 있으면 출현 배치가 있다. 공급은 적을 정의 객체로 참조한다(ContentLoader가 ID를 해석하며 진단한다).
    // [3] 적 풀 ID가 유일하고, 풀의 적 종류는 이 콘텐츠의 종류다.
    // [4] 단계는 하나 이상이고 1부터 차례로 번호가 붙으며, 단계의 적 풀은 이 콘텐츠의 풀이다.
    // [5] 업그레이드 노드 ID가 유일하고, 선행 노드는 실재하며 순환하지 않는다. 질량 증가 노드는 질량 단계 표 안이다.
    //     Grant는 적을 정의 객체로 참조한다(ContentLoader가 ID를 해석하며 진단한다).
    // 오류가 있는 콘텐츠의 경로별 보고는 ContentLoader가 맡는다.
    public sealed class GameContent
    {
        private readonly Dictionary<string, EnemyDefinition> _enemiesById;
        private readonly Dictionary<string, EnemyPoolDefinition> _poolsById;
        private readonly Dictionary<string, UpgradeNodeDefinition> _upgradesById;

        public TimeLimitDefinition TimeLimit { get; }
        public IReadOnlyList<EnemyDefinition> Enemies { get; }
        // 출현 위치. 공급이 없으면 null일 수 있다.
        public EnemyPlacementDefinition EnemyPlacement { get; }
        // 전투 시작 공급. 전투를 시작할 때 한 번 공급한다.
        public IReadOnlyList<SupplyRequest> StartSupply { get; }
        public IReadOnlyList<EnemyPoolDefinition> EnemyPools { get; }
        // 진행도(적의 강도 단계) 표. Stages[i]가 (i + 1)단계다. HQ 성장 단계와 다르다.
        public IReadOnlyList<StageDefinition> Stages { get; }
        // 단계의 수. 단계는 1부터 이 수까지다.
        public int StageCount => Stages.Count;
        // 업그레이드 노드(콘텐츠 순서).
        public IReadOnlyList<UpgradeNodeDefinition> Upgrades { get; }

        public GameContent(
            TimeLimitDefinition timeLimit,
            IReadOnlyList<EnemyDefinition> enemies,
            EnemyPlacementDefinition enemyPlacement,
            IReadOnlyList<SupplyRequest> startSupply,
            IReadOnlyList<EnemyPoolDefinition> enemyPools,
            IReadOnlyList<StageDefinition> stages,
            IReadOnlyList<UpgradeNodeDefinition> upgrades)
        {
            TimeLimit = timeLimit ?? throw new ArgumentNullException(nameof(timeLimit));
            Enemies = Copy(enemies);
            EnemyPlacement = enemyPlacement;
            StartSupply = Copy(startSupply);
            EnemyPools = Copy(enemyPools);
            Stages = Copy(stages);
            Upgrades = Copy(upgrades);

            var diagnostics = new List<ContentDiagnostic>();
            ContentInvariants.CollectEnemies(Enemies, diagnostics, out _enemiesById);
            ContentInvariants.CollectPools(EnemyPools, diagnostics, out _poolsById);
            ContentInvariants.CollectUpgrades(Upgrades, diagnostics, out _upgradesById);
            VerifyGrants(diagnostics);

            if (StartSupply.Count > 0 && EnemyPlacement == null)
                diagnostics.Add(new ContentDiagnostic("EnemyPlacement", "공급이 있으면 출현 배치가 필요하다."));

            VerifyPools(diagnostics);
            VerifyStages(diagnostics);

            if (diagnostics.Count > 0)
                throw new ArgumentException(diagnostics[0].ToString());
        }

        public bool TryGetEnemy(string id, out EnemyDefinition enemy)
        {
            enemy = null;
            return id != null && _enemiesById.TryGetValue(id, out enemy);
        }

        public bool TryGetPool(string id, out EnemyPoolDefinition pool)
        {
            pool = null;
            return id != null && _poolsById.TryGetValue(id, out pool);
        }

        public bool TryGetUpgrade(string id, out UpgradeNodeDefinition upgrade)
        {
            upgrade = null;
            return id != null && _upgradesById.TryGetValue(id, out upgrade);
        }

        // Grant가 가리키는 적 종류는 이 콘텐츠의 종류다.
        private void VerifyGrants(ICollection<ContentDiagnostic> into)
        {
            foreach (UpgradeNodeDefinition node in Upgrades)
            {
                if (node == null)
                    continue;

                foreach (EnemyGrant grant in node.Grants)
                {
                    if (!_enemiesById.TryGetValue(grant.Enemy.Id, out EnemyDefinition known) || known != grant.Enemy)
                        into.Add(new ContentDiagnostic($"Upgrades[{node.Id}]", $"이 콘텐츠의 적 종류가 아니다: '{grant.Enemy.Id}'."));
                }
            }
        }

        // number단계(1 ~ StageCount).
        public StageDefinition GetStage(int number)
        {
            if (number < 1 || number > StageCount)
                throw new ArgumentOutOfRangeException(nameof(number), $"단계는 1부터 {StageCount}까지다. 받은 값: {number}.");

            return Stages[number - 1];
        }

        private void VerifyPools(ICollection<ContentDiagnostic> into)
        {
            for (int i = 0; i < EnemyPools.Count; i++)
            {
                EnemyPoolDefinition pool = EnemyPools[i];

                if (pool == null)
                    continue;

                foreach (EnemyPoolEntry entry in pool.Entries)
                {
                    if (!_enemiesById.TryGetValue(entry.Enemy.Id, out EnemyDefinition known) || known != entry.Enemy)
                        into.Add(new ContentDiagnostic($"EnemyPools[{pool.Id}]", $"이 콘텐츠의 적 종류가 아니다: '{entry.Enemy.Id}'."));
                }
            }
        }

        private void VerifyStages(ICollection<ContentDiagnostic> into)
        {
            if (Stages.Count == 0)
                into.Add(new ContentDiagnostic("Stages", "단계가 하나 이상 필요하다."));

            for (int i = 0; i < Stages.Count; i++)
            {
                StageDefinition stage = Stages[i];

                if (stage == null)
                    into.Add(new ContentDiagnostic($"Stages[{i}]", "단계 정의가 null이다."));
                else if (stage.Number != i + 1)
                    into.Add(new ContentDiagnostic($"Stages[{i}]", $"{i + 1}단계 자리에 {stage.Number}단계가 있다."));
                else if (!_poolsById.TryGetValue(stage.Pool.Id, out EnemyPoolDefinition known) || known != stage.Pool)
                    into.Add(new ContentDiagnostic($"Stages[{i}].Pool", $"이 콘텐츠의 적 풀이 아니다: '{stage.Pool.Id}'."));
            }
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
