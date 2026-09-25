using BlackHole.Core;
using UnityEngine;

namespace BlackHole.Unity
{
    // 적 종류 하나의 저작 에셋: 규칙 수치, 행동, 외형. 종류를 더할 때는 코드를 고치지 않고 에셋을 하나 만든다.
    // 규칙 칸은 Core의 저작 형식(EnemyData)으로 옮겨져 ContentLoader가 검증한다.
    // 외형 칸은 Core로 가지 않고 화면(EnemyView)만 읽는다. 규칙과 외형이 한 에셋에 있어 외형 연결이 빠지지 않는다.
    [CreateAssetMenu(fileName = "EnemyKind", menuName = "BlackHole/Enemy Kind")]
    public sealed class EnemyKind : ScriptableObject
    {
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
        };
    }
}
