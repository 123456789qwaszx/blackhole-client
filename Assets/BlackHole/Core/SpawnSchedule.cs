using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 이번 샘플은 난수 없이 교대 출현한다. 생성 진행은 대상 행동과 별개다.
    public sealed class SpawnSchedule
    {
        private readonly TargetDefinition[] _definitions;
        private readonly float _interval;
        private readonly float _radius;
        private readonly int _capacity;
        private float _remaining;
        private int _spawned;

        public SpawnSchedule(IReadOnlyList<TargetDefinition> definitions,
            float interval, float radius, int capacity)
        {
            if (definitions == null || definitions.Count == 0)
                throw new ArgumentException("대상 정의가 필요하다.", nameof(definitions));
            _definitions = new TargetDefinition[definitions.Count];
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < definitions.Count; i++)
            {
                TargetDefinition definition = definitions[i];
                if (definition == null || !ids.Add(definition.Id))
                    throw new ArgumentException("대상 정의가 null이거나 ID가 중복됐다.");
                _definitions[i] = definition;
            }
            _interval = DefinitionGuard.Positive(interval, nameof(interval));
            _radius = DefinitionGuard.Positive(radius, nameof(radius));
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            _capacity = capacity;
        }

        internal void Advance(float delta, TargetWorld world)
        {
            _remaining -= delta;
            while (_remaining <= 0)
            {
                if (world.Targets.Count < _capacity)
                {
                    // 황금각 배치로 같은 위치에 겹쳐서 태어나는 것을 피한다.
                    world.Spawn(_definitions[_spawned % _definitions.Length],
                        _radius, (_spawned * 2.399963f) % ((float)Math.PI * 2));
                    _spawned++;
                }
                _remaining += _interval;
            }
        }
    }
}
