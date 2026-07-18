using System;

namespace CTCS0_Ats
{
    internal class CabSignal
    {
        /// <summary>
        /// 机车信号码定义
        /// </summary>
        internal enum CabSignalCode
        {
            HU = 0,
            U = 2,
            LU,
            L,
            L2,
            L3,
            L4,
            L5,
            H,
            HB,
            UU,
            UUS,
            U2,
            U2S,
            B
        }
        /// <summary>
        /// 当前机车信号码（全局）
        /// </summary>
        internal static CabSignalCode currentSignal;

        internal static void DecodeSignal(int signalIndex)
        {
            Tool.DebugWriteLine("SetSignal接收: signalIndex=" + signalIndex);
            if (Enum.IsDefined(typeof(CabSignalCode), signalIndex))
            {
                currentSignal = (CabSignalCode)signalIndex;
            }
            else
            {
                currentSignal = CabSignalCode.B;
            }
            Tool.DebugWriteLine("机车信号解码: " + currentSignal.ToString());
        }
    }
}
