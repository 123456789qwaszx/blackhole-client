using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    public enum UpgradeResult { Purchased, InsufficientCredits, MaxLevel, UnknownUpgrade, SessionInactive }

    // 강화의 대상이 되는 두 계산. 범용 능력치 엔진이 아니라 지금 필요한 보정만 둔다.
    public enum UpgradeStat { DamageMultiplier, AbsorptionRadius }

    // 강화 한 종류의 공유 정의. n단계 비용 = BaseCost × n, 단계당 보정 = PerLevel.
    public sealed class UpgradeDefinition
    {
        public string Id { get; }
        public int BaseCost { get; }
        public int MaxLevel { get; }
        public UpgradeStat Stat { get; }
        public float PerLevel { get; }

        public UpgradeDefinition(string id, int baseCost, int maxLevel, UpgradeStat stat, float perLevel)
        {
            Id = DefinitionGuard.Id(id, nameof(id));
            BaseCost = DefinitionGuard.Positive(baseCost, nameof(baseCost));
            MaxLevel = DefinitionGuard.Positive(maxLevel, nameof(maxLevel));
            if ((long)baseCost * maxLevel > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(baseCost), "최고 단계 강화 비용이 int 범위를 넘는다.");
            if (!Enum.IsDefined(typeof(UpgradeStat), stat))
                throw new ArgumentOutOfRangeException(nameof(stat), $"정의되지 않은 강화 대상 값 {(int)stat}.");
            Stat = stat;
            PerLevel = DefinitionGuard.Positive(perLevel, nameof(perLevel));
        }

        public int CostFor(int level) => BaseCost * level;
    }

    // 한 판의 강화별 획득 단계. 획득 여부의 유일한 원본이다(SkillTree도 여기서 읽는다).
    // 보정 값은 저장하지 않고 단계와 정의에서 매번 계산한다.
    // 단계는 UpgradePurchase를 통해서만 오른다.
    public sealed class UpgradeState
    {
        private readonly Dictionary<string, int> _indexById = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly int[] _levels;

        public IReadOnlyList<UpgradeDefinition> Definitions { get; }

        internal UpgradeState(IReadOnlyList<UpgradeDefinition> definitions)
        {
            Definitions = definitions;
            _levels = new int[definitions.Count];
            for (int i = 0; i < definitions.Count; i++)
                _indexById.Add(definitions[i].Id, i);
        }

        public bool Contains(string id) => TryIndex(id, out _);

        // 알 수 없는 ID는 0단계로 읽는다. 구매 요청의 참조 검증은 UpgradePurchase가 한다.
        public int Level(string id) => TryIndex(id, out int i) ? _levels[i] : 0;

        public bool IsMaxed(string id) => TryIndex(id, out int i) && _levels[i] >= Definitions[i].MaxLevel;

        // 다음 단계의 비용. 알 수 없거나 최고 단계면 0.
        public int NextCost(string id)
        {
            if (!TryIndex(id, out int i) || _levels[i] >= Definitions[i].MaxLevel) return 0;
            return Definitions[i].CostFor(_levels[i] + 1);
        }

        // 해당 계산에 대한 획득 강화의 보정 합: Σ 단계 × PerLevel.
        public float Bonus(UpgradeStat stat)
        {
            float bonus = 0;
            for (int i = 0; i < _levels.Length; i++)
                if (Definitions[i].Stat == stat) bonus += _levels[i] * Definitions[i].PerLevel;
            return bonus;
        }

        internal void Raise(string id)
        {
            int i = _indexById[id];
            if (_levels[i] >= Definitions[i].MaxLevel)
                throw new InvalidOperationException($"강화 '{id}'는 이미 최고 단계다.");
            _levels[i]++;
        }

        private bool TryIndex(string id, out int index)
        {
            index = -1;
            return id != null && _indexById.TryGetValue(id, out index);
        }
    }

    // 강화 구매의 유일한 경로. 모든 판정을 끝낸 뒤에만 상태를 바꾼다.
    //
    // 1. 참조: 알 수 없는 강화면 거절한다.
    // 2. 자격: 최고 단계면 거절한다.
    // 3. 비용: 잔액이 부족하면 거절한다.
    // 4. 확정: 재화 차감 → 단계 상승. 1~3이 실패 조건을 모두 소진했으므로 두 변경은 실패하지 않는다.
    //    확정 구간에는 외부 I/O나 임의 콜백을 두지 않는다. 두 객체를 연속으로 바꾼다는 사실만으로
    //    원자성이 생기는 것이 아니라, 앞선 판정 때문에 부분 변경이 일어날 수 없는 것이다.
    internal sealed class UpgradePurchase
    {
        private readonly UpgradeState _upgrades;
        private readonly WalletState _wallet;

        public UpgradePurchase(UpgradeState upgrades, WalletState wallet)
        {
            _upgrades = upgrades;
            _wallet = wallet;
        }

        public UpgradeResult TryPurchase(string id)
        {
            if (!_upgrades.Contains(id)) return UpgradeResult.UnknownUpgrade;
            if (_upgrades.IsMaxed(id)) return UpgradeResult.MaxLevel;

            int cost = _upgrades.NextCost(id);
            if (!_wallet.CanAfford(cost)) return UpgradeResult.InsufficientCredits;

            _wallet.Withdraw(cost);
            _upgrades.Raise(id);
            return UpgradeResult.Purchased;
        }
    }
}
