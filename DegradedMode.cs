using System;

namespace CTCS0_Ats
{
    internal class DegradedMode : IOperationMode
    {
        private SpeedSupervisor supervisor;
        private AntiSlipMonitor antiSlip;
        private float entryLocation;
        private CabSignal.CabSignalCode currentSignal;
        private float stopSignalLocation;
        private bool hasStopSignalLocation;
        private bool limitCurveDirty;
        private int frameCount;
        private bool signalStabilized;
        private float maxForwardLocation;

        internal DegradedMode()
        {
            supervisor = new SpeedSupervisor();
            supervisor.Thresholds = SupervisionThresholds.Normal();
            antiSlip = new AntiSlipMonitor();
        }

        public void Enter()
        {
            entryLocation = (float)AtsMain.vehicleState.Location;
            currentSignal = CabSignal.currentSignal;
            hasStopSignalLocation = false;
            stopSignalLocation = 0;
            limitCurveDirty = true;
            frameCount = 0;
            signalStabilized = false;
            maxForwardLocation = entryLocation;
            antiSlip.Reset();
            Tool.DebugWriteLine("降级模式: 进入, Location=" + entryLocation);
        }

        public void Exit()
        {
            Tool.DebugWriteLine("降级模式: 退出");
        }

        public BrakeAction Update(AtsMain.AtsVehicleState state, float deltaTime)
        {
            if (currentSignal == CabSignal.CabSignalCode.H && signalStabilized)
            {
                if (Math.Abs(state.Speed) >= 0.5f)
                {
                    supervisor.CurrentLimitSpeed = 0;
                    supervisor.CurrentServiceBrakeSpeed = 0;
                    supervisor.CurrentEmergencySpeed = 0;
                    supervisor.SupervisionPowerPosition = 0;
                    supervisor.SupervisionBrakePosition = AtsMain.vehicleSpec.BrakeNotches + 1;
                    return BrakeAction.Emergency;
                }
                else
                {
                    supervisor.CurrentLimitSpeed = 0;
                    supervisor.CurrentServiceBrakeSpeed = 0;
                    supervisor.CurrentEmergencySpeed = 0;
                    supervisor.SupervisionPowerPosition = AtsMain.vehicleSpec.PowerNotches;
                    supervisor.SupervisionBrakePosition = 0;
                    return BrakeAction.None;
                }
            }

            if (!signalStabilized)
            {
                frameCount++;
                if (frameCount >= 10)
                {
                    signalStabilized = true;
                    entryLocation = (float)state.Location;
                    maxForwardLocation = entryLocation;
                    stopSignalLocation = entryLocation;
                    hasStopSignalLocation = IsStopSignal(CabSignal.currentSignal);
                    currentSignal = CabSignal.currentSignal;
                    limitCurveDirty = true;
                    Tool.DebugWriteLine(string.Format(
                        "降级模式: 信号稳定, Location={0}, Signal={1}",
                        entryLocation, currentSignal.ToString()));
                }
                else
                {
                    return BrakeAction.None;
                }
            }

            if (limitCurveDirty)
            {
                RebuildLimitCurve(state);
                limitCurveDirty = false;
            }

            float currentLocation = (float)state.Location;
            if (currentLocation > maxForwardLocation)
            {
                maxForwardLocation = currentLocation;
            }

            float reverseDistance = maxForwardLocation - currentLocation;
            if (reverseDistance > 20f)
            {
                Tool.DebugWriteLine(string.Format(
                    "降级模式: 退行{0:F1}m超过20m, 转入限速模式",
                    reverseDistance));
                AtsMain.modeController.SwitchMode(OperationMode.Restricted);
                return BrakeAction.None;
            }

            BrakeAction speedAction = supervisor.Evaluate(currentLocation, state.Speed);
            BrakeAction slipAction = antiSlip.Evaluate(state, deltaTime);

            if ((slipAction & BrakeAction.Emergency) != 0)
            {
                supervisor.SupervisionPowerPosition = 0;
                supervisor.SupervisionBrakePosition = AtsMain.vehicleSpec.BrakeNotches + 1;
            }

            return speedAction | slipAction;
        }

        public void OnSignalChange(CabSignal.CabSignalCode signal)
        {
            if (!signalStabilized) return;

            if (signal == currentSignal) return;

            bool wasStop = IsStopSignal(currentSignal);
            currentSignal = signal;
            bool isStop = IsStopSignal(currentSignal);

            if (signal == CabSignal.CabSignalCode.H)
            {
                Tool.DebugWriteLine("降级模式: 收到H码, 立即紧急制动");
                return;
            }

            if (!wasStop && isStop)
            {
                stopSignalLocation = (float)AtsMain.vehicleState.Location;
                hasStopSignalLocation = true;
                Tool.DebugWriteLine(string.Format(
                    "降级模式: 信号变为停车, Location={0}, 800m后限速20km/h",
                    stopSignalLocation));
            }

            if (wasStop && !isStop)
            {
                entryLocation = (float)AtsMain.vehicleState.Location;
                hasStopSignalLocation = false;
                Tool.DebugWriteLine(string.Format(
                    "降级模式: 信号升级为进行, Location={0}, 2000m后限速60km/h",
                    entryLocation));
            }

            limitCurveDirty = true;
            Tool.DebugWriteLine(string.Format(
                "降级模式: 信号变化→{0}",
                currentSignal.ToString()));
        }

