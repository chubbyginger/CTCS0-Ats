using System;

namespace CTCS0_Ats
{
    internal class ShuntingMode : IOperationMode
    {
        private SpeedSupervisor supervisor;
        private AntiSlipMonitor antiSlip;
        private const float SHUNTING_LIMIT = 40f;

        internal ShuntingMode()
        {
            supervisor = new SpeedSupervisor();
            supervisor.Thresholds = SupervisionThresholds.Shunting();
            antiSlip = new AntiSlipMonitor();
        }

        public void Enter()
        {
            BuildLimitCurve();
            antiSlip.Reset();
            Tool.DebugWriteLine("调车模式: 进入, 限速" + SHUNTING_LIMIT + "km/h");
        }

        public void Exit()
        {
            Tool.DebugWriteLine("调车模式: 退出");
        }

        public BrakeAction Update(AtsMain.AtsVehicleState state, float deltaTime)
        {
            float currentLocation = (float)state.Location;

            BrakeAction speedAction = supervisor.Evaluate(currentLocation, Math.Abs(state.Speed));
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
            supervisor.LimitCurve = SpeedCurve.Constant(currentLocation, farLocation, SHUNTING_LIMIT);
            supervisor.SetStationStop(false);
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
        public SpeedCurve EmergencyBrakeCurve => supervisor.EmergencyBrakeCurve;
        public SpeedCurve LimitCurve => supervisor.LimitCurve;
        public float ServiceBrakeOffset => supervisor.Thresholds.ServiceBrakeOffset;
    }
}
