namespace BlackHole.Core
{
    // 한 전투의 난수. 판 조립 때 seed로 만들고 판이 끝나면 버린다.
    // seed와 입력과 진행 시간이 같으면 같은 값이 같은 순서로 나온다(기준 상황 재현, S12).
    // 엔진의 난수를 쓰지 않는다. 쓰는 곳은 적의 출현 배치(판 seed 그대로)와 참가자마다의 레이저 시작점(Stream)이다.
    internal sealed class BattleRandom
    {
        // 쓰는 곳의 번호. 같은 판 seed에서 쓰는 곳마다 다른 순서가 나온다.
        public const uint LaserStream = 0x4C415352u;

        private uint _state;

        public BattleRandom(int seed)
        {
            _state = unchecked((uint)seed);
        }

        // 판 seed에서 한 참가자의 한 쓰는 곳만 쓰는 난수. 한 곳이 뽑는 횟수가 다른 곳의 순서를 바꾸지 않는다.
        public static BattleRandom Stream(int seed, PlayerId player, uint use) =>
            new BattleRandom(unchecked((int)((uint)seed ^ ((uint)player.Value * 0x9E3779B1u) ^ (use * 0x85EBCA77u))));

        // [0, 1) 구간의 값. 32비트 SplitMix 방식이라 플랫폼과 런타임에 관계없이 같다.
        public float NextFloat()
        {
            unchecked
            {
                _state += 0x9E3779B9u;
                uint z = _state;
                z = (z ^ (z >> 16)) * 0x85EBCA6Bu;
                z = (z ^ (z >> 13)) * 0xC2B2AE35u;
                z ^= z >> 16;
                return (z >> 8) * (1f / 16777216f);
            }
        }
    }
}
