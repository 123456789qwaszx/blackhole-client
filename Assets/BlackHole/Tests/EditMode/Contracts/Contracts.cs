using System;
using System.Collections.Generic;

namespace BlackHole.Core.Tests
{
    // 계약 하나: 이름과 실행. Unity EditMode(ContractTests)와 .NET 실행기(tests/CoreSmoke)가 같은 목록을 실행한다.
    // 이름은 "시스템.사례" 형식으로 쓴다.
    public readonly struct Contract
    {
        public string Name { get; }
        public Action Run { get; }

        public Contract(string name, Action run)
        {
            Name = name;
            Run = run;
        }
    }

    // 계약은 시스템별 파일에 두고 여기에서 모은다. 엔진 대역은 쓰지 않는다.
    public static class Contracts
    {
        public static IEnumerable<Contract> All()
        {
            foreach (Contract contract in HarnessContracts.Cases()) yield return contract;
            foreach (Contract contract in ContentContracts.Cases()) yield return contract;
            foreach (Contract contract in SessionContracts.Cases()) yield return contract;
            foreach (Contract contract in WorldContracts.Cases()) yield return contract;
            foreach (Contract contract in EnemyContracts.Cases()) yield return contract;
            foreach (Contract contract in SkillContracts.Cases()) yield return contract;
            foreach (Contract contract in LaserContracts.Cases()) yield return contract;
            foreach (Contract contract in DeathContracts.Cases()) yield return contract;
            foreach (Contract contract in DeathEffectContracts.Cases()) yield return contract;
            foreach (Contract contract in GrowthContracts.Cases()) yield return contract;
            foreach (Contract contract in UpgradeContracts.Cases()) yield return contract;
            foreach (Contract contract in UpgradeSkillContracts.Cases()) yield return contract;
        }
    }
}
