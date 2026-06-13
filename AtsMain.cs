using System;
using System.IO;
using System.Runtime.InteropServices;
using Zbx1425.DXDynamicTexture;

namespace CTCS0_Ats
{
    public partial class AtsMain
    {
        public static int userPowerPosition, userBrakePosition, userReverserPosition;
        public static int actualPowerPosition, actualBrakePosition;
        public static string dllParentPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        /// <summary>
        /// Called when this plug-in is unloaded
        /// </summary>
        [DllExport(CallingConvention.StdCall)]
        public static void Dispose()
        {
            TextureManager.Dispose();
        }

        /// <summary>
        /// Called every frame
        /// </summary>
        /// <param name="vehicleState">Current state of vehicle.</param>
        /// <param name="panel">Current state of panel.</param>
        /// <param name="sound">Current state of sound.</param>
        /// <returns>Driving operations of vehicle.</returns>
        [DllExport(CallingConvention.StdCall)]
        public static AtsHandles Elapse(AtsVehicleState vehicleState, IntPtr panel, IntPtr sound)
        {
            var panelArray = new AtsIoArray(panel);
            var soundArray = new AtsIoArray(sound);
            DMI.Frame(vehicleState);
            actualPowerPosition = userPowerPosition;
            actualBrakePosition = userBrakePosition;
            return new AtsHandles() { Power = actualPowerPosition, Brake = actualBrakePosition, ConstantSpeed = AtsCscInstruction.Continue, Reverser = userReverserPosition };
        }
    }
}
