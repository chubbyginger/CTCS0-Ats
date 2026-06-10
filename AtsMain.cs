using System.Runtime.InteropServices;
using Zbx1425.DXDynamicTexture;

namespace CTCS0_Ats
{
    public partial class AtsMain
    {
        public static int userPowerPosition, userBrakePosition, userReverserPosition;
        /// <summary>
        /// Called when this plug-in is unloaded
        /// </summary>
        [DllExport(CallingConvention.StdCall)]
        public static void Dispose()
        {
            TextureManager.Dispose();
        }
    }
}
