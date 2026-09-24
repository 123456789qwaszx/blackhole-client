using System.Collections.Generic;

namespace BlackHole.Core
{
    // 공급 요청이 어디서 왔는가. 구매 보정이 어느 공급에 붙는지 가른다.
    internal enum SupplySource
    {
        // 전투 시작 배치
        Start,
        // HQ 성장
        Growth
    }

    // 한 판의 Enemy 공급 창구.
    // 무엇을·얼마나는 요청하는 쪽(전투 시작 배치, 성장 진행)이 정하고, 어디에·어떤 수치로는 Spawn이 정한다.
    // 요청은 모아 두었다가 단계 끝에 요청 순서대로 생성한다.
    //
    // 요청 수량이 실제 생성 수가 되는 유일한 자리다. 구매한 성장 공급 보정은 여기서 더해진다.
    internal sealed class EnemySupply
    {
        private readonly EnemySpawner _spawner;
        private readonly UpgradeModifiers _modifiers;
        private readonly List<SupplyRequest> _pending = new List<SupplyRequest>();

        public EnemySupply(
            EnemySpawner spawner,
            UpgradeModifiers modifiers)
        {
            _spawner = spawner;
            _modifiers = modifiers;
        }

        public void Request(IReadOnlyList<SupplyRequest> requests, SupplySource source)
        {
            for (int i = 0; i < requests.Count; i++)
            {
                SupplyRequest request = requests[i];

                if (source == SupplySource.Growth)
                {
                    int bonus = _modifiers.GrowthSupplyBonus(request.Enemy);

                    if (bonus > 0)
                        request = new SupplyRequest(request.Enemy, checked(request.Count + bonus));
                }

                _pending.Add(request);
            }
        }

        public void Release(World world)
        {
            for (int i = 0; i < _pending.Count; i++)
            {
                SupplyRequest request = _pending[i];

                for (int n = 0; n < request.Count; n++)
                {
                    _spawner.Spawn(request.Enemy, world);
                }
            }

            _pending.Clear();
        }
    }
}
