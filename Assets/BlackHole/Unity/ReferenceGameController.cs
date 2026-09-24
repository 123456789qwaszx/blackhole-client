using System.Collections.Generic;
using BlackHole.Core;
using UnityEngine;

namespace BlackHole.Unity
{
    // Unity 수명과 한 프레임의 순서를 소유하는 진입점(조립 루트).
    // - Awake: 콘텐츠 로드·검증, 화면·입력·HUD·판 시작 흐름 조립. 판은 만들지 않는다.
    // - OnEnable / OnDisable: 판 시작 / 종료. 최초 활성화와 재활성화가 같은 경로를 쓴다.
    //
    // 한 프레임(Update):
    //   재시작 요청(있으면 새 판을 보이고 그 프레임 끝) → 일시정지 전환 → 진행 → 시전·강화 요청 → 화면 갱신.
    //   요청은 진행 뒤에 적용한다. 그 진행에서 판이 끝났다면 Session이 SessionInactive로 거절한다.
    //   HUD 버튼 요청은 OnGUI, 곧 그 프레임의 진행 뒤에 같은 요청 경로로 적용된다.
    public sealed class ReferenceGameController : MonoBehaviour
    {
        private ReferenceWorldView _view;
        private ReferenceInput _input;
        private ReferenceHud _hud;
        private SessionLauncher _launcher;

        #region Unity 수명

        private void Awake()
        {
            ReferencePresentation presentation = ReferencePresentation.CreateSample();
            Camera camera = ConfigureCamera(presentation);
            _view = new ReferenceWorldView(transform, presentation);

            if (!TryLoadContent(out ContentCatalog catalog))
            {
                enabled = false;
                return;
            }
            presentation.Report(catalog, message => Debug.LogWarning("[표현] " + message, this));

            _input = new ReferenceInput(camera);
            _hud = new ReferenceHud(presentation, catalog);
            _launcher = new SessionLauncher(catalog, _view);
        }

        private void OnEnable()
        {
            if (_launcher != null) Restart();
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
                Restart();
                return;
            }

            GameSession session = _launcher.Current;
            if (_input.TogglePause) session.TogglePause();

            session.Advance(Time.deltaTime);

            foreach (int slot in _input.CastSlots) Cast(slot, _input.Aim);
            foreach (int slot in _input.UpgradeSlots) Upgrade(ReferenceHud.DirectUpgradeAt(session.Field, slot));

            _view.Synchronize(session.Field, Time.deltaTime);
        }

        private void OnGUI()
        {
            switch (_hud.Draw(_launcher.Current, ReferenceInput.UpgradeKeyNames, out string target))
            {
                case HudRequest.Upgrade: Upgrade(target); break;
                case HudRequest.AcquireNode: AcquireNode(target); break;
                case HudRequest.TogglePause: _launcher.Current.TogglePause(); break;
                case HudRequest.Stop: _launcher.Stop(); break;
                case HudRequest.Restart: Restart(); break;
            }
        }

        #endregion

        #region 요청 (키보드·HUD 공통)

        private void Restart()
        {
            _launcher.StartNew();
            _hud.Show("New session. Aim with mouse, cast with number keys.");
        }

        private void Cast(int slot, Point2 aim)
        {
            GameSession session = _launcher.Current;
            IReadOnlyList<SkillState> skills = session.Field.Skills;
            if (slot >= skills.Count) return;

            string id = skills[slot].Definition.Id;
            CastResult result = session.TryCast(id, aim, out CastReport report);
            _hud.Show(id + ": " + result);
            // 연출은 실제로 적용된 내용(발동 결과)으로 한다. 스킬 수치를 다시 읽지 않는다.
            if (result == CastResult.Cast) _view.ShowCast(report.Aim, report.AreaRadius);
        }

        // 비어 있는 칸(null)은 무시한다.
        private void Upgrade(string id)
        {
            if (id == null) return;
            _hud.Show(id + ": " + _launcher.Current.TryPurchaseUpgrade(id));
        }

        private void AcquireNode(string nodeId) =>
            _hud.Show(nodeId + ": " + _launcher.Current.TryAcquireNode(nodeId));

        #endregion

        #region 조립

        private Camera ConfigureCamera(ReferencePresentation presentation)
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                var cameraObject = new GameObject("Reference Camera");
                cameraObject.transform.SetParent(transform);
                camera = cameraObject.AddComponent<Camera>();
                camera.tag = "MainCamera";
            }
            camera.transform.position = new Vector3(0, 0, -10);
            camera.orthographic = true;
            camera.orthographicSize = presentation.CameraSize;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = presentation.Background;
            return camera;
        }

        // 오류가 있는 콘텐츠로는 판을 시작하지 않는다. 모든 진단을 위치와 함께 남긴다.
        private bool TryLoadContent(out ContentCatalog catalog)
        {
            ContentLoadResult content = ContentLoader.Load(ReferenceGame.CreateContent());
            foreach (ContentDiagnostic diagnostic in content.Diagnostics)
                Debug.LogError("[콘텐츠] " + diagnostic, this);
            catalog = content.Catalog;
            return content.Succeeded;
        }

        #endregion
    }
}
