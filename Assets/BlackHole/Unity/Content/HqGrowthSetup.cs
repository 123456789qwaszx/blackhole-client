using System;
using System.Collections.Generic;
using BlackHole.Core;
using UnityEngine;

namespace BlackHole.Unity
{
    // 블랙홀 성장 설정 에셋: Level 표(BLACKHOLE_GROWTH_PLAN 4.2). 판은 Level 1에서 시작하고, 줄 i가 Level (i + 2)에 닿는 누적 EXP다.
    // 성장 효과(Level업마다의 시간·공급)는 여기에 두지 않는다 — 노드 목록의 성장 노드가 정한다.
    // 이정표: 그 Level에 닿으면 판이 바로 끝나고 결산이 번 Gold 대신 보상을 준다. 금액은 기획자가 이정표마다 정한다(BLACKHOLE_LEVEL_PLAN 4.4).
    // 적이 주는 EXP는 적 종류 에셋의 색 등급 칸에 있다.
    [CreateAssetMenu(fileName = "HqGrowthSetup", menuName = "BlackHole/Hq Growth Setup")]
    public sealed class HqGrowthSetup : ScriptableObject
    {
        [Serializable]
        public struct Milestone
        {
            [Tooltip("이 Level(Level 표 안, 2 이상)에 닿으면 판이 끝난다. 앞 이정표보다 커야 한다.")]
            public int level;
            [Tooltip("결산이 그 판에서 번 Gold 대신 주는 고정 보상.")]
            public long reward;
        }

        [Header("Level 표 (누적 EXP, 앞 줄보다 커야 한다)")]
        [Tooltip("줄 0이 Level 2에 닿는 누적 EXP다. 표 끝에서는 Level이 더 오르지 않고 EXP만 쌓인다.")]
        [SerializeField] private List<long> levelExp = new List<long>();

        [Header("이정표 (Level이 커지는 순서)")]
        [SerializeField] private List<Milestone> milestones = new List<Milestone>();

        // Core 저작 형식에 Level 표와 이정표를 채운다. 검증은 ContentLoader가 한다.
        public void WriteTo(ContentData data)
        {
            data.Growth = new HqGrowthData { LevelExp = new List<long>(levelExp) };

            foreach (Milestone milestone in milestones)
                data.Growth.Milestones.Add(new HqMilestoneData { Level = milestone.level, Reward = milestone.reward });
        }
    }
}
