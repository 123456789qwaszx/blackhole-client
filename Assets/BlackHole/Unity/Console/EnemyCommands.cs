using System;
using System.Collections.Generic;
using BlackHole.Core;

namespace BlackHole.Unity
{
    // 적 명령 흐름(개발용). 실제 게임 시스템이 적 시스템에 보낼 명령 묶음을 흉내 낸다.
    //
    // 적 시스템이 받는 명령은 두 가지뿐이다: 파괴 요청(World.RequestDestroy)과 생성 요청(World.RequestSpawn).
    // 여기의 흐름은 누가 그 명령을 어떤 묶음으로 보내는지를 흉내 낸다 — 파괴는 공격(Skill)이, 생성은 레벨업이나
    // 천체의 추가 생성 효과가 보낸다. 그 시스템들이 생기면 각 흐름은 그 시스템의 흐름이 되고, 보내는 명령은 그대로다.
    // 같은 프레임에 보낸 명령은 다음 Step 하나에서 처리된다: 파괴(사망 확정)가 먼저, 생성이 나중이다(GAME_RULES 13절).
    // 그래서 "부순 결과로 곧바로 생성"은 부순 자리(풀의 최대 수)를 채울 수 있다. 생성 요청 중 풀 여과 장치가 거른 것은 버려진다.
    //
    // 흐름은 한 번에 한 종류의 적을 다룬다(수와 최대 수를 종류별로 계산하므로). 여러 종류는 종류마다 한 번씩 부른다.
    // [임시] 파괴 대상은 이 흉내의 선택이며 게임 규칙이 아니다: 그 종류에서 아직 파괴를 요청하지 않은 살아 있는 적 중 무작위.
    // 판의 난수(seed)는 쓰지 않는다 — 판의 재현성을 건드리지 않는다. 살아 있는 적이 모자라면 있는 만큼만 요청한다.
    internal sealed class EnemyCommands
    {
        private readonly Random _random = new Random();
        private readonly List<Enemy> _candidates = new List<Enemy>();

        // 1. 공격이 무작위 5마리를 부쉈다. 생성 요청은 없다.
        public CommandResult DestroyFive(World world, EnemyDefinition kind) =>
            new CommandResult(DestroyRandom(world, kind, 5), 5, 0);

        // 2. 공격이 무작위 10마리를 부쉈고, 그 결과(레벨업, 또는 소행성의 추가 생성 효과)로 곧바로 8마리 생성 요청이 들어왔다.
        public CommandResult DestroyTenSpawnEight(World world, EnemyDefinition kind) =>
            new CommandResult(DestroyRandom(world, kind, 10), 10, Spawn(world, kind, 8));

        // 3. 1마리만 부쉈는데 레벨업으로 10마리 생성 요청이 들어왔다.
        public CommandResult DestroyOneSpawnTen(World world, EnemyDefinition kind) =>
            new CommandResult(DestroyRandom(world, kind, 1), 1, Spawn(world, kind, 10));

        // 이 종류 무작위 count마리의 파괴 요청. 실제로 요청한 수를 돌려준다.
        private int DestroyRandom(World world, EnemyDefinition kind, int count)
        {
            _candidates.Clear();
            IReadOnlyList<Enemy> enemies = world.Enemies;

            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i].Definition == kind && !IsDestroyRequested(world, enemies[i]))
                    _candidates.Add(enemies[i]);
            }

            int picks = Math.Min(count, _candidates.Count);

            // 앞에서부터 한 자리씩, 남은 후보 중 하나를 골라 그 자리로 옮긴다.
            for (int i = 0; i < picks; i++)
            {
                int pick = _random.Next(i, _candidates.Count);
                (_candidates[i], _candidates[pick]) = (_candidates[pick], _candidates[i]);
                world.RequestDestroy(_candidates[i]);
            }

            _candidates.Clear();
            return picks;
        }

        // 이 종류 count마리의 생성 요청. 요청한 수를 돌려준다(몇 마리가 실제로 나올지는 처리 때 정해진다).
        private static int Spawn(World world, EnemyDefinition kind, int count)
        {
            world.RequestSpawn(new SupplyRequest(kind, count));
            return count;
        }

        private static bool IsDestroyRequested(World world, Enemy enemy)
        {
            IReadOnlyList<Enemy> requested = world.PendingDestroys;

            for (int i = 0; i < requested.Count; i++)
            {
                if (requested[i] == enemy)
                    return true;
            }

            return false;
        }

        // 명령 하나가 보낸 요청 수. 파괴는 의도한 수보다 적을 수 있다(살아 있는 적이 모자랄 때).
        public readonly struct CommandResult
        {
            public int Destroyed { get; }
            public int IntendedDestroys { get; }
            public int Spawned { get; }

            public CommandResult(int destroyed, int intendedDestroys, int spawned)
            {
                Destroyed = destroyed;
                IntendedDestroys = intendedDestroys;
                Spawned = spawned;
            }
        }
    }
}
