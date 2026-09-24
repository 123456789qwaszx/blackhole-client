using System.Collections.Generic;
using BlackHole.Core;
using UnityEngine;

namespace BlackHole.Unity
{
    internal enum HudRequest { None, Upgrade, TogglePause, Stop, Restart }

    // 판 상태를 읽어 표시하고 버튼 요청을 돌려준다. 게임 상태와 판 수명을 직접 바꾸지 않는다.
    // 표시할 메시지(마지막 요청 결과)만 화면 상태로 가진다.
    // 강화와 스킬은 목록으로 그린다. 새 강화·스킬이 추가돼도 여기는 바뀌지 않는다.
    internal sealed class ReferenceHud
    {
        private string _message = string.Empty;

        public void Show(string message) => _message = message;

        // IMGUI는 한 프레임에 여러 번 그린다. 버튼 요청은 클릭 이벤트에서 한 번만 돌아온다.
        // Upgrade 요청이면 slot은 강화 목록의 칸 번호다.
        public HudRequest Draw(GameSession session, IReadOnlyList<string> upgradeKeys, out int slot)
        {
            HudRequest request = HudRequest.None;
            slot = -1;
            Playfield field = session.Field;

            GUILayout.BeginArea(new Rect(16, 16, 380, 420), GUI.skin.box);
            GUILayout.Label("BLACK HOLE / Architecture Reference");
            GUILayout.Label($"{session.Phase}  |  {session.Remaining:F1}s remaining");
            GUILayout.Label($"Mass {field.BlackHole.Mass}   Credits {field.Wallet.Credits}   Absorbed {field.BlackHole.AbsorbedCount}");
            GUILayout.Label($"Damage x{field.DamageMultiplier:F2}   Absorption radius {field.BlackHole.AbsorptionRadius:F2}");
            GUILayout.Space(8);
            GUILayout.Label("Mouse: aim   Number key: cast skill in that slot");
            GUILayout.Label("P: pause/resume   R: restart");
            for (int i = 0; i < field.Skills.Count; i++)
            {
                SkillState skill = field.Skills[i];
                GUILayout.Label($"{i + 1}  {skill.Definition.Id}: {skill.RemainingCooldown:F1}s");
            }
            GUILayout.Label("Cyan: shard   Orange: heavy   Gray: defeated");
            GUILayout.Label("Defeated targets fall inward. Absorption grants rewards.");
            GUILayout.Space(8);

            IReadOnlyList<UpgradeDefinition> upgrades = field.Upgrades.Definitions;
            for (int i = 0; i < upgrades.Count; i++)
            {
                UpgradeDefinition upgrade = upgrades[i];
                bool maxed = field.Upgrades.IsMaxed(upgrade.Id);
                string key = i < upgradeKeys.Count ? upgradeKeys[i] + ": " : string.Empty;
                string label = $"{key}{upgrade.Id} Lv.{field.Upgrades.Level(upgrade.Id)}/{upgrade.MaxLevel}" +
                               (maxed ? " (max)" : $" ({field.Upgrades.NextCost(upgrade.Id)} credits)");
                GUI.enabled = session.Phase == SessionPhase.Running && !maxed;
                if (GUILayout.Button(label))
                {
                    request = HudRequest.Upgrade;
                    slot = i;
                }
            }

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
