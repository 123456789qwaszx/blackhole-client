using System.Collections.Generic;
using BlackHole.Core;
using BlackHole.Sample;
using UnityEngine;

namespace BlackHole.Unity
{
    // Unity 수명과 한 프레임을 가진 진입점(조립 루트).
    // - Awake: 콘텐츠·노드 트리 로드·검증, 적 화면, 적·전투 시스템, 오케스트레이터, UI(UIManager와 전투 화면),
    //   화면 흐름, 조종 콘솔·전투 시작·종료 콘솔·적 명령 콘솔·노드 콘솔(개발용) 조립.
    // - Start: 전투 화면을 연다. 판은 아직 없다 — 전투 시작은 오케스트레이터에 요청한다(지금은 전투 시작·종료 콘솔의 Start).
    // - Update: 적·전투 시스템 → 화면 → 콘솔 순서로 한 프레임을 넘긴다.
    //
    // 콘텐츠: 판 설정은 SampleContent(C#), 적 종류는 적 종류 목록 에셋,
    // 출현 배치와 전투 시작 공급은 적 공급 설정 에셋, 진행도(단계)와 적 풀은 단계 표 에셋이 채운다.
    // 노드 트리는 판 조립 콘텐츠와 따로 노드 목록 에셋에서 읽는다.
    // 화면 프리팹을 연결하지 않으면(Root Layer가 비어 있으면) 코드로 만든 임시 화면을 쓴다(PlaceholderScreens).
    // Presentation을 비워 두면 아무것도 바꾸지 않는 빈 Presentation을 쓴다.
    public sealed class GameHost : MonoBehaviour
    {
        // 판에 참가하는 로컬 Player. 지금은 1명이다(Players.Count == 1일 뿐 전역 Player가 아니다).
        private static readonly PlayerId[] LocalPlayers = { new PlayerId(1) };

        [Header("Content")]
        [SerializeField] private EnemyCatalog enemyCatalog;
        [SerializeField] private EnemySupplySetup enemySupply;
        [SerializeField] private StageTable stageTable;
        [SerializeField] private NodeCatalog nodeCatalog;

        [Header("UI Layers (비우면 임시 화면을 만든다)")]
        [SerializeField] private RectTransform rootLayer;
        [SerializeField] private RectTransform panelLayer;

        [Header("Registered Views")]
        [SerializeField] private UIBase[] views;

        [Header("Presentations (비우면 빈 Presentation)")]
        [SerializeField] private UIPresentationSpec battlePresentation;

        [Header("UI Context")]
        [SerializeField] private string themeId = "Light";
        [SerializeField] private string localeId = "ko-KR";

        [Header("Runtime")]
        [SerializeField] private UIDisplayRefreshDriver displayRefreshDriver;

        private readonly List<UIPresentationSpec> _emptyPresentations = new List<UIPresentationSpec>();
        private EnemyLooks _enemyLooks;
        private EnemyView _enemyView;
        private BattleSystem _battle;
        private BattleOrchestrator _orchestrator;
        private ScreenFlow _flow;
        private ControlConsole _console;
        private BattleLifecycleConsole _lifecycleConsole;
        private EnemyCommandConsole _commandConsole;
        private NodeConsole _nodeConsole;

        #region Unity 수명

