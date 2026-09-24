using System;
using System.Collections.Generic;

namespace BlackHole.Core.Tests
{
    // 계약의 판정 도우미. 실패하면 InvalidOperationException을 던진다(NUnit에 의존하지 않는다).
    internal static class Expect
    {
        public static void True(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }

        public static void Equal<T>(T expected, T actual) =>
            True(EqualityComparer<T>.Default.Equals(expected, actual), $"Expected {expected}, got {actual}");

        public static void Near(float expected, float actual, float tolerance = 0.001f) =>
            True(Math.Abs(expected - actual) <= tolerance, $"Expected {expected} ± {tolerance}, got {actual}");

        public static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException("Expected exception: " + typeof(T).Name);
        }
    }
}
