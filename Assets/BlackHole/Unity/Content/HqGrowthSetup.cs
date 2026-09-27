using System.Collections.Generic;
using BlackHole.Core;
using UnityEngine;

namespace BlackHole.Unity
{
    // 블랙홀 성장 설정 에셋: Level 표(BLACKHOLE_GROWTH_PLAN 4.2). 판은 Level 1에서 시작하고, 줄 i가 Level (i + 2)에 닿는 누적 EXP다.
    // 성장 효과(Level업마다의 시간·공급)는 여기에 두지 않는다 — 노드 목록의 성장 노드가 정한다.
    // 적이 주는 EXP는 적 종류 에셋의 색 등급 칸에 있다.
    [CreateAssetMenu(fileName = "HqGrowthSetup", menuName = "BlackHole/Hq Growth Setup")]
    public sealed class HqGrowthSetup : ScriptableObject
    {
        [Header("Level 표 (누적 EXP, 앞 줄보다 커야 한다)")]
        [Tooltip("줄 0이 Level 2에 닿는 누적 EXP다. 표 끝에서는 Level이 더 오르지 않고 EXP만 쌓인다.")]
        [SerializeField] private List<long> levelExp = new List<long>();

        // Core 저작 형식에 Level 표를 채운다. 검증은 ContentLoader가 한다.
        public void WriteTo(ContentData data)
        {
            data.Growth = new HqGrowthData { LevelExp = new List<long>(levelExp) };
        }
    }
}
