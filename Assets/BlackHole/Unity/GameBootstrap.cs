using System.Collections.Generic;
using BlackHole.Core;
using BlackHole.Sample;
using UnityEngine;

namespace BlackHole.Unity
{
    // 씬의 직렬화 설정으로 게임을 조립하는 Unity 진입점.
    // - Awake: 콘텐츠·노드 트리 로드·검증, 적 화면·스킬 화면·사망 효과 화면, 적·전투 시스템, 오케스트레이터, 조준 입력,
    //   UI(UIManager와 업그레이드·전투·결산 화면), 화면 흐름, 조종 콘솔·전투 시작·종료 콘솔·적 명령 콘솔·업그레이드 콘솔·스킬 콘솔(개발용) 조립.
    // - Start/Update: 조립한 GameHost에 Unity 수명을 전달한다.
    //
    // 콘텐츠: 판 설정은 SampleContent(C#), 스킬은 스킬 설정 에셋, 적 종류는 적 종류 목록 에셋,
    // 출현 배치와 전투 시작 공급은 적 공급 설정 에셋, 진행도(단계)와 적 풀은 단계 표 에셋이 채운다.
    // 노드 트리는 판 조립 콘텐츠와 따로 노드 목록 에셋에서 읽는다. 업그레이드 화면은 같은 에셋의 격자 칸으로 노드를 놓는다.
    // 화면은 씬의 UI Canvas에 놓인 화면 프리팹(UpgradeScreen·BattleScreen·SettlementScreen)을 Root Layer와 Registered Views로 받는다.
    // 연결하지 않으면(Root Layer가 비어 있으면) 코드로 만든 임시 화면을 쓴다(PlaceholderScreens, 개발용 대체).
    // Presentation을 비워 두면 아무것도 바꾸지 않는 빈 Presentation을 쓴다.
    public sealed class GameBootstrap : MonoBehaviour
    {
        // 판에 참가하는 로컬 Player. 지금은 1명이다(Players.Count == 1일 뿐 전역 Player가 아니다).
        private static readonly PlayerId[] LocalPlayers = { new PlayerId(1) };

        [Header("Content")]
        [SerializeField] private EnemyCatalog enemyCatalog;
        [SerializeField] private EnemySupplySetup enemySupply;
        [SerializeField] private StageTable stageTable;
        [SerializeField] private NodeCatalog nodeCatalog;
        [SerializeField] private SkillSetup skillSetup;

        [Header("UI Layers (비우면 임시 화면을 만든다)")]
        [SerializeField] private RectTransform rootLayer;
        [SerializeField] private RectTransform panelLayer;

        [Header("Registered Views")]
        [SerializeField] private UIBase[] views;

        [Header("Presentations (비우면 빈 Presentation)")]
        [SerializeField] private UIPresentationSpec battlePresentation;
        [SerializeField] private UIPresentationSpec upgradePresentation;
        [SerializeField] private UIPresentationSpec settlementPresentation;

        [Header("UI Context")]
        [SerializeField] private string themeId = "Light";
        [SerializeField] private string localeId = "ko-KR";

        [Header("Runtime")]
        [SerializeField] private UIDisplayRefreshDriver displayRefreshDriver;

        private readonly List<UIPresentationSpec> _emptyPresentations = new List<UIPresentationSpec>();
        private GameHost _host;

        #region Unity 수명

        private void Awake()
        {
            if (!TryLoadContent(out GameContent content)
                || !TryLoadNodeTree(out NodeTreeData layout, out NodeTree nodeTree)
                || !NodesFitContent(content, nodeTree))
            {
                enabled = false;
                return;
            }

            BootstrapGame(content, layout, nodeTree);
        }

