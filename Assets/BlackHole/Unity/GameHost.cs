using System.Collections.Generic;
using BlackHole.Core;
using UnityEngine;

namespace BlackHole.Unity
{
    // Unity 수명과 한 프레임을 가진 진입점(조립 루트).
    // - Awake: 노드 트리 로드·검증, 진행 상태, UI(UIManager), 화면 흐름 조립.
    //
    // 이 브랜치(feature/노드트리)에는 노드 트리만 있다. 적·전투·업그레이드는 feature/업그레이드연결에 있다.
    // 노드 트리는 노드 목록 에셋(NodeCatalog)에서 읽는다.
    // 화면 프리팹을 연결하지 않으면(Root Layer가 비어 있으면) 코드로 만든 임시 화면을 쓴다(PlaceholderScreens).
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

        [Header("UI Context")]
        [SerializeField] private string themeId = "Light";
        [SerializeField] private string localeId = "ko-KR";

        [Header("Runtime")]
        [SerializeField] private UIDisplayRefreshDriver displayRefreshDriver;

        private ScreenFlow _flow;

        #region Unity 수명

        private void Awake()
        {
            if (!TryLoadNodeTree(out NodeTree _))
            {
                enabled = false;
                return;
            }

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

            _flow = new ScreenFlow(ui);

            if (displayRefreshDriver != null)
                displayRefreshDriver.Initialize(ui);
        }

        private void OnDestroy() => _flow?.Dispose();

        #endregion

        #region 조립

        // 오류가 있는 노드 트리로는 시작하지 않는다. 모든 진단을 위치와 함께 남긴다.
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

        #endregion
    }
}
