using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 한 판을 조립하는 데 필요한 검증된 공유 정의 묶음. 읽기 전용이며 여러 판이 함께 쓴다.
    //
    // 생성자 보장(구현 = ContentInvariants):
    // [1] Enemy·Skill ID가 유일하다.
    // [2] 시작 Skill ID가 실재하고 중복되지 않는다.
    // 전투 시작 배치와 성장 노드는 Enemy를 정의 객체로 참조한다(ContentLoader가 ID를 해석하며 진단한다).
    // 오류가 있는 콘텐츠의 경로별 보고는 ContentLoader가 맡는다.
    public sealed class GameContent
    {
        private readonly Dictionary<string, EnemyDefinition> _enemiesById;
        private readonly Dictionary<string, PassiveSkillDefinition> _skillsById;

        public TimeLimitDefinition TimeLimit { get; }
        public HqDefinition Hq { get; }
        public IReadOnlyList<EnemyDefinition> Enemies { get; }
        public SpawnDefinition Spawn { get; }
        // 전투 시작 배치. 판 조립 때 한 번 공급한다.
        public IReadOnlyList<SupplyRequest> StartSupply { get; }
        public HqGrowthDefinition Growth { get; }
        public IReadOnlyList<PassiveSkillDefinition> Skills { get; }
        // 모든 Player가 판 시작 때 가지는 Skill(고정 구성).
        public IReadOnlyList<PassiveSkillDefinition> StartingSkills { get; }

        public GameContent(
            TimeLimitDefinition timeLimit,
            HqDefinition hq,
            IReadOnlyList<EnemyDefinition> enemies,
            SpawnDefinition spawn,
            IReadOnlyList<SupplyRequest> startSupply,
            HqGrowthDefinition growth,
            IReadOnlyList<PassiveSkillDefinition> skills,
            IReadOnlyList<string> startingSkills)
        {
            TimeLimit = timeLimit ?? throw new ArgumentNullException(nameof(timeLimit));
            Hq = hq ?? throw new ArgumentNullException(nameof(hq));
            Spawn = spawn ?? throw new ArgumentNullException(nameof(spawn));
            Growth = growth ?? throw new ArgumentNullException(nameof(growth));
            Enemies = Copy(enemies);
            StartSupply = Copy(startSupply);
            Skills = Copy(skills);
            IReadOnlyList<string> starting = Copy(startingSkills);

            var diagnostics = new List<ContentDiagnostic>();
            ContentInvariants.Collect(Enemies, Skills, starting, diagnostics, out _enemiesById, out _skillsById);

            if (diagnostics.Count > 0)
                throw new ArgumentException(diagnostics[0].ToString());

            StartingSkills = Resolve(starting, _skillsById);
        }

        public bool TryGetEnemy(string id, out EnemyDefinition enemy)
        {
            enemy = null;
            return id != null && _enemiesById.TryGetValue(id, out enemy);
        }

        public bool TryGetSkill(string id, out PassiveSkillDefinition skill)
        {
            skill = null;
            return id != null && _skillsById.TryGetValue(id, out skill);
        }

        private static IReadOnlyList<T> Resolve<T>(IReadOnlyList<string> ids, Dictionary<string, T> byId)
        {
            var resolved = new T[ids.Count];

            for (int i = 0; i < resolved.Length; i++)
            {
                resolved[i] = byId[ids[i]];
            }

            return Array.AsReadOnly(resolved);
        }

        // 호출자가 원본 목록을 나중에 바꿔도 따라 바뀌지 않게 한다.
        private static IReadOnlyList<T> Copy<T>(IReadOnlyList<T> source)
        {
            if (source == null)
                return Array.Empty<T>();

            var copy = new T[source.Count];

            for (int i = 0; i < copy.Length; i++)
            {
                copy[i] = source[i];
            }

            return Array.AsReadOnly(copy);
        }
    }
}
