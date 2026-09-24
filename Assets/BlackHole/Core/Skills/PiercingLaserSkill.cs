using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 예고 중인 레이저 한 발. 경로는 예고 시작 순간에 확정되고 발사 때까지 바뀌지 않는다.
    public readonly struct LaserShot
    {
        // 전투 영역 경계 위의 발사 원점.
        public Point2 Start { get; }
        // 시작점에서 예고 순간의 Aim Point 방향으로 나아가 다시 경계에 닿는 곳.
        public Point2 End { get; }
        // 발사까지 남은 시간(초). 0 이하가 되는 단계에서 발사한다.
        public float RemainingTelegraph { get; }

        internal LaserShot(Point2 start, Point2 end, float remainingTelegraph)
        {
            Start = start;
            End = end;
            RemainingTelegraph = remainingTelegraph;
        }

        internal LaserShot Elapse(float delta) => new LaserShot(Start, End, RemainingTelegraph - delta);
    }

    // 레이저 발사 한 번의 기록. 피해는 이미 처리된 뒤다. 화면·소리는 이를 읽고 발사 선과 발사음을 한 번씩 낸다.
    // 사망 기록처럼 번호(Sequence)로 소비한다. 예고 중인 발사는 PiercingLaserSkill.PendingShots가 보여 준다.
    public readonly struct LaserFireRecord
    {
        public long Sequence { get; }
        public PlayerId Owner { get; }
        public PiercingLaserDefinition Laser { get; }
        public Point2 Start { get; }
        public Point2 End { get; }
        // 이 발사에 쓰인 굵기(판정과 표시가 같은 값).
        public float Width { get; }
        // 이 발사가 피해를 준 Enemy 수.
        public int HitCount { get; }

        internal LaserFireRecord(long sequence, PlayerId owner, PiercingLaserDefinition laser, LaserShot shot, float width, int hitCount)
        {
            Sequence = sequence;
            Owner = owner;
            Laser = laser;
            Start = shot.Start;
            End = shot.End;
            Width = width;
            HitCount = hitCount;
        }
    }

    // 관통 레이저의 실행 상태(CONTENT_DEFINITION 2.3).
    //
    // 시간표: 예고는 0, I, 2I, …에 시작하고 각 예고는 T 뒤에 발사한다(I = Interval, T = TelegraphDuration).
    // 예고 시작: 시작점을 경계 위 무작위 지점으로 고르고, 그 순간의 Aim Point로 방향을 확정한다.
    // 발사: 경로의 폭 안에 있는 살아 있는 Enemy에게 한 번씩 피해를 준다. 이후의 굵은 선은 표현의 일이다.
    // 동시에 예고 중인 발사는 최대 ⌈T / I⌉개다.
    public sealed class PiercingLaserSkill : PassiveSkill
    {
        private readonly PiercingLaserDefinition _definition;
        private readonly List<LaserShot> _pending = new List<LaserShot>();
        private readonly List<Enemy> _targets = new List<Enemy>();
        private float _untilNextTelegraph;

        // 실행 수치. 판 조립 때 정해진다.
        public PiercingLaserStats Stats { get; }
        // 예고 중인 발사(예고 순서). 게임 상태이며 바깥에서 바꿀 수 없다. 예고 표현도 이것을 읽을 수 있다.
        // 발사한 순간 여기서 빠지므로, 발사 선·발사음을 정확히 한 번 내려면 발사 기록 같은 것이 따로 필요할 수 있다(CA-005).
        public IReadOnlyList<LaserShot> PendingShots { get; }

        internal PiercingLaserSkill(PiercingLaserDefinition definition, PiercingLaserStats stats, Player owner)
            : base(definition, owner)
        {
            _definition = definition;
            Stats = stats;
            PendingShots = _pending.AsReadOnly();
            // [임시] 첫 예고는 판 시작(0초)이다.
            _untilNextTelegraph = 0;
        }

        internal override void Advance(float delta, World world)
        {
            for (int i = 0; i < _pending.Count; i++)
                _pending[i] = _pending[i].Elapse(delta);

            _untilNextTelegraph -= delta;
            while (_untilNextTelegraph <= 0)
            {
                // 이 예고가 시작됐어야 할 시각부터 이미 지난 시간만큼 예고가 줄어 있다. 발사 시각이 프레임 길이에 따라 밀리지 않는다.
                Telegraph(world, Stats.TelegraphDuration + _untilNextTelegraph);
                _untilNextTelegraph += Stats.Interval;
            }

            for (int i = 0; i < _pending.Count;)
            {
                if (_pending[i].RemainingTelegraph > 0)
                {
                    i++;
                    continue;
                }

                LaserShot shot = _pending[i];
                _pending.RemoveAt(i);
                Fire(shot, world);
            }
        }

        private void Telegraph(World world, float remaining)
        {
            // [임시] 조준점이 없으면 이 예고 자리는 빈다. 주기는 그대로 흐른다.
            if (!Owner.AimPoint.HasValue)
                return;

            Point2 hq = world.Hq.Position;
            Point2 aim = Owner.AimPoint.Value;
            float radius = _definition.BoundaryRadius;

            // [임시] 전투 영역 밖을 조준하면 경로가 정해지지 않으므로 빈 발사다.
            if (aim.DistanceSquared(hq) >= radius * radius)
                return;

            double angle = world.Random.NextFloat() * 2 * Math.PI;
            var start = new Point2(
                hq.X + radius * (float)Math.Cos(angle),
                hq.Y + radius * (float)Math.Sin(angle));

            // 조준점이 원 안에 있으므로 시작점(원 위)과 같을 수 없다.
            float dx = aim.X - start.X;
            float dy = aim.Y - start.Y;
            float length = (float)Math.Sqrt(dx * dx + dy * dy);
            dx /= length;
            dy /= length;

            // 시작점 S에서 방향 d로 나아가 다시 원에 닿는 거리: t = −2 (S − HQ)·d.
            float travel = -2 * ((start.X - hq.X) * dx + (start.Y - hq.Y) * dy);
            var end = new Point2(start.X + travel * dx, start.Y + travel * dy);

            _pending.Add(new LaserShot(start, end, remaining));
        }

        private void Fire(LaserShot shot, World world)
        {
            float halfWidth = Stats.Width / 2;
            _targets.Clear();
            IReadOnlyList<Enemy> enemies = world.Enemies;

            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i].IsAlive
                    && DistanceSquaredToPath(enemies[i].Position, shot) <= halfWidth * halfWidth)
                {
                    _targets.Add(enemies[i]);
                }
            }

            world.RecordLaserFire(Owner.Id, _definition, shot, Stats.Width, _targets.Count);

            var damage = new Damage(Stats.Damage, Owner.Id);

            foreach (Enemy target in _targets)
                world.DealDamage(target, damage);

            _targets.Clear();
        }

        // "경로의 폭 안" 판정의 유일한 자리. [임시]로 Enemy의 중심과 경로 선분의 거리를 본다.
        // Enemy 크기를 더할지(거리 ≤ 반지름 + 폭/2)는 미정이다.
        private static float DistanceSquaredToPath(Point2 point, LaserShot shot)
        {
            float sx = shot.End.X - shot.Start.X;
            float sy = shot.End.Y - shot.Start.Y;
            float lengthSquared = sx * sx + sy * sy;
            float t = ((point.X - shot.Start.X) * sx + (point.Y - shot.Start.Y) * sy) / lengthSquared;
            t = Math.Max(0, Math.Min(1, t));
            var nearest = new Point2(shot.Start.X + t * sx, shot.Start.Y + t * sy);
            return point.DistanceSquared(nearest);
        }
    }
}
