using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 한 판의 출현 진행(타이머·순번)을 소유한다. 대상 행동과는 별개다.
    // 이번 샘플은 난수 없이 정의의 순서대로 교대 출현한다.
    internal sealed class SpawnSchedule
    {
        private readonly SpawnDefinition _definition;
        private readonly IReadOnlyList<TargetDefinition> _order;
        private float _remaining;
        private int _spawned;

        // order: _definition.TargetOrder를 카탈로그에서 해석한 정의 목록.
        public SpawnSchedule(SpawnDefinition definition, IReadOnlyList<TargetDefinition> order)
        {
            _definition = definition;
            _order = order;
        }

        public void Advance(float delta, TargetWorld world)
        {
            _remaining -= delta;
            while (_remaining <= 0)
            {
                if (world.Targets.Count < _definition.Capacity)
                {
                    // 출현마다 각도를 AngleStep만큼 돌려 같은 위치에 겹쳐 태어나는 것을 피한다.
                    world.Spawn(_order[_spawned % _order.Count],
                        _definition.Radius, (_spawned * _definition.AngleStep) % ((float)Math.PI * 2));
                    _spawned++;
                }
                _remaining += _definition.Interval;
            }
        }
    }
}
