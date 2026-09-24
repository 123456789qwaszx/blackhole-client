using System;

namespace BlackHole.Core
{
    // 공급된 Enemy를 실제로 만든다: HQ 기준 위치, 출현 때 확정하는 실행 수치, 행동.
    // 나온 뒤의 움직임은 Enemy의 행동이 맡는다.
    //
    // [임시] 배치: 판 안에서 n번째로 나오는 Enemy는 HQ로부터 Distance, 각도 n × AngleStep에 놓인다.
    internal sealed class EnemySpawner
    {
        private readonly SpawnDefinition _definition;
        private readonly EnemyBehaviorResolver _behaviors;
        private int _spawned;

        public EnemySpawner(
            SpawnDefinition definition,
            EnemyBehaviorResolver behaviors)
        {
            _definition = definition;
            _behaviors = behaviors;
        }

        public void Spawn(EnemyDefinition definition, World world)
        {
            float angle = _spawned * _definition.AngleStep;
            Point2 hq = world.Hq.Position;
            var position = new Point2(
                hq.X + _definition.Distance * (float)Math.Cos(angle),
                hq.Y + _definition.Distance * (float)Math.Sin(angle));

            // 실행 수치는 출현 때 한 번 확정한다. 구매 보정은 M6에서 연결한다(지금은 보정 없음).
            EnemyStats stats = EnemyStatCalculator.Compute(definition, Array.Empty<IEnemyStatModifier>());

            world.AddEnemy(
                definition,
                stats,
                position,
                _behaviors(definition.Behavior));

            _spawned++;
        }
    }
}
