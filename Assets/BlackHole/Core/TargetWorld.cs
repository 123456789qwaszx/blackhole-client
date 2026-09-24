using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BlackHole.Core
{
    // 한 판에 존재하는 대상 목록과 ID 발급을 소유한다.
    public sealed class TargetWorld
    {
        private readonly List<TargetState> _targets = new List<TargetState>();
        private readonly ReadOnlyCollection<TargetState> _view;
        private int _nextId = 1;

        public IReadOnlyList<TargetState> Targets => _view;

        public TargetWorld() { _view = _targets.AsReadOnly(); }

        internal void Spawn(TargetDefinition definition, float radius, float angle)
        {
            _targets.Add(new TargetState(_nextId++, definition, radius, angle));
        }

        internal void Move(float delta, float absorptionRadius)
        {
            foreach (TargetState target in _targets)
                target.Move(delta, absorptionRadius);
        }

        internal TargetState Find(int id)
        {
            foreach (TargetState target in _targets)
                if (target.Id == id) return target;
            return null;
        }

        internal void RemoveAbsorbed()
        {
            for (int i = _targets.Count - 1; i >= 0; i--)
                if (_targets[i].Phase == TargetPhase.Absorbed) _targets.RemoveAt(i);
        }
    }
}
