using System;

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
        public PlayerId Id { get; }
        public PlayerState State { get; } = new PlayerState();

        internal Player(PlayerId id)
        {
            Id = id;
        }
    }

    // Player 한 명의 진행 상태. Player마다 따로 있다.
    // 지금은 담을 값이 없다. M4에서 Gold/EXP가 들어온다. Level은 계산하지 않는다(D4).
    public sealed class PlayerState
    {
        internal PlayerState() { }
    }
}
