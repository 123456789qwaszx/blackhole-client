using System;
using System.Collections.Generic;
using BlackHole.Core;
using UnityEngine;

namespace BlackHole.Unity
{
    // 대상 한 종류의 외형. 게임 규칙이 아니며 Core는 이것을 모른다.
    internal sealed class TargetAppearance
    {
        public Color Color { get; }
        public float Size { get; }
        // HUD 범례에 쓰는 이름.
        public string ColorName { get; }

        public TargetAppearance(Color color, float size, string colorName)
        {
            if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size), "크기는 양수여야 한다.");
            Color = color;
            Size = size;
            ColorName = colorName ?? string.Empty;
        }
    }

    // 표현 정의: 콘텐츠 ID별 외형과 화면 설정. 화면·HUD는 종류별 분기 대신 여기서 읽는다.
    // 콘텐츠 ID로 연결되지만 콘텐츠 정의가 아니다 — 외형이 없는 대상도 기본 외형으로 플레이할 수 있다.
    // 지금은 코드 샘플이다. Unity에서 편집할 필요가 생기면 콘텐츠 SO와 같은 시점에 ScriptableObject로 옮긴다.
    internal sealed class ReferencePresentation
    {
        private readonly Dictionary<string, TargetAppearance> _targets =
            new Dictionary<string, TargetAppearance>(StringComparer.Ordinal);

        public float CameraSize { get; } = 7.5f;
        public Color Background { get; } = new Color(0.025f, 0.035f, 0.065f);
        public Color HoleColor { get; } = new Color(0.005f, 0.005f, 0.012f);
        public Color HaloColor { get; } = new Color(0.38f, 0.18f, 0.8f);
        public float HaloExtra { get; } = 0.22f;
        public Color CastColor { get; } = new Color(0.4f, 0.7f, 1, 0.2f);
        public float CastFlashSeconds { get; } = 0.18f;
        public Color DefeatedColor { get; } = new Color(0.42f, 0.44f, 0.5f);
        public string DefeatedColorName { get; } = "Gray";
        // 살아 있는 대상은 HP 비율에 따라 이 배율부터 원래 색까지 밝아진다.
        public float DamagedBrightness { get; } = 0.4f;
        public TargetAppearance Fallback { get; } = new TargetAppearance(Color.white, 0.4f, "White");

        public IReadOnlyDictionary<string, TargetAppearance> Targets => _targets;

        public static ReferencePresentation CreateSample()
        {
            var presentation = new ReferencePresentation();
            presentation._targets.Add("shard", new TargetAppearance(new Color(0.2f, 0.9f, 0.95f), 0.32f, "Cyan"));
            presentation._targets.Add("heavy", new TargetAppearance(new Color(1, 0.55f, 0.23f), 0.55f, "Orange"));
            return presentation;
        }

        public TargetAppearance Target(string contentId) =>
            contentId != null && _targets.TryGetValue(contentId, out TargetAppearance appearance)
                ? appearance
                : Fallback;

        // 조립 시점의 확인: 외형이 없는 대상(기본 외형 사용)과 콘텐츠에 없는 외형(쓰이지 않음)을 알려 준다.
        // 둘 다 플레이를 막지 않는 경고다.
        public void Report(ContentCatalog catalog, Action<string> warn)
        {
            var contentIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (TargetDefinition target in catalog.Targets)
            {
                contentIds.Add(target.Id);
                if (!_targets.ContainsKey(target.Id))
                    warn($"대상 '{target.Id}'의 외형 정의가 없어 기본 외형을 쓴다.");
            }
            foreach (string id in _targets.Keys)
                if (!contentIds.Contains(id))
                    warn($"외형 정의 '{id}'에 해당하는 대상이 콘텐츠에 없다.");
        }
    }
}
