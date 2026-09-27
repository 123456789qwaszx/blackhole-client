using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlackHole.Core;

namespace BlackHole.Unity
{
    // 전투의 시작과 끝의 순서를 책임지는 상위 오케스트레이터. 각 시스템은 여기서 불릴 때만 시작하고 정리한다.
    //
    // 시작 순서: (사운드 — 그 시스템이 붙으면 이 앞에) → 적·전투 시스템 시작.
    // 종료 순서: 적·전투 시스템 정리(결산 포함) → (사운드 정리 — 붙으면 이 뒤에).
    // 스킬은 판의 일부라 따로 준비·정리하지 않는다: 판 조립 때 생기고, 스킬 화면은 적·전투 시스템이 적 화면과 함께 정리한다.
    // 조준 입력(AimInput)은 시작·정리할 상태가 없다 — 매 프레임 진행 중인 판에 조준점을 넣을 뿐이다.
    //
    // 진행 상태(PlayerState: Gold, 산 노드)와 다음 전투의 진행도(적의 강도 단계)를 가진다.
    // 진행 상태는 전투 밖에서만 바뀐다: 노드 구매(업그레이드 화면, 전투 중에는 살 수 없다)와
    // 판의 결산(GameSession.Settle — 적·전투 시스템의 정리 순서 안에서 한 번). 그래서 저장은 전투 밖(휴식 공간)에서만 하면 된다.
    // 시작·종료 요청의 성공 결과만 돌려준다. 화면 전환은 요청한 흐름이 결정한다.
    internal sealed class BattleOrchestrator
    {
        private readonly GameContent _content;
        private readonly BattleSystem _battle;
        private readonly PlayerId[] _participants;
        // 진행 상태는 오케스트레이터를 만들 때 만들고, 전투 사이에 이어진다(저장은 없다). 첫 전투 전의 구매(노드 콘솔)도 이것을 쓴다.
        private readonly PlayerState[] _progress;
        private int _stage = SessionAssembler.FirstStage;

        // 참가자마다의 진행 상태(Gold, 산 노드). 참가자 순서다.
        public IReadOnlyList<PlayerState> Progress => _progress;
        // 다음 전투의 판 구성: 지금 산 노드로 계산한다(판 조립과 같은 계산). 콘솔이 다음 판을 미리 보여 줄 때 쓴다.
        public IReadOnlyDictionary<EnemyDefinition, EnemyComposition> NextCompositions => _battle.PreviewCompositions(_progress);
        public int Stage => _stage;
        public int StageCount => _content.StageCount;
        // 지금 진행도의 단계 정의(쓰는 적 풀 포함).
        public StageDefinition SelectedStage => _content.GetStage(_stage);
        // 시작 또는 종료 순서를 처리하는 중인가. 이 동안 들어온 요청은 무시한다.
        public bool Busy { get; private set; }
        public bool CanStart => !Busy && _battle.IsIdle;
        public bool CanEnd => !Busy && _battle.CanShutdown;

        public BattleOrchestrator(GameContent content, BattleSystem battle, PlayerId[] participants)
        {
            _content = content;
            _battle = battle;
            _participants = participants;
            _progress = NewProgress();
        }

        // 진행도를 바꾼다. 범위 밖의 값은 가장 가까운 단계가 된다. 진행 중인 전투는 바뀌지 않고 다음 전투부터 쓴다.
        public void SetStage(int stage) =>
            _stage = Math.Max(SessionAssembler.FirstStage, Math.Min(StageCount, stage));

        public async Task<bool> StartBattleAsync()
        {
            if (!CanStart)
                return false;

            Busy = true;

            try
            {
                // 전투마다 seed를 새로 정한다. 쓴 seed는 판과 원자료에 남는다.
                await _battle.StartAsync(_progress, _stage, Environment.TickCount);
                return true;
            }
            finally
            {
                Busy = false;
            }
        }

        public async Task<BattleRawData> EndBattleAsync(SessionEndReason reason)
        {
            if (!CanEnd)
                return null;

            Busy = true;

            try
            {
                return await _battle.ShutdownAsync(reason);
            }
            finally
            {
                Busy = false;
            }
        }

        private PlayerState[] NewProgress()
        {
            var progress = new PlayerState[_participants.Length];

            for (int i = 0; i < progress.Length; i++)
                progress[i] = new PlayerState(_participants[i]);

            return progress;
        }
    }
}
