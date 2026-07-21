using System;
using System.IO;
using System.Runtime.InteropServices;
using Zbx1425.DXDynamicTexture;

namespace CTCS0_Ats
{
    public partial class AtsMain
    {
        public static AtsVehicleSpec vehicleSpec;
        public static int userPowerPosition, userBrakePosition, userReverserPosition;
        public static int actualPowerPosition, actualBrakePosition;
        public static AtsVehicleState vehicleState;

        public static int doorInterlockPowerPosition;
        public static string dllParentPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        internal static ModeController modeController;

        [DllExport(CallingConvention.StdCall)]
        public static void Dispose()
        {
            TextureManager.Dispose();
        }

        [DllExport(CallingConvention.StdCall)]
        public static void SetSignal(int signalIndex)
        {
            CabSignal.DecodeSignal(signalIndex);
            if (modeController != null)
            {
                modeController.OnSignalChange(CabSignal.currentSignal);
            }
        }

        [DllExport(CallingConvention.StdCall)]
        public static void SetBeaconData(AtsBeaconData beaconData)
        {
        }

        [DllExport(CallingConvention.StdCall)]
        public static void DoorOpen()
        {
            if (!Config.DoorInterlockIso)
            {
                doorInterlockPowerPosition = 0;
            }
        }

        [DllExport(CallingConvention.StdCall)]
        public static void DoorClose()
        {
            doorInterlockPowerPosition = vehicleSpec.PowerNotches;
        }

        [DllExport(CallingConvention.StdCall)]
        public static AtsHandles Elapse(AtsVehicleState vehicleState, IntPtr panel, IntPtr sound)
        {
            AtsMain.vehicleState = vehicleState;
            var panelArray = new AtsIoArray(panel);
            var soundArray = new AtsIoArray(sound);

            if (modeController != null)
            {
                modeController.Update(vehicleState);
            }

            DMI.Frame(vehicleState, CabSignal.currentSignal);

            int supervisionPower = (modeController != null) ? modeController.SupervisionPowerPosition : vehicleSpec.PowerNotches;
            int supervisionBrake = (modeController != null) ? modeController.SupervisionBrakePosition : 0;

            actualPowerPosition = Math.Min(userPowerPosition, Math.Min(doorInterlockPowerPosition, supervisionPower));
            actualBrakePosition = Math.Max(userBrakePosition, supervisionBrake);

            return new AtsHandles()
            {
                Power = actualPowerPosition,
                Brake = actualBrakePosition,
                ConstantSpeed = AtsCscInstruction.Continue,
                Reverser = userReverserPosition
            };
        }
    }
}
