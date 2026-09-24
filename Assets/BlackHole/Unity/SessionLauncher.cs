using BlackHole.Core;

namespace BlackHole.Unity
{
    // 판 시작·교체·종료의 유일한 흐름. 입력과 HUD는 초기화 순서를 모르고 여기에 요청한다.
    // 교체 순서: 이전 판 종료(결과 확정, 이후 요청 거절) → 화면 정리 → 새 판 조립 → 첫 화면 동기화.
    // 요청은 언제나 Current로 간다. 이전 판을 붙잡는 구독이나 참조가 없다.
    //
    // 수명 정책(이 샘플): 호스트가 비활성화되면 현재 판을 끝내고, 다시 활성화되면 새 판을 시작한다.
    internal sealed class SessionLauncher
    {
        private readonly ContentCatalog _catalog;
        private readonly ReferenceWorldView _view;

        public GameSession Current { get; private set; }

        public SessionLauncher(ContentCatalog catalog, ReferenceWorldView view)
        {
            _catalog = catalog;
            _view = view;
        }

        public void StartNew()
        {
            Current?.Stop();
            _view.Reset();
            Current = SessionAssembler.Create(_catalog);
            _view.Synchronize(Current.Field, 0);
        }

        public void Stop() => Current?.Stop();
    }
}
