using System;
using BlackHole.Core.Tests;

internal static class Program
{
    private static int Main()
    {
        int passed = 0;
        int failed = 0;
        foreach (Contract contract in Contracts.All())
        {
            try
            {
                contract.Run();
                passed++;
                Console.WriteLine("PASS " + contract.Name);
            }
            catch (Exception error)
            {
                failed++;
                Console.Error.WriteLine("FAIL " + contract.Name + "\n" + error);
            }
        }
        Console.WriteLine($"{passed} passed, {failed} failed");
        return failed == 0 ? 0 : 1;
    }
}
