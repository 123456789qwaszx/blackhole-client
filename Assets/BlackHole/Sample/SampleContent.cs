using System.Collections.Generic;
using BlackHole.Core;

namespace BlackHole.Sample
{
    // 샘플 콘텐츠. 여기의 값은 전부 [임시]다 — 화면 흐름을 돌리기 위해 채운 값이며 기획 결정이 아니다.
    // Core는 이 어셈블리를 참조하지 않는다(샘플과 Core 규칙의 분리).
    // 판 설정과 업그레이드 노드만 채운다. 적 종류·공급·배치는 Unity 쪽 에셋(EnemySupplySetup)이 채운다.
    public static class SampleContent
    {
        // 호출마다 새 데이터를 만든다. 호출자가 고쳐도 다른 호출에 영향이 없다.
        public static ContentData Create() => new ContentData
        {
            // [임시] 한 판의 시간(시간제는 현재 후보).
            Session = new SessionData { TimeLimit = 30 },
            // 진행도(적의 강도 단계)는 1~50단계다. 단계별 적 풀과 계수 표는 기획 데이터로 따로 만든다.
            StageCount = 50,
            // [임시] 업그레이드 샘플 트리. 원래는 노드 저작 툴이 만들 데이터다.
            // ID는 CONTENT_DEFINITION의 대표 노드를 그대로 둔다. 효과는 대상 시스템과 함께 지웠다.
            Upgrades = new List<UpgradeData>
            {
                Upgrade("breaker-damage", 10, null),
                Upgrade("breaker-radius", 15, "breaker-damage"),
                Upgrade("growth-supply", 25, "breaker-radius"),
                Upgrade("breaker-speed", 20, "breaker-damage"),
                Upgrade("golden-touch", 40, "breaker-speed"),
                Upgrade("dense-matter", 30, "breaker-damage"),
                Upgrade("laser-unlock", 30, "breaker-damage"),
                Upgrade("laser-width", 20, "laser-unlock")
            }
        };

        private static UpgradeData Upgrade(string id, int price, string requires) =>
            new UpgradeData { Id = id, Price = price, Requires = requires };
    }
}
