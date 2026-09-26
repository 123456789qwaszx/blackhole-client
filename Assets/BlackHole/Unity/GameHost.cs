using System.Collections.Generic;
using BlackHole.Core;
using UnityEngine;

namespace BlackHole.Unity
{
    // Unity 수명과 한 프레임을 가진 진입점(조립 루트).
    // - Awake: 노드 트리 로드·검증, 진행 상태, UI(UIManager와 업그레이드 화면), 화면 흐름, 업그레이드 콘솔(개발용) 조립.
    // - Start: 업그레이드 화면을 연다.
    // - Update: 화면 → 콘솔 순서로 한 프레임을 넘긴다.
    //
    // 이 브랜치(feature/노드트리)에는 노드 트리만 있다. 적·전투·업그레이드 효과는 feature/업그레이드연결에 있다.
    // 노드 트리는 노드 목록 에셋(NodeCatalog)에서 읽는다. 화면은 같은 에셋의 격자 칸으로 노드를 놓는다.
    // 화면 프리팹을 연결하지 않으면(Root Layer가 비어 있으면) 코드로 만든 임시 화면을 쓴다(PlaceholderScreens).
    // Presentation을 비워 두면 아무것도 바꾸지 않는 빈 Presentation을 쓴다.
    public sealed class GameHost : MonoBehaviour
    {
        // 로컬 Player. 지금은 1명이다(1명일 뿐 전역 Player가 아니다).
        private static readonly PlayerId LocalPlayer = new PlayerId(1);

        [Header("Content")]
        [SerializeField] private NodeCatalog nodeCatalog;

        [Header("UI Layers (비우면 임시 화면을 만든다)")]
        [SerializeField] private RectTransform rootLayer;
        [SerializeField] private RectTransform panelLayer;

        [Header("Registered Views")]
        [SerializeField] private UIBase[] views;

        [Header("Presentations (비우면 빈 Presentation)")]
        [SerializeField] private UIPresentationSpec upgradePresentation;

        [Header("UI Context")]
        [SerializeField] private string themeId = "Light";
        [SerializeField] private string localeId = "ko-KR";

        [Header("Runtime")]
        [SerializeField] private UIDisplayRefreshDriver displayRefreshDriver;

        private readonly List<UIPresentationSpec> _emptyPresentations = new List<UIPresentationSpec>();
        private ScreenFlow _flow;
        private UpgradeConsole _console;

        #region Unity 수명

        private void Awake()
        {
            if (!TryLoadNodeTree(out NodeTreeData layout, out NodeTree tree))
            {
                enabled = false;
                return;
            }

            // 진행 상태(Gold, 산 노드). 앱을 끄면 사라진다(저장은 F04).
            var player = new PlayerState(LocalPlayer);

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

            _flow = new ScreenFlow(ui, tree, layout, player, OrEmpty(upgradePresentation, "Upgrade"));

            if (displayRefreshDriver != null)
                displayRefreshDriver.Initialize(ui);

            // 업그레이드 콘솔은 개발용이다. 에디터와 개발 빌드에서만 만든다.
            if (Debug.isDebugBuild)
                _console = new UpgradeConsole(transform, player, tree);
        }

        private void Start() => _flow.OpenUpgradeScreen();

        private void Update()
        {
            _flow.Tick();
            _console?.Tick();
        }

        private void OnDestroy()
        {
            _console?.Dispose();
            _flow?.Dispose();

            foreach (UIPresentationSpec presentation in _emptyPresentations)
                Destroy(presentation);
        }

        #endregion

        #region 조립

        // 오류가 있는 노드 트리로는 시작하지 않는다. 모든 진단을 위치와 함께 남긴다.
        // 화면은 트리(규칙)와 함께 저작 데이터(격자 칸)도 받는다.
        private bool TryLoadNodeTree(out NodeTreeData layout, out NodeTree tree)
        {
            layout = null;
            tree = null;

            if (nodeCatalog == null)
            {
                Debug.LogError("[노드 트리] GameHost에 노드 목록(NodeCatalog)을 연결해야 한다.", this);
                return false;
            }

            layout = nodeCatalog.ToData();
            NodeTreeLoadResult result = NodeTreeLoader.Load(layout);

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
