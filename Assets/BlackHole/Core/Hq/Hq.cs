using System;

namespace BlackHole.Core
{
    // HQ의 공유 정의: 위치(월드의 기준점). 성장 노드는 HqGrowthDefinition에 있다.
    public sealed class HqDefinition
    {
        public Point2 Position { get; }

        public HqDefinition(Point2 position)
        {
            Position = position;
        }
    }

    // 판 안의 HQ. 출현과 Enemy 행동이 원점이 아니라 이 위치를 참조한다(B2).
    // HQ는 Player가 아니며, Enemy AI를 직접 실행하지도 않는다.
    // 성장 상태(누적 EXP, Level)를 가진다. Level에 따른 효과는 성장 진행이, 시각 크기는 화면이 맡는다.
    public sealed class Hq
    {
        private readonly HqGrowthDefinition _growth;

        public Point2 Position { get; }
        public int Exp { get; private set; }
        public int Level { get; private set; } = HqGrowthDefinition.StartLevel;

        // 다음 Level의 임계값(누적 EXP). 마지막 Level이면 null.
        public int? NextLevelExp =>
            _growth.TryGetLevel(Level + 1, out HqLevelDefinition next)
                ? next.Exp
                : (int?)null;

        internal Hq(HqDefinition definition, HqGrowthDefinition growth)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            _growth = growth ?? throw new ArgumentNullException(nameof(growth));
            Position = definition.Position;
        }

        // 누적 EXP가 다음 Level의 임계값에 닿을 때마다 Level이 오른다. 한 번의 획득으로 여러 Level을 오를 수 있다.
        internal void GainExp(int amount)
        {
            Exp = checked(Exp + amount);

            while (_growth.TryGetLevel(Level + 1, out HqLevelDefinition next)
                && Exp >= next.Exp)
            {
                Level++;
            }
        }
    }
}
