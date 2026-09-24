using System;

namespace BlackHole.Core
{
    // 정의 생성자가 쓰는 수치 규칙. 로더는 같은 생성자를 호출해 이 규칙을 진단으로 모은다.
    internal static class DefinitionGuard
    {
        public static string Id(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("ID가 비어 있다.", name);
            return value;
        }

        public static float Finite(float value, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                throw new ArgumentOutOfRangeException(name, "유한한 값이 필요하다.");
            return value;
        }

        public static float Positive(float value, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0)
                throw new ArgumentOutOfRangeException(name, "유한한 양수가 필요하다.");
            return value;
        }

        public static float NonNegative(float value, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0)
                throw new ArgumentOutOfRangeException(name, "0 이상의 유한한 값이 필요하다.");
            return value;
        }

        public static int Positive(int value, string name)
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(name, "양의 정수가 필요하다.");
            return value;
        }

        // 실행 중 요청 값(진행 시간, 당김 거리)의 검사. 정의가 아니라 호출 계약이다.
        public static void Delta(float value)
        {
            NonNegative(value, nameof(value));
        }
    }
}
