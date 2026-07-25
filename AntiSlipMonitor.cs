using System;

namespace CTCS0_Ats
{
    internal enum AntiSlipType
    {
        None = 0,
        PipePressure = 1,
        Handle = 2,
        Phase = 3,
        Release = 4
    }

    internal class AntiSlipMonitor
    {
        private const float PIPE_DELAY = 5f;
        private const float ACTION_TIMEOUT = 10f;
        private const float HANDLE_SPEED_THRESHOLD = 3f;
        private const float HANDLE_DIST_THRESHOLD = 10f;
        private const float PHASE_HIGH_SPEED = 10f;
        private const float PRESSURE_THRESHOLD = 80f;
        private const float RELEASE_DELAY = 60f;
        private const float RELEASE_TIMEOUT = 90f;

        private AntiSlipType activeType;
        private float alarmTimer;
        private bool wasStopped;
        private bool hadStopped;
        private float stopLocation;
        private bool emergencyLatched;

        private float pipeDelayTimer;
        private bool pipeCheckDone;

        private bool releaseArmed;
        private float releaseDelayTimer;

        private bool hadTractionSinceDeparture;

        internal AntiSlipType ActiveType => activeType;
        internal bool IsAlarming => activeType != AntiSlipType.None;
        internal float AlarmCountdown
        {
            get
            {
                if (activeType == AntiSlipType.Release) return Math.Max(0f, RELEASE_TIMEOUT - alarmTimer);
                if (activeType != AntiSlipType.None) return Math.Max(0f, ACTION_TIMEOUT - alarmTimer);
                return 0f;
            }
        }

        internal void Reset()
        {
            activeType = AntiSlipType.None;
            alarmTimer = 0;
            wasStopped = true;
            hadStopped = false;
            stopLocation = 0;
            emergencyLatched = false;
            pipeDelayTimer = 0;
            pipeCheckDone = false;
            releaseArmed = false;
            releaseDelayTimer = 0;
            hadTractionSinceDeparture = false;
        }

        internal BrakeAction Evaluate(AtsMain.AtsVehicleState state, float deltaTime)
        {
            float absSpeed = Math.Abs(state.Speed);
            bool isStopped = absSpeed < 0.5f;
            float currentLocation = (float)state.Location;

            if (isStopped && !wasStopped)
            {
                stopLocation = currentLocation;
                hadStopped = true;
                pipeDelayTimer = 0;
                pipeCheckDone = false;
                releaseArmed = false;
                releaseDelayTimer = 0;
                hadTractionSinceDeparture = false;
            }

            if (!isStopped && AtsMain.userPowerPosition > 0)
            {
                hadTractionSinceDeparture = true;
            }

            if (emergencyLatched)
            {
                return BrakeAction.Emergency | BrakeAction.PowerCut;
            }

            BrakeAction action = BrakeAction.None;

            action |= EvaluatePipePressure(state, deltaTime, isStopped);
            action |= EvaluateRelease(state, deltaTime, isStopped);
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
                    releaseArmed = false;
                    releaseDelayTimer = 0;
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
                    hadStopped = false;
                    ClearAlarm();
                    break;
                case AntiSlipType.Phase:
                    alarmTimer = 0;
                    break;
                case AntiSlipType.Release:
                    ClearAlarm();
                    releaseArmed = false;
                    releaseDelayTimer = 0;
                    break;
                case AntiSlipType.None:
                    if (releaseDelayTimer > 0)
                    {
                        releaseArmed = false;
                        releaseDelayTimer = 0;
                    }
                    break;
            }
        }

