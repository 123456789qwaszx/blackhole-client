using System;

namespace BlackHole.Core
{
    // 개발용 치트: 진행 상태를 구매 규칙을 거치지 않고 바꾼다. 개발용 콘솔만 부른다.
    // 게임 규칙이 아니다. 진행 상태의 약속 두 가지는 지킨다: Gold는 음수가 되지 않고, 진행 상태는 전투 밖에서만 바뀐다.
    // 그래서 전투 중에는 모두 거부한다(InvalidOperationException).
    public static class ProgressCheats
    {
        // Gold를 뺀다. 가진 것보다 많이 빼면 0이 된다. 실제로 뺀 양을 돌려준다.
        public static long TakeGold(PlayerState state, long amount)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            RefuseDuringBattle(state);

            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "0 이상이어야 한다.");

            return state.TakeGold(amount);
        }

        // 트리의 모든 노드를 산 것으로 한다. Gold를 쓰지 않고, 숨은 노드도 포함한다.
        public static void UnlockAllNodes(PlayerState state, NodeTree tree)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            RefuseDuringBattle(state);

            if (tree == null)
                throw new ArgumentNullException(nameof(tree));

            foreach (NodeDefinition node in tree.Nodes)
                state.Own(node.Id);
        }

        // 산 노드를 모두 지운다. Gold는 돌려주지 않는다.
        public static void LockAllNodes(PlayerState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            RefuseDuringBattle(state);

            state.ForgetAllNodes();
        }

        private static void RefuseDuringBattle(PlayerState state)
        {
            if (state.InBattle)
                throw new InvalidOperationException($"{state.Id}는 전투 중이라 진행 상태를 바꿀 수 없다.");
        }
    }
}
