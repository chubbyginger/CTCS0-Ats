using System;
using System.Drawing;
using Zbx1425.DXDynamicTexture;

namespace CTCS0_Ats
{
    internal class DMI
    {
        /// <summary>
        /// 帧计数器，用于实现闪烁效果
        /// </summary>
        public static int frameCounter;
        /// <summary>
        /// DMI机车信号闪烁用的，
        /// </summary>
        public static int previousBlinkState;

        /// <summary>
        /// 被替换纹理的handle
        /// </summary>
        private static TextureHandle tHandle;

        // 各种位图资源
        private static Bitmap baseBitmap;
        private static Bitmap currentSpeedDigitBitmap, limitSpeedDigitBitmap, targetDistanceDigitBitmap, nullDigitBigBitmap, targetDistanceNullBitmap;
        private static Bitmap dateDigitBitmap, timeDigitBitmap;
        // DL_DMU: 内燃机车、内燃动车组，自空制动机。DMU_Straight：内燃动车组直通制动机。EL：电力机车自空制动机。EMU：电动车组直通制动机。
        private static Bitmap DL_DMU_StatusWindowBitmap, DMU_StraightStatusWindowBitmap, EL_StatusWindowBitmap, EMU_StatusWindowBitmap;
        private static Bitmap statusWhiteDigitBitmap, statusGreenDigitBitmap, statusGreyDigitBitmap, statusNullDigitBitmap;
        private static Bitmap reverserBitmap, notchZeroBitmap, tractionBrakeBitmap;

        private static Bitmap passengerFreightBitmap;

        private static Bitmap cabSignalBitmap;

        private static Bitmap notchCutBitmap, serviceBrakeBitmap, emergencyBrakeBitmap;
        private static Bitmap downgradeBitmap, shuntingBitmap, departBitmap, restrictedModeBitmap;
        private static Bitmap reverseWarningBitmap;
        private static Bitmap runawayProtectionCountdownBitmap;
        private static Bitmap releaseAntiSlipCountdownBitmap;

        private static Pen brakeCurvePen;
        private static Pen emergencyCurvePen;
        private static Pen speedTrailPen;

        /// <summary>
        /// GDIHelper封装，适用于整个DMI
        /// </summary>
        private static GDIHelper bitmapGDI;

        internal static void Load()
        {
            frameCounter = 0;
            previousBlinkState = 0;
            try
            {
                TextureManager.Initialize();
                tHandle = TextureManager.Register(Config.TexturePath, Config.TextureWidth, Config.TextureHeight);
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
            bitmapGDI = new GDIHelper(Config.TextureWidth, Config.TextureHeight);

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
                passengerFreightBitmap = new Bitmap(Bitmap.FromFile(AtsMain.dllParentPath + "/assets/pf.png"));
                cabSignalBitmap = new Bitmap(Bitmap.FromFile(AtsMain.dllParentPath + "/assets/cabsignal.png"));
                targetDistanceNullBitmap = new Bitmap(Bitmap.FromFile(AtsMain.dllParentPath + "/assets/target_distance_null.png"));
                notchCutBitmap = new Bitmap(Bitmap.FromFile(AtsMain.dllParentPath + "/assets/notchcut.png"));
                serviceBrakeBitmap = new Bitmap(Bitmap.FromFile(AtsMain.dllParentPath + "/assets/servicebrake.png"));
                emergencyBrakeBitmap = new Bitmap(Bitmap.FromFile(AtsMain.dllParentPath + "/assets/emergencybrake.png"));
                downgradeBitmap = new Bitmap(Bitmap.FromFile(AtsMain.dllParentPath + "/assets/downgrade.png"));
                shuntingBitmap = new Bitmap(Bitmap.FromFile(AtsMain.dllParentPath + "/assets/shunting.png"));
                departBitmap = new Bitmap(Bitmap.FromFile(AtsMain.dllParentPath + "/assets/depart.png"));
                restrictedModeBitmap = new Bitmap(Bitmap.FromFile(AtsMain.dllParentPath + "/assets/restricted_mode.png"));
                reverseWarningBitmap = new Bitmap(Bitmap.FromFile(AtsMain.dllParentPath + "/assets/reverse_warning.png"));
                runawayProtectionCountdownBitmap = new Bitmap(Bitmap.FromFile(AtsMain.dllParentPath + "/assets/runaway_countdown_window.png"));
                releaseAntiSlipCountdownBitmap = new Bitmap(Bitmap.FromFile(AtsMain.dllParentPath + "/assets/release_antislip_countdown_window.png"));
            }
            catch (Exception ex)
            {

                Tool.DebugWriteLine("DMI位图资源加载阶段错误：" + ex.ToString());
            }

            brakeCurvePen = new Pen(Color.FromArgb(0xfb, 0x00, 0x00), 1);
            emergencyCurvePen = new Pen(Color.FromArgb(0x00, 0x00, 0xa7), 1);
            speedTrailPen = new Pen(Color.FromArgb(0x00, 0xa7, 0x00), 1);
        }

