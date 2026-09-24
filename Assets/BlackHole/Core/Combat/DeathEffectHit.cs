namespace BlackHole.Core
{
    // 사망 효과의 적중 한 번. 피해는 이미 처리된 뒤다. 화면은 이를 읽고 번개 같은 연출을 만든다.
    public readonly struct DeathEffectHit
    {
        public long Sequence { get; }
        public DeathEffectDefinition Effect { get; }
        public Point2 From { get; }
        public Point2 To { get; }
        public EnemyId Target { get; }

        internal DeathEffectHit(
            long sequence,
            DeathEffectDefinition effect,
            Point2 from,
            Point2 to,
            EnemyId target)
        {
            Sequence = sequence;
            Effect = effect;
            From = from;
            To = to;
            Target = target;
        }
    }
}
