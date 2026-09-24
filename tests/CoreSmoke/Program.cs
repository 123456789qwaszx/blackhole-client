using System;
using BlackHole.Core.Tests;

internal static class Program
{
    private static int Main()
    {
        int passed = 0;
        int failed = 0;
        foreach (var test in CoreContracts.Cases())
        {
            try
            {
                test.Value();
                passed++;
                Console.WriteLine("PASS " + test.Key);
            }
            catch (Exception error)
            {
                failed++;
                Console.Error.WriteLine("FAIL " + test.Key + "\n" + error);
            }
        }
        Console.WriteLine($"{passed} passed, {failed} failed");
        return failed == 0 ? 0 : 1;
    }
}
