using System.Collections.Generic;

namespace BlackHole.Core
{
    // 한 판 안에 존재하는 것들: HQ, 참가 Player 목록, Enemy 목록. 한 단계 안의 처리 순서를 가진다.
    public sealed class World
    {
        private readonly List<Player> _players;
        private readonly List<Enemy> _enemies = new List<Enemy>();
        private readonly List<DeathRecord> _deaths = new List<DeathRecord>();
        private readonly List<DeathEffectHit> _deathEffectHits = new List<DeathEffectHit>();
        private readonly List<LaserFireRecord> _laserFires = new List<LaserFireRecord>();
        private readonly DeathEffects _deathEffects = new DeathEffects();
        private readonly EnemySupply _supply;
        private readonly GrowthProgression _growth;
        private int _nextEnemyId = 1;
        private long _nextDeathSequence = 1;
        private long _nextDeathEffectHitSequence = 1;
        private long _nextLaserFireSequence = 1;

        public Hq Hq { get; }
        // Player는 목록이다. "첫 번째 Player" 같은 전역 가정 없이 Id로 찾는다.
        public IReadOnlyList<Player> Players { get; }
        public IReadOnlyList<Enemy> Enemies { get; }
        // 마지막 Advance 동안의 모든 하위 단계 사망 기록. 다음 Advance 시작 때 비운다.
        public IReadOnlyList<DeathRecord> Deaths { get; }
        // 마지막 Advance 동안의 모든 사망 효과 적중 기록. Deaths와 같은 때 비운다.
        public IReadOnlyList<DeathEffectHit> DeathEffectHits { get; }
        // 마지막 Advance 동안의 모든 레이저 발사 기록. Deaths와 같은 때 비운다.
        public IReadOnlyList<LaserFireRecord> LaserFires { get; }
        // 이 판의 난수. 판 조립 때 seed로 만든다.
        internal BattleRandom Random { get; }

        internal World(
            Hq hq,
            List<Player> players,
            EnemySupply supply,
            GrowthProgression growth,
            BattleRandom random)
        {
            Hq = hq;
            _players = players;
            _supply = supply;
            _growth = growth;
            Random = random;
            Players = players.AsReadOnly();
            Enemies = _enemies.AsReadOnly();
            Deaths = _deaths.AsReadOnly();
            DeathEffectHits = _deathEffectHits.AsReadOnly();
            LaserFires = _laserFires.AsReadOnly();
        }

        public bool TryGetPlayer(PlayerId id, out Player player)
        {
            foreach (Player candidate in _players)
            {
                if (!candidate.Id.Equals(id))
                    continue;

                player = candidate;
                return true;
            }

            player = null;
            return false;
        }

        // 전투 시작 배치. 판 조립 때(0초) 한 번 공급한다. 그래서 0초 첫 틱이 이 적을 맞힐 수 있다.
        internal void PlaceStartingEnemies(IReadOnlyList<SupplyRequest> requests)
        {
            _supply.Request(requests, SupplySource.Start);
            _supply.Release(this);
        }

        // 한 단계. 순서가 중요한 처리는 여기에 문장 순서대로 쓴다.
        // 1. 이동: 이미 있는 Enemy가 행동에 따라 움직인다.
        // 2. Passive Skill: Player 목록 순서, 각 Player의 Skill 순서로 주기를 진행한다. 이동한 위치를 공격한다.
        //    죽은 적은 그 자리에서 보상·HQ EXP(Level 상승)·사망 기록을 남기고 목록에서 빠진다.
        //    사망 효과가 있으면 대기열에 들어간다.
        // 3. 사망 효과: 대기열을 순서대로 처리한다. 효과로 죽은 적도 2와 같은 사망 절차를 거친다.
        // 4. 성장 진행: 이번 단계에 새로 도달한 Level의 효과를 실행한다(공급 요청, 시간 연장).
        //    효과로 죽은 적의 EXP도 여기에 들어간다([임시] 순서).
        // 5. 공급: 요청된 Enemy가 나온다. 이번 단계에 나온 Enemy는 다음 단계부터 움직이고 맞는다.
        // 종료 판정은 이 단계가 끝난 뒤 SessionRunner가 한다. 그래서 늘어난 시간이 이번 단계의 판정에 들어간다.
        internal void Step(float delta)
        {
            Point2 hq = Hq.Position;

            for (int i = 0; i < _enemies.Count; i++)
            {
                _enemies[i].Move(delta, hq);
            }

            for (int p = 0; p < _players.Count; p++)
            {
                IReadOnlyList<PassiveSkill> skills = _players[p].Skills;

                for (int s = 0; s < skills.Count; s++)
                {
                    skills[s].Advance(delta, this);
                }
            }

            _deathEffects.Resolve(this);

            _growth.Advance(Hq, _supply);

            _supply.Release(this);
        }

        internal void BeginAdvance()
        {
            _deaths.Clear();
            _deathEffectHits.Clear();
            _laserFires.Clear();
        }

        // 사망은 피해를 수용한 Enemy가 판정한다. World는 보상·목록·사망 기록을 한 번 확정한다.
        // 사망 효과는 여기서 처리하지 않고 대기열에 넣는다(재귀 없음).
        internal void DealDamage(Enemy enemy, Damage damage)
        {
            if (!enemy.IsAlive || !enemy.ApplyDamage(damage))
                return;

            // 현재 실제 보상 구성은 단일 Player다. 조립 때 그 전제를 검증한다.
            if (_players.Count == 1)
                _players[0].State.EarnGold(enemy.Reward.Gold);

            Hq.GainExp(enemy.Reward.HqExp);

            _deaths.Add(new DeathRecord(_nextDeathSequence++, enemy));

            _enemies.Remove(enemy);

            if (enemy.Definition.DeathEffect != null)
                _deathEffects.Enqueue(enemy.Definition.DeathEffect, enemy.Position, damage.Source);
        }

        internal void RecordDeathEffectHit(
            DeathEffectDefinition effect,
            Point2 from,
            Point2 to,
            EnemyId target)
        {
            _deathEffectHits.Add(
                new DeathEffectHit(
                    _nextDeathEffectHitSequence++,
                    effect,
                    from,
                    to,
                    target));
        }

        internal void RecordLaserFire(
            PlayerId owner,
            PiercingLaserDefinition laser,
            LaserShot shot,
            float width,
            int hitCount)
        {
            _laserFires.Add(new LaserFireRecord(_nextLaserFireSequence++, owner, laser, shot, width, hitCount));
        }

        // 출현 요청을 받는다. 목록과 ID 발급은 World가 가진다.
        internal void AddEnemy(
            EnemyDefinition definition,
            EnemyStats stats,
            EnemyReward reward,
            Point2 position,
            IEnemyBehavior behavior)
        {
            _enemies.Add(
                new Enemy(
                    new EnemyId(_nextEnemyId++),
                    definition,
                    stats,
                    reward,
                    position,
                    behavior));
        }
    }
}
