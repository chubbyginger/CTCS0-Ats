using System.Runtime.InteropServices;

namespace CTCS0_Ats
{
    public partial class AtsMain
    {
        /// <summary>
        /// Called when this plug-in is loaded
        /// </summary>
        [DllExport(CallingConvention.StdCall)]
        public static void Load()
        {
            Tool.DebugWriteLine("CTCS-0插件已加载");
            Config.Load();
            DMI.Load();
            modeController = new ModeController();
            modeController.Initialize();
        }
    }
}
