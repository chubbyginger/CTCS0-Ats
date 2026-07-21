using System;

namespace CTCS0_Ats
{
    internal enum AntiSlipType
    {
        None = 0,
        PipePressure = 1,
        Handle = 2,
        Phase = 3
    }

    internal class AntiSlipMonitor
    {
        private const float PIPE_DELAY = 5f;
        private const float ACTION_TIMEOUT = 10f;
        private const float HANDLE_SPEED_THRESHOLD = 3f;
        private const float HANDLE_DIST_THRESHOLD = 10f;
        private const float PHASE_HIGH_SPEED = 10f;
        private const float PRESSURE_THRESHOLD = 80f;

        private AntiSlipType activeType;
        private float alarmTimer;
        private bool wasStopped;
        private float stopLocation;
        private int handlePowerAtStop;
        private int handleBrakeAtStop;
        private bool emergencyLatched;

        internal AntiSlipType ActiveType => activeType;
        internal bool IsAlarming => activeType != AntiSlipType.None;
        internal float AlarmCountdown => IsAlarming ? Math.Max(0f, ACTION_TIMEOUT - alarmTimer) : 0f;

        internal void Reset()
        {
            activeType = AntiSlipType.None;
            alarmTimer = 0;
            wasStopped = true;
            stopLocation = 0;
            handlePowerAtStop = 0;
            handleBrakeAtStop = 0;
            emergencyLatched = false;
        }

        internal BrakeAction Evaluate(AtsMain.AtsVehicleState state, float deltaTime)
        {
            float absSpeed = Math.Abs(state.Speed);
            bool isStopped = absSpeed < 0.5f;
            float currentLocation = (float)state.Location;

            if (isStopped && !wasStopped)
            {
                stopLocation = currentLocation;
                handlePowerAtStop = AtsMain.userPowerPosition;
                handleBrakeAtStop = AtsMain.userBrakePosition;
            }

            if (emergencyLatched)
            {
                return BrakeAction.Emergency;
            }

            BrakeAction action = BrakeAction.None;

            action |= EvaluatePipePressure(state, deltaTime, isStopped);
            action |= EvaluateHandle(state, deltaTime, isStopped, absSpeed, currentLocation);
            action |= EvaluatePhase(state, deltaTime, isStopped, absSpeed, currentLocation);

            wasStopped = isStopped;
            return action;
        }

        internal void OnVigilanceKey()
        {
            if (emergencyLatched)
            {
                if (Math.Abs(AtsMain.vehicleState.Speed) < 0.5f
                    && AtsMain.userBrakePosition >= AtsMain.vehicleSpec.BrakeNotches + 1)
                {
                    emergencyLatched = false;
                    activeType = AntiSlipType.None;
                    alarmTimer = 0;
                    Tool.DebugWriteLine("防溜: 紧急制动撤除");
                }
                return;
            }

            if (!IsAlarming) return;

            switch (activeType)
            {
                case AntiSlipType.PipePressure:
                    ClearAlarm();
                    break;
                case AntiSlipType.Handle:
                    ClearAlarm();
                    break;
                case AntiSlipType.Phase:
                    alarmTimer = 0;
                    break;
            }
        }

        private BrakeAction EvaluatePipePressure(AtsMain.AtsVehicleState state, float deltaTime, bool isStopped)
        {
            if (!isStopped) return BrakeAction.None;
            if (activeType != AntiSlipType.None && activeType != AntiSlipType.PipePressure) return BrakeAction.None;

            float bcPressure = state.BcPressure;
            float bpReduction = GetBpReduction(state);

            bool pressureOk = bcPressure >= PRESSURE_THRESHOLD || bpReduction >= PRESSURE_THRESHOLD;

            if (pressureOk)
            {
                if (activeType == AntiSlipType.PipePressure)
                {
                    ClearAlarm();
                }
                return BrakeAction.None;
            }

            if (activeType == AntiSlipType.PipePressure)
            {
                alarmTimer += deltaTime;
                if (alarmTimer >= ACTION_TIMEOUT)
                {
                    emergencyLatched = true;
                    return BrakeAction.Emergency;
                }
                return BrakeAction.Warning;
            }

            if (activeType == AntiSlipType.None)
            {
                alarmTimer += deltaTime;
                if (alarmTimer >= PIPE_DELAY)
                {
                    activeType = AntiSlipType.PipePressure;
                    alarmTimer = PIPE_DELAY;
                    Tool.DebugWriteLine("防溜: 管压防溜报警");
                }
                return BrakeAction.None;
            }

            return BrakeAction.None;
        }

