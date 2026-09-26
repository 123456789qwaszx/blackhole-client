using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 한 판 안에 존재하는 것들과 한 단계의 처리 순서.
    // 지금 판 안에 있는 것은 적뿐이다. 다른 전투 시스템(Skill, 사망 효과, HQ 성장)은 붙을 때
    // Step의 정해진 자리(GAME_RULES 13절)에 들어간다.
    //
    // 적이 생기고 죽는 일은 요청으로 들어와 쌓이고, Step의 정해진 자리에서 요청 순서대로 처리된다.
    // - 파괴 요청(RequestDestroy) → 13절 3. Damage / Death 자리: 그 적의 사망을 확정한다(피해·HP 계산 없음).
    // - 생성 요청(RequestSpawn)  → 13절 7. Enemy Supply 자리: 풀 여과 장치를 거쳐 한 마리씩 생성한다.
    // 같은 Step에서 사망이 생성보다 먼저다. 그래서 죽어서 비운 자리(풀의 최대 수)에 같은 Step의 생성이 들어갈 수 있다.
    // 생성된 적은 다음 Step부터 움직이고 공격 대상이 된다. 처리되지 않은 요청은 판 정리가 버린다.
    public sealed class World
    {
        private readonly EnemyRoster _enemies = new EnemyRoster();
        private readonly PoolFilter _filter;
        private readonly EnemyStatTable _stats;
        private readonly EnemyPlacementDefinition _placement;
        private readonly List<SupplyRequest> _spawnRequests = new List<SupplyRequest>();
        private readonly List<Enemy> _destroyRequests = new List<Enemy>();

        // 살아 있는 적. 죽은 적은 즉시 빠진다.
        public IReadOnlyList<Enemy> Enemies => _enemies.Alive;
        // 마지막 진행 동안 확정된 사망. 다음 진행이 시작될 때 비운다.
        public IReadOnlyList<DeathRecord> Deaths => _enemies.Deaths;
        // 아직 처리되지 않은 생성 요청과 파괴 요청(들어온 순서).
        public IReadOnlyList<SupplyRequest> PendingSpawns { get; }
        public IReadOnlyList<Enemy> PendingDestroys { get; }
        // 이 판의 단계가 쓰는 적 풀. 생성 요청은 이 풀을 거쳐서만 적이 된다.
        public EnemyPoolDefinition Pool { get; }
        // 이 판의 난수. 판 조립 때 seed로 만든다.
        internal BattleRandom Random { get; }

        internal World(BattleRandom random, EnemyPoolDefinition pool, EnemyStatTable stats, EnemyPlacementDefinition placement)
        {
            Random = random;
            Pool = pool ?? throw new ArgumentNullException(nameof(pool));
            _stats = stats ?? throw new ArgumentNullException(nameof(stats));
            _placement = placement;
            _filter = new PoolFilter(pool);
            PendingSpawns = _spawnRequests.AsReadOnly();
            PendingDestroys = _destroyRequests.AsReadOnly();
        }

        // 지금 살아 있는 이 종류의 적 수.
        public int CountAlive(EnemyDefinition kind) => _enemies.CountAlive(kind);

        // 이 판에서 이 종류가 받는 수치. 판 조립 때 정해졌고 이 판 동안 바뀌지 않는다.
        public EnemyStats StatsOf(EnemyDefinition kind) => _stats.Of(kind);

        // 이 판에서 지금까지 확정된 처치 수(모든 종류).
        public int TotalKills
        {
            get
            {
                int total = 0;

                foreach (EnemyKillCount kill in _enemies.Kills())
                    total += kill.Count;

                return total;
            }
        }

        // 이 판에서 확정된 사망의 Gold 합계. 사망 순간에 늘어난다.
        // 진행 상태(Gold)에는 판이 끝난 뒤 결산(GameSession.Settle)이 한 번 더한다 — 전투 중에는 진행 상태를 바꾸지 않는다.
        public long EarnedGold => _enemies.EarnedGold;

        // 확정된 사망 중 아직 처리가 끝나지 않은 것이 있는가. 판을 정리하기 전에 이것이 false여야 한다.
        // 지금은 사망 처리(목록에서 빠짐·사망 기록·처치 수·Gold 합계)가 사망 확정 순간에 모두 끝나므로 늘 false다.
        // 처리되지 않은 파괴 요청은 아직 사망이 아니다 — 판이 끝나면 처리되지 않고 판 정리가 버린다.
        // 사망 효과 대기열이 붙으면 그것이 빌 때까지 true다.
        public bool HasPendingDeathProcessing => false;

        internal IReadOnlyList<EnemyKillCount> Kills() => _enemies.Kills();

        // 생성 요청: 이 종류를 몇 마리. 다음 공급 처리(Step의 Enemy Supply 자리) 때 처리된다.
        // 이 판의 종류가 아니거나, 콘텐츠에 출현 배치가 없으면 요청 때 거부한다.
        // 풀에 없거나 최대 수에 닿은 종류는 처리 때 풀 여과 장치가 거른다.
        public void RequestSpawn(SupplyRequest request)
        {
            _stats.Of(request.Enemy);

            if (_placement == null)
                throw new InvalidOperationException("이 판의 콘텐츠에는 출현 배치가 없어 적을 생성할 수 없다.");

            _spawnRequests.Add(request);
        }

        // 파괴 요청: 이 적의 사망을 확정하라. 다음 사망 처리(Step의 Damage / Death 자리) 때 처리된다.
        // 처리 때 이미 죽었거나 판에 없는 적의 요청은 아무것도 하지 않는다(같은 적의 두 번째 요청도 그렇다).
        public void RequestDestroy(Enemy enemy)
        {
            _destroyRequests.Add(enemy ?? throw new ArgumentNullException(nameof(enemy)));
        }

        // 판이 끝난 뒤 남은 적과 처리되지 않은 요청을 치운다. 처치가 아니다(사망 기록·처치 수 없음). 치운 적의 수를 돌려준다.
        internal int ClearRemainingEnemies()
        {
            _spawnRequests.Clear();
            _destroyRequests.Clear();
            return _enemies.ClearAlive();
        }

        // 공급 처리: 쌓인 생성 요청을 요청 순서대로, 한 마리씩 풀 여과 장치를 거쳐 배치 띠 안에 생성한다.
        // 거른 요청은 버린다 — 나중에 자리가 나도 다시 나오지 않는다. 전투 시작 공급은 Begin(0초)이 바로 부른다.
        internal void ProcessSpawnRequests()
        {
            foreach (SupplyRequest request in _spawnRequests)
            {
                for (int i = 0; i < request.Count; i++)
                {
                    if (_filter.Allows(request.Enemy, _enemies))
                        _enemies.Spawn(request.Enemy, _stats.Of(request.Enemy), _placement.Pick(Random));
                }
            }

            _spawnRequests.Clear();
        }

        internal void BeginAdvance() => _enemies.BeginAdvance();

        // 한 단계. 순서가 중요한 처리는 여기에 문장 순서대로 쓴다(GAME_RULES 13절의 번호).
        // 1. Enemy Action: 살아 있는 적이 행동에 따라 움직인다.
        // 3. Damage / Death: 쌓인 파괴 요청의 사망을 확정한다.
        // 7. Enemy Supply: 쌓인 생성 요청을 처리한다.
        internal void Step(float delta)
        {
            _enemies.Move(delta);
            ProcessDestroyRequests();
            ProcessSpawnRequests();
        }

        // 적에게 피해를 주는 입구. 피해를 주는 쪽(Skill·사망 효과)은 모두 여기로 요청한다.
        // 죽은 적(또는 이미 목록에서 빠진 적)은 무시한다. true는 이번 피해로 처음 죽었다는 뜻이다.
        public bool DealDamage(Enemy enemy, Damage damage)
        {
            if (enemy == null)
                throw new ArgumentNullException(nameof(enemy));

            return _enemies.DealDamage(enemy, damage);
        }

        private void ProcessDestroyRequests()
        {
            foreach (Enemy enemy in _destroyRequests)
                _enemies.Destroy(enemy);

            _destroyRequests.Clear();
        }
    }
}
