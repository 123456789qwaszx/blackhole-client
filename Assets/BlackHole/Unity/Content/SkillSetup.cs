using BlackHole.Core;
using UnityEngine;

namespace BlackHole.Unity
{
    // 스킬 설정 에셋: 스킬 종류마다 기본 수치 칸을 따로 둔다(한 칸에 모든 종류의 수치를 섞지 않는다).
    // 칸의 값은 Core의 저작 형식(BreakerData)으로 옮겨져 ContentLoader가 검증한다. 값은 모두 [임시]다(샌드박스의 샘플 값).
    // 모든 참가자가 여기의 스킬을 받는다. 노드로 스킬을 여는 것은 업그레이드 연결 때 한다(SKILL_SYSTEM_PLAN D7).
    [CreateAssetMenu(fileName = "SkillSetup", menuName = "BlackHole/Skill Setup")]
    public sealed class SkillSetup : ScriptableObject
    {
        [Header("Breaker: 조준점 중심 원 안의 적 전부를 주기마다 친다")]
        [SerializeField] private float breakerDamage = 2;
        [Tooltip("공격 주기(초).")]
        [SerializeField] private float breakerInterval = 1;
        [Tooltip("공격 원의 반지름. 화면의 범위 표시도 이 값이다.")]
        [SerializeField] private float breakerRadius = 1.5f;

        // Core 저작 형식에 스킬을 채운다. 검증은 ContentLoader가 한다.
        public void WriteTo(ContentData data)
        {
            data.Breaker = new BreakerData
            {
                Damage = breakerDamage,
                Interval = breakerInterval,
                Radius = breakerRadius,
            };
        }
    }
}
