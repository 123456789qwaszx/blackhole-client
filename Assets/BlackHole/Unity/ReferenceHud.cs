using BlackHole.Core;
using UnityEngine;

namespace BlackHole.Unity
{
    internal enum HudRequest { None, Upgrade, TogglePause, Stop, Restart }

    // 판 상태를 읽어 표시하고 버튼 요청을 돌려준다. 게임 상태와 판 수명을 직접 바꾸지 않는다.
    // 표시할 메시지(마지막 요청 결과)만 화면 상태로 가진다.
    internal sealed class ReferenceHud
    {
        private string _message = string.Empty;

        public void Show(string message) => _message = message;

        // IMGUI는 한 프레임에 여러 번 그린다. 버튼 요청은 클릭 이벤트에서 한 번만 돌아온다.
        public HudRequest Draw(GameSession session)
        {
            HudRequest request = HudRequest.None;
            GrowthState growth = session.Field.Growth;

            GUILayout.BeginArea(new Rect(16, 16, 360, 365), GUI.skin.box);
            GUILayout.Label("BLACK HOLE / Architecture Reference");
            GUILayout.Label($"{session.Phase}  |  {session.Remaining:F1}s remaining");
            GUILayout.Label($"Mass {growth.Mass}   Credits {growth.Credits}   Absorbed {growth.AbsorbedCount}");
            GUILayout.Label($"Power Lv.{growth.PowerLevel}   Damage x{growth.DamageMultiplier:F2}");
            GUILayout.Space(8);
            GUILayout.Label("Mouse: aim   Number key: cast skill in that slot");
            GUILayout.Label("U: upgrade   P: pause/resume   R: restart");
            for (int i = 0; i < session.Field.Skills.Count; i++)
            {
                SkillState skill = session.Field.Skills[i];
                GUILayout.Label($"{i + 1}  {skill.Definition.Id}: {skill.RemainingCooldown:F1}s");
            }
            GUILayout.Label("Cyan: shard   Orange: heavy   Gray: defeated");
            GUILayout.Label("Defeated targets fall inward. Absorption grants rewards.");
            GUILayout.Space(8);

            GUI.enabled = session.Phase == SessionPhase.Running && !growth.IsMaxLevel;
            if (GUILayout.Button(growth.IsMaxLevel ? "Power maxed" : $"Upgrade power ({growth.NextUpgradeCost} credits)"))
                request = HudRequest.Upgrade;
            GUI.enabled = session.Phase != SessionPhase.Ended;
            if (GUILayout.Button(session.Phase == SessionPhase.Paused ? "Resume" : "Pause"))
                request = HudRequest.TogglePause;
            if (GUILayout.Button("End session"))
                request = HudRequest.Stop;
            GUI.enabled = true;
            if (GUILayout.Button("Restart"))
                request = HudRequest.Restart;

            GUILayout.Label(_message);
            if (session.Result != null)
                GUILayout.Label($"Result: {session.Result.Reason}, mass {session.Result.Mass}");
            GUILayout.EndArea();
            return request;
        }
    }
}
