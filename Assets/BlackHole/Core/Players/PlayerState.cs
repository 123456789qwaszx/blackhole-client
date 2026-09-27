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

    // Player 한 명의 진행 상태: Gold, 산 노드, 블랙홀의 누적 EXP. 전투 사이에 유지된다(앱 종료 후 저장은 하지 않는다).
    // 새 진행은 새 PlayerState로 시작한다. 판은 PlayerState 목록을 받는다 — 지금 1명일 뿐 하나로 고정된 것이 아니다.
    // 산 노드는 전투 밖에서만 바뀐다(NodePurchase.TryPurchase, 전투 중에는 살 수 없다).
    public sealed class PlayerState
    {
        private readonly List<string> _ownedNodes = new List<string>();
        private readonly HashSet<string> _owned = new HashSet<string>(StringComparer.Ordinal);

        public PlayerId Id { get; }
        // 원작의 금액은 T(조) 단위까지 오르므로 int(약 21억)가 아니라 long이다.
        public long Gold { get; private set; }
        // 산 노드의 ID(산 순서). ID로 기록하므로 트리를 다시 불러와도 이어진다.
        public IReadOnlyList<string> OwnedNodes { get; }
        // 진행 중인 전투에 들어가 있는가. 한 진행 상태는 한 번에 한 전투에만 들어간다.
        public bool InBattle { get; private set; }
        // 블랙홀의 누적 EXP. 판이 이어받아 키우고 결산이 돌려놓는다. 줄지 않는다(BLACKHOLE_LEVEL_PLAN 4.1).
        // Level과 이정표 진행도는 Level 표와 이 값으로 계산한다(Hq) — 따로 저장하지 않는다.
        public long HqExp { get; private set; }

        public PlayerState(PlayerId id)
        {
            Id = id;
            OwnedNodes = _ownedNodes.AsReadOnly();
        }

        public bool Owns(string nodeId) =>
            nodeId != null && _owned.Contains(nodeId);

        // Gold를 더한다. 전투 중에는 부르지 않는다 — 판이 끝난 뒤 결산(GameSession.Settle)이 그 판이 번 Gold로 한 번 부른다.
        // 개발용 업그레이드 콘솔도 전투 밖에서 부른다. 그래서 진행 상태는 전투 밖에서만 바뀌고, 저장 시점도 전투 밖이다.
        public void EarnGold(long amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "0 이상이어야 한다.");

            Gold = checked(Gold + amount);
        }

        // 결산(GameSession.Settle)이 판의 블랙홀 EXP를 돌려놓는다. 판은 이 값에서 시작했으므로 줄지 않는다.
        internal void KeepHqExp(long exp)
        {
            if (exp < HqExp)
                throw new ArgumentOutOfRangeException(nameof(exp), $"블랙홀 EXP는 줄지 않는다. 지금 {HqExp}, 받은 값 {exp}.");

            HqExp = exp;
        }

        // 구매 규칙(NodePurchase.TryPurchase)이 확인한 뒤에만 부른다.
        internal void Buy(NodeDefinition node)
        {
            Gold -= node.Price;
            Own(node.Id);
        }

        // 아래는 개발용 치트(ProgressCheats)만 부른다. 구매 규칙을 거치지 않는다.

        // 가진 Gold보다 많이 빼면 0이 된다. 실제로 뺀 양을 돌려준다.
        internal long TakeGold(long amount)
        {
            long taken = Math.Min(amount, Gold);
            Gold -= taken;
            return taken;
        }

        internal void Own(string nodeId)
        {
            if (_owned.Add(nodeId))
                _ownedNodes.Add(nodeId);
        }

        internal void ForgetAllNodes()
        {
            _owned.Clear();
            _ownedNodes.Clear();
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
