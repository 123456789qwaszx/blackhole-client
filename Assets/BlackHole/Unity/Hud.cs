using System.Collections.Generic;
using BlackHole.Core;
using UnityEngine;

namespace BlackHole.Unity
{
    internal enum HudRequest { None, TogglePause, Stop, NewRun }

    internal enum ShopRequestKind { None, Purchase, NextBattle, NewRun }

    // 구매 화면의 요청. 구매일 때만 State와 NodeId가 있다. 노드 정의는 넘기지 않는다 — 게임이 ID로 찾는다.
    internal readonly struct ShopRequest
    {
        public ShopRequestKind Kind { get; }
        public PlayerState State { get; }
        public string NodeId { get; }

        public ShopRequest(
            ShopRequestKind kind,
            PlayerState state = null,
            string nodeId = null)
        {
            Kind = kind;
            State = state;
            NodeId = nodeId;
        }
    }

    // 판 상태를 읽어 표시하고 버튼 요청을 돌려준다. 게임 상태와 판 수명을 직접 바꾸지 않는다.
    // 테스트용 화면(IMGUI)이다. 최종 UI가 아니다. 공간 배치 트리 화면은 별도 작업이다(M6).
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
            GUILayout.Label(Growth(session.World.Hq));
            foreach (Player player in session.World.Players)
                GUILayout.Label($"{player.Id}  Gold {player.State.Gold}  aim {(player.AimPoint.HasValue ? Format(player.AimPoint.Value) : "none")}  ticks {Ticks(player)}");
            GUILayout.Label("Mouse: aim   P: pause/resume   R: new run");
            GUILayout.Space(8);

            GUI.enabled = session.Phase != SessionPhase.Ended;
            if (GUILayout.Button(session.Phase == SessionPhase.Paused ? "Resume" : "Pause"))
                request = HudRequest.TogglePause;
            if (GUILayout.Button("End session"))
                request = HudRequest.Stop;
            GUI.enabled = true;
            if (GUILayout.Button("New run"))
                request = HudRequest.NewRun;

            GUILayout.EndArea();
            return request;
        }

        // 전투 밖 구매 화면. 노드는 콘텐츠 순서로, 선행 노드 깊이만큼 들여 쓴다.
        public ShopRequest DrawShop(
            GameSession finished,
            IReadOnlyList<PlayerState> progress,
            IReadOnlyList<UpgradeNodeDefinition> nodes)
        {
            var request = new ShopRequest(ShopRequestKind.None);

            GUILayout.BeginArea(new Rect(16, 16, 460, 520), GUI.skin.box);
            GUILayout.Label("UPGRADES / between battles");
            GUILayout.Label($"Battle over: {finished.Result.Reason}, {finished.Result.PlayedSeconds:F1}s, {Growth(finished.World.Hq)}");
            GUILayout.Space(6);

            foreach (PlayerState state in progress)
            {
                GUILayout.Label($"{state.Id}  Gold {state.Gold}");

                foreach (UpgradeNodeDefinition node in nodes)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(12 + 16 * Depth(node, nodes));
                    GUILayout.Label($"{node.Id}: {Describe(node)}", GUILayout.Width(300));

                    PurchaseResult check = UpgradePurchase.Check(state, node);
                    GUI.enabled = check == PurchaseResult.Purchased;

                    if (GUILayout.Button(ButtonText(check, node), GUILayout.Width(110)))
                        request = new ShopRequest(ShopRequestKind.Purchase, state, node.Id);

                    GUI.enabled = true;
                    GUILayout.EndHorizontal();
                }

                GUILayout.Space(6);
            }

            if (GUILayout.Button("Next battle"))
                request = new ShopRequest(ShopRequestKind.NextBattle);
            if (GUILayout.Button("New run (R)"))
                request = new ShopRequest(ShopRequestKind.NewRun);

            GUILayout.EndArea();
            return request;
        }

        private static string ButtonText(PurchaseResult check, UpgradeNodeDefinition node)
        {
            switch (check)
            {
                case PurchaseResult.Purchased: return $"Buy {node.Price}G";
                case PurchaseResult.AlreadyOwned: return "Owned";
                case PurchaseResult.MissingPrerequisite: return "Locked";
                case PurchaseResult.NotEnoughGold: return $"{node.Price}G";
                default: return check.ToString();
            }
        }

        // 효과 설명은 표현의 일이다. 종류 이름을 짧은 영문으로 옮긴다(IMGUI 기본 글꼴).
        private static string Describe(UpgradeNodeDefinition node)
        {
            var parts = new List<string>();

            foreach (UpgradeEffect effect in node.Effects)
            {
                string target = effect.Skill?.Id ?? effect.Enemy?.Id ?? "all";

                switch (effect.Kind)
                {
                    case UpgradeEffectKind.SkillDamageAdd: parts.Add($"damage +{effect.Value} ({target})"); break;
                    case UpgradeEffectKind.SkillRadiusAdd: parts.Add($"radius +{effect.Value} ({target})"); break;
                    case UpgradeEffectKind.SkillIntervalMultiply: parts.Add($"interval x{effect.Value} ({target})"); break;
                    case UpgradeEffectKind.EnemyHealthMultiply: parts.Add($"enemy HP x{effect.Value} ({target})"); break;
                    case UpgradeEffectKind.GoldMultiply: parts.Add($"gold x{effect.Value} ({target})"); break;
                    case UpgradeEffectKind.HqExpMultiply: parts.Add($"HQ EXP x{effect.Value} ({target})"); break;
                    case UpgradeEffectKind.GrowthSupplyAdd: parts.Add($"growth supply +{effect.Value} ({target})"); break;
                    case UpgradeEffectKind.SkillUnlock: parts.Add($"unlock {target}"); break;
                    case UpgradeEffectKind.LaserDamageAdd: parts.Add($"damage +{effect.Value} ({target})"); break;
                    case UpgradeEffectKind.LaserIntervalMultiply: parts.Add($"interval x{effect.Value} ({target})"); break;
                    case UpgradeEffectKind.LaserWidthAdd: parts.Add($"width +{effect.Value} ({target})"); break;
                    default: parts.Add(effect.Kind.ToString()); break;
                }
            }

            return string.Join(", ", parts);
        }

        private static int Depth(UpgradeNodeDefinition node, IReadOnlyList<UpgradeNodeDefinition> nodes)
        {
            int depth = 0;
            string requires = node.Requires;

            while (requires != null && depth < nodes.Count)
            {
                depth++;
                requires = Find(requires, nodes)?.Requires;
            }

            return depth;
        }

        private static UpgradeNodeDefinition Find(string id, IReadOnlyList<UpgradeNodeDefinition> nodes)
        {
            foreach (UpgradeNodeDefinition node in nodes)
            {
                if (node.Id == id)
                    return node;
            }

            return null;
        }

        private static string Growth(Hq hq) =>
            hq.NextLevelExp.HasValue
                ? $"HQ Lv {hq.Level}   EXP {hq.Exp} / {hq.NextLevelExp.Value}"
                : $"HQ Lv {hq.Level} (max)   EXP {hq.Exp}";

        private static string Format(Point2 point) => $"({point.X:F1}, {point.Y:F1})";

        private static int Ticks(Player player)
        {
            int ticks = 0;
            foreach (PassiveSkill skill in player.Skills)
                if (skill is BreakerSkill breaker) ticks += breaker.TickCount;
            return ticks;
        }
    }
}
