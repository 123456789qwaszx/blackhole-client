using System.Collections.Generic;

namespace BlackHole.Core
{
    // 번개가 한 번 옮겨 간 기록: 어디서 어디로. 화면은 이것을 읽어 그릴 뿐 피해를 다시 계산하지 않는다.
    public readonly struct LightningHit
    {
        // 판 안에서 늘어나는 번호. 같은 기록을 두 번 그리지 않는 데 쓴다.
        public long Sequence { get; }
        public Point2 From { get; }
        public Point2 To { get; }

        internal LightningHit(long sequence, Point2 from, Point2 to)
        {
            Sequence = sequence;
            From = from;
            To = to;
        }
    }

    // 폭발 한 번의 기록.
    public readonly struct ExplosionBlast
    {
        public long Sequence { get; }
        public Point2 Center { get; }
        public float Radius { get; }
        // 피해를 준 적의 수.
        public int HitCount { get; }

        internal ExplosionBlast(long sequence, Point2 center, float radius, int hitCount)
        {
            Sequence = sequence;
            Center = center;
            Radius = radius;
            HitCount = hitCount;
        }
    }

    // 한 판의 사망 효과 대기열과 처리(World.Step의 4. Death Effect 자리, SYSTEM_CATALOG S06).
    //
    // 대기열: 효과를 가진 적이 피해로 처음 죽는 순간(World.DealDamage) 그 효과·죽은 자리·마지막 피해의 출처를 넣는다.
    // 파괴 요청으로 죽은 적(피해 출처가 없는 사망)은 넣지 않는다 [제안, SKILL_SYSTEM_PLAN D5].
    // 처리: 넣은 순서(사망 순서)대로 효과를 실행하고 대기열을 비운다. 효과 피해도 World.DealDamage로 준다 —
    // 사망 1회·사망 기록·처치 수는 적 시스템이 맡는다. 효과를 가진 적은 효과 피해를 받지 않으므로 효과로 생긴 사망은
    // 대기열에 새 효과를 넣지 않는다. 그래서 한 번 훑으면 끝난다.
    // 효과 피해의 출처는 그 효과를 가진 적을 죽인 참가자다. 기록일 뿐이며 귀속 규칙이 아니다(Damage.Source).
    // 판이 끝나면 처리되지 않은 효과는 버린다(끝난 판은 새 결과를 만들지 않는다).
    public sealed class DeathEffects
    {
        private readonly List<Pending> _pending = new List<Pending>();
        private readonly List<LightningHit> _lightningHits = new List<LightningHit>();
        private readonly List<ExplosionBlast> _explosions = new List<ExplosionBlast>();
        private readonly List<Enemy> _targets = new List<Enemy>();
        private readonly HashSet<Enemy> _struck = new HashSet<Enemy>();
        private long _nextSequence = 1;

        private readonly struct Pending
        {
            public DeathEffectDefinition Effect { get; }
            public Point2 Position { get; }
            public PlayerId Source { get; }

            public Pending(DeathEffectDefinition effect, Point2 position, PlayerId source)
            {
                Effect = effect;
                Position = position;
                Source = source;
            }
        }

        // 마지막 진행 동안의 번개 이동과 폭발(일어난 순서). 다음 진행이 시작될 때 비운다.
        public IReadOnlyList<LightningHit> LightningHits { get; }
        public IReadOnlyList<ExplosionBlast> Explosions { get; }
        // 처리되지 않은 효과가 남아 있는가.
        public bool HasPending => _pending.Count > 0;

        internal DeathEffects()
        {
            LightningHits = _lightningHits.AsReadOnly();
            Explosions = _explosions.AsReadOnly();
        }

        // 막 죽은 적(피해로 처음 죽음)의 효과를 대기열에 넣는다. 효과가 없는 적이면 아무 일도 없다.
        internal void Enqueue(Enemy enemy, PlayerId source)
        {
            if (enemy.Definition.DeathEffect != null)
                _pending.Add(new Pending(enemy.Definition.DeathEffect, enemy.Position, source));
        }

        internal void BeginAdvance()
        {
            _lightningHits.Clear();
            _explosions.Clear();
        }

        internal void Clear() => _pending.Clear();

        internal void Resolve(World world)
        {
            // 효과 처리 중에 대기열이 늘지 않지만(효과 보유 적은 효과 피해를 받지 않는다), 늘어도 끝까지 처리한다.
            for (int i = 0; i < _pending.Count; i++)
            {
                Pending pending = _pending[i];

                switch (pending.Effect)
                {
                    case ChainLightningDefinition chain:
                        Chain(chain, pending, world);
                        break;
                    case ExplosionDefinition explosion:
                        Explode(explosion, pending, world);
                        break;
                }
            }

            _pending.Clear();
        }

        // 효과 피해를 받을 수 있는 적: 살아 있고 효과가 없는 적(World.Enemies는 살아 있는 적뿐이다).
        private static bool CanBeStruck(Enemy enemy) => enemy.Definition.DeathEffect == null;

        private void Chain(ChainLightningDefinition chain, Pending pending, World world)
        {
            _struck.Clear();
            Point2 origin = pending.Position;
            float radiusSquared = chain.Radius * chain.Radius;
            var damage = new Damage(chain.Damage, pending.Source);

            for (int hop = 0; hop < chain.MaxTargets; hop++)
            {
                Enemy nearest = null;
                float nearestSquared = radiusSquared;
                IReadOnlyList<Enemy> enemies = world.Enemies;

                for (int i = 0; i < enemies.Count; i++)
                {
                    Enemy enemy = enemies[i];

                    if (!CanBeStruck(enemy) || _struck.Contains(enemy))
                        continue;

                    float distanceSquared = origin.DistanceSquared(enemy.Position);

                    if (distanceSquared <= nearestSquared && (nearest == null || distanceSquared < nearestSquared))
                    {
                        nearest = enemy;
                        nearestSquared = distanceSquared;
                    }
                }

                if (nearest == null)
                    break;

                _struck.Add(nearest);
                _lightningHits.Add(new LightningHit(_nextSequence++, origin, nearest.Position));
                origin = nearest.Position;
                world.DealDamage(nearest, damage);
            }
        }

        private void Explode(ExplosionDefinition explosion, Pending pending, World world)
        {
            _targets.Clear();
            float radiusSquared = explosion.Radius * explosion.Radius;
            IReadOnlyList<Enemy> enemies = world.Enemies;

            for (int i = 0; i < enemies.Count; i++)
            {
                if (CanBeStruck(enemies[i]) && pending.Position.DistanceSquared(enemies[i].Position) <= radiusSquared)
                    _targets.Add(enemies[i]);
            }

            var damage = new Damage(explosion.Damage, pending.Source);

            foreach (Enemy target in _targets)
                world.DealDamage(target, damage);

            _explosions.Add(new ExplosionBlast(_nextSequence++, pending.Position, explosion.Radius, _targets.Count));
        }
    }
}
