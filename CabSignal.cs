using System;

namespace CTCS0_Ats
{
    internal class CabSignal
    {
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

        internal static CabSignalCode currentSignal;

        internal static bool forceNoCode;

        internal static void DecodeSignal(int signalIndex)
        {
            if (forceNoCode)
            {
                currentSignal = CabSignalCode.B;
                return;
            }

            if (Enum.IsDefined(typeof(CabSignalCode), signalIndex))
            {
                currentSignal = (CabSignalCode)signalIndex;
            }
            else
            {
                currentSignal = CabSignalCode.B;
            }
        }

        internal static void SetForceNoCode(bool enabled)
        {
            forceNoCode = enabled;
            if (enabled)
            {
                currentSignal = CabSignalCode.B;
                if (AtsMain.modeController != null)
                {
                    AtsMain.modeController.OnSignalChange(CabSignalCode.B);
                }
            }
        }
    }
}
