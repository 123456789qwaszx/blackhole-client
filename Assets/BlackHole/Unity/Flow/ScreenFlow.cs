using System;
using System.Collections.Generic;
using BlackHole.Core;

namespace BlackHole.Unity
{
    // 화면 전환과, 화면이 전투 Session·업그레이드와 만나는 경계.
    // 화면(View)은 버튼 사건을 알리고 표시 값을 받을 뿐이다. Session과 구매 규칙을 부르는 곳은 여기뿐이다.
    //
    //   타이틀 ─시작→ 전투 ─끝→ 업그레이드 ─다음 전투→ 전투
    //     │                        └─타이틀→ 타이틀
    //     └─설정→ 설정 ─뒤로→ 타이틀
    //
    // 타이틀의 시작은 늘 새 진행이다(저장이 없다). 다음 전투는 같은 진행 상태(Gold, 산 노드)를 이어받는다.
    // 화면마다 partial 파일 하나가 전환과 사건 연결을 가진다. 연결은 화면이 닫힐 때 모두 푼다.
    internal sealed partial class ScreenFlow : IDisposable
    {
        private readonly UIManager _ui;
        private readonly GameContent _content;
        // 전투 화면과 함께 보이는 판 안의 적. 전투 화면이 닫히면 비운다.
        private readonly EnemyView _enemyView;
        private readonly IReadOnlyList<PlayerId> _participants;
        // 업그레이드 화면을 보는 Player. 지금은 로컬 1명이다.
        private readonly PlayerId _viewer;
        private readonly UIPresentationSpec _titlePresentation;
        private readonly UIPresentationSpec _settingsPresentation;
        private readonly UIPresentationSpec _battlePresentation;
        private readonly UIPresentationSpec _upgradePresentation;

        private PlayerState[] _progress;
        private GameSession _battle;
        // 진행도: 적의 강도 단계(1 ~ 콘텐츠의 단계 수). HQ 성장 단계와 다르다.
        // 다음에 조립하는 전투가 이 단계로 만들어진다. 지금 바꾸는 곳은 조종 콘솔뿐이다.
        private int _stage = SessionAssembler.FirstStage;

        public ScreenFlow(
            UIManager ui,
            GameContent content,
            EnemyView enemyView,
            IReadOnlyList<PlayerId> participants,
            PlayerId viewer,
            UIPresentationSpec titlePresentation,
            UIPresentationSpec settingsPresentation,
            UIPresentationSpec battlePresentation,
            UIPresentationSpec upgradePresentation)
        {
            _ui = ui;
            _content = content;
            _enemyView = enemyView;
            _participants = participants;
            _viewer = viewer;
            _titlePresentation = titlePresentation;
            _settingsPresentation = settingsPresentation;
            _battlePresentation = battlePresentation;
            _upgradePresentation = upgradePresentation;
        }

        public void OpenTitle() => GoToTitle();

        public int Stage => _stage;
        public int StageCount => _content.StageCount;
        // 마지막으로 조립한 전투의 seed. 전투를 연 적이 없으면 null이다.
        public int? BattleSeed => _battle?.Seed;

        // 진행도를 바꾼다. 범위 밖의 값은 가장 가까운 단계가 된다. 진행 중인 전투는 바뀌지 않고 다음 전투부터 쓴다.
        public void SetStage(int stage) =>
            _stage = Math.Max(SessionAssembler.FirstStage, Math.Min(StageCount, stage));

        // 한 프레임. 전투 화면이 열려 있을 때만 전투 시간이 흐른다.
        public void Tick(float delta) => TickBattle(delta);

        #region 연결

        private readonly Dictionary<UIBase, List<Action>> _cleanupByScreen = new Dictionary<UIBase, List<Action>>();

        private void BindView<T>(T screen, Action<T> apply) where T : UIBase
        {
            Unbind(screen);
            apply(screen);
        }

        private void AddBinding<T>(T screen, Action<T> attach, Action<T> detach) where T : UIBase
        {
            attach(screen);
            AddCleanup(screen, () => detach(screen));
        }

        private void AddCleanup(UIBase screen, Action cleanup)
        {
            if (!_cleanupByScreen.TryGetValue(screen, out List<Action> cleanups))
            {
                cleanups = new List<Action>();
                _cleanupByScreen[screen] = cleanups;
            }

            cleanups.Add(cleanup);
        }

        private void Unbind(UIBase screen)
        {
            if (!_cleanupByScreen.TryGetValue(screen, out List<Action> cleanups))
                return;

            _cleanupByScreen.Remove(screen);
            RunCleanups(cleanups);
        }

        private static void RunCleanups(List<Action> cleanups)
        {
            for (int i = cleanups.Count - 1; i >= 0; i--)
                cleanups[i]?.Invoke();
        }

        public void Dispose()
        {
            foreach (List<Action> cleanups in _cleanupByScreen.Values)
                RunCleanups(cleanups);

            _cleanupByScreen.Clear();
        }

        #endregion
    }
}
