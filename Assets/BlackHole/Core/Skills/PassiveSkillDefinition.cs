using System;

namespace BlackHole.Core
{
    // Skill이 어디를 기준으로 발동하는가. 모든 Skill이 AimPoint 기반이라고 가정하지 않기 위해 데이터로 둔다.
    // 지금 값은 하나다. PlayerCharacter 주변, HQ 주변 등은 실제 Skill이 생길 때 추가한다.
    public enum SkillOrigin { OwnerAimPoint }

    // Passive Skill의 수치 묶음. 기본 수치와 실행 수치가 같은 모양을 쓴다(실행 = 기본 + 보정).
    public readonly struct PassiveSkillStats
    {
        public float Radius { get; }
        // 공격 주기(초).
        public float Interval { get; }
        public float Damage { get; }

        public PassiveSkillStats(float radius, float interval, float damage)
        {
            Radius = DefinitionGuard.Positive(radius, nameof(radius));
            Interval = DefinitionGuard.Positive(interval, nameof(interval));
            Damage = DefinitionGuard.Positive(damage, nameof(damage));
        }
    }

    // Passive Skill 하나의 공유 정의. 지금 있는 Skill은 "기준점 주변 원 안의 Enemy를 주기마다 공격"하는 한 가지다.
    // 동작이 다른 두 번째 Skill이 생기기 전에는 Skill 종류를 나누지 않는다.
    public sealed class PassiveSkillDefinition
    {
        public string Id { get; }
        public SkillOrigin Origin { get; }
        public PassiveSkillStats BaseStats { get; }

        public PassiveSkillDefinition(string id, SkillOrigin origin, PassiveSkillStats baseStats)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("ID가 비어 있다.", nameof(id));
            if (!Enum.IsDefined(typeof(SkillOrigin), origin))
                throw new ArgumentOutOfRangeException(nameof(origin), $"정의되지 않은 기준점 종류 값 {(int)origin}.");
            Id = id;
            Origin = origin;
            BaseStats = baseStats;
        }
    }
}
