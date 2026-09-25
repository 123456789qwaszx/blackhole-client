using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 검증된 콘텐츠와 참가자의 진행 상태로 한 전투를 새로 조립하는 유일한 진입점.
    // 정의는 공유하고, 전투의 실행 상태(시간, 상태, 결과, 판 안의 적)는 전투마다 새로 만든다.
    // 누가 참가하는지는 콘텐츠가 아니라 판 설정이다 — 호스트가 넘긴다(지금은 로컬 1명).
    //
    // 진행 상태(Gold, 산 노드)는 전투 사이에 이어진다. 새 진행을 시작할지는 호출하는 쪽이 새 PlayerState로 정한다.
    // 산 노드가 전투를 바꾸는 효과는 효과의 대상 시스템이 돌아올 때 여기서 반영한다.
    //
    // stage는 진행도(적의 강도 단계, 1 ~ 콘텐츠의 단계 수)다. HQ 성장 단계와 다르다.
    // 그 단계의 적 풀이 이 판의 풀 여과 장치가 된다. 체력·크기 계수는 단계 표에 붙을 때 여기서 쓴다.
    // seed는 이 전투의 난수(BattleRandom)를 정한다. 같은 콘텐츠·단계·seed·진행 시간이면 같은 결과가 나온다.
    public static class SessionAssembler
    {
        public const int FirstStage = 1;
        public const int DefaultSeed = 0;

        public static GameSession CreateBattle(GameContent content, IReadOnlyList<PlayerState> states) =>
            CreateBattle(content, states, FirstStage, DefaultSeed);

        public static GameSession CreateBattle(GameContent content, IReadOnlyList<PlayerState> states, int stage, int seed)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            if (stage < FirstStage || stage > content.StageCount)
                throw new ArgumentOutOfRangeException(
                    nameof(stage), $"단계는 {FirstStage}부터 {content.StageCount}까지다. 받은 값: {stage}.");

            VerifyParticipants(states);

            var players = new List<PlayerState>(states);
            var world = new World(new BattleRandom(seed), content.GetStage(stage).Pool);

            // 전투 시작 배치. 0초에 한 번 공급한다.
            world.PlaceStartingEnemies(content.StartSupply, content.EnemyPlacement);

            var session = new GameSession(world, new TimeLimitRule(content.TimeLimit), stage, seed, players.AsReadOnly());

            // 모든 검사를 통과한 뒤에 전투에 들인다. 조립이 실패하면 PlayerState는 묶이지 않는다.
            foreach (PlayerState state in players)
            {
                state.EnterBattle();
            }

            return session;
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
