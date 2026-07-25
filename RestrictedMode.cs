using System;

namespace CTCS0_Ats
{
    internal class RestrictedMode : IOperationMode
    {
        private SpeedSupervisor supervisor;
        private AntiSlipMonitor antiSlip;
        private bool limitActive;
        private float stoppedTime;
        private const float STOPPED_DELAY = 120f;

        internal RestrictedMode()
        {
            supervisor = new SpeedSupervisor();
            supervisor.Thresholds = SupervisionThresholds.Limit20();
            antiSlip = new AntiSlipMonitor();
        }

        public void Enter()
        {
            limitActive = false;
            stoppedTime = 0;
            antiSlip.Reset();
            Tool.DebugWriteLine("限速模式: 进入, 限速0km/h");
        }

        public void Exit()
        {
            Tool.DebugWriteLine("限速模式: 退出");
        }

        public BrakeAction Update(AtsMain.AtsVehicleState state, float deltaTime)
        {
            if (!limitActive)
            {
                if (Math.Abs(state.Speed) < 0.5f)
                {
                    stoppedTime += deltaTime;
                    if (stoppedTime >= STOPPED_DELAY)
                    {
                        limitActive = true;
                        BuildLimitCurve();
                        Tool.DebugWriteLine("限速模式: 停车2分钟, 限速20km/h");
                    }
                }
                else
                {
                    stoppedTime = 0;
                }

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
                    BrakeAction slipAction = antiSlip.Evaluate(state, deltaTime);
                    if ((slipAction & BrakeAction.Emergency) != 0)
                    {
                        supervisor.SupervisionPowerPosition = 0;
                        supervisor.SupervisionBrakePosition = AtsMain.vehicleSpec.BrakeNotches + 1;
                    }
                    return slipAction;
                }
            }

            BrakeAction speedAction = supervisor.Evaluate((float)state.Location, Math.Abs(state.Speed));
            BrakeAction slipAction2 = antiSlip.Evaluate(state, deltaTime);
            if ((slipAction2 & BrakeAction.Emergency) != 0)
            {
                supervisor.SupervisionPowerPosition = 0;
                supervisor.SupervisionBrakePosition = AtsMain.vehicleSpec.BrakeNotches + 1;
            }
            return speedAction | slipAction2;
        }

        public void OnSignalChange(CabSignal.CabSignalCode signal)
        {
        }

        public void OnKeyDown(int keyIndex)
        {
            if (keyIndex == (int)AtsMain.AtsKey.S)
            {
                antiSlip.OnVigilanceKey();
            }
        }

        private void BuildLimitCurve()
        {
            float currentLocation = (float)AtsMain.vehicleState.Location;
            float farLocation = currentLocation + 50000;
            supervisor.LimitCurve = SpeedCurve.Constant(currentLocation, farLocation, 20);
            supervisor.BuildBrakeCurves(Config.BrakeDeceleration, Config.EmptyRunTime);
        }

        public float CurrentLimitSpeed => supervisor.CurrentLimitSpeed;
        public float CurrentServiceBrakeSpeed => supervisor.CurrentServiceBrakeSpeed;
        public float CurrentEmergencySpeed => supervisor.CurrentEmergencySpeed;
        public int SupervisionPowerPosition => supervisor.SupervisionPowerPosition;
        public int SupervisionBrakePosition => supervisor.SupervisionBrakePosition;
        public float TargetDistance => 0;
        public bool HasValidTarget => false;
        public bool IsReversing => false;
        public AntiSlipType ActiveAntiSlipType => antiSlip.ActiveType;
        public float AntiSlipCountdown => antiSlip.AlarmCountdown;
        public SpeedCurve ServiceBrakeCurve => supervisor.ServiceBrakeCurve;
    }
}
