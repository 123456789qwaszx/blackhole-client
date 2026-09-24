using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 한 판을 조립하는 데 필요한 검증된 공유 정의 묶음. 읽기 전용이며 여러 판이 함께 쓴다.
    //
    // 생성자 보장(구현 = ContentInvariants):
    // [1] Enemy ID가 유일하다.
    // [2] 출현 순서의 Enemy ID가 실재한다.
    // 오류가 있는 콘텐츠의 경로별 보고는 ContentLoader가 맡는다.
    public sealed class GameContent
    {
        private readonly Dictionary<string, EnemyDefinition> _enemiesById;

        public TimeLimitDefinition TimeLimit { get; }
        public HqDefinition Hq { get; }
        public IReadOnlyList<EnemyDefinition> Enemies { get; }
        public SpawnDefinition Spawn { get; }
        // Spawn.Order를 해석한 정의 목록.
        internal IReadOnlyList<EnemyDefinition> SpawnOrder { get; }

        public GameContent(TimeLimitDefinition timeLimit, HqDefinition hq,
            IReadOnlyList<EnemyDefinition> enemies, SpawnDefinition spawn)
        {
            TimeLimit = timeLimit ?? throw new ArgumentNullException(nameof(timeLimit));
            Hq = hq ?? throw new ArgumentNullException(nameof(hq));
            Enemies = Copy(enemies);
            Spawn = spawn;

            var diagnostics = new List<ContentDiagnostic>();
            ContentInvariants.Collect(Enemies, Spawn, diagnostics, out _enemiesById);
            if (diagnostics.Count > 0)
                throw new ArgumentException(diagnostics[0].ToString());

            var order = new EnemyDefinition[Spawn.Order.Count];
            for (int i = 0; i < order.Length; i++) order[i] = _enemiesById[Spawn.Order[i]];
            SpawnOrder = Array.AsReadOnly(order);
        }

        public bool TryGetEnemy(string id, out EnemyDefinition enemy)
        {
            enemy = null;
            return id != null && _enemiesById.TryGetValue(id, out enemy);
        }

        // 호출자가 원본 목록을 나중에 바꿔도 따라 바뀌지 않게 한다.
        private static IReadOnlyList<T> Copy<T>(IReadOnlyList<T> source)
        {
            if (source == null) return Array.Empty<T>();
            var copy = new T[source.Count];
            for (int i = 0; i < copy.Length; i++) copy[i] = source[i];
            return Array.AsReadOnly(copy);
        }
    }
}