        private BrakeAction EvaluateHandle(AtsMain.AtsVehicleState state, float deltaTime, bool isStopped, float absSpeed, float currentLocation)
        {
            if (activeType != AntiSlipType.None && activeType != AntiSlipType.Handle) return BrakeAction.None;

            if (isStopped) return BrakeAction.None;

            bool handleChanged = AtsMain.userPowerPosition != handlePowerAtStop
                || AtsMain.userBrakePosition != handleBrakeAtStop;
            bool isLoaded = AtsMain.userPowerPosition > 0;

            if (handleChanged || isLoaded)
            {
                if (activeType == AntiSlipType.Handle)
                {
                    ClearAlarm();
                }
                return BrakeAction.None;
            }

            float moveDist = Math.Abs(currentLocation - stopLocation);

            if (absSpeed >= HANDLE_SPEED_THRESHOLD || moveDist >= HANDLE_DIST_THRESHOLD)
            {
                if (activeType == AntiSlipType.Handle)
                {
                    alarmTimer += deltaTime;
                    if (alarmTimer >= ACTION_TIMEOUT)
                    {
                        emergencyLatched = true;
                        return BrakeAction.Emergency;
                    }
                    return BrakeAction.Warning;
                }

                if (activeType == AntiSlipType.None)
                {
                    activeType = AntiSlipType.Handle;
                    alarmTimer = 0;
                    Tool.DebugWriteLine("防溜: 手柄防溜报警");
                    return BrakeAction.Warning;
                }
            }

            return BrakeAction.None;
        }

        private BrakeAction EvaluatePhase(AtsMain.AtsVehicleState state, float deltaTime, bool isStopped, float absSpeed, float currentLocation)
        {
            if (activeType != AntiSlipType.None && activeType != AntiSlipType.Phase) return BrakeAction.None;

            if (isStopped) return BrakeAction.None;

            bool isReverse = state.Speed < 0;
            if (!isReverse) return BrakeAction.None;

            float moveDist = Math.Abs(currentLocation - stopLocation);

            if (absSpeed >= PHASE_HIGH_SPEED)
            {
                if (activeType == AntiSlipType.Phase)
                {
                    return BrakeAction.Warning;
                }
            }

            if (absSpeed >= HANDLE_SPEED_THRESHOLD || moveDist >= HANDLE_DIST_THRESHOLD)
            {
                if (activeType == AntiSlipType.Phase)
                {
                    alarmTimer += deltaTime;
                    if (alarmTimer >= ACTION_TIMEOUT)
                    {
                        emergencyLatched = true;
                        return BrakeAction.Emergency;
                    }
                    return BrakeAction.Warning;
                }

                if (activeType == AntiSlipType.None)
                {
                    activeType = AntiSlipType.Phase;
                    alarmTimer = 0;
                    Tool.DebugWriteLine("防溜: 相位防溜报警");
                    return BrakeAction.Warning;
                }
            }

            return BrakeAction.None;
        }

        private void ClearAlarm()
        {
            Tool.DebugWriteLine("防溜: " + activeType.ToString() + "报警解除");
            activeType = AntiSlipType.None;
            alarmTimer = 0;
        }

        private static float GetBpReduction(AtsMain.AtsVehicleState state)
        {
            return Math.Max(0, 500f - state.BpPressure);
        }
    }
}
