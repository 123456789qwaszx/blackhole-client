using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // HQ 성장 노드 하나: 이 Level에 도달하는 누적 EXP 임계값과, 처음 도달했을 때의 성장 효과.
    // 효과 값은 Level마다 따로 둔다. 모든 Level이 같은 효과를 갖는다고 가정하지 않는다(GAME_RULES 9절).
    public sealed class HqLevelDefinition
    {
        public int Exp { get; }
        // 판 시간 연장(초). 0이면 늘리지 않는다.
        public float ExtraTime { get; }
        // 추가 공급. 비어 있으면 공급하지 않는다.
        public IReadOnlyList<SupplyRequest> Supply { get; }

        public HqLevelDefinition(
            int exp,
            float extraTime,
            IReadOnlyList<SupplyRequest> supply)
        {
            if (exp <= 0)
                throw new ArgumentOutOfRangeException(nameof(exp), "양의 정수가 필요하다.");

            Exp = exp;
            ExtraTime = DefinitionGuard.NonNegative(extraTime, nameof(extraTime));
            Supply = Copy(supply);
        }

        private static IReadOnlyList<SupplyRequest> Copy(IReadOnlyList<SupplyRequest> source)
        {
            if (source == null)
                return Array.Empty<SupplyRequest>();

            var copy = new SupplyRequest[source.Count];

            for (int i = 0; i < copy.Length; i++)
            {
                copy[i] = source[i];
            }

            return Array.AsReadOnly(copy);
        }
    }

    // HQ 성장의 공유 정의: Level 노드 목록.
    // 시작 Level은 1이다. Levels[i]는 Level (i + 2)의 노드이며, 임계값은 앞 노드보다 커야 한다.
    // Level의 주인은 HQ(Hq.Level)이고, 성장 효과의 실행은 성장 진행(GrowthProgression)이 맡는다.
    public sealed class HqGrowthDefinition
    {
        public const int StartLevel = 1;

        public static readonly HqGrowthDefinition None =
            new HqGrowthDefinition(Array.Empty<HqLevelDefinition>());

        public IReadOnlyList<HqLevelDefinition> Levels { get; }
        public int MaxLevel => StartLevel + Levels.Count;

        public HqGrowthDefinition(IReadOnlyList<HqLevelDefinition> levels)
        {
            if (levels == null)
                throw new ArgumentNullException(nameof(levels));

            var copy = new HqLevelDefinition[levels.Count];

            for (int i = 0; i < copy.Length; i++)
            {
                HqLevelDefinition level = levels[i]
                    ?? throw new ArgumentException($"Levels[{i}]가 비어 있다.", nameof(levels));

                if (i > 0 && level.Exp <= copy[i - 1].Exp)
                    throw new ArgumentException(
                        $"Levels[{i}]의 임계값 {level.Exp}는 앞 노드의 {copy[i - 1].Exp}보다 커야 한다.",
                        nameof(levels));

                copy[i] = level;
            }

            Levels = Array.AsReadOnly(copy);
        }

        // 시작 Level보다 높은 level의 노드. 시작 Level 이하이거나 마지막 Level을 넘으면 false.
        public bool TryGetLevel(int level, out HqLevelDefinition definition)
        {
            int index = level - StartLevel - 1;

            if (index < 0 || index >= Levels.Count)
            {
                definition = null;
                return false;
            }

            definition = Levels[index];
            return true;
        }
    }
}
