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

        /// <summary>
        /// 通过Beacon解码当前机车信号
        /// </summary>
        /// <param name="beaconData"></param>
        internal static void DecodeBeacon(AtsMain.AtsBeaconData beaconData)
        {
            Tool.DebugWriteLine(string.Format("Beacon接收: Type={0}, Signal={1}, Distance={2}, Optional={3}", beaconData.Type, beaconData.Signal, beaconData.Distance, beaconData.Optional));
            if (beaconData.Signal >= 0 && beaconData.Signal <= 14 && beaconData.Signal != 1)
            {
                currentSignal = (CabSignalCode)beaconData.Signal;
            }
            else
            {
                currentSignal = CabSignalCode.B;
            }
            Tool.DebugWriteLine("机车信号解码: " + currentSignal.ToString());
        }

        internal static void DecodeSignal()
        {

        }
    }
}
