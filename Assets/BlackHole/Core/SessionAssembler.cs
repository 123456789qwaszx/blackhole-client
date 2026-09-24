using System;

namespace BlackHole.Core
{
    // 검증된 카탈로그에서 한 판의 실행 객체를 새로 조립하는 유일한 진입점.
    // 정의는 공유하고, HP·쿨다운·질량·잔액·강화 단계·출현 진행 같은 실행 상태는 판마다 새로 만든다.
    // 새 시스템이 판에 들어오면 조립 순서는 여기에만 추가한다.
    public static class SessionAssembler
    {
        public static GameSession Create(ContentCatalog catalog)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));

            var upgrades = new UpgradeState(catalog.Upgrades);
            var wallet = new WalletState();
            var blackHole = new BlackHoleState(catalog.BlackHole, upgrades);

            var world = new TargetWorld(catalog.TargetRules);
            var spawn = new SpawnSchedule(catalog.Spawn, catalog.SpawnOrder);
            var loadout = new SkillLoadout(catalog.Skills);
            var field = new Playfield(world, spawn, loadout, blackHole, wallet, upgrades);

            var mode = new TimeLimitMode(catalog.TimeLimit);
            return new GameSession(field, mode);
        }
    }
}
