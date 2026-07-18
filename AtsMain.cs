using System;
using System.IO;
using System.Runtime.InteropServices;
using Zbx1425.DXDynamicTexture;

namespace CTCS0_Ats
{
    public partial class AtsMain
    {
        /// <summary>
        /// 公用的车辆规格信息
        /// </summary>
        public static AtsVehicleSpec vehicleSpec;
        // 用户输入的手柄位置
        public static int userPowerPosition, userBrakePosition, userReverserPosition;
        // 列控将返回的手柄位置
        public static int actualPowerPosition, actualBrakePosition;

        /// <summary>
        /// 门互锁功率手柄位置，客室门全部关闭/互锁旁路除时为最大功率手柄位置，其余时候为0
        /// </summary>
        public static int doorInterlockPowerPosition = vehicleSpec.PowerNotches;
        /// <summary>
        /// 本插件所在目录
        /// </summary>
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
        /// Called when current signal is changed
        /// </summary>
        /// <param name="signalIndex">Index of signal.</param>
        [DllExport(CallingConvention.StdCall)]
        public static void SetSignal(int signalIndex)
        {
            CabSignal.DecodeSignal(signalIndex);
        }

        [DllExport(CallingConvention.StdCall)]
        public static void SetBeaconData(AtsBeaconData beaconData)
        {
        }

        /// <summary>
        /// Called when the door is opened
        /// </summary>
        [DllExport(CallingConvention.StdCall)]
        public static void DoorOpen()
        {
            if (!Config.DoorInterlockIso)
            {
                doorInterlockPowerPosition = 0;
            }
        }

        /// <summary>
        /// Called when the door is closed
        /// </summary>
        [DllExport(CallingConvention.StdCall)]
        public static void DoorClose()
        {

            doorInterlockPowerPosition = vehicleSpec.PowerNotches;
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
            DMI.Frame(vehicleState, CabSignal.currentSignal);
            actualPowerPosition = Math.Min(userPowerPosition, doorInterlockPowerPosition);
            actualBrakePosition = Math.Max(userBrakePosition, 0);
            return new AtsHandles() { Power = actualPowerPosition, Brake = actualBrakePosition, ConstantSpeed = AtsCscInstruction.Continue, Reverser = userReverserPosition };
        }
    }
}
