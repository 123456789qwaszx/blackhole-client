using BlackHole.Core;
using UnityEngine;

namespace BlackHole.Unity
{
    internal enum HudRequest { None, TogglePause, Stop, Restart }

    // 판 상태를 읽어 표시하고 버튼 요청을 돌려준다. 게임 상태와 판 수명을 직접 바꾸지 않는다.
    // 테스트용 화면(IMGUI)이다. 최종 UI가 아니다.
    internal sealed class Hud
    {
        // IMGUI는 한 프레임에 여러 번 그린다. 버튼 요청은 클릭 이벤트에서 한 번만 돌아온다.
        public HudRequest Draw(GameSession session)
        {
            HudRequest request = HudRequest.None;

            GUILayout.BeginArea(new Rect(16, 16, 340, 270), GUI.skin.box);
            GUILayout.Label("BLACK HOLE / Reference v2");
            GUILayout.Label($"{session.Phase}  |  {session.Remaining:F1}s remaining");
            GUILayout.Label($"Players {session.World.Players.Count}   Enemies {session.World.Enemies.Count}");
            GUILayout.Label($"HQ EXP {session.World.Hq.Exp}");
            foreach (Player player in session.World.Players)
                GUILayout.Label($"{player.Id}  Gold {player.State.Gold}  aim {(player.AimPoint.HasValue ? Format(player.AimPoint.Value) : "none")}  ticks {Ticks(player)}");
            GUILayout.Label("Mouse: aim   P: pause/resume   R: restart");
            GUILayout.Space(8);

            GUI.enabled = session.Phase != SessionPhase.Ended;
            if (GUILayout.Button(session.Phase == SessionPhase.Paused ? "Resume" : "Pause"))
                request = HudRequest.TogglePause;
            if (GUILayout.Button("End session"))
                request = HudRequest.Stop;
            GUI.enabled = true;
            if (GUILayout.Button("Restart"))
                request = HudRequest.Restart;

            if (session.Result != null)
                GUILayout.Label($"Result: {session.Result.Reason}, {session.Result.PlayedSeconds:F1}s");
            GUILayout.EndArea();
            return request;
        }

        private static string Format(Point2 point) => $"({point.X:F1}, {point.Y:F1})";

        private static int Ticks(Player player)
        {
            int ticks = 0;
            foreach (PassiveSkill skill in player.Skills) ticks += skill.TickCount;
            return ticks;
        }
    }
}
