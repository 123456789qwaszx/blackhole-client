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
            Camera camera = ConfigureCamera();
            _view = new ReferenceWorldView(transform);

            if (!TryLoadContent(out ContentCatalog catalog))
            {
                enabled = false;
                return;
            }

            _input = new ReferenceInput(camera);
            _hud = new ReferenceHud();
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
            if (_input.Upgrade) Upgrade();

            _view.Synchronize(session.Field, Time.deltaTime);
        }

        private void OnGUI()
        {
            switch (_hud.Draw(_launcher.Current))
            {
                case HudRequest.Upgrade: Upgrade(); break;
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

            SkillDefinition skill = skills[slot].Definition;
            CastResult result = session.TryCast(skill.Id, aim);
            _hud.Show(skill.Id + ": " + result);
            // 연출 반경은 정의에서 읽는다. 실제 적용 범위를 발동 결과로 받는 것은 S4에서 한다.
            if (result == CastResult.Cast) _view.ShowCast(aim, skill.Radius);
        }

        private void Upgrade() => _hud.Show("Upgrade: " + _launcher.Current.TryUpgrade());

        #endregion

        #region 조립

        private Camera ConfigureCamera()
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
            camera.orthographicSize = 7.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.035f, 0.065f);
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
