using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 한 단계 안에 생긴 사망 효과의 대기열과 처리(GAME_RULES 11절).
    // 적은 자기 사망만 확정한다. 효과의 대상과 피해는 여기서 정하고, 피해는 World의 사망 절차를 그대로 거친다.
    internal sealed class DeathEffects
    {
        private readonly List<PendingDeathEffect> _pending = new List<PendingDeathEffect>();
        private readonly List<Enemy> _struck = new List<Enemy>();

        // 출처는 효과를 일으킨 적을 죽인 피해의 출처다([임시]). 기록일 뿐이며 보상 귀속 규칙으로 쓰지 않는다.
        public void Enqueue(DeathEffectDefinition effect, Point2 origin, PlayerId source)
        {
            _pending.Add(new PendingDeathEffect(effect, origin, source));
        }

        // 대기열을 들어온 순서대로 비운다. 처리 중에 생긴 효과는 같은 대기열 뒤에 붙어 이어서 처리된다(재귀 없음).
        public void Resolve(World world)
        {
            for (int i = 0; i < _pending.Count; i++)
            {
                PendingDeathEffect pending = _pending[i];

                switch (pending.Effect)
                {
                    case ChainLightningDefinition chain:
                        ResolveChainLightning(chain, pending, world);
                        break;
                    default:
                        throw new InvalidOperationException(
                            $"처리 규칙이 연결되지 않은 사망 효과 종류 '{pending.Effect.GetType().Name}'.");
                }
            }

            _pending.Clear();
        }

        // 사망 효과가 피해를 줄 수 있는 적인가. 사망 효과를 가진 적은 대상이 아니다(GAME_RULES 11절, 레퍼런스와 같다).
        // 그래서 효과가 다른 효과를 부르는 연쇄가 생기지 않는다.
        private static bool CanHit(Enemy enemy) =>
            enemy.IsAlive && enemy.Definition.DeathEffect == null;

        // 죽은 자리에서 시작해 Range 안의 가장 가까운 대상을 맞히고, 맞힌 적의 자리에서 다시 튄다. 최대 Chains번.
        // [임시] 같은 번개는 같은 적을 두 번 맞히지 않는다. 거리는 중심 거리이고, 같으면 목록 앞의 적을 고른다.
        private void ResolveChainLightning(
            ChainLightningDefinition chain,
            PendingDeathEffect pending,
            World world)
        {
            var damage = new Damage(chain.Damage, pending.Source);
            Point2 from = pending.Origin;
            _struck.Clear();

            for (int hit = 0; hit < chain.Chains; hit++)
            {
                Enemy target = Nearest(world.Enemies, from, chain.Range);

                if (target == null)
                    break;

                Point2 to = target.Position;
                _struck.Add(target);
                world.RecordDeathEffectHit(chain, from, to, target.Id);
                world.DealDamage(target, damage);
                from = to;
            }

            _struck.Clear();
        }

        private Enemy Nearest(IReadOnlyList<Enemy> enemies, Point2 from, float range)
        {
            Enemy nearest = null;
            float best = range * range;

            for (int i = 0; i < enemies.Count; i++)
            {
                Enemy enemy = enemies[i];

                if (!CanHit(enemy) || _struck.Contains(enemy))
                    continue;

                float distance = enemy.Position.DistanceSquared(from);

                if (distance > best || (nearest != null && distance == best))
                    continue;

                nearest = enemy;
                best = distance;
            }

            return nearest;
        }

        private readonly struct PendingDeathEffect
        {
            public DeathEffectDefinition Effect { get; }
            public Point2 Origin { get; }
            public PlayerId Source { get; }

            public PendingDeathEffect(DeathEffectDefinition effect, Point2 origin, PlayerId source)
            {
                Effect = effect;
                Origin = origin;
                Source = source;
            }
        }
    }
}
