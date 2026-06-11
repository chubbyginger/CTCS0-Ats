using System;
using System.IO;
using System.Reflection;

namespace CTCS0_Ats
{
    public partial class AtsMain
    {
        static AtsMain()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }
        private static System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (args.Name.Contains("DXDynamicTexture"))
            {
                var libPath = Path.GetFullPath(Path.Combine(
                    Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                    "."
                ));
                var fileName = "Zbx1425.DXDynamicTexture-net48.dll";
                return Assembly.LoadFile(Path.Combine(libPath, fileName));
            }
            return null;
        }
    }
}
