using System;

namespace BlackHole.Core
{
    // Passive Skill 하나의 공유 정의. 공통으로 가지는 것은 ID뿐이다.
    // 무엇을 언제 어디에 하는지는 종류마다 다르다 — Aim Point를 언제 읽는지도 실행 방식의 일부다.
    // 새 종류: 하위 정의 + 실행 상태(PassiveSkill) + PassiveSkills.Create 분기 + ContentLoader의 종류 이름.
    public abstract class PassiveSkillDefinition
    {
        public string Id { get; }

        private protected PassiveSkillDefinition(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("ID가 비어 있다.", nameof(id));
            Id = id;
        }
    }

    // Breaker의 수치 묶음. 기본 수치와 실행 수치가 같은 모양을 쓴다(실행 = 기본 + 보정).
    public readonly struct BreakerStats
    {
        public float Radius { get; }
        // 공격 주기(초).
        public float Interval { get; }
        public float Damage { get; }

        public BreakerStats(float radius, float interval, float damage)
        {
            Radius = DefinitionGuard.Positive(radius, nameof(radius));
            Interval = DefinitionGuard.Positive(interval, nameof(interval));
            Damage = DefinitionGuard.Positive(damage, nameof(damage));
        }
    }

    // Breaker: 주기마다 소유 Player의 현재 Aim Point 주변 원 안의 Enemy를 전부 공격한다.
    public sealed class BreakerSkillDefinition : PassiveSkillDefinition
    {
        public BreakerStats BaseStats { get; }

        public BreakerSkillDefinition(string id, BreakerStats baseStats) : base(id)
        {
            BaseStats = baseStats;
        }
    }

    // 관통 레이저의 수치 묶음. 기본 수치와 실행 수치가 같은 모양을 쓴다.
    public readonly struct PiercingLaserStats
    {
        // 발사와 다음 발사 사이의 시간(초). 예고는 0, Interval, 2·Interval, …에 시작한다.
        public float Interval { get; }
        public float Damage { get; }
        // 판정과 표시에 쓰는 굵기.
        public float Width { get; }
        // 예고 시작부터 발사까지의 시간(초). Interval보다 길면 예고가 겹친다.
        public float TelegraphDuration { get; }

        public PiercingLaserStats(float interval, float damage, float width, float telegraphDuration)
        {
            Interval = DefinitionGuard.Positive(interval, nameof(interval));
            Damage = DefinitionGuard.Positive(damage, nameof(damage));
            Width = DefinitionGuard.Positive(width, nameof(width));
            TelegraphDuration = DefinitionGuard.Positive(telegraphDuration, nameof(telegraphDuration));
        }
    }

    // 관통 레이저: 주기마다 전투 영역 경계의 무작위 지점에서, 예고 시작 순간의 Aim Point를 향해 쏜다.
    // 예고가 끝나는 순간 경로의 폭 안에 있는 Enemy에게 한 번씩 피해를 준다(CONTENT_DEFINITION 2.3).
    public sealed class PiercingLaserDefinition : PassiveSkillDefinition
    {
        public PiercingLaserStats BaseStats { get; }
        // [임시] HQ로부터 시작점까지의 거리이자 경로가 끝나는 경계. Skill 수치가 아니라 이 레이저의 공간 값이다.
        // 화면이 이 원 안쪽만 보여 줘야 시작점이 화면 밖에 있다.
        public float BoundaryRadius { get; }

        public PiercingLaserDefinition(string id, PiercingLaserStats baseStats, float boundaryRadius) : base(id)
        {
            BaseStats = baseStats;
            BoundaryRadius = DefinitionGuard.Positive(boundaryRadius, nameof(boundaryRadius));
        }
    }
}
