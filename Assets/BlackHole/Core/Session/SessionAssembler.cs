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
            // 두 Player의 상태/조준점 테스트는 계속 가능하지만, 보상 있는 다인 전투는
            // 귀속 정책이 정해지기 전에 시작하지 않는다.
            if (players.Count != 1)
                foreach (EnemyDefinition enemy in content.Enemies)
                    if (enemy.Gold != 0 || enemy.HqExp != 0)
                        throw new InvalidOperationException("보상 있는 다인 전투의 귀속 정책이 없다.");
            foreach (Player player in players) GiveStartingSkills(player, content.StartingSkills);

            var spawner = new EnemySpawner(content.Spawn, content.SpawnOrder, behaviors);
            var world = new World(new Hq(content.Hq), players, spawner);
            var timeLimit = new TimeLimitRule(content.TimeLimit);
            return new GameSession(world, timeLimit);
        }

        // 모든 Player가 같은 시작 구성을 받는다(Character 1종, 고정 구성). Skill 획득 구조가 아니다.
        // 실행 수치는 여기서 한 번 계산한다. 보정의 출처가 미정이라 지금은 보정이 없다.
        private static void GiveStartingSkills(Player player, IReadOnlyList<PassiveSkillDefinition> startingSkills)
        {
            foreach (PassiveSkillDefinition definition in startingSkills)
            {
                PassiveSkillStats stats = PassiveSkillStatCalculator.Compute(definition, Array.Empty<IPassiveSkillModifier>());
                player.AddSkill(new PassiveSkill(definition, stats, player));
            }
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
