using System;

namespace BlackHole.Core
{
    // 적의 수치 묶음. 종류의 기본 수치와 판 안의 실행 수치가 같은 모양을 쓴다.
    // 실행 수치는 출현 때 한 번 정해지고, 그 적이 살아 있는 동안 바뀌지 않는다.
    public readonly struct EnemyStats
    {
        public float MaxHealth { get; }
        // 이동 속도(초당 거리). 행동이 이 값을 읽는다.
        public float MoveSpeed { get; }
        // 크기(반지름). 화면이 이 값으로 그린다.
        public float Size { get; }

        public EnemyStats(float maxHealth, float moveSpeed, float size)
        {
            MaxHealth = DefinitionGuard.Positive(maxHealth, nameof(maxHealth));
            MoveSpeed = DefinitionGuard.Positive(moveSpeed, nameof(moveSpeed));
            Size = DefinitionGuard.Positive(size, nameof(size));
        }
    }

    // 적 종류 하나의 공유 정의: 기본 수치, 행동, 사망 효과(특성).
    // 사망 보상은 그 시스템이 붙을 때 더한다. 외형은 Core가 모른다(Unity 쪽 종류 에셋이 가진다).
    public sealed class EnemyDefinition
    {
        public string Id { get; }
        public EnemyStats BaseStats { get; }
        public EnemyBehaviorDefinition Behavior { get; }
        // 이 종류가 죽을 때의 효과. 없으면 null이다. 효과를 가진 적은 사망 효과의 피해를 받지 않는다.
        public DeathEffectDefinition DeathEffect { get; }

        public EnemyDefinition(string id, EnemyStats baseStats, EnemyBehaviorDefinition behavior, DeathEffectDefinition deathEffect = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID가 비어 있다.", nameof(id));

            Id = id;
            BaseStats = baseStats;
            Behavior = behavior ?? throw new ArgumentNullException(nameof(behavior), "행동 정의가 필요하다.");
            DeathEffect = deathEffect;
        }
    }

    // 행동 종류의 정의. 적 본체는 이것이 어떤 행동인지 모른다.
    // 새 행동: 하위 정의 + 행동 구현(IEnemyBehavior) + EnemyBehaviors.Create 분기 + ContentLoader의 종류 이름.
    public abstract class EnemyBehaviorDefinition
    {
        private protected EnemyBehaviorDefinition() { }
    }

    // HQ 주위를 돈다. 지금 게임에 있는 유일한 행동이다(GAME_RULES 7절).
    public sealed class OrbitBehaviorDefinition : EnemyBehaviorDefinition
    {
        public bool Clockwise { get; }

        public OrbitBehaviorDefinition(bool clockwise)
        {
            Clockwise = clockwise;
        }
    }
}
