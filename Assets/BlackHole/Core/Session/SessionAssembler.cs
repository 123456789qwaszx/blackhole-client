using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 검증된 콘텐츠와 참가 Player 목록으로 한 판을 새로 조립하는 유일한 진입점.
    // 정의는 공유하고, 실행 상태(시간, Player, Enemy, 출현 진행)는 판마다 새로 만든다.
    // 누가 참가하는지는 콘텐츠가 아니라 판 설정이다 — 호스트가 넘긴다(지금은 로컬 1명).
    public static class SessionAssembler
    {
        public static GameSession Create(GameContent content, IReadOnlyList<PlayerId> participants) =>
            Create(content, participants, EnemyBehaviors.Standard);

        // 행동 해석을 바꿔 끼우는 자리(D3). 게임은 위의 Standard 경로를 쓴다.
        // 계약 테스트는 여기에 Fake 해석기를 넣어, Enemy·출현·Session이 행동 구현에 기대지 않음을 확인한다.
        public static GameSession Create(GameContent content, IReadOnlyList<PlayerId> participants,
            EnemyBehaviorResolver behaviors)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            if (behaviors == null) throw new ArgumentNullException(nameof(behaviors));
            List<Player> players = CreatePlayers(participants);

            var spawner = new EnemySpawner(content.Spawn, content.SpawnOrder, behaviors);
            var world = new World(new Hq(content.Hq), players, spawner);
            var timeLimit = new TimeLimitRule(content.TimeLimit);
            return new GameSession(world, timeLimit);
        }

        private static List<Player> CreatePlayers(IReadOnlyList<PlayerId> participants)
        {
            if (participants == null || participants.Count == 0)
                throw new ArgumentException("참가 Player가 한 명 이상 필요하다.", nameof(participants));

            var ids = new HashSet<PlayerId>();
            var players = new List<Player>(participants.Count);
            foreach (PlayerId id in participants)
            {
                if (!ids.Add(id))
                    throw new ArgumentException($"{id}가 두 번 참가했다.", nameof(participants));
                players.Add(new Player(id));
            }
            return players;
        }
    }
}
