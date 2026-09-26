using BlackHole.Core;
using UnityEngine;

namespace BlackHole.Unity
{
    // 적 종류 하나의 저작 에셋: 규칙 수치, 행동, 외형. 종류를 더할 때는 코드를 고치지 않고
    // 에셋을 하나 만들어 적 종류 목록(EnemyCatalog)에 넣는다.
    // 규칙 칸은 Core의 저작 형식(EnemyData)으로 옮겨져 ContentLoader가 검증한다.
    // 외형 칸은 Core로 가지 않고 화면(EnemyView)만 읽는다. 규칙과 외형이 한 에셋에 있어 외형 연결이 빠지지 않는다.
    // 체력·크기는 기본값이다. 최종 값은 단계 계수 등이 더해져 정해진다(그 규칙은 단계 표와 함께 붙는다).
    // 특수 효과(전기·폭발·처치 버프)는 종류가 아니라 종류에 붙는 특성이다: 사망 효과 칸. 효과를 가진 적은 사망 효과의 피해를 받지 않는다.
    [CreateAssetMenu(fileName = "EnemyKind", menuName = "BlackHole/Enemy Kind")]
    public sealed class EnemyKind : ScriptableObject
    {
        // 사망 효과 종류. None은 효과가 없다. 이름이 Core 저작 형식의 종류 이름이 된다.
        public enum DeathEffectKind { None, ChainLightning, Explosion }

        [Tooltip("공급과 다른 데이터가 이 종류를 가리키는 식별자. 정한 뒤에는 바꾸지 않는다.")]
        [SerializeField] private string id;

        [Header("수치 (출현 때 정해진다)")]
        [SerializeField] private float maxHealth = 10;
        [Tooltip("초당 이동 거리.")]
        [SerializeField] private float moveSpeed = 1;
        [Tooltip("반지름. 화면에 그리는 크기도 이 값이다.")]
        [SerializeField] private float size = 0.3f;

        [Header("행동: HQ 공전")]
        [SerializeField] private bool clockwise;

        [Header("사망 효과 (특성)")]
        [SerializeField] private DeathEffectKind deathEffect;
        [Tooltip("ChainLightning·Explosion: 효과 피해.")]
        [SerializeField] private float effectDamage;
        [Tooltip("ChainLightning: 번개가 한 번 옮겨 가는 최대 거리. Explosion: 폭발 반경.")]
        [SerializeField] private float effectRadius;
        [Tooltip("ChainLightning: 번개가 옮겨 가는 최대 횟수.")]
        [SerializeField] private int effectMaxTargets;

        [Header("외형 (Core는 모른다)")]
        [Tooltip("비우면 임시 원으로 그린다.")]
        [SerializeField] private Sprite sprite;
        [SerializeField] private Color color = Color.white;

        public string Id => id;
        public Sprite Sprite => sprite;
        public Color Color => color;

        internal EnemyData ToData() => new EnemyData
        {
            Id = id,
            MaxHealth = maxHealth,
            MoveSpeed = moveSpeed,
            Size = size,
            Behavior = new EnemyBehaviorData { Kind = "Orbit", Clockwise = clockwise },
            DeathEffect = deathEffect == DeathEffectKind.None ? null : new DeathEffectData
            {
                Kind = deathEffect.ToString(),
                Damage = effectDamage,
                Radius = effectRadius,
                MaxTargets = effectMaxTargets,
            },
        };
    }
}
