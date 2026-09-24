using System;

namespace BlackHole.Core
{
    // HQ의 공유 정의. 성장 곡선과 레벨 구간은 M5에서 정한다.
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
    public sealed class Hq
    {
        public Point2 Position { get; }
        public int Exp { get; private set; }

        internal Hq(HqDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            Position = definition.Position;
        }

        internal void GainExp(int amount) => Exp = checked(Exp + amount);
    }
}
