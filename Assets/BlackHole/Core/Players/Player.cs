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

        public bool Equals(PlayerId other) =>
            Value == other.Value;
        public override bool Equals(object obj) =>
            obj is PlayerId other && Equals(other);
        public override int GetHashCode() => Value;
        public override string ToString() => $"Player {Value}";
    }

    // 게임 안의 사용자 단위(B1). 판은 Player 목록을 가진다 — 지금 1명일 뿐 하나로 고정된 것이 아니다.
    // PlayerCharacter·Character는 지금 게임플레이 책임이 없어 두지 않는다(미래에 Player가 소유할 수 있다).
    // Player는 한 전투의 실행 상태(조준점, Skill)를 가진다. 전투 사이에 남는 진행 상태는 PlayerState다.
    public sealed class Player
    {
        private readonly List<PassiveSkill> _skills = new List<PassiveSkill>();

        public PlayerId Id => State.Id;
        public PlayerState State { get; }

        // 이 Player의 조준점. 누가 채우는지는 모른다 — 지금은 호스트가 마우스 위치로 채운다.
        // Player가 마우스를 가진다는 뜻이 아니다. 없으면 null.
        public Point2? AimPoint { get; private set; }
        // 이 Player가 가진 Passive Skill.
        // 지금은 콘텐츠의 시작 구성으로 판 조립 때 정해진다(획득 구조 없음).

        public IReadOnlyList<PassiveSkill> Skills { get; }

        internal Player(PlayerState state)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            Skills = _skills.AsReadOnly();
        }

        internal void SetAimPoint(Point2? aimPoint) => AimPoint = aimPoint;

        internal void AddSkill(PassiveSkill skill) => _skills.Add(skill);
    }

    // Player 한 명의 진행 상태: Gold와 산 업그레이드 노드. 전투 사이에 유지된다(앱 종료 후 저장은 하지 않는다).
    // HQ 성장 EXP는 HQ의 상태다. 새 진행은 새 PlayerState로 시작한다.
    public sealed class PlayerState
    {
        private readonly List<string> _upgrades = new List<string>();
        private readonly HashSet<string> _owned = new HashSet<string>(StringComparer.Ordinal);

        public PlayerId Id { get; }
        public int Gold { get; private set; }
        // 산 노드의 ID(산 순서). ID로 기록하므로 콘텐츠를 다시 불러와도 이어진다.
        public IReadOnlyList<string> Upgrades { get; }
        // 진행 중인 전투에 들어가 있는가. 이 동안에는 구매할 수 없다.
        public bool InBattle { get; private set; }

        public PlayerState(PlayerId id)
        {
            Id = id;
            Upgrades = _upgrades.AsReadOnly();
        }

        public bool Owns(string upgradeId) =>
            upgradeId != null && _owned.Contains(upgradeId);

        internal void EarnGold(int amount) => Gold = checked(Gold + amount);

        // 구매 규칙(UpgradePurchase)이 확인한 뒤에만 부른다.
        internal void Buy(UpgradeNodeDefinition node)
        {
            Gold -= node.Price;
            _owned.Add(node.Id);
            _upgrades.Add(node.Id);
        }

        internal void EnterBattle()
        {
            if (InBattle)
                throw new InvalidOperationException($"{Id}는 이미 진행 중인 전투에 들어가 있다.");

            InBattle = true;
        }

        internal void LeaveBattle() => InBattle = false;
    }
}
