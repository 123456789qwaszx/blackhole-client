using BlackHole.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BlackHole.Unity
{
    // Unity 입력과 수명을 게임 요청으로 연결하는 진입점.
    public sealed class ReferenceGameController : MonoBehaviour
    {
        private ContentCatalog _catalog;
        private GameSession _session;
        private ReferenceWorldView _view;
        private Camera _camera;
        private string _message = "Aim at a target. Press 1 / 2 to cast.";
        private Vector2 _lastAim;
        private float _flashRemaining;
        private float _flashRadius;

        private void Awake()
        {
            _camera = Camera.main;
            if (_camera == null)
            {
                var cameraObject = new GameObject("Reference Camera");
                cameraObject.transform.SetParent(transform);
                _camera = cameraObject.AddComponent<Camera>();
                _camera.tag = "MainCamera";
            }
            _camera.transform.position = new Vector3(0, 0, -10);
            _camera.orthographic = true;
            _camera.orthographicSize = 7.5f;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.025f, 0.035f, 0.065f);
            _view = new ReferenceWorldView(transform);

            ContentLoadResult content = ContentLoader.Load(ReferenceGame.CreateContent());
            if (!content.Succeeded)
            {
                foreach (ContentDiagnostic diagnostic in content.Diagnostics)
                    Debug.LogError("[콘텐츠] " + diagnostic, this);
                enabled = false;
                return;
            }
            _catalog = content.Catalog;
            Restart();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
            {
                Restart();
                return;
            }
            if (keyboard != null && keyboard.pKey.wasPressedThisFrame) _session.TogglePause();

            _session.Advance(Time.deltaTime);

            if (keyboard != null)
            {
                if (keyboard.digit1Key.wasPressedThisFrame) Cast(ReferenceGame.StrikeId);
                if (keyboard.digit2Key.wasPressedThisFrame) Cast(ReferenceGame.PulseId);
                if (keyboard.uKey.wasPressedThisFrame) Upgrade();
            }

            _flashRemaining = Mathf.Max(0, _flashRemaining - Time.deltaTime);
            _view.Synchronize(_session.Field, _lastAim, _flashRadius, _flashRemaining > 0);
        }

        private void Cast(string id)
        {
            Mouse mouse = Mouse.current;
            if (mouse == null) return;
            Vector2 screen = mouse.position.ReadValue();
            Vector3 world = _camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 10));
            CastResult result = _session.TryCast(id, new Point2(world.x, world.y));
            _message = id + ": " + result;
            if (result != CastResult.Cast) return;
            _lastAim = world;
            _flashRadius = FindSkillRadius(id);
            _flashRemaining = 0.18f;
        }

        // 연출 반경을 정의에서 읽는다. 실제 적용 범위를 발동 결과로 받는 것은 S4에서 한다.
        private float FindSkillRadius(string id)
        {
            foreach (SkillState skill in _session.Field.Skills)
                if (skill.Definition.Id == id) return skill.Definition.Radius;
            return 0;
        }

        private void Upgrade() => _message = "Upgrade: " + _session.TryUpgrade();

        private void Restart()
        {
            _session?.Stop();
            _session = SessionAssembler.Create(_catalog);
            _view.ClearTargets();
            _flashRemaining = 0;
            _message = "New session. Aim with mouse, cast with 1 / 2.";
            _view.Synchronize(_session.Field, Vector2.zero, 0, false);
        }

        private void OnDisable() { _session?.Stop(); }

        private void OnDestroy() { _view?.Dispose(); }

        private void OnGUI()
        {
            if (_session == null) return;
            GrowthState growth = _session.Field.Growth;
            GUILayout.BeginArea(new Rect(16, 16, 360, 365), GUI.skin.box);
            GUILayout.Label("BLACK HOLE / Architecture Reference");
            GUILayout.Label($"{_session.Phase}  |  {_session.Remaining:F1}s remaining");
            GUILayout.Label($"Mass {growth.Mass}   Credits {growth.Credits}   Absorbed {growth.AbsorbedCount}");
            GUILayout.Label($"Power Lv.{growth.PowerLevel}   Damage x{growth.DamageMultiplier:F2}");
            GUILayout.Space(8);
            GUILayout.Label("Mouse: aim   1: focused strike   2: gravity pulse");
            GUILayout.Label("U: upgrade   P: pause/resume   R: restart");
            foreach (SkillState skill in _session.Field.Skills)
                GUILayout.Label($"{skill.Definition.Id}: {skill.RemainingCooldown:F1}s");
            GUILayout.Label("Cyan: shard   Orange: heavy   Gray: defeated");
            GUILayout.Label("Defeated targets fall inward. Absorption grants rewards.");
            GUILayout.Space(8);
            GUI.enabled = _session.Phase == SessionPhase.Running && !growth.IsMaxLevel;
            if (GUILayout.Button(growth.IsMaxLevel ? "Power maxed" : $"Upgrade power ({growth.NextUpgradeCost} credits)"))
                Upgrade();
            GUI.enabled = _session.Phase != SessionPhase.Ended;
            if (GUILayout.Button(_session.Phase == SessionPhase.Paused ? "Resume" : "Pause")) _session.TogglePause();
            if (GUILayout.Button("End session")) _session.Stop();
            GUI.enabled = true;
            if (GUILayout.Button("Restart")) Restart();
            GUILayout.Label(_message);
            if (_session.Result != null)
                GUILayout.Label($"Result: {_session.Result.Reason}, mass {_session.Result.Mass}");
            GUILayout.EndArea();
        }
    }
}
