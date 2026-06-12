using System;
using System.Drawing;
using Zbx1425.DXDynamicTexture;

namespace CTCS0_Ats
{
    internal class DMI
    {
        /// <summary>
        /// 被替换纹理的handle
        /// </summary>
        private static TextureHandle tHandle;

        // 各种位图资源
        private static Bitmap baseBitmap;
        private static Bitmap currentSpeedDigitBitmap, limitSpeedDigitBitmap, targetDistanceDigitBitmap, nullDigitBigBitmap;
        private static Bitmap dateDigitBitmap, timeDigitBitmap;
        // DL_DMU: 内燃机车、内燃动车组，自空制动机。DMU_Straight：内燃动车组直通制动机。EL：电力机车自空制动机。EMU：电动车组直通制动机。
        private static Bitmap DL_DMU_StatusWindowBitmap, DMU_StraightStatusWindowBitmap, EL_StatusWindowBitmap, EMU_StatusWindowBitmap;
        private static Bitmap statusWhiteDigitBitmap, statusGreenDigitBitmap, statusGreyDigitBitmap, statusNullDigitBitmap;
        private static Bitmap reverserBitmap, notchZeroBitmap, tractionBrakeBitmap;

        /// <summary>
        /// GDIHelper封装，适用于整个DMI
        /// </summary>
        private static GDIHelper bitmapGDI;

        // 以下texture相关常量将来都需要用config模块读取
        /// <summary>
        /// 要被替换的纹理路径，必须是Scenarios下的相对路径
        /// </summary>
        private const string textureToBeReplaced = "chubbyginger/InnoSig-E531/image/DMI.png";
        /// <summary>
        /// 被替换纹理的宽度（必须为2的次幂）
        /// </summary>
        private const int replacedTextureW = 1024;
        /// <summary>
        /// 被替换纹理的高度（必须为2的次幂）
        /// </summary>
        private const int replacedTextureH = 1024;
        /// <summary>
        /// DMI屏幕最大帧率
        /// </summary>
        private const int targetFPS = 10;

        internal static void Load()
        {
            try
            {
                TextureManager.Initialize();
                tHandle = TextureManager.Register(textureToBeReplaced, 1024, 1024);
                if (tHandle.IsCreated)
                {
                    Tool.DebugWriteLine("DMI动态纹理创建成功");
                }
                else
                {
                    Tool.DebugWriteLine("DMI动态纹理创建失败");
                }
            }
            catch (Exception ex)
            {
                Tool.DebugWriteLine("DMI动态纹理初始化阶段错误：" + ex.ToString());
            }
            bitmapGDI = new GDIHelper(replacedTextureW, replacedTextureH);

            // 加载位图资源
            try
            {
                baseBitmap = new Bitmap(Bitmap.FromFile(AtsMain.dllParentPath + "/assets/base.png"));
                currentSpeedDigitBitmap = new Bitmap(Bitmap.FromFile(AtsMain.dllParentPath + "/assets/current_speed_digits.png"));
                limitSpeedDigitBitmap = new Bitmap(Bitmap.FromFile(AtsMain.dllParentPath + "/assets/limit_speed_digits.png"));
                targetDistanceDigitBitmap = new Bitmap(Bitmap.FromFile(AtsMain.dllParentPath + "/assets/target_distance_digits.png"));
                nullDigitBigBitmap = new Bitmap(Bitmap.FromFile(AtsMain.dllParentPath + "/assets/null_digits.png"));
                dateDigitBitmap = new Bitmap(Bitmap.FromFile(AtsMain.dllParentPath + "/assets/date_digits.png"));
                timeDigitBitmap = new Bitmap(Bitmap.FromFile(AtsMain.dllParentPath + "/assets/time_digits.png"));
                DL_DMU_StatusWindowBitmap = new Bitmap(Bitmap.FromFile(AtsMain.dllParentPath + "/assets/status_window_dl_dmu.png"));
                DMU_StraightStatusWindowBitmap = new Bitmap(Bitmap.FromFile(AtsMain.dllParentPath + "/assets/status_window_dmu_straightbrake.png"));
                EL_StatusWindowBitmap = new Bitmap(Bitmap.FromFile(AtsMain.dllParentPath + "/assets/status_window_el.png"));
                EMU_StatusWindowBitmap = new Bitmap(Bitmap.FromFile(AtsMain.dllParentPath + "/assets/status_window_emu.png"));
                statusWhiteDigitBitmap = new Bitmap(Bitmap.FromFile(AtsMain.dllParentPath + "/assets/status_white_digits.png"));
                statusGreenDigitBitmap = new Bitmap(Bitmap.FromFile(AtsMain.dllParentPath + "/assets/status_green_digits.png"));
                statusGreyDigitBitmap = new Bitmap(Bitmap.FromFile(AtsMain.dllParentPath + "/assets/status_grey_digits.png"));
                statusNullDigitBitmap = new Bitmap(Bitmap.FromFile(AtsMain.dllParentPath + "/assets/status_null_digits.png"));
                reverserBitmap = new Bitmap(Bitmap.FromFile(AtsMain.dllParentPath + "/assets/reverser.png"));
                notchZeroBitmap = new Bitmap(Bitmap.FromFile(AtsMain.dllParentPath + "/assets/notchzero.png"));
                tractionBrakeBitmap = new Bitmap(Bitmap.FromFile(AtsMain.dllParentPath + "/assets/tractionbrake.png"));
            }
            catch (Exception ex)
            {

                Tool.DebugWriteLine("DMI位图资源加载阶段错误：" + ex.ToString());
            }

        }

