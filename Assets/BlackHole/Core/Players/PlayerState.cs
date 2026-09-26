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

    // Player 한 명의 진행 상태: Gold와 산 노드. 앱 종료 후 저장은 하지 않는다(F04).
    // 새 진행은 새 PlayerState로 시작한다. 지금 1명일 뿐 하나로 고정된 것이 아니다.
    // 산 노드는 구매 규칙(NodePurchase.TryPurchase)으로만 늘어난다.
    public sealed class PlayerState
    {
        private readonly List<string> _ownedNodes = new List<string>();
        private readonly HashSet<string> _owned = new HashSet<string>(StringComparer.Ordinal);

        public PlayerId Id { get; }
        // 원작의 금액은 T(조) 단위까지 오르므로 int(약 21억)가 아니라 long이다.
        public long Gold { get; private set; }
        // 산 노드의 ID(산 순서). ID로 기록하므로 트리를 다시 불러와도 이어진다.
        public IReadOnlyList<string> OwnedNodes { get; }

        public PlayerState(PlayerId id)
        {
            Id = id;
            OwnedNodes = _ownedNodes.AsReadOnly();
        }

        public bool Owns(string nodeId) =>
            nodeId != null && _owned.Contains(nodeId);

        // Gold를 더한다. 지금 부르는 곳은 개발용 콘솔뿐이다 — 처치 보상이 붙으면 그쪽이 부른다.
        public void EarnGold(long amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "0 이상이어야 한다.");

            Gold = checked(Gold + amount);
        }

        // 구매 규칙(NodePurchase.TryPurchase)이 확인한 뒤에만 부른다.
        internal void Buy(NodeDefinition node)
        {
            Gold -= node.Price;
            _owned.Add(node.Id);
            _ownedNodes.Add(node.Id);
        }
    }
}
