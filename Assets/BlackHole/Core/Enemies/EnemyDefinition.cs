using System;

namespace BlackHole.Core
{
    // Enemy의 수치 묶음. 종류 정의의 기본 수치와 판 안의 실행 수치(Runtime Stat)가 같은 모양을 쓴다.
    // 둘은 같은 개념이 아니다 — 실행 수치 = 기본 수치 + 보정(EnemyStatCalculator).
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

    // Enemy 종류 하나의 공유 정의: 기본 수치, 행동, 사망 시 보상, 사망 효과.
    public sealed class EnemyDefinition
    {
        public string Id { get; }
        public EnemyStats BaseStats { get; }
        public EnemyBehaviorDefinition Behavior { get; }
        public int Gold { get; }
        public int HqExp { get; }
        public EnemyReward BaseReward => new EnemyReward(Gold, HqExp);
        // 죽을 때 일어나는 효과. null이면 없다.
        public DeathEffectDefinition DeathEffect { get; }

        public EnemyDefinition(
            string id, 
            EnemyStats baseStats,
            EnemyBehaviorDefinition behavior,
            int gold = 0,
            int hqExp = 0,
            DeathEffectDefinition deathEffect = null)
        {
            if (string.IsNullOrWhiteSpace(id)) 
                throw new ArgumentException("ID가 비어 있다.", nameof(id));
            
            if (gold < 0) 
                throw new ArgumentOutOfRangeException(nameof(gold));
            
            if (hqExp < 0) 
                throw new ArgumentOutOfRangeException(nameof(hqExp));
            
            Id = id;
            BaseStats = baseStats;
            Behavior = behavior ?? throw new ArgumentNullException(nameof(behavior), "행동 정의가 필요하다.");
            Gold = gold;
            HqExp = hqExp;
            DeathEffect = deathEffect;
        }
    }

    // 행동 종류의 정의. Enemy 본체는 이것이 어떤 행동인지 모른다(B3).
    // 새 행동: 하위 정의 + 행동 구현(IEnemyBehavior) + EnemyBehaviors.Standard 분기 + ContentLoader의 종류 이름.
    public abstract class EnemyBehaviorDefinition
    {
        private protected EnemyBehaviorDefinition() { }
    }

    // HQ 주위를 돈다. 지금 게임에 있는 유일한 행동이다.
    public sealed class OrbitHqBehaviorDefinition : EnemyBehaviorDefinition
    {
        // [임시] 회전 방향. 값은 샘플 콘텐츠가 정한다.
        public bool Clockwise { get; }

        public OrbitHqBehaviorDefinition(bool clockwise)
        {
            Clockwise = clockwise;
        }
    }
}
