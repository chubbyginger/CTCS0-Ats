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

        private static void DrawStatus(AtsMain.AtsVehicleState state)
        {
            int[] speedDigits = Tool.SplitDigits3(Math.Abs((int)state.Speed));
            bitmapGDI.BeginGDI();
            // 当前速度绘制
            if (speedDigits[0] != 0)
            {
                bitmapGDI.DrawImage(currentSpeedDigitBitmap, 129, 14, speedDigits[0] * 51, 51);
            }
            else
            {
                bitmapGDI.DrawImage(nullDigitBigBitmap, 129, 14);
            }
            if (speedDigits[1] != 0 || speedDigits[0] != 0)
            {
                bitmapGDI.DrawImage(currentSpeedDigitBitmap, 158, 14, speedDigits[1] * 51, 51);
            }
            else
            {
                bitmapGDI.DrawImage(nullDigitBigBitmap, 158, 14);
            }
            bitmapGDI.DrawImage(currentSpeedDigitBitmap, 187, 14, speedDigits[2] * 51, 51);

            // 绘制日期（实际日期）
            DateTime now = DateTime.Now;
            int[] yearDigits = Tool.SplitDigits4(now.Year);
            int[] monthDigits = Tool.SplitDigits2(now.Month);
            int[] dayDigits = Tool.SplitDigits2(now.Day);
            bitmapGDI.DrawImage(dateDigitBitmap, 691, 13, yearDigits[0] * 17, 17);
            bitmapGDI.DrawImage(dateDigitBitmap, 701, 13, yearDigits[1] * 17, 17);
            bitmapGDI.DrawImage(dateDigitBitmap, 711, 13, yearDigits[2] * 17, 17);
            bitmapGDI.DrawImage(dateDigitBitmap, 721, 13, yearDigits[3] * 17, 17);
            bitmapGDI.DrawImage(dateDigitBitmap, 742, 13, monthDigits[0] * 17, 17);
            bitmapGDI.DrawImage(dateDigitBitmap, 752, 13, monthDigits[1] * 17, 17);
            bitmapGDI.DrawImage(dateDigitBitmap, 772, 13, dayDigits[0] * 17, 17);
            bitmapGDI.DrawImage(dateDigitBitmap, 782, 13, dayDigits[1] * 17, 17);

            // 绘制时间
            int sec = Convert.ToInt32(state.Time) / 1000 % 60;
            int min = Convert.ToInt32(state.Time) / 1000 / 60 % 60;
            int hrs = Convert.ToInt32(state.Time) / 1000 / 3600 % 60;
            int[] secDigits = Tool.SplitDigits2(sec);
            int[] minDigits = Tool.SplitDigits2(min);
            int[] hrsDigits = Tool.SplitDigits2(hrs);
            bitmapGDI.DrawImage(timeDigitBitmap, 693, 38, hrsDigits[0] * 25, 25);
            bitmapGDI.DrawImage(timeDigitBitmap, 707, 38, hrsDigits[1] * 25, 25);
            bitmapGDI.DrawImage(timeDigitBitmap, 728, 38, minDigits[0] * 25, 25);
            bitmapGDI.DrawImage(timeDigitBitmap, 742, 38, minDigits[1] * 25, 25);
            bitmapGDI.DrawImage(timeDigitBitmap, 763, 38, secDigits[0] * 25, 25);
            bitmapGDI.DrawImage(timeDigitBitmap, 777, 38, secDigits[1] * 25, 25);

            // 绘制物理状态

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
