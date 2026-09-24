using System.Collections.Generic;
using BlackHole.Core;

namespace BlackHole.Unity
{
    // 판 시작·교체·종료의 유일한 흐름. 입력과 HUD는 초기화 순서를 모르고 여기에 요청한다.
    // 교체 순서: 이전 판 종료(결과 확정) → 화면 정리 → 새 판 조립 → 첫 화면 동기화.
    // 참가 Player 목록은 호스트가 정한다(지금은 로컬 1명). 콘텐츠의 일이 아니다.
    internal sealed class SessionLauncher
    {
        private readonly GameContent _content;
        private readonly IReadOnlyList<PlayerId> _participants;
        private readonly WorldView _view;

        public GameSession Current { get; private set; }

        public SessionLauncher(
            GameContent content, 
            IReadOnlyList<PlayerId> participants, 
            WorldView view)
        {
            _content = content;
            _participants = participants;
            _view = view;
        }

        public void StartNew()
        {
            Current?.Stop();
            _view.Reset();
            
            Current = SessionAssembler.Create(_content, _participants);
            
            _view.Synchronize(Current.World);
        }

        public void Stop() => Current?.Stop();
    }
}
