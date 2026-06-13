using System;
using System.IO;

namespace CTCS0_Ats
{
    internal static class Config
    {
        public static string TexturePath = "chubbyginger/InnoSig-E531/image/DMI.png";
        public static int TextureWidth = 1024;
        public static int TextureHeight = 1024;
        public static int TargetFPS = 10;
        public static int VehicleType = 3;
        // 0: 客运, 1: 货运
        public static int PassengerFreight = 0;

        internal static void Load()
        {
            string path = AtsMain.dllParentPath + "/config.ini";
            if (!File.Exists(path))
            {
                Tool.DebugWriteLine("config.ini不存在，使用默认配置");
                return;
            }
            try
            {
                foreach (string line in File.ReadAllLines(path))
                {
                    string trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(";") || trimmed.StartsWith("#") || trimmed.StartsWith("["))
                        continue;
                    int eq = trimmed.IndexOf('=');
                    if (eq < 0) continue;
                    string key = trimmed.Substring(0, eq).Trim();
                    string val = trimmed.Substring(eq + 1).Trim();
                    switch (key)
                    {
                        case "TexturePath": TexturePath = val; break;
                        case "TextureWidth": TextureWidth = int.Parse(val); break;
                        case "TextureHeight": TextureHeight = int.Parse(val); break;
                        case "TargetFPS": TargetFPS = int.Parse(val); break;
                        case "VehicleType": VehicleType = int.Parse(val); break;
                        case "PassengerFreight": PassengerFreight = int.Parse(val); break;
                    }
                }
                Tool.DebugWriteLine("config.ini加载完成");
            }
            catch (Exception ex)
            {
                Tool.DebugWriteLine("config.ini读取错误：" + ex.ToString());
            }
        }
    }
}
