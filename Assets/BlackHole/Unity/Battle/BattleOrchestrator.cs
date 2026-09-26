using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlackHole.Core;
using UnityEngine;

namespace BlackHole.Unity
{
    // 전투의 시작과 끝의 순서를 책임지는 상위 오케스트레이터. 각 시스템은 여기서 불릴 때만 시작하고 정리한다.
    //
    // 시작 순서: (Skill 준비·사운드·조준 — 그 시스템이 붙으면 이 앞에) → 적·전투 시스템 시작.
    // 종료 순서: (Skill 정리 — 붙으면 이 앞에) → 적·전투 시스템 정리(결산 포함) → (사운드 정리, 조준 정리 — 붙으면 이 뒤에).
    //
    // 진행 상태(PlayerState: Gold, 산 노드)와 다음 전투의 진행도(적의 강도 단계)를 가진다.
    // 판 구성(종류별 질량 단계·황금 비율·황금 배율)은 전투를 시작할 때 산 노드에서 계산한다(Loadout.EnemiesFor).
    // 진행 상태는 전투 밖에서만 바뀐다: 노드 구매(업그레이드 콘솔, 전투 중에는 살 수 없다)와
    // 판의 결산(GameSession.Settle — 적·전투 시스템의 정리 순서 안에서 한 번). 그래서 저장은 전투 밖(휴식 공간)에서만 하면 된다.
    // 콘솔·전투 화면의 버튼과 판의 시간 종료는 모두 여기로 요청한다. 나중의 GoToBattle·GoToUpgrade도 여기를 쓴다.
    internal sealed class BattleOrchestrator : IDisposable
    {
        private readonly GameContent _content;
        private readonly BattleSystem _battle;
        private readonly PlayerState[] _progress;
        private int _stage = SessionAssembler.FirstStage;

        public int Stage => _stage;
        public int StageCount => _content.StageCount;
        // 진행 상태(Gold, 산 노드). 새 진행으로 만들고 이후 전투에 이어진다(저장은 없다).
        public IReadOnlyList<PlayerState> Progress => _progress;
        // 지금 진행도의 단계 정의(쓰는 적 풀 포함).
        public StageDefinition SelectedStage => _content.GetStage(_stage);
        // 다음 전투의 판 구성: 지금 산 노드로 계산한다. 콘솔이 다음 판을 미리 보여 줄 때 쓴다.
        public IReadOnlyDictionary<EnemyDefinition, EnemyComposition> NextCompositions => Loadout.EnemiesFor(_content, _progress);
        // 시작 또는 종료 순서를 처리하는 중인가. 이 동안 들어온 요청은 무시한다.
        public bool Busy { get; private set; }
        public bool CanStart => !Busy && _battle.IsIdle;
        public bool CanEnd => !Busy && _battle.CanShutdown;

        public BattleOrchestrator(GameContent content, BattleSystem battle, PlayerId[] participants)
        {
            _content = content;
            _battle = battle;
            _progress = new PlayerState[participants.Length];

            for (int i = 0; i < _progress.Length; i++)
                _progress[i] = new PlayerState(participants[i]);

            _battle.TimeExpired += OnTimeExpired;
        }

        // 진행도를 바꾼다. 범위 밖의 값은 가장 가까운 단계가 된다. 진행 중인 전투는 바뀌지 않고 다음 전투부터 쓴다.
        public void SetStage(int stage) =>
            _stage = Math.Max(SessionAssembler.FirstStage, Math.Min(StageCount, stage));

        public async Task StartBattleAsync()
        {
            if (!CanStart)
                return;

            Busy = true;

            try
            {
                // 전투마다 seed를 새로 정한다. 쓴 seed는 판과 원자료에 남는다.
                // 판 구성은 지금 산 노드로 계산해 넘긴다. 조립이 판의 적 수치 표로 옮겨 적으므로 전투 중에는 바뀌지 않는다.
                await _battle.StartAsync(_progress, _stage, Environment.TickCount, Loadout.EnemiesFor(_content, _progress));
            }
            finally
            {
                Busy = false;
            }
        }

        public async Task EndBattleAsync(SessionEndReason reason)
        {
            if (!CanEnd)
                return;

            Busy = true;

            try
            {
                await _battle.ShutdownAsync(reason);
            }
            finally
            {
                Busy = false;
            }
        }

        // 버튼·사건에서 부르는 입구. 순서 처리 중 난 예외는 로그로 남긴다.
        public async void RequestStart()
        {
            try { await StartBattleAsync(); }
            catch (Exception error) { Debug.LogException(error); }
        }

        public async void RequestEnd(SessionEndReason reason)
        {
            try { await EndBattleAsync(reason); }
            catch (Exception error) { Debug.LogException(error); }
        }

        public void Dispose() => _battle.TimeExpired -= OnTimeExpired;

        private void OnTimeExpired() => RequestEnd(SessionEndReason.TimeExpired);
    }
}
