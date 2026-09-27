using System;
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
    // 업그레이드 화면 안의 트리 보기 페이지(NodeTreeView)도 Registered Views에 넣는다.
    // 세 화면·트리 보기 페이지와 Root/Panel Layer를 씬에서 연결해야 한다. 누락된 연결은 조립 전에 오류로 알린다.
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

        [Header("UI Layers")]
        [SerializeField] private RectTransform rootLayer;
        [SerializeField] private RectTransform panelLayer;

        [Header("Registered Views")]
        [SerializeField] private UIBase[] views;

        [Header("Presentations (비우면 빈 Presentation)")]
        [SerializeField] private UIPresentationSpec battlePresentation;
        [SerializeField] private UIPresentationSpec upgradePresentation;
        [SerializeField] private UIPresentationSpec settlementPresentation;
        [SerializeField] private UIPresentationSpec nodeTreePresentation;

        [Header("UI Context")]
        [SerializeField] private string themeId = "Light";
        [SerializeField] private string localeId = "ko-KR";

        [Header("Runtime")]
        [SerializeField] private UIDisplayRefreshDriver displayRefreshDriver;

        private readonly List<UIPresentationSpec> _emptyPresentations = new List<UIPresentationSpec>();
        private GameContent _content;
        private NodeTreeData _layout;
        private NodeTree _nodeTree;
        private EnemyLooks _enemyLooks;
        private EnemyView _enemyView;
        private SkillView _skillView;
        private DeathEffectView _deathEffectView;
        private BattleSystem _battle;
        private BattleOrchestrator _orchestrator;
        private PlayerState _viewer;
        private AimInput _aim;
        private UIManager _ui;
        private ScreenFlow _screens;
        private ControlConsole _console;
        private BattleLifecycleConsole _lifecycleConsole;
        private EnemyCommandConsole _commandConsole;
        private UpgradeConsole _upgradeConsole;
        private SkillConsole _skillConsole;
        private GameHost _host;

        #region Unity 수명

        private void Awake()
        {
            if (!TryLoadContent(out _content)
                || !TryLoadNodeTree(out _layout, out _nodeTree)
                || !NodesFitContent(_content, _nodeTree)
                || !HasConfiguredUI())
            {
                enabled = false;
                return;
            }

            BootstrapBattleViews();
            BootstrapBattle();
            BootstrapUI();
            BootstrapScreenFlow();
            BootstrapDevelopmentConsoles();
            BootstrapHost();
        }

        private void BootstrapBattleViews()
        {
            _enemyLooks = new EnemyLooks(enemyCatalog.Kinds());
            _enemyView = new EnemyView(transform, _enemyLooks);
            _skillView = new SkillView(transform);
            _deathEffectView = new DeathEffectView(transform);
        }

        private void BootstrapBattle()
        {
            _battle = new BattleSystem(_content, _nodeTree, _enemyView, _skillView, _deathEffectView);
            _orchestrator = new BattleOrchestrator(_content, _battle, LocalPlayers);
            // 업그레이드 화면과 콘솔이 보는 진행 상태: 지금 실제 구성인 로컬 Player 1명.
            _viewer = _orchestrator.Progress[0];
            // 마우스가 조준하는 참가자: 같은 로컬 Player.
            _aim = new AimInput(_battle, _viewer.Id);
        }

        private void BootstrapUI()
        {
            _ui = new UIManager(
                rootLayer,
                panelLayer,
                new UIResolver(new UIContext(themeId, localeId)),
                new UIPresentationApplier());

            foreach (UIBase view in views)
            {
                if (view == null)
                    continue;

                view.gameObject.SetActive(false);
                _ui.Register(view);
            }

            if (displayRefreshDriver != null)
                displayRefreshDriver.Initialize(_ui);
        }

        private void BootstrapScreenFlow()
        {
            _screens = new ScreenFlow(
                _ui,
                OrEmpty(battlePresentation, "Battle"),
                OrEmpty(upgradePresentation, "Upgrade"),
                OrEmpty(settlementPresentation, "Settlement"),
                OrEmpty(nodeTreePresentation, "NodeTree"),
                _battle, _orchestrator, _nodeTree, BuildNodeItems(_nodeTree, _layout), _viewer);
        }

        private void BootstrapDevelopmentConsoles()
        {
            if (!Debug.isDebugBuild)
                return;

            // 스킬 콘솔은 GameHost가 전투 Step보다 먼저 갱신한다.
            _console = new ControlConsole(transform, _orchestrator, _battle, _content, _nodeTree, _enemyLooks);
            _lifecycleConsole = new BattleLifecycleConsole(transform, _orchestrator, _battle, _nodeTree,
                _viewer.Id, _screens.HandleLifecycleStartBattleClicked,
                _screens.HandleLifecyclePauseClicked, _screens.HandleLifecycleEndBattleClicked);
            _commandConsole = new EnemyCommandConsole(transform, _battle, _content.Enemies);
            _upgradeConsole = new UpgradeConsole(transform, _viewer, _nodeTree, _screens.HandleUpgradeConsoleProgressChanged);
            _skillConsole = new SkillConsole(transform, _content, _battle, _viewer.Id);
        }

        private void BootstrapHost()
        {
            _host = new GameHost(_ui, _battle, _aim, _screens,
                _enemyLooks, _enemyView, _skillView, _deathEffectView,
                _console, _lifecycleConsole, _commandConsole, _upgradeConsole, _skillConsole);
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

        private bool HasConfiguredUI()
        {
            if (rootLayer != null && panelLayer != null && views != null)
            {
                bool hasUpgrade = false;
                bool hasBattle = false;
                bool hasSettlement = false;
                bool hasNodeTree = false;

                foreach (UIBase view in views)
                {
                    hasUpgrade |= view is UpgradeScreen;
                    hasBattle |= view is BattleScreen;
                    hasSettlement |= view is SettlementScreen;
                    hasNodeTree |= view is NodeTreeView;
                }

                if (hasUpgrade && hasBattle && hasSettlement && hasNodeTree)
                    return true;
            }

            Debug.LogError(
                "[UI] GameBootstrap에 Root Layer, Panel Layer와 UpgradeScreen·BattleScreen·SettlementScreen, " +
                "업그레이드 화면 안의 트리 보기 페이지(NodeTreeView)를 Registered Views로 연결해야 한다.",
                this);
            return false;
        }

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

        // 업그레이드 화면에 그릴 노드. 격자 칸은 화면 배치용이라 규칙 트리가 아니라 같은 저작 데이터에서 읽는다.
        // 로더가 같은 데이터로 트리를 만들었으니 트리의 모든 노드에 칸이 있다.
        private static IReadOnlyList<NodeTreeView.NodeItem> BuildNodeItems(NodeTree tree, NodeTreeData layout)
        {
            var cells = new Dictionary<string, (int X, int Y)>(StringComparer.Ordinal);
            foreach (NodeData node in layout.Nodes)
            {
                if (node?.Id != null && !cells.ContainsKey(node.Id))
                    cells.Add(node.Id, (node.X, node.Y));
            }

            var nodes = new List<NodeTreeView.NodeItem>(tree.Nodes.Count);
            foreach (NodeDefinition node in tree.Nodes)
            {
                (int x, int y) = cells.TryGetValue(node.Id, out (int X, int Y) cell) ? cell : (0, 0);
                nodes.Add(new NodeTreeView.NodeItem(node.Id, x, y, node.Price));
            }

            return nodes;
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
