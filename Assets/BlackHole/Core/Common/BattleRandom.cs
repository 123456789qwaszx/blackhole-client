namespace BlackHole.Core
{
    // 한 전투의 난수. 판 조립 때 seed로 하나 만들고 판이 끝나면 버린다.
    // seed와 입력과 진행 시간이 같으면 같은 값이 같은 순서로 나온다(기준 상황 재현, S12).
    // 엔진의 난수를 쓰지 않는다. 지금 쓰는 곳은 적의 출현 배치와 색 등급의 몫(QuotaPicker)이다.
    internal sealed class BattleRandom
    {
        private uint _state;

        public BattleRandom(int seed)
        {
            _state = unchecked((uint)seed);
        }

        // 같은 seed에서 용도가 다른 난수. 용도마다 번호가 다르면, 한 용도가 난수를 더 쓰거나 덜 써도 다른 용도의 순서는 그대로다
        // (예: 색 비율을 바꿔도 출현 위치는 같다). 번호 0은 seed만 받는 생성자와 같다.
        public BattleRandom(int seed, int stream)
        {
            _state = unchecked((uint)seed ^ ((uint)stream * 0x85EBCA6Bu));
        }

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
