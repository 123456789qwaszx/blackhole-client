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
    // 종료 순서: (Skill 정리 — 붙으면 이 앞에) → 적·전투 시스템 정리 → 결산 → (사운드 정리, 조준 정리 — 붙으면 이 뒤에).
    //
    // 진행 상태(PlayerState)와 다음 전투의 진행도(적의 강도 단계)를 가진다.
    // 진행 상태를 바꾸는 것은 결산뿐이다: 정리가 끝나 원자료가 나온 뒤, 그 판이 번 Gold를 한 번 더한다.
    // 전투 중에는 진행 상태가 바뀌지 않으므로 저장은 전투 밖(휴식 공간)에서만 하면 된다.
    // 콘솔·전투 화면의 버튼과 판의 시간 종료는 모두 여기로 요청한다. 나중의 GoToBattle·GoToUpgrade도 여기를 쓴다.
    internal sealed class BattleOrchestrator : IDisposable
    {
        private readonly GameContent _content;
        private readonly BattleSystem _battle;
        private readonly PlayerId[] _participants;
        private PlayerState[] _progress;
        private int _stage = SessionAssembler.FirstStage;

        public int Stage => _stage;
        public int StageCount => _content.StageCount;
        // 진행 상태(Gold). 첫 전투를 시작하기 전에는 null이다.
        public IReadOnlyList<PlayerState> Progress => _progress;
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
                // 진행 상태는 처음 시작할 때 만들고, 이후 전투에 이어진다(저장은 없다).
                _progress ??= NewProgress();
                // 전투마다 seed를 새로 정한다. 쓴 seed는 판과 원자료에 남는다.
                await _battle.StartAsync(_progress, _stage, Environment.TickCount);
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
                // 정리가 실패하면(Faulted) 원자료가 나오지 않아 결산하지 않는다. 다시 요청해 정리가 끝났을 때 한 번 결산한다.
                BattleRawData raw = await _battle.ShutdownAsync(reason);
                Settle(raw);
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

        // 결산: 끝난 판이 번 Gold를 진행 상태에 더한다. 지금 실제 구성은 로컬 Player 1명이다.
        // 다인 플레이의 보상 귀속은 미정이라 참가자가 둘 이상이면 아무도 받지 않는다(d71c0f4의 전제와 같다).
        private void Settle(BattleRawData raw)
        {
            if (_progress.Length == 1)
                _progress[0].EarnGold(raw.EarnedGold);
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
