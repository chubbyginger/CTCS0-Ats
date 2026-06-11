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

        /// <summary>
        /// 三位数每一位数字分离
        /// </summary>
        /// <param name="num">输入三位数</param>
        /// <returns>一个长度为3的数组，0，1，2分别是百位、十位和个位</returns>
        internal static int[] SplitDigits3(int num)
        {
            int[] digits = new int[3];
            digits[0] = num / 100; // 百位
            digits[1] = (num / 10) % 10; // 十位
            digits[2] = num % 10; // 个位
            return digits;
        }
        /// <summary>
        /// 两位数每一位数字分离
        /// </summary>
        /// <param name="num">输入两位数</param>
        /// <returns>一个长度为2的数组，0，1分别是十位和个位</returns>
        internal static int[] SplitDigits2(int num)
        {
            int[] digits = new int[2];
            digits[0] = num / 10; // 十位
            digits[1] = num % 10; // 个位
            return digits;
        }
        // 四位数每一位数字分离，直接写
        internal static int[] SplitDigits4(int num)
        {
            int[] digits = new int[4];
            digits[0] = num / 1000; // 千位
            digits[1] = (num / 100) % 10; // 百位
            digits[2] = (num / 10) % 10; // 十位
            digits[3] = num % 10; // 个位
            return digits;
        }
    }
}