        public void OnKeyDown(int keyIndex)
        {
            if (keyIndex == (int)AtsMain.AtsKey.S)
            {
                antiSlip.OnVigilanceKey();
            }
        }

        private void RebuildLimitCurve(AtsMain.AtsVehicleState state)
        {
            float currentLocation = (float)state.Location;
            float farLocation = currentLocation + 50000;

            SpeedCurve limitCurve;

            if (IsStopSignal(currentSignal))
            {
                if (!hasStopSignalLocation)
                {
                    stopSignalLocation = currentLocation;
                    hasStopSignalLocation = true;
                }

                float targetLoc = stopSignalLocation + 800;
                if (currentLocation < targetLoc)
                {
                    limitCurve = SpeedCurve.StepDown(currentLocation, Config.MaxSpeed, targetLoc, 20);
                    limitCurve.Points.Add(new SpeedPoint(farLocation, 20));
                    Tool.DebugWriteLine(string.Format(
                        "降级模式: 停车信号, 距20km/h限速点{0:F0}m",
                        targetLoc - currentLocation));
                }
                else
                {
                    limitCurve = SpeedCurve.Constant(currentLocation, farLocation, 20);
                    Tool.DebugWriteLine("降级模式: 停车信号, 已过800m, 限速20km/h");
                }
            }
            else
            {
                float distSinceEntry = currentLocation - entryLocation;
                if (distSinceEntry < 2000)
                {
                    float remainDist = 2000 - distSinceEntry;
                    float targetLoc = currentLocation + remainDist;
                    limitCurve = SpeedCurve.StepDown(currentLocation, Config.MaxSpeed, targetLoc, 60);
                    limitCurve.Points.Add(new SpeedPoint(farLocation, 60));
                    Tool.DebugWriteLine(string.Format(
                        "降级模式: 进行信号, 走行{0:F0}m, 距60km/h限速点{1:F0}m",
                        distSinceEntry, remainDist));
                }
                else
                {
                    limitCurve = SpeedCurve.Constant(currentLocation, farLocation, 60);
                    Tool.DebugWriteLine("降级模式: 进行信号走行≥2000m, 限速60km/h");
                }
            }

            supervisor.LimitCurve = limitCurve;
            supervisor.SetStationStop(hasStopSignalLocation);
        }

        private static bool IsStopSignal(CabSignal.CabSignalCode signal)
        {
            return signal == CabSignal.CabSignalCode.HU
                || signal == CabSignal.CabSignalCode.H
                || signal == CabSignal.CabSignalCode.B;
        }

        public float CurrentLimitSpeed => supervisor.CurrentLimitSpeed;
        public float CurrentServiceBrakeSpeed => supervisor.CurrentServiceBrakeSpeed;
        public float CurrentEmergencySpeed => supervisor.CurrentEmergencySpeed;
        public int SupervisionPowerPosition => supervisor.SupervisionPowerPosition;
        public int SupervisionBrakePosition => supervisor.SupervisionBrakePosition;
        public float TargetDistance
        {
            get
            {
                if (!HasValidTarget) return 0;
                if (supervisor.LimitCurve == null || supervisor.LimitCurve.Points.Count == 0)
                    return 0;
                float currentLocation = (float)AtsMain.vehicleState.Location;
                for (int i = 0; i < supervisor.LimitCurve.Points.Count; i++)
                {
                    if (supervisor.LimitCurve.Points[i].Location > currentLocation
                        && supervisor.LimitCurve.Points[i].Speed < Config.MaxSpeed)
                    {
                        return supervisor.LimitCurve.Points[i].Location - currentLocation;
                    }
                }
                return 0;
            }
        }

        public bool HasValidTarget
        {
            get
            {
                if (currentSignal == CabSignal.CabSignalCode.H) return false;
                if (IsStopSignal(currentSignal) && hasStopSignalLocation
                    && (float)AtsMain.vehicleState.Location >= stopSignalLocation + 800)
                    return false;
                if (!IsStopSignal(currentSignal)
                    && (float)AtsMain.vehicleState.Location - entryLocation >= 2000)
                    return false;
                return true;
            }
        }

        public bool IsReversing => (float)AtsMain.vehicleState.Location < maxForwardLocation;
        public AntiSlipType ActiveAntiSlipType => antiSlip.ActiveType;
        public float AntiSlipCountdown => antiSlip.AlarmCountdown;
        public SpeedCurve ServiceBrakeCurve => supervisor.ServiceBrakeCurve;
        public SpeedCurve EmergencyBrakeCurve => supervisor.EmergencyBrakeCurve;
    }
}
