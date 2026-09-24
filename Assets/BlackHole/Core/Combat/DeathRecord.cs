namespace BlackHole.Core
{
    // 규칙에서 확정된 사망 결과. View는 이를 읽고 사망 후 연출을 만든다.
    public readonly struct DeathRecord
    {
        public long Sequence { get; }
        public EnemyId EnemyId { get; }
        public string EnemyTypeId { get; }
        public Point2 Position { get; }
        public float Size { get; }

        internal DeathRecord(long sequence, Enemy enemy)
        {
            Sequence = sequence;
            EnemyId = enemy.Id;
            EnemyTypeId = enemy.Definition.Id;
            Position = enemy.Position;
            Size = enemy.Stats.Size;
        }
    }
}
