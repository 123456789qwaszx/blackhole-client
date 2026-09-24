using System.Collections.Generic;

namespace BlackHole.Core
{
    // 한 판 안에 존재하는 것들: HQ, 참가 Player 목록, Enemy 목록. 한 단계 안의 처리 순서를 가진다.
    public sealed class World
    {
        private readonly List<Player> _players;
        private readonly List<Enemy> _enemies = new List<Enemy>();
        private readonly EnemySpawner _spawner;
        private int _nextEnemyId = 1;

        public Hq Hq { get; }
        // Player는 목록이다. "첫 번째 Player" 같은 전역 가정 없이 Id로 찾는다.
        public IReadOnlyList<Player> Players { get; }
        public IReadOnlyList<Enemy> Enemies { get; }

        internal World(Hq hq, List<Player> players, EnemySpawner spawner)
        {
            Hq = hq;
            _players = players;
            _spawner = spawner;
            Players = players.AsReadOnly();
            Enemies = _enemies.AsReadOnly();
        }

        public bool TryGetPlayer(PlayerId id, out Player player)
        {
            foreach (Player candidate in _players)
            {
                if (!candidate.Id.Equals(id)) continue;
                player = candidate;
                return true;
            }
            player = null;
            return false;
        }

        // 한 단계. 순서가 중요한 처리는 여기에 문장 순서대로 쓴다.
        // 1. 이동: 이미 있는 Enemy가 행동에 따라 움직인다.
        // 2. 출현: 새 Enemy가 나온다. 이번 단계에 나온 Enemy는 다음 단계부터 움직인다.
        internal void Step(float delta)
        {
            Point2 hq = Hq.Position;
            for (int i = 0; i < _enemies.Count; i++) _enemies[i].Move(delta, hq);
            _spawner.Advance(delta, this);
        }

        // 출현 요청을 받는다. 목록과 ID 발급은 World가 가진다.
        internal void AddEnemy(EnemyDefinition definition, EnemyStats stats, Point2 position, IEnemyBehavior behavior)
        {
            _enemies.Add(new Enemy(new EnemyId(_nextEnemyId++), definition, stats, position, behavior));
        }
    }
}
