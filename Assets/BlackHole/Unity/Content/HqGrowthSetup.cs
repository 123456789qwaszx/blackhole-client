using System;
using System.Collections.Generic;
using BlackHole.Core;
using UnityEngine;

namespace BlackHole.Unity
{
    // 블랙홀 성장 설정 에셋(GAME_RULES 3.2·11절, BATTLE_COMPOSITION_PLAN 8절).
    // - 성장도 표: 줄 s가 성장도 s의 판에서 쓰는 판 Level 표와 목표 Level이다. 판은 Level 0에서 시작하고, 표의 줄 i가 Level (i + 1)에 닿는 이 판의 누적 EXP다.
    //   이 판이 목표 Level에 닿으면 결산 때 성장도가 1 오른다. 마지막 줄만 목표 0(목표 없음)일 수 있다.
    // - 이정표: 그 성장도에 닿으면 받는다. 그 앞 성장도의 판이 목표 Level에 닿는 순간 판이 끝나고, 결산이 번 Gold 대신 보상을 준다.
    // 성장 효과(판 Level업마다의 시간·공급)는 여기에 두지 않는다 — 노드 목록의 성장 노드가 정한다.
    // 적이 주는 EXP는 적 종류 에셋의 색 등급 칸에 있다.
    [CreateAssetMenu(fileName = "HqGrowthSetup", menuName = "BlackHole/Hq Growth Setup")]
    public sealed class HqGrowthSetup : ScriptableObject
    {
        [Serializable]
        public struct Stage
        {
            [Tooltip("이 판에서 Level 1, 2, …에 닿는 누적 EXP. 앞 줄보다 커야 한다. 표 끝에서는 EXP만 쌓인다.")]
            public List<long> levelExp;
            [Tooltip("이 판에서 이 Level에 닿으면 결산 때 성장도가 1 오른다. 1 이상, 표 안. 마지막 성장도만 0(목표 없음)일 수 있다.")]
            public int goalLevel;
        }

        [Serializable]
        public struct Milestone
        {
            [Tooltip("이 성장도(1 이상, 성장도 표 안)에 닿으면 받는다. 앞 이정표보다 커야 한다.")]
            public int stage;
            [Tooltip("결산이 그 판에서 번 Gold 대신 주는 고정 보상.")]
            public long reward;
        }

        [Header("성장도 표 (줄 번호 = 성장도, 0부터)")]
        [SerializeField] private List<Stage> stages = new List<Stage>();

        [Header("이정표 (성장도가 커지는 순서)")]
        [SerializeField] private List<Milestone> milestones = new List<Milestone>();

        // Core 저작 형식에 성장도 표와 이정표를 채운다. 검증은 ContentLoader가 한다.
        public void WriteTo(ContentData data)
        {
            data.Growth = new HqGrowthData();

            foreach (Stage stage in stages)
            {
                data.Growth.Stages.Add(new GrowthStageData
                {
                    LevelExp = stage.levelExp != null ? new List<long>(stage.levelExp) : new List<long>(),
                    GoalLevel = stage.goalLevel,
                });
            }

            foreach (Milestone milestone in milestones)
                data.Growth.Milestones.Add(new HqMilestoneData { Stage = milestone.stage, Reward = milestone.reward });
        }
    }
}