        private void BootstrapGame(GameContent content, NodeTreeData layout, NodeTree nodeTree)
        {
            var enemyLooks = new EnemyLooks(enemyCatalog.Kinds());
            var enemyView = new EnemyView(transform, enemyLooks);
            var skillView = new SkillView(transform);
            var deathEffectView = new DeathEffectView(transform);
            var battle = new BattleSystem(content, nodeTree, enemyView, skillView, deathEffectView);
            var orchestrator = new BattleOrchestrator(content, battle, LocalPlayers);
            // 업그레이드 화면과 콘솔이 보는 진행 상태: 지금 실제 구성인 로컬 Player 1명.
            PlayerState viewer = orchestrator.Progress[0];
            // 마우스가 조준하는 참가자: 같은 로컬 Player.
            var aim = new AimInput(battle, viewer.Id);

            UIManager ui = BootstrapUI();

            var upgradePresenter = new UpgradePresenter(ui, viewer, nodeTree);
            var screens = new ScreenFlow(
                ui,
                OrEmpty(battlePresentation, "Battle"),
                OrEmpty(upgradePresentation, "Upgrade"),
                OrEmpty(settlementPresentation, "Settlement"),
                battle, orchestrator, nodeTree, layout, viewer, upgradePresenter);

            if (displayRefreshDriver != null)
                displayRefreshDriver.Initialize(ui);

            // 조종 콘솔, 전투 시작·종료 콘솔, 적 명령 콘솔, 업그레이드 콘솔, 스킬 콘솔은 개발용이다. 에디터와 개발 빌드에서만 만든다.
            ControlConsole console = null;
            BattleLifecycleConsole lifecycleConsole = null;
            EnemyCommandConsole commandConsole = null;
            UpgradeConsole upgradeConsole = null;
            SkillConsole skillConsole = null;
            if (Debug.isDebugBuild)
            {
                console = new ControlConsole(transform, orchestrator, battle, enemyLooks);
                lifecycleConsole = new BattleLifecycleConsole(transform, orchestrator, battle, nodeTree, viewer.Id);
                commandConsole = new EnemyCommandConsole(transform, battle, content.Enemies);
                upgradeConsole = new UpgradeConsole(transform, viewer, nodeTree, upgradePresenter.PresentCurrent);
                skillConsole = new SkillConsole(transform, content, battle, viewer.Id);
            }

            _host = new GameHost(ui, battle, orchestrator, aim, screens,
                enemyLooks, enemyView, skillView, deathEffectView,
                console, lifecycleConsole, commandConsole, upgradeConsole, skillConsole);
        }

        private UIManager BootstrapUI()
        {
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

            return ui;
        }

        private void Start() => _host?.Start();

        private void Update() => _host?.Tick(Time.deltaTime);

        private void OnDestroy()
        {
            _host?.Dispose();

            foreach (UIPresentationSpec presentation in _emptyPresentations)
                Destroy(presentation);
        }

        #endregion

        #region 조립

        // 오류가 있는 콘텐츠로는 시작하지 않는다. 모든 진단을 위치와 함께 남긴다.
        private bool TryLoadContent(out GameContent content)
        {
            content = null;

            if (enemyCatalog == null || enemySupply == null || stageTable == null || skillSetup == null)
            {
                Debug.LogError(
                    "[콘텐츠] GameBootstrap에 적 종류 목록(EnemyCatalog), 적 공급 설정(EnemySupplySetup), 단계 표(StageTable), 스킬 설정(SkillSetup)을 연결해야 한다.",
                    this);
                return false;
            }

            ContentData data = SampleContent.Create();
            skillSetup.WriteTo(data);
            enemyCatalog.WriteTo(data);
            enemySupply.WriteTo(data);
            stageTable.WriteTo(data);
            ContentLoadResult result = ContentLoader.Load(data);

            foreach (ContentDiagnostic diagnostic in result.Diagnostics)
                Debug.LogError("[콘텐츠] " + diagnostic, this);

            content = result.Content;
            return result.Succeeded;
        }

        // 오류가 있는 노드 트리로도 시작하지 않는다. 업그레이드 화면은 트리(규칙)와 함께 저작 데이터(격자 칸)도 받는다.
        private bool TryLoadNodeTree(out NodeTreeData layout, out NodeTree tree)
        {
            layout = null;
            tree = null;

            if (nodeCatalog == null)
            {
                Debug.LogError("[노드 트리] GameBootstrap에 노드 목록(NodeCatalog)을 연결해야 한다.", this);
                return false;
            }

            layout = nodeCatalog.ToData();
            NodeTreeLoadResult result = NodeTreeLoader.Load(layout);

            foreach (ContentDiagnostic diagnostic in result.Diagnostics)
                Debug.LogError("[노드 트리] " + diagnostic, this);

            tree = result.Tree;
            return result.Succeeded;
        }

        // 노드를 모두 산 경우에도 판을 조립할 수 있어야 한다(질량 단계 범위, 황금이 되는 종류, 전체 개체 수 상한).
        // 두 데이터는 따로 불러오므로 여기서 함께 본다. 오류가 있으면 언젠가 전투 시작이 실패하므로 시작하지 않는다.
        private bool NodesFitContent(GameContent content, NodeTree tree)
        {
            IReadOnlyList<ContentDiagnostic> diagnostics = UpgradeContentCheck.Check(content, tree);

            foreach (ContentDiagnostic diagnostic in diagnostics)
                Debug.LogError("[노드 트리 × 콘텐츠] " + diagnostic, this);

            return diagnostics.Count == 0;
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
