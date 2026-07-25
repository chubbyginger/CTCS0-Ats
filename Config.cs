using System;
using System.IO;

namespace CTCS0_Ats
{
    internal static class Config
    {
        internal static string TexturePath = "chubbyginger/InnoSig-E531/image/DMI.png";
        internal static int TextureWidth = 1024;
        internal static int TextureHeight = 1024;
        internal static int TargetFPS = 10;

        internal enum VehicleTypeEnum
        {
            DL = 0,
            DMU,
            EL,
            EMU
        }

        internal enum BrakeTypeEnum
        {
            Automatic = 0,
            Straight
        }

        internal enum PassengerFreightEnum
        {
            Passenger = 0,
            Freight
        }

        /// <summary>
        /// 车辆类型（机车/动车组，电力/内燃）
        /// </summary>
        internal static VehicleTypeEnum VehicleType = VehicleTypeEnum.EMU;
        /// <summary>
        /// 车辆制动类型（自动/直通）
        /// </summary>
        internal static BrakeTypeEnum BrakeType = BrakeTypeEnum.Straight;
        /// <summary>
        /// 车辆客货状态
        /// </summary>
        internal static PassengerFreightEnum PassengerFreight = PassengerFreightEnum.Passenger;
        /// <summary>
        /// 门互锁旁路，false：定位，true：旁路。该设置仅对动车组列车有效，机车默认无互锁
        /// </summary>
        internal static bool DoorInterlockIso = false;
        /// <summary>
        /// 强制机车信号显示，-1表示不强制（使用实际信号），0~14对应CabSignalCode枚举值
        /// </summary>
        internal static int ForceCabSignal = -1;
        /// <summary>
        /// 列车标定常用制动减速度 (km/h/s)
        /// </summary>
        internal static float BrakeDeceleration = 3.6f;
        /// <summary>
        /// 空走时间 (s)
        /// </summary>
        internal static float EmptyRunTime = 3.0f;
        /// <summary>
        /// 列车最高允许速度 (km/h)
        /// </summary>
        internal static float MaxSpeed = 160f;
        internal static float BpNominalPressure = 500f;

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
                        case "VehicleType": VehicleType = (VehicleTypeEnum)int.Parse(val); break;
                        case "BrakeType": BrakeType = (BrakeTypeEnum)int.Parse(val); break;
                        case "PassengerFreight": PassengerFreight = (PassengerFreightEnum)int.Parse(val); break;
                        case "DoorInterlockIso": DoorInterlockIso = bool.Parse(val); break;
                        case "ForceCabSignal": ForceCabSignal = int.Parse(val); break;
                        case "BrakeDeceleration": BrakeDeceleration = float.Parse(val); break;
                        case "EmptyRunTime": EmptyRunTime = float.Parse(val); break;
                        case "MaxSpeed": MaxSpeed = float.Parse(val); break;
                        case "BpNominalPressure": BpNominalPressure = float.Parse(val); break;
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
