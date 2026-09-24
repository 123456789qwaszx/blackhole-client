using System.Collections.Generic;

namespace BlackHole.Core
{
    // 한 판 안에 존재하는 것들: HQ와 참가 Player 목록. 한 단계 안의 처리 순서를 가진다.
    // 지금은 단계 안에서 할 일이 없다. M2부터 출현·Enemy 행동이, M3부터 Skill이 여기에 순서대로 들어온다.
    public sealed class World
    {
        private readonly List<Player> _players;

        public Hq Hq { get; }
        // Player는 목록이다. "첫 번째 Player" 같은 전역 가정 없이 Id로 찾는다.
        public IReadOnlyList<Player> Players { get; }

        internal World(Hq hq, List<Player> players)
        {
            Hq = hq;
            _players = players;
            Players = players.AsReadOnly();
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
        internal void Step(float delta)
        {
        }
    }
}
