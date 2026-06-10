using System.Diagnostics;

namespace CTCS0_Ats
{
    internal static class DebugDumper
    {
        /// <summary>
        /// Dump debug lines using Debug.WriteLine, with prefix.
        /// </summary>
        internal static void WriteLine(string debugText)
        {
            Debug.WriteLine("BVE Trainsim Plugin: " + debugText);
        }
    }
}