        private void Awake()
        {
            if (!TryLoadContent(out GameContent content) || !TryLoadNodeTree(out NodeTree nodeTree))
            {
                enabled = false;
                return;
            }

            _enemyLooks = new EnemyLooks(enemyCatalog.Kinds());
            _enemyView = new EnemyView(transform, _enemyLooks);
            _battle = new BattleSystem(content, _enemyView);
            _orchestrator = new BattleOrchestrator(content, _battle, LocalPlayers);

            if (rootLayer == null)
            {
                PlaceholderScreens.Result placeholder = PlaceholderScreens.Build(transform);
                rootLayer = placeholder.RootLayer;
                panelLayer = placeholder.PanelLayer;
                views = placeholder.Views;
            }

            var ui = new UIManager(
                rootLayer,
                panelLayer,
                new UIResolver(new UIContext(themeId, localeId)),
                new UIPresentationApplier());

            foreach (UIBase view in views ?? new UIBase[0])
            {
                if (view == null)
                    continue;

                view.gameObject.SetActive(false);
                ui.Register(view);
            }

            _flow = new ScreenFlow(ui, _battle, _orchestrator, OrEmpty(battlePresentation, "Battle"));

            if (displayRefreshDriver != null)
                displayRefreshDriver.Initialize(ui);

            // 조종 콘솔, 전투 시작·종료 콘솔, 적 명령 콘솔, 노드 콘솔은 개발용이다. 에디터와 개발 빌드에서만 만든다.
            if (Debug.isDebugBuild)
            {
                _console = new ControlConsole(transform, _orchestrator, _battle, _enemyLooks);
                _lifecycleConsole = new BattleLifecycleConsole(transform, _orchestrator, _battle);
                _commandConsole = new EnemyCommandConsole(transform, _battle, content.Enemies);
                _nodeConsole = new NodeConsole(transform, nodeTree, _orchestrator.Progress[0]);
            }
        }

        private void Start() => _flow.OpenBattleScreen();

        private void Update()
        {
            _battle.Tick(Time.deltaTime);
            _flow.Tick();
            _console?.Tick();
            _lifecycleConsole?.Tick();
            _commandConsole?.Tick();
            _nodeConsole?.Tick();
        }

        private void OnDestroy()
        {
            _nodeConsole?.Dispose();
            _commandConsole?.Dispose();
            _lifecycleConsole?.Dispose();
            _console?.Dispose();
            _flow?.Dispose();
            _orchestrator?.Dispose();
            _enemyView?.Dispose();
            _enemyLooks?.Dispose();

            foreach (UIPresentationSpec presentation in _emptyPresentations)
                Destroy(presentation);
        }

        #endregion

        #region 조립

        // 오류가 있는 콘텐츠로는 시작하지 않는다. 모든 진단을 위치와 함께 남긴다.
        private bool TryLoadContent(out GameContent content)
        {
            content = null;

            if (enemyCatalog == null || enemySupply == null || stageTable == null)
            {
                Debug.LogError(
                    "[콘텐츠] GameHost에 적 종류 목록(EnemyCatalog), 적 공급 설정(EnemySupplySetup), 단계 표(StageTable)를 연결해야 한다.",
                    this);
                return false;
            }

            ContentData data = SampleContent.Create();
            enemyCatalog.WriteTo(data);
            enemySupply.WriteTo(data);
            stageTable.WriteTo(data);
            ContentLoadResult result = ContentLoader.Load(data);

            foreach (ContentDiagnostic diagnostic in result.Diagnostics)
                Debug.LogError("[콘텐츠] " + diagnostic, this);

            content = result.Content;
            return result.Succeeded;
        }

        // 오류가 있는 노드 트리로도 시작하지 않는다.
        private bool TryLoadNodeTree(out NodeTree tree)
        {
            tree = null;

            if (nodeCatalog == null)
            {
                Debug.LogError("[노드 트리] GameHost에 노드 목록(NodeCatalog)을 연결해야 한다.", this);
                return false;
            }

            NodeTreeLoadResult result = NodeTreeLoader.Load(nodeCatalog.ToData());

            foreach (ContentDiagnostic diagnostic in result.Diagnostics)
                Debug.LogError("[노드 트리] " + diagnostic, this);

            tree = result.Tree;
            return result.Succeeded;
        }

        private UIPresentationSpec OrEmpty(UIPresentationSpec presentation, string id)
        {
            if (presentation != null)
                return presentation;

            var empty = ScriptableObject.CreateInstance<UIPresentationSpec>();
            empty.name = id;
            empty.presentationId = id;
            _emptyPresentations.Add(empty);
            return empty;
        }

        #endregion
    }
}
