using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 판에 참가하는 사용자 단위의 식별자. 누가 참가하는지는 호스트가 정한다(지금은 로컬 1명).
    public readonly struct PlayerId : IEquatable<PlayerId>
    {
        public int Value { get; }

        public PlayerId(int value)
        {
            Value = value;
        }

        public bool Equals(PlayerId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is PlayerId other && Equals(other);
        public override int GetHashCode() => Value;
        public override string ToString() => $"Player {Value}";
    }

    // 게임 안의 사용자 단위(B1). 판은 Player 목록을 가진다 — 지금 1명일 뿐 하나로 고정된 것이 아니다.
    // PlayerCharacter·Character는 지금 게임플레이 책임이 없어 두지 않는다(미래에 Player가 소유할 수 있다).
    public sealed class Player
    {
        private readonly List<PassiveSkill> _skills = new List<PassiveSkill>();

        public PlayerId Id { get; }
        public PlayerState State { get; } = new PlayerState();
        // 이 Player의 조준점. 누가 채우는지는 모른다 — 지금은 호스트가 마우스 위치로 채운다.
        // Player가 마우스를 가진다는 뜻이 아니다. 없으면 null.
        public Point2? AimPoint { get; private set; }
        // 이 Player가 가진 Passive Skill. 지금은 콘텐츠의 시작 구성으로 판 조립 때 정해진다(획득 구조 없음).
        public IReadOnlyList<PassiveSkill> Skills { get; }

        internal Player(PlayerId id)
        {
            Id = id;
            Skills = _skills.AsReadOnly();
        }

        internal void SetAimPoint(Point2? aimPoint) => AimPoint = aimPoint;

        internal void AddSkill(PassiveSkill skill) => _skills.Add(skill);
    }

    // Player 한 명의 진행 상태. Player마다 따로 있다.
    // 구매 재화. HQ 성장 EXP는 HQ의 상태다.
    public sealed class PlayerState
    {
        public int Gold { get; private set; }

        internal PlayerState() { }

        internal void EarnGold(int amount) => Gold = checked(Gold + amount);
    }
}
