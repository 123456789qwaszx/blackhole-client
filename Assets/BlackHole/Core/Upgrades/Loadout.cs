using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 산 노드 → 판 조립의 입력. 노드 트리와 전투 조립이 만나는 유일한 자리다(SKILL_TREE_PLAN 4.4).
    // 지금 있는 몫은 적 종류의 판 구성뿐이다(BATTLE_COMPOSITION_PLAN BC-005).
    public static class Loadout
    {
        // 참가자가 산 노드의 적 Grant를 모아 콘텐츠의 모든 종류에 대한 판 구성을 만든다. 산 순서와 무관하다.
        // 수치마다 기본값(질량 단계 0, 황금 비율 0, 황금 배율 = 종류의 기본값)에서 시작해
        // 정하기(가장 큰 값) → 더하기(합) → 곱하기(곱) 순서로 합친다 [임시]. 황금 비율은 1을 넘지 않는다.
        // 적 종류는 모든 참가자가 함께 쓰므로, 지금 실제 구성인 참가자 1명의 노드만 쓴다. 둘 이상이면 노드 보정 없이 기본값이다
        // (여러 Player의 구매를 공유 대상에 합치는 정책은 미정이다. d71c0f4의 전제와 같다).
        public static IReadOnlyDictionary<EnemyDefinition, EnemyComposition> EnemiesFor(
            GameContent content, IReadOnlyList<PlayerState> participants)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            var sums = new Dictionary<EnemyDefinition, Sum[]>();

            if (participants != null && participants.Count == 1)
            {
                PlayerState player = participants[0];

                foreach (UpgradeNodeDefinition node in content.Upgrades)
                {
                    if (!player.Owns(node.Id))
                        continue;

                    foreach (EnemyGrant grant in node.Grants)
                    {
                        if (!sums.TryGetValue(grant.Enemy, out Sum[] stats))
                        {
                            stats = new[] { Sum.None, Sum.None, Sum.None };
                            sums.Add(grant.Enemy, stats);
                        }

                        stats[(int)grant.Stat] = stats[(int)grant.Stat].With(grant.Operation, grant.Value);
                    }
                }
            }

            var compositions = new Dictionary<EnemyDefinition, EnemyComposition>();

            foreach (EnemyDefinition kind in content.Enemies)
            {
                EnemyComposition composition = EnemyComposition.Base(kind);

                if (sums.TryGetValue(kind, out Sum[] stats))
                {
                    composition = new EnemyComposition(
                        (int)stats[(int)EnemyUpgradeStat.MassLevel].Apply(composition.MassLevel),
                        Math.Min(1, stats[(int)EnemyUpgradeStat.GoldenRatio].Apply(composition.GoldenRatio)),
                        stats[(int)EnemyUpgradeStat.GoldenMultiplier].Apply(composition.GoldenMultiplier));
                }

                compositions.Add(kind, composition);
            }

            return compositions;
        }

        // 한 수치에 모인 Grant. 산 순서와 무관하게 합쳐진다.
        private readonly struct Sum
        {
            public static readonly Sum None = new Sum(false, 0, 0, 1);

            private readonly bool _hasSet;
            private readonly float _set;
            private readonly float _add;
            private readonly float _multiply;

            private Sum(bool hasSet, float set, float add, float multiply)
            {
                _hasSet = hasSet;
                _set = set;
                _add = add;
                _multiply = multiply;
            }

            public Sum With(GrantOperation operation, float value)
            {
                switch (operation)
                {
                    case GrantOperation.Set: return new Sum(true, _hasSet ? Math.Max(_set, value) : value, _add, _multiply);
                    case GrantOperation.Add: return new Sum(_hasSet, _set, _add + value, _multiply);
                    default: return new Sum(_hasSet, _set, _add, _multiply * value);
                }
            }

            public float Apply(float baseValue) => ((_hasSet ? _set : baseValue) + _add) * _multiply;
        }
    }
}
