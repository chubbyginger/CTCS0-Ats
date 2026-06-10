using System;
using Zbx1425.DXDynamicTexture;

namespace CTCS0_Ats
{
    internal class DMI
    {
        private static TextureHandle tHandle;
        internal static void Load()
        {
            try
            {
                TextureManager.Initialize();
                // 注意：目前是写死的，将来需要用config模块读取，自定义。
                tHandle = TextureManager.Register("../../../image/DMI.png", 1024, 1024);
                DebugDumper.WriteLine("DMI动态纹理加载成功");
            }
            catch (Exception ex)
            {
                DebugDumper.WriteLine("DMI初始化错误" + ex.ToString());
            }
        }
    }
}
