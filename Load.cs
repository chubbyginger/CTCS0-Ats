using System;
using System.IO;
using System.Reflection;
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
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                string asmName = new System.Reflection.AssemblyName(args.Name).Name + ".dll";
                string asmPath = System.IO.Path.Combine(dllParentPath, asmName);
                if (System.IO.File.Exists(asmPath))
                    return System.Reflection.Assembly.LoadFrom(asmPath);
                return null;
            };

            Tool.DebugWriteLine("CTCS-0插件已加载");
            Config.Load();
            DMI.Load();
            modeController = new ModeController();
            modeController.Initialize();
            trackDatabase = new TrackDatabase();
            if (!string.IsNullOrEmpty(Config.TrackDataDir))
            {
                string trackPath = System.IO.Path.Combine(dllParentPath, Config.TrackDataDir);
                trackDatabase.Load(trackPath);
                trackDatabase.DumpInfo();
            }
            else
            {
                Tool.DebugWriteLine("TrackDatabase: 未配置TrackDataDir, 线路数据库未加载");
            }
        }
    }
}