        private static void DrawBase()
        {
            bitmapGDI.DrawImage(baseBitmap, 0, 0);
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

        /// <summary>
        /// 绘制右对齐数字
        /// </summary>
        /// <param name="fullNumber">完整数字</param>
        /// <param name="digits">一共有几位</param>
        /// <param name="digitBitmap">数字的位图</param>
        /// <param name="nullBitmap">对应的空位图</param>
        /// <param name="x">最右边数字左上角X坐标</param>
        /// <param name="y">最右边数字左上角Y坐标</param>
        /// <param name="w">一个数字高度</param>
        /// <param name="h">一个数字宽度</param>
        private static void DrawMonospaceNumber(int fullNumber, int digits, Bitmap digitBitmap, Bitmap nullBitmap, int x, int y, int w, int h)
        {
            for (int d = 0; d < digits; d++)
            {
                DrawOneDigit(fullNumber, d, digitBitmap, nullBitmap, x - d * w, y, h);
            }
        }

        private static void DrawTrainPhysics(AtsMain.AtsVehicleState state)
        {
            int bcPressureY = 109;
            int engineStatusY = 134;
            int threeSpeedY = 189;

            if (Config.VehicleType == Config.VehicleTypeEnum.EMU && Config.BrakeType == Config.BrakeTypeEnum.Straight)
            {
                bcPressureY = 109;
                engineStatusY = 134;
                threeSpeedY = 189;
                bitmapGDI.DrawImage(EMU_StatusWindowBitmap, 561, 81);
                // 电流
                DrawMonospaceNumber(Math.Abs((int)state.Current), 4, statusWhiteDigitBitmap, statusNullDigitBitmap, 724, 86, 11, 17);
                // 牵引/制动工况
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

                // 三通道速度
                int absSpeed3 = Math.Abs((int)Math.Ceiling(state.Speed));
                DrawMonospaceNumber(absSpeed3, 3, statusGreenDigitBitmap, statusNullDigitBitmap, 664, threeSpeedY, 11, 17);
                DrawMonospaceNumber(absSpeed3, 3, statusGreyDigitBitmap, statusNullDigitBitmap, 697, threeSpeedY, 11, 17);
                DrawMonospaceNumber(absSpeed3, 3, statusGreyDigitBitmap, statusNullDigitBitmap, 730, threeSpeedY, 11, 17);
            }
        }

        private static void DrawTopBar(AtsMain.AtsVehicleState state)
        {
            int absSpeed = Math.Abs((int)Math.Ceiling(state.Speed));
            DrawOneDigit(absSpeed, 2, currentSpeedDigitBitmap, nullDigitBigBitmap, 129, 14, 51);
            DrawOneDigit(absSpeed, 1, currentSpeedDigitBitmap, nullDigitBigBitmap, 158, 14, 51);
            DrawOneDigit(absSpeed, 0, currentSpeedDigitBitmap, nullDigitBigBitmap, 187, 14, 51);

            int limitSpeed = (int)Math.Floor(AtsMain.modeController.CurrentServiceBrakeSpeed);
            DrawMonospaceNumber(limitSpeed, 3, limitSpeedDigitBitmap, nullDigitBigBitmap, 307, 14, 29, 51);

            if (AtsMain.modeController.HasValidTarget)
            {
                int targetDist = (int)Math.Floor(AtsMain.modeController.TargetDistance);
                DrawMonospaceNumber(targetDist, 4, targetDistanceDigitBitmap, nullDigitBigBitmap, 483, 14, 29, 51);
            }
            else
            {
                bitmapGDI.DrawImage(targetDistanceNullBitmap, 374, 14);
            }
        }

        private static void DrawVehicleStatus(AtsMain.AtsVehicleState state)
        {
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
            DrawTrainPhysics(state);
        }

        internal static void DrawRightStatusBar()
        {
            switch (Config.PassengerFreight)
            {
                case Config.PassengerFreightEnum.Passenger:
                    bitmapGDI.DrawImage(passengerFreightBitmap, 754, 330, 0, 24);
                    break;
                case Config.PassengerFreightEnum.Freight:
                    bitmapGDI.DrawImage(passengerFreightBitmap, 754, 330, 24, 24);
                    break;
                default:
                    Tool.DebugWriteLine("客货状态是无效的");
                    break;
            }
        }

        internal static void DrawCabSignal(CabSignal.CabSignalCode signal)
        {
            if (signal == CabSignal.CabSignalCode.UUS)
            {
                if (frameCounter >= (0.5 * Config.TargetFPS))
                {
                    frameCounter = 0;
                    previousBlinkState = 1 - previousBlinkState;
                }
                switch (previousBlinkState)
                {
                    case 0:
                        bitmapGDI.DrawImage(cabSignalBitmap, 8, 8, 708, 59);
                        break;
                    case 1:
                        bitmapGDI.DrawImage(cabSignalBitmap, 8, 8, 944, 59);
                        break;
                }
            }
            else if (signal == CabSignal.CabSignalCode.U2S)
            {
                if (frameCounter >= (0.5 * Config.TargetFPS))
                {
                    frameCounter = 0;
                    previousBlinkState = 1 - previousBlinkState;
                }
                switch (previousBlinkState)
                {
                    case 0:
                        bitmapGDI.DrawImage(cabSignalBitmap, 8, 8, 826, 59);
                        break;
                    case 1:
                        bitmapGDI.DrawImage(cabSignalBitmap, 8, 8, 944, 59);
                        break;
                }
            }
            else if (signal == CabSignal.CabSignalCode.HB)
            {
                if (frameCounter >= (0.5 * Config.TargetFPS))
                {
                    frameCounter = 0;
                    previousBlinkState = 1 - previousBlinkState;
                }
                switch (previousBlinkState)
                {
                    case 0:
                        bitmapGDI.DrawImage(cabSignalBitmap, 8, 8, 590, 59);
                        break;
                    case 1:
                        bitmapGDI.DrawImage(cabSignalBitmap, 8, 8, 944, 59);
                        break;
                }
            }
            else if (signal == CabSignal.CabSignalCode.HU
                || signal == CabSignal.CabSignalCode.U
                || signal == CabSignal.CabSignalCode.LU
                || signal == CabSignal.CabSignalCode.L
                || signal == CabSignal.CabSignalCode.L2
                || signal == CabSignal.CabSignalCode.L3
                || signal == CabSignal.CabSignalCode.L4
                || signal == CabSignal.CabSignalCode.L5
                || signal == CabSignal.CabSignalCode.H
                || signal == CabSignal.CabSignalCode.UU
                || signal == CabSignal.CabSignalCode.U2
                || signal == CabSignal.CabSignalCode.B)
            {
                bitmapGDI.DrawImage(cabSignalBitmap, 8, 8, (int)signal * 59, 59);
            }
        }

        internal static void DrawInterventionStatus(BrakeAction action, OperationMode mode)
        {
            if ((action & BrakeAction.Emergency) != 0)
            {
                bitmapGDI.DrawImage(emergencyBrakeBitmap, 754, 134);
            }
            if ((action & BrakeAction.ServiceBrake) != 0)
            {
                bitmapGDI.DrawImage(serviceBrakeBitmap, 754, 162);
            }
            if ((action & BrakeAction.PowerCut) != 0)
            {
                bitmapGDI.DrawImage(notchCutBitmap, 754, 190);
            }
        }

        internal static void DrawOperationMode(OperationMode mode)
        {
            switch (mode)
            {
                case OperationMode.Degraded:
                    bitmapGDI.DrawImage(downgradeBitmap, 754, 106);
                    break;
                case OperationMode.Restricted:
                    bitmapGDI.DrawImage(restrictedModeBitmap, 344, 270);
                    break;
            }
        }

        internal static void DrawReverseControl()
        {
            bitmapGDI.DrawImage(reverseWarningBitmap, 250, 158);
        }

        internal static void DrawAntiSlipStatus(AntiSlipType type, float countdown)
        {
            bitmapGDI.DrawImage(runawayProtectionCountdownBitmap, 276, 241, ((int)type - 1) * 120, 120);
            DrawMonospaceNumber((int)Math.Floor(countdown), 2, limitSpeedDigitBitmap, nullDigitBigBitmap, 401, 302, 29, 51);
        }

        private static void DrawSingleCurve(System.Drawing.Graphics g, SpeedCurve curve, Pen pen,
            float currentLocation, float fromLoc, float toLoc,
            float pixelsPerMeter, float pixelsPerKmh)
        {
            if (curve == null || curve.Points.Count < 2) return;

            const int CUR_POS_X = 184;
            const int CURVE_TOP = 84;
            const int CURVE_BOTTOM = 452;
            const int CURVE_LEFT = 45;
            const int CURVE_RIGHT = 747;

            var points = new System.Collections.Generic.List<Point>();
            foreach (var p in curve.Points)
            {
                if (p.Location < fromLoc || p.Location > toLoc) continue;
                int px = CUR_POS_X + (int)((p.Location - currentLocation) * pixelsPerMeter);
                int py = CURVE_BOTTOM - (int)(p.Speed * pixelsPerKmh);
                py = Math.Max(CURVE_TOP, Math.Min(CURVE_BOTTOM, py));
                points.Add(new Point(px, py));
            }

            if (points.Count == 0 || points[0].X > CURVE_LEFT)
            {
                float edgeSpeed = curve.GetSpeedAt(fromLoc);
                int px = CUR_POS_X + (int)((fromLoc - currentLocation) * pixelsPerMeter);
                int py = CURVE_BOTTOM - (int)(edgeSpeed * pixelsPerKmh);
                py = Math.Max(CURVE_TOP, Math.Min(CURVE_BOTTOM, py));
                points.Insert(0, new Point(px, py));
            }
            if (points.Count <= 1 || points[points.Count - 1].X < CURVE_RIGHT)
            {
                float edgeSpeed = curve.GetSpeedAt(toLoc);
                int px = CUR_POS_X + (int)((toLoc - currentLocation) * pixelsPerMeter);
                int py = CURVE_BOTTOM - (int)(edgeSpeed * pixelsPerKmh);
                py = Math.Max(CURVE_TOP, Math.Min(CURVE_BOTTOM, py));
                points.Add(new Point(px, py));
            }

            if (points.Count >= 2)
            {
                g.DrawLines(pen, points.ToArray());
            }
        }

        private static void DrawBrakeTrail(System.Drawing.Graphics g, Pen pen,
            System.Collections.Generic.List<BrakeCurveTrailPoint> trailPoints, bool useEmergency,
            float currentLocation, float fromLoc,
            float pixelsPerMeter, float pixelsPerKmh)
        {
            if (trailPoints.Count < 2) return;

            const int CUR_POS_X = 184;
            const int CURVE_TOP = 84;
            const int CURVE_BOTTOM = 452;

            var points = new System.Collections.Generic.List<Point>();
            foreach (var p in trailPoints)
            {
                float speed = useEmergency ? p.EmergencyBrakeSpeed : p.ServiceBrakeSpeed;
                int px = CUR_POS_X + (int)((p.Location - currentLocation) * pixelsPerMeter);
                int py = CURVE_BOTTOM - (int)(speed * pixelsPerKmh);
                py = Math.Max(CURVE_TOP, Math.Min(CURVE_BOTTOM, py));
                points.Add(new Point(px, py));
            }

            g.DrawLines(pen, points.ToArray());
        }

        private static void DrawBrakeCurve()
        {
            const int CUR_POS_X = 184;
            const int LEFT_WIDTH = 139;
            const int RIGHT_WIDTH = 563;
            const int CURVE_TOP = 84;
            const int CURVE_BOTTOM = 452;
            const int SCALE_Y = 157;
            const int CURVE_LEFT = CUR_POS_X - LEFT_WIDTH;
            const int CURVE_W = LEFT_WIDTH + RIGHT_WIDTH;
            const int CURVE_H = CURVE_BOTTOM - CURVE_TOP;

            float displayDistance = Config.CurveDisplayDistance;
            int leftDist = (int)Math.Round(displayDistance * LEFT_WIDTH / RIGHT_WIDTH);
            float scaleSpeed = (float)Config.CurveSpeedScale;
            float pixelsPerMeter = (float)RIGHT_WIDTH / displayDistance;
            float pixelsPerKmh = (float)(CURVE_BOTTOM - SCALE_Y) / scaleSpeed;

            float currentLocation = (float)AtsMain.vehicleState.Location;
            float fromLoc = currentLocation - leftDist;
            float toLoc = currentLocation + displayDistance;

            var g = bitmapGDI.Graphics;
            g.SetClip(new Rectangle(CURVE_LEFT, CURVE_TOP, CURVE_W, CURVE_H));

            bool useEmergency = Config.BrakeType != Config.BrakeTypeEnum.Straight;

            var brakeTrailPoints = AtsMain.modeController.brakeTrail.GetTrailInRange(
                fromLoc, currentLocation);
            DrawBrakeTrail(g, brakeCurvePen, brakeTrailPoints, useEmergency,
                currentLocation, fromLoc, pixelsPerMeter, pixelsPerKmh);

            SpeedCurve curPrimary = AtsMain.modeController.ServiceBrakeCurve
                ?? AtsMain.modeController.EmergencyBrakeCurve;
            DrawSingleCurve(g, curPrimary, brakeCurvePen,
                currentLocation, currentLocation, toLoc, pixelsPerMeter, pixelsPerKmh);

            if (Config.ShowEmergencyBrakeCurve)
            {
                DrawBrakeTrail(g, emergencyCurvePen, brakeTrailPoints, true,
                    currentLocation, fromLoc, pixelsPerMeter, pixelsPerKmh);

                SpeedCurve curEmg = AtsMain.modeController.EmergencyBrakeCurve;
                DrawSingleCurve(g, curEmg, emergencyCurvePen,
                    currentLocation, currentLocation, toLoc, pixelsPerMeter, pixelsPerKmh);
            }

            if (!AtsMain.modeController.IsReversing)
            {
                var trailPoints = AtsMain.modeController.speedTrail.GetTrailInRange(
                    currentLocation - leftDist, currentLocation);

                if (trailPoints.Count >= 2)
                {
                    var drawPoints = new Point[trailPoints.Count];
                    for (int i = 0; i < trailPoints.Count; i++)
                    {
                        int px = CUR_POS_X + (int)((trailPoints[i].Location - currentLocation) * pixelsPerMeter);
                        int py = CURVE_BOTTOM - (int)(trailPoints[i].Speed * pixelsPerKmh);
                        py = Math.Max(CURVE_TOP, Math.Min(CURVE_BOTTOM, py));
                        drawPoints[i] = new Point(px, py);
                    }
                    g.DrawLines(speedTrailPen, drawPoints);
                }
            }

            g.ResetClip();
        }

        internal static void Frame(AtsMain.AtsVehicleState state, CabSignal.CabSignalCode signal)
        {
            if (tHandle.HasEnoughTimePassed(Config.TargetFPS))
            {
                frameCounter += 1;
                bitmapGDI.BeginGDI();
                DrawBase();
                DrawTopBar(state);
                DrawVehicleStatus(state);
                DrawRightStatusBar();
                DrawCabSignal(Config.ForceCabSignal >= 0 ? (CabSignal.CabSignalCode)Config.ForceCabSignal : signal);
                DrawInterventionStatus(AtsMain.modeController.LastBrakeAction, AtsMain.modeController.CurrentModeType);
                DrawOperationMode(AtsMain.modeController.CurrentModeType);
                if (AtsMain.modeController.IsReversing)
                {
                    DrawReverseControl();
                }
                if (AtsMain.modeController.ActiveAntiSlipType != AntiSlipType.None)
                {
                    DrawAntiSlipStatus(AtsMain.modeController.ActiveAntiSlipType, AtsMain.modeController.AntiSlipCountdown);
                }
                bitmapGDI.EndGDI();
                DrawBrakeCurve();
                tHandle.Update(bitmapGDI);
            }
        }
    }
}
