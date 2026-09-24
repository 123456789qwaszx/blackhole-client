using BlackHole.Core;
using BlackHole.Sample;
using UnityEngine;

namespace BlackHole.Unity
{
    // Unity 수명과 한 프레임의 순서를 가진 진입점(조립 루트).
    // - Awake: 콘텐츠 로드·검증, 화면·입력·HUD·판 시작 흐름 조립. 판은 만들지 않는다.
    // - OnEnable / OnDisable: 판 시작 / 종료. 최초 활성화와 재활성화가 같은 경로를 쓴다.
    //
    // 한 프레임(Update): 재시작 요청(있으면 새 판을 보이고 그 프레임 끝) → 일시정지 전환 → 진행 → 화면 갱신.
    // HUD 버튼 요청은 OnGUI, 곧 그 프레임의 진행 뒤에 적용된다.
    public sealed class GameHost : MonoBehaviour
    {
        // 판에 참가하는 로컬 Player. 지금은 1명이다(Players.Count == 1일 뿐 전역 Player가 아니다).
        private static readonly PlayerId[] LocalPlayers = { new PlayerId(1) };

        private WorldView _view;
        private HostInput _input;
        private Hud _hud;
        private SessionLauncher _launcher;

        #region Unity 수명

        private void Awake()
        {
            var presentation = new SamplePresentation();
            Camera camera = ConfigureCamera(presentation);
            _view = new WorldView(transform, camera, presentation);

            if (!TryLoadContent(out GameContent content))
            {
                enabled = false;
                return;
            }

            _input = new HostInput();
            _hud = new Hud();
            _launcher = new SessionLauncher(content, LocalPlayers, _view);
        }

        private void OnEnable()
        {
            _launcher?.StartNew();
        }

        private void OnDisable() => _launcher?.Stop();

        private void OnDestroy() => _view?.Dispose();

        #endregion

        #region 프레임

        private void Update()
        {
            _input.Read();
            if (_input.Restart)
            {
                _launcher.StartNew();
                return;
            }

            GameSession session = _launcher.Current;
            if (_input.TogglePause) session.TogglePause();

            session.Advance(Time.deltaTime);

            _view.Synchronize(session.World);
        }

        private void OnGUI()
        {
            switch (_hud.Draw(_launcher.Current))
            {
                case HudRequest.TogglePause: _launcher.Current.TogglePause(); break;
                case HudRequest.Stop: _launcher.Stop(); break;
                case HudRequest.Restart: _launcher.StartNew(); break;
            }
        }

        #endregion

        #region 조립

        private Camera ConfigureCamera(SamplePresentation presentation)
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                var cameraObject = new GameObject("Camera");
                cameraObject.transform.SetParent(transform);
                camera = cameraObject.AddComponent<Camera>();
                camera.tag = "MainCamera";
            }
            // 규칙 평면은 z = 0. x, y는 WorldView가 HQ 위치에 맞춘다.
            camera.transform.position = new Vector3(0, 0, -10);
            camera.orthographic = true;
            camera.orthographicSize = presentation.CameraSize;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = presentation.Background;
            return camera;
        }

        // 오류가 있는 콘텐츠로는 판을 시작하지 않는다. 모든 진단을 위치와 함께 남긴다.
        private bool TryLoadContent(out GameContent content)
        {
            ContentLoadResult result = ContentLoader.Load(SampleContent.Create());
            foreach (ContentDiagnostic diagnostic in result.Diagnostics)
                Debug.LogError("[콘텐츠] " + diagnostic, this);
            content = result.Content;
            return result.Succeeded;
        }

        #endregion
    }
}
