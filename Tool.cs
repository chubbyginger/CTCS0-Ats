using System.Diagnostics;

namespace CTCS0_Ats
{
    internal static class Tool
    {
        /// <summary>
        /// Dump debug lines using Debug.WriteLine, with prefix.
        /// </summary>
        internal static void DebugWriteLine(string debugText)
        {
            Debug.WriteLine("BVE Trainsim Plugin: " + debugText);
        }

        private static readonly int[] pow10 = new int[] { 1, 10, 100, 1000, 10000, 100000 };
        internal static int D(int src, int digit, bool needEmptyDigits)
        {
            if (pow10[digit] > src && !needEmptyDigits)
            {
                return 10;
            }
            else if (digit == 0 && src == 0)
            {
                return 0;
            }
            else
            {
                return src / pow10[digit] % 10;
            }
        }
    }
}