        private BrakeAction EvaluatePipePressure(AtsMain.AtsVehicleState state, float deltaTime, bool isStopped)
        {
            if (!isStopped)
            {
                if (activeType == AntiSlipType.PipePressure)
                {
                    ClearAlarm();
                }
                pipeDelayTimer = 0;
                pipeCheckDone = false;
                return BrakeAction.None;
            }
            if (activeType != AntiSlipType.None && activeType != AntiSlipType.PipePressure) return BrakeAction.None;

            if (activeType == AntiSlipType.PipePressure)
            {
                if (IsPressureOk(state))
                {
                    ClearAlarm();
                    return BrakeAction.None;
                }
                alarmTimer += deltaTime;
                if (alarmTimer >= ACTION_TIMEOUT)
                {
                    emergencyLatched = true;
                    activeType = AntiSlipType.None;
                    return BrakeAction.Emergency | BrakeAction.PowerCut;
                }
                return BrakeAction.Warning;
            }

            pipeDelayTimer += deltaTime;
            if (pipeDelayTimer < PIPE_DELAY) return BrakeAction.None;

            if (pipeCheckDone) return BrakeAction.None;
            pipeCheckDone = true;

            if (IsPressureOk(state)) return BrakeAction.None;

            activeType = AntiSlipType.PipePressure;
            alarmTimer = 0;
            Tool.DebugWriteLine("防溜: 管压防溜报警");
            return BrakeAction.Warning;
        }

        private BrakeAction EvaluateRelease(AtsMain.AtsVehicleState state, float deltaTime, bool isStopped)
        {
            if (activeType != AntiSlipType.None && activeType != AntiSlipType.Release) return BrakeAction.None;

            if (!isStopped)
            {
                if (activeType == AntiSlipType.Release)
                {
                    ClearAlarm();
                }
                releaseArmed = false;
                releaseDelayTimer = 0;
                return BrakeAction.None;
            }

            if (activeType == AntiSlipType.PipePressure) return BrakeAction.None;

            if (!pipeCheckDone) return BrakeAction.None;

            if (IsPressureOk(state))
            {
                if (activeType == AntiSlipType.Release)
                {
                    ClearAlarm();
                }
                releaseArmed = true;
                releaseDelayTimer = 0;
                return BrakeAction.None;
            }

            if (!releaseArmed)
            {
                releaseDelayTimer = 0;
                return BrakeAction.None;
            }

            if (activeType == AntiSlipType.Release)
            {
                alarmTimer += deltaTime;
                if (alarmTimer >= RELEASE_TIMEOUT)
                {
                    emergencyLatched = true;
                    activeType = AntiSlipType.None;
                    return BrakeAction.Emergency | BrakeAction.PowerCut;
                }
                return BrakeAction.Warning;
            }

            releaseDelayTimer += deltaTime;
            if (releaseDelayTimer >= RELEASE_DELAY)
            {
                activeType = AntiSlipType.Release;
                alarmTimer = 0;
                Tool.DebugWriteLine("防溜: 缓解防溜报警");
                return BrakeAction.Warning;
            }

            return BrakeAction.None;
        }

        private BrakeAction EvaluateHandle(AtsMain.AtsVehicleState state, float deltaTime, bool isStopped, float absSpeed, float currentLocation)
        {
            if (activeType != AntiSlipType.None && activeType != AntiSlipType.Handle) return BrakeAction.None;

            if (isStopped) return BrakeAction.None;

            if (!hadStopped) return BrakeAction.None;

            bool isLoaded = AtsMain.userPowerPosition > 0;

            if (hadTractionSinceDeparture || isLoaded)
            {
                if (activeType == AntiSlipType.Handle)
                {
                    hadStopped = false;
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
                        activeType = AntiSlipType.None;
                        return BrakeAction.Emergency | BrakeAction.PowerCut;
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

            if (isStopped)
            {
                if (activeType == AntiSlipType.Phase)
                {
                    ClearAlarm();
                }
                return BrakeAction.None;
            }

            if (!hadStopped) return BrakeAction.None;

            bool isReverse = state.Speed < 0;
            if (!isReverse)
            {
                if (activeType == AntiSlipType.Phase)
                {
                    ClearAlarm();
                }
                return BrakeAction.None;
            }

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
                        activeType = AntiSlipType.None;
                        return BrakeAction.Emergency | BrakeAction.PowerCut;
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

        private static bool IsPressureOk(AtsMain.AtsVehicleState state)
        {
            if (Config.BrakeType == Config.BrakeTypeEnum.Straight)
            {
                return state.BcPressure >= PRESSURE_THRESHOLD;
            }
            else
            {
                float bpReduction = Math.Max(0, Config.BpNominalPressure - state.BpPressure);
                return state.BcPressure >= PRESSURE_THRESHOLD || bpReduction >= PRESSURE_THRESHOLD;
            }
        }
    }
}
