namespace BlackHole.Core
{
    // 한 전투의 난수. 판 조립 때 seed로 하나 만들고 판이 끝나면 버린다.
    // seed와 입력과 진행 시간이 같으면 같은 값이 같은 순서로 나온다(기준 상황 재현, S12).
    // 엔진의 난수를 쓰지 않는다. 지금 쓰는 곳은 관통 레이저의 시작점뿐이다.
    internal sealed class BattleRandom
    {
        private uint _state;

        public BattleRandom(int seed)
        {
            _state = unchecked((uint)seed);
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
