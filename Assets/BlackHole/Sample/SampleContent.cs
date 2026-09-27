using BlackHole.Core;

namespace BlackHole.Sample
{
    // 샘플 콘텐츠. 여기의 값은 전부 [임시]다 — 화면 흐름을 돌리기 위해 채운 값이며 기획 결정이 아니다.
    // Core는 이 어셈블리를 참조하지 않는다(샘플과 Core 규칙의 분리).
    // 판 설정만 채운다. 적 종류·공급·배치·전체 개체 수 상한은
    // Unity 쪽 에셋(EnemyCatalog, EnemySupplySetup)이 채운다.
    public static class SampleContent
    {
        // 호출마다 새 데이터를 만든다. 호출자가 고쳐도 다른 호출에 영향이 없다.
        public static ContentData Create() => new ContentData
        {
            // 한 판의 기본 시간 14초 [사용자]. 블랙홀 성장(성장 노드)이 그 판에서만 늘린다.
            Session = new SessionData { TimeLimit = 14 },
        };
    }
}
