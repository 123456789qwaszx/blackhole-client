using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 한 판을 조립하는 데 필요한 검증된 공유 정의 묶음. 읽기 전용이며 여러 판이 함께 쓴다.
    //
    // 생성자 보장(구현 = ContentInvariants):
    // [1] 대상·스킬·강화 ID가 유일하다.
    // [2] 출현 순서의 대상 ID가 실재한다.
    // [3] 모든 스킬과 대상 이동이 실행 규칙으로 해석된다.
    // 오류가 있는 콘텐츠의 위치별 보고는 ContentLoader가 맡는다.
    public sealed class ContentCatalog
    {
        private readonly Dictionary<string, TargetDefinition> _targetsById;

        public TimeLimitDefinition TimeLimit { get; }
        public TargetRulesDefinition TargetRules { get; }
        public BlackHoleDefinition BlackHole { get; }
        public IReadOnlyList<TargetDefinition> Targets { get; }
        // 현재 스킬 사용자 한 명이 보유하는 스킬. 사용자가 여럿이 되면 사용자별 보유 목록으로 나눈다.
        public IReadOnlyList<SkillDefinition> Skills { get; }
        public IReadOnlyList<UpgradeDefinition> Upgrades { get; }
        public SpawnDefinition Spawn { get; }
        // Spawn.TargetOrder를 해석한 정의 목록.
        internal IReadOnlyList<TargetDefinition> SpawnOrder { get; }

        public ContentCatalog(
            TimeLimitDefinition timeLimit,
            TargetRulesDefinition targetRules,
            BlackHoleDefinition blackHole,
            IReadOnlyList<TargetDefinition> targets,
            IReadOnlyList<SkillDefinition> skills,
            IReadOnlyList<UpgradeDefinition> upgrades,
            SpawnDefinition spawn)
        {
            TimeLimit = timeLimit ?? throw new ArgumentNullException(nameof(timeLimit));
            TargetRules = targetRules ?? throw new ArgumentNullException(nameof(targetRules));
            BlackHole = blackHole ?? throw new ArgumentNullException(nameof(blackHole));
            Targets = Copy(targets);
            Skills = Copy(skills);
            Upgrades = Copy(upgrades);
            Spawn = spawn;

            var diagnostics = new List<ContentDiagnostic>();
            ContentInvariants.Collect(Targets, Skills, Upgrades, Spawn, diagnostics, out _targetsById);
            if (diagnostics.Count > 0)
                throw new ArgumentException(diagnostics[0].ToString());

            var order = new TargetDefinition[Spawn.TargetOrder.Count];
            for (int i = 0; i < order.Length; i++)
                order[i] = _targetsById[Spawn.TargetOrder[i]];
            SpawnOrder = Array.AsReadOnly(order);
        }

        public bool TryGetTarget(string id, out TargetDefinition target)
        {
            target = null;
            return id != null && _targetsById.TryGetValue(id, out target);
        }

        // 호출자가 원본 목록을 나중에 바꿔도 카탈로그가 따라 바뀌지 않게 한다.
        private static IReadOnlyList<T> Copy<T>(IReadOnlyList<T> source)
        {
            if (source == null) return Array.Empty<T>();
            var copy = new T[source.Count];
            for (int i = 0; i < copy.Length; i++) copy[i] = source[i];
            return Array.AsReadOnly(copy);
        }
    }
}
