using System.Collections.Generic;
using BlackHole.Core;
using BlackHole.Sample;
using UnityEngine;

namespace BlackHole.Unity
{
    // Unity 수명과 한 프레임을 가진 진입점(조립 루트).
    // - Awake: 콘텐츠 로드·검증, UI(UIManager와 화면 4개) 조립, 화면 흐름 조립.
    // - Start: 타이틀 화면을 연다.
    // - Update: 화면 흐름에 프레임 시간을 넘긴다. 전투 시간은 전투 화면이 열려 있을 때만 흐른다.
    //
    // 화면 프리팹을 연결하지 않으면(Root Layer가 비어 있으면) 코드로 만든 임시 화면을 쓴다(PlaceholderScreens).
    // Presentation을 비워 두면 아무것도 바꾸지 않는 빈 Presentation을 쓴다.
    public sealed class GameHost : MonoBehaviour
    {
        // 판에 참가하는 로컬 Player. 지금은 1명이다(Players.Count == 1일 뿐 전역 Player가 아니다).
        private static readonly PlayerId[] LocalPlayers = { new PlayerId(1) };
        // 업그레이드 화면을 보는 로컬 Player.
        private static readonly PlayerId LocalViewer = LocalPlayers[0];

        [Header("UI Layers (비우면 임시 화면을 만든다)")]
        [SerializeField] private RectTransform rootLayer;
        [SerializeField] private RectTransform panelLayer;

        [Header("Registered Views")]
        [SerializeField] private UIBase[] views;

        [Header("Presentations (비우면 빈 Presentation)")]
        [SerializeField] private UIPresentationSpec titlePresentation;
        [SerializeField] private UIPresentationSpec settingsPresentation;
        [SerializeField] private UIPresentationSpec battlePresentation;
        [SerializeField] private UIPresentationSpec upgradePresentation;

        [Header("UI Context")]
        [SerializeField] private string themeId = "Light";
        [SerializeField] private string localeId = "ko-KR";

        [Header("Runtime")]
        [SerializeField] private UIDisplayRefreshDriver displayRefreshDriver;

        private readonly List<UIPresentationSpec> _emptyPresentations = new List<UIPresentationSpec>();
        private ScreenFlow _flow;

        #region Unity 수명

        private void Awake()
        {
            if (!TryLoadContent(out GameContent content))
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

            _flow = new ScreenFlow(
                ui,
                content,
                LocalPlayers,
                LocalViewer,
                OrEmpty(titlePresentation, "Title"),
                OrEmpty(settingsPresentation, "Settings"),
                OrEmpty(battlePresentation, "Battle"),
                OrEmpty(upgradePresentation, "Upgrade"));

            if (displayRefreshDriver != null)
                displayRefreshDriver.Initialize(ui);
        }

        private void Start() => _flow.OpenTitle();

        private void Update() => _flow.Tick(Time.deltaTime);

        private void OnDestroy()
        {
            _flow?.Dispose();

            foreach (UIPresentationSpec presentation in _emptyPresentations)
                Destroy(presentation);
        }

        #endregion

        #region 조립

        // 오류가 있는 콘텐츠로는 시작하지 않는다. 모든 진단을 위치와 함께 남긴다.
        private bool TryLoadContent(out GameContent content)
        {
            ContentLoadResult result = ContentLoader.Load(SampleContent.Create());

            foreach (ContentDiagnostic diagnostic in result.Diagnostics)
                Debug.LogError("[콘텐츠] " + diagnostic, this);

            content = result.Content;
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
