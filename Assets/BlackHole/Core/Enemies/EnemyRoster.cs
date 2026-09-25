using System.Collections.Generic;

namespace BlackHole.Core
{
    // 한 판의 적 목록과 사망 절차. 적 시스템이 판 안에서 가진 상태는 여기에 모인다.
    // - 출현: 정의와 위치로 적을 만들고 번호를 준다. 실행 수치는 이때 정해진다.
    // - 이동: 살아 있는 적이 행동에 따라 움직인다.
    // - 피해: 살아 있는 적만 받는다. 처음 죽은 순간 목록에서 빠지고 사망 기록을 한 번 남긴다.
    // 사망 보상(HQ EXP·Gold)과 사망 효과는 그 시스템이 사망 기록이나 이 절차에 붙는다.
    // 판이 끝나며 남은 적이 사라지는 것은 처치가 아니다 — 판을 버릴 뿐 이 절차를 거치지 않는다.
    internal sealed class EnemyRoster
    {
        private readonly List<Enemy> _alive = new List<Enemy>();
        private readonly List<DeathRecord> _deaths = new List<DeathRecord>();
        private int _nextEnemyId = 1;
        private long _nextDeathSequence = 1;

        // 살아 있는 적(출현 순서). 죽은 적은 즉시 빠진다.
        public IReadOnlyList<Enemy> Alive { get; }
        // 마지막 진행 동안 확정된 사망. 다음 진행이 시작될 때 비운다.
        public IReadOnlyList<DeathRecord> Deaths { get; }

        public EnemyRoster()
        {
            Alive = _alive.AsReadOnly();
            Deaths = _deaths.AsReadOnly();
        }

        public Enemy Spawn(EnemyDefinition definition, Point2 position)
        {
            var enemy = new Enemy(
                new EnemyId(_nextEnemyId++),
                definition,
                definition.BaseStats,
                position,
                EnemyBehaviors.Create(definition.Behavior));

            _alive.Add(enemy);
            return enemy;
        }

        public void Move(float delta)
        {
            for (int i = 0; i < _alive.Count; i++)
                _alive[i].Move(delta);
        }

        // true는 이번 피해로 처음 죽었다는 뜻이다.
        public bool DealDamage(Enemy enemy, Damage damage)
        {
            if (!enemy.ApplyDamage(damage))
                return false;

            _deaths.Add(new DeathRecord(_nextDeathSequence++, enemy));
            _alive.Remove(enemy);
            return true;
        }

        public void BeginAdvance() => _deaths.Clear();
    }
}
