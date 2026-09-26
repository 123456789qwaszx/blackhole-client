using System;
using System.Collections.Generic;
using BlackHole.Core;
using UnityEngine;

namespace BlackHole.Unity
{
    // 적 종류 하나의 저작 에셋: 규칙 수치, 행동, 외형. 종류를 더할 때는 코드를 고치지 않고
    // 에셋을 하나 만들어 적 종류 목록(EnemyCatalog)에 넣는다.
    // 규칙 칸은 Core의 저작 형식(EnemyData)으로 옮겨져 ContentLoader가 검증한다.
    // 외형 칸(스프라이트, 색 등급의 색)은 Core로 가지 않고 화면(EnemyView)만 읽는다. 규칙과 외형이 한 에셋에 있어 외형 연결이 빠지지 않는다.
    //
    // 종류는 계열(소행성·행성·별·달·혜성)이고 색은 종류 안에 둔다(BATTLE_COMPOSITION_PLAN 4.1).
    // - 색 등급: 같은 윤곽(스프라이트)에 색마다 색·크기·HP·Gold가 다르다. 색이 없는 종류는 한 줄이다.
    // - 질량 단계: 질량 증가를 산 수마다 한 줄. 색마다 나오는 비율과 HP·Gold 계수. 판 조립 때 한 줄이 골라진다.
    // 특수 효과(전기·폭발·처치 버프)는 종류가 아니라 종류에 붙는 특성이다. 사망 효과 시스템이 붙을 때 더한다.
    [CreateAssetMenu(fileName = "EnemyKind", menuName = "BlackHole/Enemy Kind")]
    public sealed class EnemyKind : ScriptableObject
    {
        [Serializable]
        public struct Tier
        {
            [Tooltip("이 색 등급을 그리는 색. Core는 모른다.")]
            public Color color;
            public float maxHealth;
            [Tooltip("반지름. 화면에 그리는 크기도 이 값이다.")]
            public float size;
            [Tooltip("사망 때 판의 합계에 드는 Gold의 기본값. 0 이상.")]
            public long gold;
        }

        [Serializable]
        public struct MassLevel
        {
            [Tooltip("색 등급 표와 같은 순서·개수. 0 이상이고 합이 0보다 커야 한다(합이 1이 아니어도 된다).")]
            public List<float> tierRatios;
            [Tooltip("색 등급의 HP에 곱한다.")]
            public float healthMultiplier;
            [Tooltip("색 등급의 Gold에 곱한다(반올림).")]
            public float goldMultiplier;
        }

        [Tooltip("공급과 다른 데이터가 이 종류를 가리키는 식별자. 정한 뒤에는 바꾸지 않는다.")]
        [SerializeField] private string id;

        [Tooltip("초당 이동 거리. 모든 색 등급이 같다.")]
        [SerializeField] private float moveSpeed = 1;

        [Header("색 등급 (번호가 적의 색 등급)")]
        [SerializeField] private List<Tier> tiers = new List<Tier>();

        [Header("질량 단계 (0 = 질량 증가를 사지 않음)")]
        [SerializeField] private List<MassLevel> massLevels = new List<MassLevel>();

        [Header("행동: HQ 공전")]
        [SerializeField] private bool clockwise;

        [Header("외형 (Core는 모른다)")]
        [Tooltip("모든 색 등급이 같은 윤곽을 쓴다. 비우면 임시 원으로 그린다.")]
        [SerializeField] private Sprite sprite;

        public string Id => id;
        public Sprite Sprite => sprite;

        // 색 등급의 색. 없는 번호는 흰색이다.
        public Color ColorOf(int tier) => tier >= 0 && tier < tiers.Count ? tiers[tier].color : Color.white;

        internal EnemyData ToData()
        {
            var data = new EnemyData
            {
                Id = id,
                MoveSpeed = moveSpeed,
                Behavior = new EnemyBehaviorData { Kind = "Orbit", Clockwise = clockwise },
            };

            foreach (Tier tier in tiers)
                data.Tiers.Add(new EnemyTierData { MaxHealth = tier.maxHealth, Size = tier.size, Gold = tier.gold });

            foreach (MassLevel level in massLevels)
            {
                data.MassLevels.Add(new MassLevelData
                {
                    TierRatios = level.tierRatios != null ? new List<float>(level.tierRatios) : new List<float>(),
                    HealthMultiplier = level.healthMultiplier,
                    GoldMultiplier = level.goldMultiplier,
                });
            }

            return data;
        }
    }
}
