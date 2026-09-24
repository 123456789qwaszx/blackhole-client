using System;
using System.Collections.Generic;
using BlackHole.Core;

namespace BlackHole.Unity
{
    // 전투의 시작·교체·종료와 진행(PlayerState)의 수명을 가진 유일한 흐름.
    // 입력과 HUD는 순서를 모르고 여기에 요청한다.
    //
    // - 새 진행(StartNew): 빈 PlayerState로 첫 전투를 연다. 이전 진행은 버린다.
    // - 전투 종료: 결과 확정 → 적·연출 정리 → 구매 화면(InShop). 정리는 처치가 아니다(보상 없음).
    // - 다음 전투(NextBattle): 같은 PlayerState로 새 전투를 연다. Gold·구매가 이어진다.
    // 참가 Player 목록은 호스트가 정한다(지금은 로컬 1명). 콘텐츠의 일이 아니다.
    internal sealed class SessionLauncher
    {
        private readonly GameContent _content;
        private readonly IReadOnlyList<PlayerId> _participants;
        private readonly WorldView _view;
        private readonly BattleAudio _audio;
        private PlayerState[] _progress;

        public GameSession Current { get; private set; }
        // 전투가 끝나 구매 화면에 있는가. 이때 Current는 결과를 보여 줄 끝난 전투다.
        public bool InShop { get; private set; }
        public IReadOnlyList<PlayerState> Progress => _progress;
        public IReadOnlyList<UpgradeNodeDefinition> Upgrades => _content.Upgrades;

        public SessionLauncher(
            GameContent content,
            IReadOnlyList<PlayerId> participants,
            WorldView view,
            BattleAudio audio)
        {
            _content = content;
            _participants = participants;
            _view = view;
            _audio = audio;
        }

        public void StartNew()
        {
            Current?.Stop();

            _progress = new PlayerState[_participants.Count];

            for (int i = 0; i < _progress.Length; i++)
            {
                _progress[i] = new PlayerState(_participants[i]);
            }

            StartBattle();
        }

        public void NextBattle()
        {
            if (!InShop)
                return;

            StartBattle();
        }

        // 전투를 끝낸다(결과 확정). 구매 화면은 OpenShopIfEnded가 연다.
        public void Stop() => Current?.Stop();

        // 끝난 전투의 적과 연출을 화면에서 치우고 구매 화면으로 간다. 끝난 전투는 다음 전투에 섞이지 않는다.
        public void OpenShopIfEnded()
        {
            if (InShop || Current == null || Current.Phase != SessionPhase.Ended)
                return;

            _view.Reset();
            InShop = true;
        }

        // 구매는 노드 ID로 요청한다. 정의 조회·조건 검사·기록은 UpgradePurchase가 한다.
        public PurchaseResult Purchase(PlayerState state, string nodeId) =>
            UpgradePurchase.TryPurchase(state, _content, nodeId);

        // 교체 순서: 화면·소리 정리 → 새 전투 조립 → 첫 화면·소리 동기화(소리는 기준만 잡는다).
        // 이전 판의 소리는 새 판을 시작할 때 멈춘다. 구매 화면으로 갈 때는 끝나 가는 소리를 끊지 않는다.
        // 전투마다 seed를 새로 정한다. 같은 전투를 재현할 방법(seed 기록·지정)은 아직 없다.
        private void StartBattle()
        {
            _view.Reset();
            _audio.Reset();

            Current = SessionAssembler.CreateBattle(_content, _progress, Environment.TickCount);
            InShop = false;

            _view.Synchronize(Current.World);
            _audio.Synchronize(Current);
        }
    }
}
