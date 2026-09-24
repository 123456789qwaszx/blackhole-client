using System.Collections.Generic;

namespace BlackHole.Core
{
    // 한 판의 Enemy 공급 창구.
    // 무엇을·얼마나는 요청하는 쪽(전투 시작 배치, 성장 진행)이 정하고, 어디에·어떤 수치로는 Spawn이 정한다.
    // 요청은 모아 두었다가 단계 끝에 요청 순서대로 생성한다.
    //
    // 요청 수량이 실제 생성 수가 되는 유일한 자리다. 구매로 공급량을 늘리는 보정(M6)은 여기에 붙는다.
    internal sealed class EnemySupply
    {
        private readonly EnemySpawner _spawner;
        private readonly List<SupplyRequest> _pending = new List<SupplyRequest>();

        public EnemySupply(EnemySpawner spawner)
        {
            _spawner = spawner;
        }

        public void Request(IReadOnlyList<SupplyRequest> requests)
        {
            for (int i = 0; i < requests.Count; i++)
            {
                _pending.Add(requests[i]);
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
