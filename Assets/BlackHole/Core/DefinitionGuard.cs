using System;

namespace BlackHole.Core
{
    // 외부에서 조립한 콘텐츠의 기본 오류는 정의 생성 시 차단한다.
    internal static class DefinitionGuard
    {
        public static string Id(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("콘텐츠 ID가 비어 있다.", nameof(value));
            return value;
        }

        public static float Positive(float value, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0)
                throw new ArgumentOutOfRangeException(name, "유한한 양수가 필요하다.");
            return value;
        }

        public static void Delta(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0)
                throw new ArgumentOutOfRangeException(nameof(value));
        }
    }
}
