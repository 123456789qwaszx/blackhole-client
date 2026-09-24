using System;
using System.Collections.Generic;
using BlackHole.Sample;
using UnityEngine;

namespace BlackHole.Unity
{
    // 표현 샘플. 화면 전용 값이며 게임 규칙이 아니다(Core는 모른다).
    // HQ 레벨/크기 연계는 M5에서 정한다. 지금 원판 크기는 표현 샘플이다.
    // Enemy 크기는 게임 수치(Stats.Size)를 그대로 쓰고, 색만 여기서 정한다.
    // Skill 범위 원의 크기는 Skill의 실행 반경이다. 여기서는 색과 연출 시간만 정한다.
    internal sealed class SamplePresentation
    {
        private readonly Dictionary<string, Color> _enemyColors = new Dictionary<string, Color>(StringComparer.Ordinal)
        {
            { SampleContent.LightEnemyId, new Color(0.2f, 0.9f, 0.95f) },
            { SampleContent.HeavyEnemyId, new Color(1, 0.55f, 0.23f) }
        };

        public float CameraSize { get; } = 7.5f;
        public Color Background { get; } = new Color(0.025f, 0.035f, 0.065f);
        public Color HqColor { get; } = new Color(0.005f, 0.005f, 0.012f);
        public Color HqGlowColor { get; } = new Color(0.38f, 0.18f, 0.8f);
        public float HqDisplayDiameter { get; } = 1.3f;
        public float HqGlowExtra { get; } = 0.22f;
        // 외형이 정의되지 않은 Enemy 종류의 색. 플레이는 막지 않는다.
        public Color FallbackEnemyColor { get; } = Color.white;

        // 피격 표시: HP가 0에 가까울수록 이 색에 가까워지고, 피해를 받은 순간 잠깐 밝아진다.
        public Color DepletedEnemyColor { get; } = new Color(0.16f, 0.12f, 0.16f);
        public Color HitFlashColor { get; } = Color.white;
        public float HitFlashSeconds { get; } = 0.12f;
        public float AbsorbSeconds { get; } = 0.55f;

        // 범위 원: 옅은 안쪽과 테두리. 틱마다 안쪽이 잠깐 밝아진다.
        public Color SkillFillColor { get; } = new Color(0.55f, 0.75f, 1, 0.06f);
        public Color SkillTickColor { get; } = new Color(0.55f, 0.75f, 1, 0.24f);
        public Color SkillRingColor { get; } = new Color(0.6f, 0.8f, 1, 0.8f);
        public float SkillTickSeconds { get; } = 0.15f;

        public Color EnemyColor(string enemyId) =>
            enemyId != null && _enemyColors.TryGetValue(enemyId, out Color color) ? color : FallbackEnemyColor;
    }
}
