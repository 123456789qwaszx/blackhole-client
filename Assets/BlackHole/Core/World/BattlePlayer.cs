namespace BlackHole.Core
{
    // 판 안의 참가자 하나: 조준점과 스킬. 판 조립 때 참가자마다 새로 만들고 판이 끝나면 버린다.
    // 전투 사이에 남는 진행 상태(Gold·산 노드)는 PlayerState다. 캐릭터가 아니다 — 판 안에서 움직이는 몸이 없다.
    // 지금 스킬은 Breaker 하나다. 모든 참가자가 콘텐츠의 스킬을 받는다(노드로 스킬을 여는 것은 업그레이드 연결 때).
    public sealed class BattlePlayer
    {
        public PlayerId Id { get; }
        // 이 참가자의 조준점. 누가 채우는지는 모른다 — 지금은 호스트가 마우스 위치로 채운다.
        // 참가자가 마우스를 가진다는 뜻이 아니다. 없으면 null.
        public Point2? AimPoint { get; private set; }
        // 콘텐츠에 Breaker가 없으면 null.
        public BreakerSkill Breaker { get; }

        internal BattlePlayer(PlayerId id, BreakerDefinition breaker)
        {
            Id = id;
            Breaker = breaker != null ? new BreakerSkill(breaker) : null;
        }

        internal void SetAimPoint(Point2? aimPoint) => AimPoint = aimPoint;

        internal void BeginAdvance() => Breaker?.BeginAdvance();

        // World.Step의 2. Passive Attack 자리에서 부른다.
        internal void Attack(float delta, World world) => Breaker?.Advance(delta, this, world);
    }
}
