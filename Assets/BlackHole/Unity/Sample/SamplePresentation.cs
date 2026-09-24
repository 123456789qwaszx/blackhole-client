using System;
using System.Collections.Generic;
using BlackHole.Core;
using BlackHole.Sample;
using UnityEngine;

namespace BlackHole.Unity
{
    // 표현 샘플. 화면 전용 값이며 게임 규칙이 아니다(Core는 모른다).
    // HQ는 Level이 오를 때마다 화면에서만 커진다. 규칙 좌표(출현 거리)는 바꾸지 않는다(M5).
    // Enemy 크기는 게임 수치(Stats.Size)를 그대로 쓰고, 색만 여기서 정한다.
    // Skill 범위 원의 크기는 Skill의 실행 반경이다. 여기서는 색과 연출 시간만 정한다.
    internal sealed class SamplePresentation
    {
        private readonly Dictionary<string, Color> _enemyColors = new Dictionary<string, Color>(StringComparer.Ordinal)
        {
            { SampleContent.LightEnemyId, new Color(0.2f, 0.9f, 0.95f) },
            { SampleContent.HeavyEnemyId, new Color(1, 0.55f, 0.23f) },
            { SampleContent.ElectricEnemyId, new Color(1, 0.92f, 0.25f) }
        };

        public float CameraSize { get; } = 7.5f;
        public Color Background { get; } = new Color(0.025f, 0.035f, 0.065f);
        public Color HqColor { get; } = new Color(0.005f, 0.005f, 0.012f);
        public Color HqGlowColor { get; } = new Color(0.38f, 0.18f, 0.8f);
        public float HqBaseDiameter { get; } = 1.3f;
        public float HqDiameterPerLevel { get; } = 0.15f;
        public float HqGlowExtra { get; } = 0.22f;
        // 외형이 정의되지 않은 Enemy 종류의 색. 플레이는 막지 않는다.
        public Color FallbackEnemyColor { get; } = Color.white;

        // 피격 표시: HP가 0에 가까울수록 이 색에 가까워지고, 피해를 받은 순간 잠깐 밝아진다.
        public Color DepletedEnemyColor { get; } = new Color(0.16f, 0.12f, 0.16f);
        public Color HitFlashColor { get; } = Color.white;
        public float HitFlashSeconds { get; } = 0.12f;
        // 흡수 연출 길이. 실제 시간으로 흘러 일시정지 중에도 끝까지 진행한다([임시]: 일시정지 시 연출 처리는 미정).
        public float AbsorbSeconds { get; } = 0.55f;

        // 범위 원: 옅은 안쪽과 테두리. 틱마다 안쪽이 잠깐 밝아진다.
        public Color SkillFillColor { get; } = new Color(0.55f, 0.75f, 1, 0.06f);
        public Color SkillTickColor { get; } = new Color(0.55f, 0.75f, 1, 0.24f);
        public Color SkillRingColor { get; } = new Color(0.6f, 0.8f, 1, 0.8f);
        public float SkillTickSeconds { get; } = 0.15f;

        // 연쇄 번개: 적중 기록마다 출발점에서 도착점까지 선을 긋고 옅어지게 한다. 피해는 이미 처리된 뒤다.
        public Color LightningColor { get; } = new Color(1, 0.97f, 0.7f);
        public float LightningWidth { get; } = 0.06f;
        public float LightningSeconds { get; } = 0.25f;

        // 관통 레이저: 예고는 얇은 선이 발사에 가까울수록 진해지고, 발사는 판정 굵기 그대로의 선이 옅어진다.
        public Color LaserTelegraphColor { get; } = new Color(1, 0.35f, 0.3f, 0.2f);
        public Color LaserTelegraphReadyColor { get; } = new Color(1, 0.45f, 0.4f, 0.85f);
        public float LaserTelegraphWidth { get; } = 0.04f;
        public Color LaserFireColor { get; } = new Color(1, 0.6f, 0.5f, 0.9f);
        public float LaserFireSeconds { get; } = 0.2f;

        // HQ 원판의 지름: 시작 Level에서 기본 지름, Level마다 일정하게 커진다.
        public float HqDiameter(int level) =>
            HqBaseDiameter + HqDiameterPerLevel * (level - HqGrowthDefinition.StartLevel);

        public Color EnemyColor(string enemyId) =>
            enemyId != null && _enemyColors.TryGetValue(enemyId, out Color color) ? color : FallbackEnemyColor;
    }
}
