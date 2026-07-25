using System;

namespace CTCS0_Ats
{
    internal static class PanelOutput
    {
        private const int PANEL_BC_CAUTION = 229;
        private const int PANEL_SPEED = 230;
        private const int PANEL_BC_PRESS0 = 235;
        private const int PANEL_BC_PRESS1 = 236;
        private const int PANEL_BC_PRESS2 = 237;
        private const int PANEL_BC_PRESS3 = 238;
        private const int PANEL_MR_PRESS0 = 240;
        private const int PANEL_MR_PRESS1 = 241;
        private const int PANEL_MR_PRESS2 = 242;

        private const int NO_DISPLAY = 10;

        private static int updateTimer = -1;
        private static int prevTime;
        private static int speed;
        private static int bcPress0, bcPress1, bcPress2, bcPress3;
        private static int mrPress0, mrPress1, mrPress2;
        private static bool bcCaution;

        internal static void Reset()
        {
            updateTimer = -1;
            prevTime = 0;
            speed = 0;
            bcPress0 = NO_DISPLAY;
            bcPress1 = NO_DISPLAY;
            bcPress2 = NO_DISPLAY;
            bcPress3 = NO_DISPLAY;
            mrPress0 = NO_DISPLAY;
            mrPress1 = NO_DISPLAY;
            mrPress2 = NO_DISPLAY;
            bcCaution = false;
        }

        internal static void Update(AtsMain.AtsVehicleState state)
        {
            int deltaT = state.Time - prevTime;
            prevTime = state.Time;
            updateTimer -= Math.Abs(deltaT);
            if (updateTimer < 0)
            {
                float absSpeed = Math.Abs(state.Speed);
                speed = (int)absSpeed;

                float bcPressure = state.BcPressure;
                float mrPressure = state.MrPressure;

                bcCaution = bcPressure < 200 && AtsMain.doorOpen;

                bcPress0 = NO_DISPLAY;
                bcPress1 = NO_DISPLAY;
                bcPress2 = NO_DISPLAY;
                bcPress3 = NO_DISPLAY;
                mrPress0 = NO_DISPLAY;
                mrPress1 = NO_DISPLAY;
                mrPress2 = NO_DISPLAY;

                if (bcPressure < 200)
                {
                    bcPress0 = (int)(bcPressure / 20);
                }
                else if (bcPressure < 400)
                {
                    bcPress1 = (int)((bcPressure - 200) / 20);
                }
                else if (bcPressure < 600)
                {
                    bcPress2 = (int)((bcPressure - 400) / 20);
                }
                else if (bcPressure < 800)
                {
                    bcPress3 = (int)((bcPressure - 600) / 20);
                }

                if (mrPressure <= 700)
                {
                    mrPress0 = NO_DISPLAY;
                }
                else if (mrPressure < 800)
                {
                    mrPress0 = (int)((mrPressure - 700) / 10);
                }
                else if (mrPressure < 900)
                {
                    mrPress1 = (int)((mrPressure - 800) / 10);
                }
                else if (mrPressure < 1000)
                {
                    mrPress2 = (int)((mrPressure - 900) / 10);
                }

                updateTimer = 200 + (state.Time % 50) * 5;
            }
        }

        internal static void WritePanel(AtsMain.AtsIoArray panel)
        {
            panel[PANEL_BC_CAUTION] = bcCaution ? ((AtsMain.vehicleState.Time % 1000) / 500) : 0;
            panel[PANEL_SPEED] = speed;
            panel[PANEL_BC_PRESS0] = bcPress0;
            panel[PANEL_BC_PRESS1] = bcPress1;
            panel[PANEL_BC_PRESS2] = bcPress2;
            panel[PANEL_BC_PRESS3] = bcPress3;
            panel[PANEL_MR_PRESS0] = mrPress0;
            panel[PANEL_MR_PRESS1] = mrPress1;
            panel[PANEL_MR_PRESS2] = mrPress2;
        }
    }
}
