using System.Collections.Generic;
using BlackHole.Core;

namespace BlackHole.Sample
{
    // 샘플 콘텐츠. 여기의 값은 전부 [임시]다 — 화면 흐름을 돌리기 위해 채운 값이며 기획 결정이 아니다.
    // Core는 이 어셈블리를 참조하지 않는다(샘플과 Core 규칙의 분리).
    // 판 설정과 업그레이드 노드만 채운다. 적 종류·공급·배치와 단계 표(적 풀)는
    // Unity 쪽 에셋(EnemyCatalog, EnemySupplySetup, StageTable)이 채운다.
    public static class SampleContent
    {
        // 노드가 가리키는 적 종류. Unity 쪽 소행성 에셋의 ID이며, 질량 단계 표가 0~8단계이고 황금이 된다.
        public const string Asteroid = "asteroid";
        // 소행성 질량 증가 노드 수. 소행성 질량 단계 표의 마지막 단계(8)와 같다.
        public const int AsteroidMassNodes = 8;

        // 호출마다 새 데이터를 만든다. 호출자가 고쳐도 다른 호출에 영향이 없다.
        public static ContentData Create() => new ContentData
        {
            // [임시] 한 판의 시간(시간제는 현재 후보).
            Session = new SessionData { TimeLimit = 30 },
            Upgrades = Upgrades(),
        };

        // [임시] 판 구성 노드(BATTLE_COMPOSITION_PLAN BC-005). 원래는 노드 저작 툴이 만들 데이터다.
        // - 소행성 질량 증가 1~8: 하나에 질량 단계 +1, 앞 노드 다음.
        // - 황금 소행성 추가: 원작처럼 파랑이 나오는 질량 단계(4) 다음. 황금 비율 0.1%.
        // - 황금 자릿수 1~3: 황금 비율 ×10(원작은 행성 노드). 황금 배율 올리기: 50 → 4200.
        // 가격은 원작처럼 빠르게 오르게 두었다.
        private static List<UpgradeData> Upgrades()
        {
            var nodes = new List<UpgradeData>();
            long price = 10;

            for (int i = 1; i <= AsteroidMassNodes; i++)
            {
                nodes.Add(Node($"asteroid-mass-{i}", price, i == 1 ? null : $"asteroid-mass-{i - 1}",
                    Grant(Asteroid, "MassLevel", "Add", 1)));
                price *= 3;
            }

            nodes.Add(Node("golden-asteroid", 500, "asteroid-mass-4", Grant(Asteroid, "GoldenRatio", "Set", 0.001f)));
            nodes.Add(Node("golden-digits-1", 2000, "golden-asteroid", Grant(Asteroid, "GoldenRatio", "Multiply", 10)));
            nodes.Add(Node("golden-digits-2", 8000, "golden-digits-1", Grant(Asteroid, "GoldenRatio", "Multiply", 10)));
            nodes.Add(Node("golden-digits-3", 32000, "golden-digits-2", Grant(Asteroid, "GoldenRatio", "Multiply", 10)));
            nodes.Add(Node("golden-multiplier", 100000, "golden-digits-1", Grant(Asteroid, "GoldenMultiplier", "Set", 4200)));
            return nodes;
        }

        private static UpgradeData Node(string id, long price, string requires, params EnemyGrantData[] grants) =>
            new UpgradeData { Id = id, Price = price, Requires = requires, Grants = new List<EnemyGrantData>(grants) };

        private static EnemyGrantData Grant(string enemy, string stat, string operation, float value) =>
            new EnemyGrantData { Enemy = enemy, Stat = stat, Operation = operation, Value = value };
    }
}
