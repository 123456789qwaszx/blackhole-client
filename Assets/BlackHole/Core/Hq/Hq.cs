using System;

namespace BlackHole.Core
{
    // HQ의 공유 정의. 지금 확정된 것은 위치(월드의 기준점)뿐이다.
    // HP·파괴·성장은 미정이라 두지 않는다(D1).
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

        internal Hq(HqDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            Position = definition.Position;
        }
    }
}
