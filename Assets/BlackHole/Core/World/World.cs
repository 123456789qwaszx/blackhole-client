using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 한 판 안에 존재하는 것들과 한 단계의 처리 순서.
    // 지금 판 안에 있는 것은 적뿐이다. 다른 전투 시스템(Skill, 사망 효과, HQ 성장)은 붙을 때
    // Step의 정해진 자리(GAME_RULES 13절)에 들어간다.
    public sealed class World
    {
        private readonly EnemyRoster _enemies = new EnemyRoster();
        private readonly PoolFilter _filter;

        // 살아 있는 적. 죽은 적은 즉시 빠진다.
        public IReadOnlyList<Enemy> Enemies => _enemies.Alive;
        // 마지막 진행 동안 확정된 사망. 다음 진행이 시작될 때 비운다.
        public IReadOnlyList<DeathRecord> Deaths => _enemies.Deaths;
        // 이 판의 단계가 쓰는 적 풀. 공급된 적은 이 풀을 거쳐서만 나온다.
        public EnemyPoolDefinition Pool { get; }
        // 이 판의 난수. 판 조립 때 seed로 만든다.
        internal BattleRandom Random { get; }

        internal World(BattleRandom random, EnemyPoolDefinition pool)
        {
            Random = random;
            Pool = pool ?? throw new ArgumentNullException(nameof(pool));
            _filter = new PoolFilter(pool);
        }

        // 지금 살아 있는 이 종류의 적 수.
        public int CountAlive(EnemyDefinition kind) => _enemies.CountAlive(kind);

        // 전투 시작 공급. 판 조립 때(0초) 한 번, 요청 순서대로 한 마리씩 풀 여과 장치를 거쳐 배치 띠 안에 내보낸다.
        // 여과 장치가 거른 요청은 버린다. 성장 공급이 붙으면 단계 끝에 요청을 모아 내보내는 자리가 따로 생긴다.
        internal void PlaceStartingEnemies(IReadOnlyList<SupplyRequest> requests, EnemyPlacementDefinition placement)
        {
            foreach (SupplyRequest request in requests)
            {
                for (int i = 0; i < request.Count; i++)
                {
                    if (_filter.Allows(request.Enemy, _enemies))
                        _enemies.Spawn(request.Enemy, placement.Pick(Random));
                }
            }
        }

        internal void BeginAdvance() => _enemies.BeginAdvance();

        // 한 단계. 순서가 중요한 처리는 여기에 문장 순서대로 쓴다.
        // 1. Enemy Action: 살아 있는 적이 행동에 따라 움직인다.
        internal void Step(float delta)
        {
            _enemies.Move(delta);
        }

        // 적에게 피해를 주는 입구. 피해를 주는 쪽(Skill·사망 효과)은 모두 여기로 요청한다.
        // 죽은 적(또는 이미 목록에서 빠진 적)은 무시한다. true는 이번 피해로 처음 죽었다는 뜻이다.
        public bool DealDamage(Enemy enemy, Damage damage)
        {
            if (enemy == null)
                throw new ArgumentNullException(nameof(enemy));

            return _enemies.DealDamage(enemy, damage);
        }
    }
}
