using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 한 판의 출현 진행(타이머·순번)을 가진다. 출현이 정하는 것은 언제·무엇을·어디에까지다.
    // 나온 뒤의 움직임은 Enemy의 행동이 맡는다.
    //
    // [임시] 규칙: 판 시작 후 Interval마다 한 번 나온다. 동시 최대 수에 걸린 출현은 미루지 않고 건너뛴다.
    internal sealed class EnemySpawner
    {
        private readonly SpawnDefinition _definition;
        private readonly IReadOnlyList<EnemyDefinition> _order;
        private readonly EnemyBehaviorResolver _behaviors;
        private float _untilNext;
        private int _spawned;

        // order: _definition.Order를 콘텐츠에서 해석한 Enemy 정의 목록.
        public EnemySpawner(SpawnDefinition definition, IReadOnlyList<EnemyDefinition> order, EnemyBehaviorResolver behaviors)
        {
            _definition = definition;
            _order = order;
            _behaviors = behaviors;
            _untilNext = definition.Interval;
        }

        public void Advance(float delta, World world)
        {
            _untilNext -= delta;
            while (_untilNext <= 0)
            {
                if (world.Enemies.Count < _definition.MaxAlive) SpawnNext(world);
                _untilNext += _definition.Interval;
            }
        }

        private void SpawnNext(World world)
        {
            EnemyDefinition definition = _order[_spawned % _order.Count];
            float angle = _spawned * _definition.AngleStep;
            Point2 hq = world.Hq.Position;
            var position = new Point2(
                hq.X + _definition.Distance * (float)Math.Cos(angle),
                hq.Y + _definition.Distance * (float)Math.Sin(angle));

            // 실행 수치는 출현 때 한 번 확정한다. 구매 보정은 M6에서 연결한다(지금은 보정 없음).
            EnemyStats stats = EnemyStatCalculator.Compute(definition, Array.Empty<IEnemyStatModifier>());
            world.AddEnemy(definition, stats, position, _behaviors(definition.Behavior));
            _spawned++;
        }
    }
}