        private static void DrawBase()
        {
            bitmapGDI.BeginGDI();
            bitmapGDI.DrawImage(baseBitmap, 0, 0);
            bitmapGDI.EndGDI();
        }

        private static void DrawOneDigit(int fullNumber, int digit, Bitmap digitBitmap, Bitmap nullBitmap, int x, int y, int h)
        {
            if (digit == 0)
            {
                bitmapGDI.DrawImage(digitBitmap, x, y, Tool.D(fullNumber, 0, true) * h, h);
            }
            else
            {
                if (Tool.D(fullNumber, digit, false) == 10)
                {
                    bitmapGDI.DrawImage(nullBitmap, x, y);
                }
                else
                {
                    bitmapGDI.DrawImage(digitBitmap, x, y, Tool.D(fullNumber, digit, false) * h, h);
                }
            }
        }

        private static void DrawMonospaceNumber(int fullNumber, int digits, Bitmap digitBitmap, Bitmap nullBitmap, int x, int y, int w, int h)
        {
            for (int d = 0; d < digits; d++)
            {
                DrawOneDigit(fullNumber, d, digitBitmap, nullBitmap, x - d * w, y, h);
            }
        }

        private static void DrawStatus(AtsMain.AtsVehicleState state)
        {
            int absSpeed = Math.Abs((int)Math.Ceiling(state.Speed));
            bitmapGDI.BeginGDI();
            // 当前速度绘制
            DrawOneDigit(absSpeed, 2, currentSpeedDigitBitmap, nullDigitBigBitmap, 129, 14, 51);
            DrawOneDigit(absSpeed, 1, currentSpeedDigitBitmap, nullDigitBigBitmap, 158, 14, 51);
            DrawOneDigit(absSpeed, 0, currentSpeedDigitBitmap, nullDigitBigBitmap, 187, 14, 51);

            // 绘制日期（实际日期）
            DateTime now = DateTime.Now;
            bitmapGDI.DrawImage(dateDigitBitmap, 691, 13, Tool.D(now.Year, 3, true) * 17, 17);
            bitmapGDI.DrawImage(dateDigitBitmap, 701, 13, Tool.D(now.Year, 2, true) * 17, 17);
            bitmapGDI.DrawImage(dateDigitBitmap, 711, 13, Tool.D(now.Year, 1, true) * 17, 17);
            bitmapGDI.DrawImage(dateDigitBitmap, 721, 13, Tool.D(now.Year, 0, true) * 17, 17);
            bitmapGDI.DrawImage(dateDigitBitmap, 742, 13, Tool.D(now.Month, 1, true) * 17, 17);
            bitmapGDI.DrawImage(dateDigitBitmap, 752, 13, Tool.D(now.Month, 0, true) * 17, 17);
            bitmapGDI.DrawImage(dateDigitBitmap, 772, 13, Tool.D(now.Day, 1, true) * 17, 17);
            bitmapGDI.DrawImage(dateDigitBitmap, 782, 13, Tool.D(now.Day, 0, true) * 17, 17);

            // 绘制时间
            int sec = Convert.ToInt32(state.Time) / 1000 % 60;
            int min = Convert.ToInt32(state.Time) / 1000 / 60 % 60;
            int hrs = Convert.ToInt32(state.Time) / 1000 / 3600 % 60;
            bitmapGDI.DrawImage(timeDigitBitmap, 693, 38, Tool.D(hrs, 1, true) * 25, 25);
            bitmapGDI.DrawImage(timeDigitBitmap, 707, 38, Tool.D(hrs, 0, true) * 25, 25);
            bitmapGDI.DrawImage(timeDigitBitmap, 728, 38, Tool.D(min, 1, true) * 25, 25);
            bitmapGDI.DrawImage(timeDigitBitmap, 742, 38, Tool.D(min, 0, true) * 25, 25);
            bitmapGDI.DrawImage(timeDigitBitmap, 763, 38, Tool.D(sec, 1, true) * 25, 25);
            bitmapGDI.DrawImage(timeDigitBitmap, 777, 38, Tool.D(sec, 0, true) * 25, 25);

            // 绘制物理状态
            int bcPressureY = 109;
            int engineStatusY = 134;
            int threeSpeedY = 189;

            switch (AtsMain.vehicleType)
            {
                case 3:
                    bcPressureY = 109;
                    engineStatusY = 134;
                    threeSpeedY = 189;
                    break;
                default:
                    break;
            }
            bitmapGDI.DrawImage(EMU_StatusWindowBitmap, 561, 81);

            // 电流
            DrawMonospaceNumber(Math.Abs((int)state.Current), 4, statusWhiteDigitBitmap, statusNullDigitBitmap, 724, 86, 11, 17);

            // 制动缸压力
            DrawMonospaceNumber((int)(state.BcPressure), 4, statusWhiteDigitBitmap, statusNullDigitBitmap, 724, bcPressureY, 11, 17);

            // 换向器
            bitmapGDI.DrawImage(reverserBitmap, 607, engineStatusY, (1 - AtsMain.userReverserPosition) * 23, 23);

            // 零位
            if (AtsMain.actualPowerPosition == 0)
            {
                bitmapGDI.DrawImage(notchZeroBitmap, 653, engineStatusY, 0, 23);
            }
            else
            {
                bitmapGDI.DrawImage(notchZeroBitmap, 653, engineStatusY, 23, 23);
            }

            // 牵引/制动
            switch (AtsMain.vehicleType)
            {
                case 3:
                    if (AtsMain.actualPowerPosition > 0)
                    {
                        bitmapGDI.DrawImage(tractionBrakeBitmap, 699, engineStatusY, 0, 23);
                    }
                    else if (AtsMain.actualPowerPosition < 0 || AtsMain.actualBrakePosition > 0)
                    {
                        bitmapGDI.DrawImage(tractionBrakeBitmap, 699, engineStatusY, 23, 23);
                    }
                    else
                    {
                        bitmapGDI.DrawImage(tractionBrakeBitmap, 699, engineStatusY, 46, 23);
                    }
                    break;
                default:
                    break;
            }

            // 三通道速度
            int absSpeed3 = Math.Abs((int)Math.Ceiling(state.Speed));
            DrawMonospaceNumber(absSpeed3, 3, statusGreenDigitBitmap, statusNullDigitBitmap, 664, threeSpeedY, 11, 17);
            DrawMonospaceNumber(absSpeed3, 3, statusGreyDigitBitmap, statusNullDigitBitmap, 697, threeSpeedY, 11, 17);
            DrawMonospaceNumber(absSpeed3, 3, statusGreyDigitBitmap, statusNullDigitBitmap, 730, threeSpeedY, 11, 17);

            // 关闭GDIHelper
            bitmapGDI.EndGDI();
        }

        internal static void Frame(AtsMain.AtsVehicleState state)
        {
            if (tHandle.HasEnoughTimePassed(targetFPS))
            {
                DrawBase();
                DrawStatus(state);
                tHandle.Update(bitmapGDI);
            }
        }
    }
}
