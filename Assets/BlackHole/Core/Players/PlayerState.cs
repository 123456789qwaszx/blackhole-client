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

        public bool Equals(PlayerId other) =>
            Value == other.Value;
        public override bool Equals(object obj) =>
            obj is PlayerId other && Equals(other);
        public override int GetHashCode() => Value;
        public override string ToString() => $"Player {Value}";
    }

    // Player 한 명의 진행 상태: Gold. 전투 사이에 유지된다(앱 종료 후 저장은 하지 않는다).
    // 새 진행은 새 PlayerState로 시작한다. 판은 PlayerState 목록을 받는다 — 지금 1명일 뿐 하나로 고정된 것이 아니다.
    // 업그레이드(산 노드)는 업그레이드 시스템과 함께 지웠다. 돌아오면 산 노드도 여기에 기록된다.
    public sealed class PlayerState
    {
        public PlayerId Id { get; }
        public int Gold { get; private set; }
        // 진행 중인 전투에 들어가 있는가. 한 진행 상태는 한 번에 한 전투에만 들어간다.
        public bool InBattle { get; private set; }

        public PlayerState(PlayerId id)
        {
            Id = id;
        }

        // Gold를 더한다. 지금 부르는 곳은 없다 — 처치 보상이 붙으면 전투가 부른다.
        public void EarnGold(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "0 이상이어야 한다.");

            Gold = checked(Gold + amount);
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
