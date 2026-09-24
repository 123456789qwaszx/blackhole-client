using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 검증된 콘텐츠와 참가 Player로 한 전투를 새로 조립하는 유일한 진입점.
    // 정의는 공유하고, 전투의 실행 상태(시간, Player, Enemy, 공급·출현, HQ 성장)는 전투마다 새로 만든다.
    // 누가 참가하는지는 콘텐츠가 아니라 판 설정이다 — 호스트가 넘긴다(지금은 로컬 1명).
    //
    // 두 가지 진입:
    // - Create: 새 진행. 참가자마다 빈 PlayerState(Gold 0, 구매 없음)로 시작한다.
    // - CreateBattle: 다음 전투. 기존 PlayerState(Gold, 구매)를 이어 받고, 구매 효과를 새 전투에 반영한다.
    //
    // seed는 이 전투의 난수(BattleRandom)를 정한다. 같은 콘텐츠·seed·입력·진행 시간이면 같은 결과가 나온다.
    // seed를 받지 않는 진입은 DefaultSeed를 쓴다. 게임 호스트는 전투마다 seed를 정해 넘긴다.
    public static class SessionAssembler
    {
        public const int DefaultSeed = 0;

        public static GameSession Create(
            GameContent content,
            IReadOnlyList<PlayerId> participants) =>
            Create(content, participants, EnemyBehaviors.Standard);

        // 행동 해석을 바꿔 끼우는 자리(D3). 게임은 위의 Standard 경로를 쓴다.
        // 계약 테스트는 여기에 Fake 해석기를 넣어, Enemy·출현·Session이 행동 구현에 기대지 않음을 확인한다.
        public static GameSession Create(
            GameContent content,
            IReadOnlyList<PlayerId> participants,
            EnemyBehaviorResolver behaviors) =>
            CreateBattle(content, NewProgress(participants), behaviors);

        public static GameSession CreateBattle(
            GameContent content,
            IReadOnlyList<PlayerState> players) =>
            CreateBattle(content, players, EnemyBehaviors.Standard);

        public static GameSession CreateBattle(
            GameContent content,
            IReadOnlyList<PlayerState> states,
            EnemyBehaviorResolver behaviors) =>
            CreateBattle(content, states, behaviors, DefaultSeed);

        public static GameSession CreateBattle(
            GameContent content,
            IReadOnlyList<PlayerState> states,
            int seed) =>
            CreateBattle(content, states, EnemyBehaviors.Standard, seed);

        public static GameSession CreateBattle(
            GameContent content,
            IReadOnlyList<PlayerState> states,
            EnemyBehaviorResolver behaviors,
            int seed)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            if (behaviors == null) throw new ArgumentNullException(nameof(behaviors));

            VerifyParticipants(states);

            var players = new List<Player>(states.Count);
            var modifiers = new List<UpgradeModifiers>(states.Count);

            foreach (PlayerState state in states)
            {
                players.Add(new Player(state));
                modifiers.Add(UpgradeModifiers.For(state, content));
            }

            // 두 Player의 상태/조준점 테스트는 계속 가능하지만, 보상 있는 다인 전투는
            // 귀속 정책이 정해지기 전에 시작하지 않는다.
            if (players.Count != 1)
                foreach (EnemyDefinition enemy in content.Enemies)
                    if (enemy.Gold != 0 || enemy.HqExp != 0)
                        throw new InvalidOperationException("보상 있는 다인 전투의 귀속 정책이 없다.");

            // 적·보상·공급은 모든 Player가 공유한다. 여러 Player의 구매를 공유 대상에 합치는 정책이 없다.
            if (players.Count != 1)
                foreach (UpgradeModifiers modifier in modifiers)
                    if (modifier.AffectsSharedWorld)
                        throw new InvalidOperationException("적·보상·공급에 영향을 주는 구매가 있는 다인 전투의 합성 정책이 없다.");

            for (int i = 0; i < players.Count; i++)
            {
                GiveSkills(players[i], content.StartingSkills, modifiers[i]);
            }

            // 공유 대상에 대한 구매 효과는 지금 실제 구성인 단일 Player의 것이다. 위 검사로 그 전제를 지킨다.
            UpgradeModifiers shared = players.Count == 1 ? modifiers[0] : UpgradeModifiers.None;

            var timeLimit = new TimeLimitRule(content.TimeLimit);
            var supply = new EnemySupply(new EnemySpawner(content.Spawn, behaviors, shared), shared);
            var growth = new GrowthProgression(content.Growth, timeLimit);
            var hq = new Hq(content.Hq, content.Growth);
            var world = new World(hq, players, supply, growth, new BattleRandom(seed));

            world.PlaceStartingEnemies(content.StartSupply);

            // 모든 검사를 통과한 뒤에 전투에 들인다. 조립이 실패하면 PlayerState는 묶이지 않는다.
            foreach (PlayerState state in states)
            {
                state.EnterBattle();
            }

            return new GameSession(world, timeLimit);
        }

        // 이 전투의 Skill = 모든 Player가 받는 시작 구성(Character 1종, 고정 구성) + 그 Player가 산 해금 노드의 Skill.
        // 획득 상태는 PlayerState의 산 노드 ID이고, Skill 목록은 전투마다 거기서 다시 조립한다(Skill 목록을 저장하지 않는다).
        // Skill 순서는 시작 구성, 그 뒤 해금 Skill(콘텐츠의 노드 순서)이다. Step 안에서 이 순서로 실행된다.
        private static void GiveSkills(
            Player player,
            IReadOnlyList<PassiveSkillDefinition> startingSkills,
            UpgradeModifiers modifiers)
        {
            foreach (PassiveSkillDefinition definition in startingSkills)
            {
                player.AddSkill(PassiveSkills.Create(definition, player, modifiers));
            }

            foreach (PassiveSkillDefinition definition in modifiers.UnlockedSkills())
            {
                player.AddSkill(PassiveSkills.Create(definition, player, modifiers));
            }
        }

        private static IReadOnlyList<PlayerState> NewProgress(IReadOnlyList<PlayerId> participants)
        {
            if (participants == null)
                throw new ArgumentException(
                    "참가 Player가 한 명 이상 필요하다.", nameof(participants));

            var states = new PlayerState[participants.Count];

            for (int i = 0; i < states.Length; i++)
            {
                states[i] = new PlayerState(participants[i]);
            }

            return states;
        }

        private static void VerifyParticipants(IReadOnlyList<PlayerState> states)
        {
            if (states == null || states.Count == 0)
                throw new ArgumentException(
                    "참가 Player가 한 명 이상 필요하다.", nameof(states));

            var ids = new HashSet<PlayerId>();

            foreach (PlayerState state in states)
            {
                if (state == null)
                    throw new ArgumentException("PlayerState가 비어 있다.", nameof(states));

                if (!ids.Add(state.Id))
                    throw new ArgumentException(
                        $"{state.Id}가 두 번 참가했다.", nameof(states));

                if (state.InBattle)
                    throw new InvalidOperationException($"{state.Id}는 이미 진행 중인 전투에 들어가 있다.");
            }
        }
    }
}
