using BlackHole.Core;
using BlackHole.Sample;
using UnityEngine;

namespace BlackHole.Unity
{
    // Unity 수명과 한 프레임의 순서를 가진 진입점(조립 루트).
    // - Awake: 콘텐츠 로드·검증, 화면·소리·입력·HUD·판 시작 흐름 조립. 판은 만들지 않는다.
    // - OnEnable / OnDisable: 새 진행 시작 / 전투 종료. 최초 활성화와 재활성화가 같은 경로를 쓴다.
    //
    // 한 프레임(Update): 입력 읽기 → 새 진행 요청(R, 있으면 그 프레임 끝) → 구매 화면이면 끝
    //   → 일시정지 전환 → AimPoint 갱신 → 진행 → 전투가 끝났으면 구매 화면으로 → 화면·소리 갱신.
    // HUD 버튼 요청(구매, 다음 전투, 전투 끝내기)은 OnGUI, 곧 그 프레임의 진행 뒤에 적용된다.
    public sealed class GameHost : MonoBehaviour
    {
        // 판에 참가하는 로컬 Player. 지금은 1명이다(Players.Count == 1일 뿐 전역 Player가 아니다).
        private static readonly PlayerId[] LocalPlayers = { new PlayerId(1) };
        // 마우스 조준이 채우는 AimPoint의 주인. 마우스와 Player를 묶는 것은 이 호스트의 배선이다(Player는 마우스를 모른다).
        private static readonly PlayerId MouseAimPlayer = LocalPlayers[0];

        private WorldView _view;
        private BattleAudio _audio;
        private HostInput _input;
        private Hud _hud;
        private SessionLauncher _launcher;

        #region Unity 수명

        private void Awake()
        {
            var presentation = new SamplePresentation();
            Camera camera = ConfigureCamera(presentation);
            _view = new WorldView(transform, camera, presentation);
            _audio = new BattleAudio(transform, new SampleSounds());

            if (!TryLoadContent(out GameContent content))
            {
                enabled = false;
                return;
            }

            _input = new HostInput(camera);
            _hud = new Hud();
            _launcher = new SessionLauncher(content, LocalPlayers, _view, _audio);
        }

        private void OnEnable()
        {
            _launcher?.StartNew();
        }

        private void OnDisable() => _launcher?.Stop();

        private void OnDestroy()
        {
            _view?.Dispose();
            _audio?.Dispose();
        }

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

            // 구매 화면에는 진행 중인 전투가 없다.
            if (_launcher.InShop)
                return;

            GameSession session = _launcher.Current;
            
            if (_input.TogglePause) 
                session.TogglePause();
            
            session.SetAimPoint(MouseAimPlayer, _input.Aim);

            session.Advance(Time.deltaTime);

            // 시간이 끝났거나 "End session"으로 끝난 전투는 정리하고 구매 화면으로 간다.
            _launcher.OpenShopIfEnded();

            if (!_launcher.InShop)
            {
                _view.Synchronize(session.World);
                _audio.Synchronize(session);
            }
        }

        private void OnGUI()
        {
            if (_launcher.InShop)
            {
                DrawShop();
                return;
            }

            switch (_hud.Draw(_launcher.Current))
            {
                case HudRequest.TogglePause: 
                    _launcher.Current.TogglePause();
                    break;
                
                case HudRequest.Stop: 
                    _launcher.Stop();
                    break;
                
                case HudRequest.NewRun: 
                    _launcher.StartNew();
                    break;
            }
        }

        private void DrawShop()
        {
            ShopRequest request = _hud.DrawShop(_launcher.Current, _launcher.Progress, _launcher.Upgrades);

            switch (request.Kind)
            {
                case ShopRequestKind.Purchase:
                    _launcher.Purchase(request.State, request.NodeId);
                    break;

                case ShopRequestKind.NextBattle:
                    _launcher.NextBattle();
                    break;

                case ShopRequestKind.NewRun:
                    _launcher.StartNew();
                    break;
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
                cameraObject.AddComponent<AudioListener>();
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
            ContentLoadResult result = 
                ContentLoader.Load(SampleContent.Create());
            
            foreach (ContentDiagnostic diagnostic in result.Diagnostics)
                Debug.LogError("[콘텐츠] " + diagnostic, this);
            
            content = result.Content;
            
            return result.Succeeded;
        }

        #endregion
    }
}
