using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BlackHole.Core
{
    // 한 판에 존재하는 대상 목록, ID 발급, 대상 공통 규칙(하한·낙하)의 적용을 소유한다.
    // 출현 시점·종류·위치는 SpawnSchedule이 정하고, 제거는 흡수 확정 뒤에만 일어난다.
    internal sealed class TargetWorld
    {
        private readonly TargetRulesDefinition _rules;
        private readonly List<TargetState> _targets = new List<TargetState>();
        private readonly ReadOnlyCollection<TargetState> _view;
        private int _nextId = 1;

        public IReadOnlyList<TargetState> Targets => _view;

        public TargetWorld(TargetRulesDefinition rules)
        {
            _rules = rules;
            _view = _targets.AsReadOnly();
        }

        // 출현 요청: 정의와 위치만 받는다. 이동 규칙은 정의에서 해석한다.
        public void Spawn(TargetDefinition definition, float radius, float angle)
        {
            IMovementRule movement = MovementRuleFactory.Create(definition.Movement);
            _targets.Add(new TargetState(_nextId++, definition, movement, radius, angle));
        }

        public void Move(float delta, float absorptionRadius)
        {
            float aliveFloor = absorptionRadius + _rules.AliveMargin;
            foreach (TargetState target in _targets)
                target.Move(delta, aliveFloor, _rules.FallSpeed);
        }

        public TargetState Find(int id)
        {
            foreach (TargetState target in _targets)
                if (target.Id == id) return target;
            return null;
        }

        public void RemoveAbsorbed()
        {
            for (int i = _targets.Count - 1; i >= 0; i--)
                if (_targets[i].Phase == TargetPhase.Absorbed) _targets.RemoveAt(i);
        }
    }
}
