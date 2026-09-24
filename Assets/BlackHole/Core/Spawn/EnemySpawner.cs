using System;

namespace BlackHole.Core
{
    // 공급된 Enemy를 실제로 만든다: HQ 기준 위치, 출현 때 확정하는 실행 수치와 보상, 행동.
    // 나온 뒤의 움직임은 Enemy의 행동이 맡는다.
    //
    // [임시] 배치: 판 안에서 n번째로 나오는 Enemy는 HQ로부터 Distance, 각도 n × AngleStep에 놓인다.
    internal sealed class EnemySpawner
    {
        private readonly SpawnDefinition _definition;
        private readonly EnemyBehaviorResolver _behaviors;
        private readonly UpgradeModifiers _modifiers;
        private readonly IEnemyStatModifier[] _statModifiers;
        private int _spawned;

        public EnemySpawner(
            SpawnDefinition definition,
            EnemyBehaviorResolver behaviors,
            UpgradeModifiers modifiers)
        {
            _definition = definition;
            _behaviors = behaviors;
            _modifiers = modifiers;
            _statModifiers = new IEnemyStatModifier[] { modifiers };
        }

        public void Spawn(EnemyDefinition definition, World world)
        {
            float angle = _spawned * _definition.AngleStep;
            Point2 hq = world.Hq.Position;
            var position = new Point2(
                hq.X + _definition.Distance * (float)Math.Cos(angle),
                hq.Y + _definition.Distance * (float)Math.Sin(angle));

            // 실행 수치와 보상은 출현 때 한 번 확정한다(기본 정의 + 구매 보정). 살아 있는 적에는 다시 적용하지 않는다.
            EnemyStats stats = EnemyStatCalculator.Compute(definition, _statModifiers);
            EnemyReward reward = _modifiers.ApplyToReward(definition, definition.BaseReward);

            world.AddEnemy(
                definition,
                stats,
                reward,
                position,
                _behaviors(definition.Behavior));

            _spawned++;
        }
    }
}
