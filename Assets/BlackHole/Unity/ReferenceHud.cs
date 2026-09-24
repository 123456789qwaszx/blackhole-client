using System.Collections.Generic;
using BlackHole.Core;
using UnityEngine;

namespace BlackHole.Unity
{
    internal enum HudRequest { None, Upgrade, AcquireNode, TogglePause, Stop, Restart }

    // 판 상태를 읽어 표시하고 버튼 요청을 돌려준다. 게임 상태와 판 수명을 직접 바꾸지 않는다.
    // 표시할 메시지(마지막 요청 결과)만 화면 상태로 가진다.
    // 스킬·강화·트리 노드는 목록으로 그린다. 새 항목이 추가돼도 여기는 바뀌지 않는다.
    internal sealed class ReferenceHud
    {
        private string _message = string.Empty;

        public void Show(string message) => _message = message;

        // 강화 목록의 칸 순서: 트리 노드가 참조하지 않는 강화만, 정의 순서대로. 키 입력도 이 순서를 쓴다.
        // 트리 강화는 트리 목록에서 요청한다(판정은 어느 경로든 같다).
        public static string DirectUpgradeAt(Playfield field, int slot)
        {
            int index = 0;
            foreach (UpgradeDefinition upgrade in field.Upgrades.Definitions)
            {
                if (field.SkillTree.References(upgrade.Id)) continue;
                if (index++ == slot) return upgrade.Id;
            }
            return null;
        }

        // IMGUI는 한 프레임에 여러 번 그린다. 버튼 요청은 클릭 이벤트에서 한 번만 돌아온다.
        // target: Upgrade면 강화 ID, AcquireNode면 노드 ID.
        public HudRequest Draw(GameSession session, IReadOnlyList<string> upgradeKeys, out string target)
        {
            HudRequest request = HudRequest.None;
            target = null;
            Playfield field = session.Field;
            bool running = session.Phase == SessionPhase.Running;

            GUILayout.BeginArea(new Rect(16, 16, 400, 560), GUI.skin.box);
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
            GUILayout.Label("Upgrades");
            for (int slot = 0; ; slot++)
            {
                string id = DirectUpgradeAt(field, slot);
                if (id == null) break;
                string key = slot < upgradeKeys.Count ? upgradeKeys[slot] + ": " : string.Empty;
                if (Button(key + UpgradeLabel(field, id), running && !field.Upgrades.IsMaxed(id)))
                {
                    request = HudRequest.Upgrade;
                    target = id;
                }
            }

            IReadOnlyList<SkillTreeNodeDefinition> nodes = field.SkillTree.Definition.Nodes;
            if (nodes.Count > 0)
            {
                GUILayout.Space(4);
                GUILayout.Label("Skill tree (requires all listed nodes)");
                foreach (SkillTreeNodeDefinition node in nodes)
                {
                    NodeStatus status = field.SkillTree.Status(node.Id);
                    string requires = node.Requires.Count == 0 ? "root" : "needs " + string.Join(" + ", node.Requires);
                    string label = $"{node.Id} [{status}] {requires} — {UpgradeLabel(field, node.UpgradeId)}";
                    if (Button(label, running && status == NodeStatus.Available))
                    {
                        request = HudRequest.AcquireNode;
                        target = node.Id;
                    }
                }
            }

            GUILayout.Space(8);
            if (Button(session.Phase == SessionPhase.Paused ? "Resume" : "Pause", session.Phase != SessionPhase.Ended))
                request = HudRequest.TogglePause;
            if (Button("End session", session.Phase != SessionPhase.Ended))
                request = HudRequest.Stop;
            if (Button("Restart", true))
                request = HudRequest.Restart;

            GUILayout.Label(_message);
            if (session.Result != null)
                GUILayout.Label($"Result: {session.Result.Reason}, mass {session.Result.Mass}");
            GUILayout.EndArea();
            return request;
        }

        private static string UpgradeLabel(Playfield field, string id)
        {
            UpgradeState upgrades = field.Upgrades;
            int max = 0;
            foreach (UpgradeDefinition upgrade in upgrades.Definitions)
                if (upgrade.Id == id) max = upgrade.MaxLevel;
            string cost = upgrades.IsMaxed(id) ? "max" : upgrades.NextCost(id) + " credits";
            return $"{id} Lv.{upgrades.Level(id)}/{max} ({cost})";
        }

        private static bool Button(string label, bool enabled)
        {
            GUI.enabled = enabled;
            bool clicked = GUILayout.Button(label);
            GUI.enabled = true;
            return clicked;
        }
    }
}
