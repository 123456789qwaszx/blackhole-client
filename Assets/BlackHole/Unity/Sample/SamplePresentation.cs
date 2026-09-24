using UnityEngine;

namespace BlackHole.Unity
{
    // 표현 샘플. 화면 전용 값이며 게임 규칙이 아니다(Core는 모른다).
    // HQ 크기는 규칙에 없다(성장 미정, D1). 원판 크기는 보이게 하려는 표현 값일 뿐이다.
    internal sealed class SamplePresentation
    {
        public float CameraSize { get; } = 7.5f;
        public Color Background { get; } = new Color(0.025f, 0.035f, 0.065f);
        public Color HqColor { get; } = new Color(0.005f, 0.005f, 0.012f);
        public Color HqGlowColor { get; } = new Color(0.38f, 0.18f, 0.8f);
        public float HqDisplayDiameter { get; } = 1.3f;
        public float HqGlowExtra { get; } = 0.22f;
    }
}
