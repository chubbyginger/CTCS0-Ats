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
        public static bool doorOpen;
        public static string dllParentPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        internal static ModeController modeController;
        internal static TrackDatabase trackDatabase;

        [DllExport(CallingConvention.StdCall)]
        public static void Dispose()
        {
            TextureManager.Dispose();
        }

        [DllExport(CallingConvention.StdCall)]
        public static void SetSignal(int signalIndex)
        {
            CabSignal.DecodeSignal(signalIndex);
            modeController?.OnSignalChange(CabSignal.currentSignal);
        }

        [DllExport(CallingConvention.StdCall)]
        public static void SetBeaconData(AtsBeaconData beaconData)
        {
            if (beaconData.Type == 12300)
            {
                CabSignal.SetForceNoCode(true);
            }
            else if (beaconData.Type == 12301)
            {
                CabSignal.SetForceNoCode(false);
            }

            if (trackDatabase != null && trackDatabase.IsLoaded)
            {
                trackDatabase.OnBeacon(beaconData.Type, beaconData.Signal);
            }
        }

        [DllExport(CallingConvention.StdCall)]
        public static void DoorOpen()
        {
            doorOpen = true;
            if (!Config.DoorInterlockIso)
            {
                doorInterlockPowerPosition = 0;
            }
        }

        [DllExport(CallingConvention.StdCall)]
        public static void DoorClose()
        {
            doorOpen = false;
            doorInterlockPowerPosition = vehicleSpec.PowerNotches;
        }

        [DllExport(CallingConvention.StdCall)]
        public static AtsHandles Elapse(AtsVehicleState vehicleState, IntPtr panel, IntPtr sound)
        {
            AtsMain.vehicleState = vehicleState;
            var panelArray = new AtsIoArray(panel);
            var soundArray = new AtsIoArray(sound);

            modeController?.Update(vehicleState);

            DMI.Frame(vehicleState, CabSignal.currentSignal);

            PanelOutput.Update(vehicleState);
            PanelOutput.WritePanel(panelArray);

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
