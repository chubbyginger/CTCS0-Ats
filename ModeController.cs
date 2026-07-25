using System;

namespace CTCS0_Ats
{
    internal class ModeController
    {
        internal OperationMode CurrentModeType;
        internal IOperationMode CurrentMode;
        internal BrakeAction LastBrakeAction;
        internal float CurrentLimitSpeed;
        internal float CurrentServiceBrakeSpeed;
        internal float CurrentEmergencySpeed;
        internal int SupervisionPowerPosition;
        internal int SupervisionBrakePosition;
        internal float TargetDistance;
        internal bool HasValidTarget;
        internal bool IsReversing;
        internal AntiSlipType ActiveAntiSlipType;
        internal float AntiSlipCountdown;
        internal SpeedCurve ServiceBrakeCurve;

        private int prevTime;

        internal ModeController()
        {
            CurrentModeType = OperationMode.Degraded;
            CurrentMode = new DegradedMode();
            LastBrakeAction = BrakeAction.None;
            prevTime = 0;
        }

        internal void Initialize()
        {
            CurrentMode.Enter();
            Tool.DebugWriteLine("模式控制器: 初始化, 默认模式=降级");
        }

        internal void SwitchMode(OperationMode newMode)
        {
            if (newMode == CurrentModeType) return;

            CurrentMode.Exit();

            CurrentModeType = newMode;
            switch (newMode)
            {
                case OperationMode.Degraded:
                    CurrentMode = new DegradedMode();
                    break;
                case OperationMode.Restricted:
                    CurrentMode = new RestrictedMode();
                    break;
                case OperationMode.Normal:
                case OperationMode.Shunting:
                    CurrentMode = new DegradedMode();
                    Tool.DebugWriteLine("模式控制器: 模式" + newMode.ToString() + "尚未实现, 回退降级");
                    break;
            }

            CurrentMode.Enter();
            Tool.DebugWriteLine("模式控制器: 切换至" + newMode.ToString());
        }

        internal BrakeAction Update(AtsMain.AtsVehicleState state)
        {
            float deltaTime = 0;
            if (prevTime > 0)
            {
                deltaTime = (state.Time - prevTime) / 1000f;
                if (deltaTime < 0) deltaTime = 0;
                if (deltaTime > 1) deltaTime = 1;
            }
            prevTime = state.Time;

            LastBrakeAction = CurrentMode.Update(state, deltaTime);

            CurrentLimitSpeed = CurrentMode.CurrentLimitSpeed;
            CurrentServiceBrakeSpeed = CurrentMode.CurrentServiceBrakeSpeed;
            CurrentEmergencySpeed = CurrentMode.CurrentEmergencySpeed;
            SupervisionPowerPosition = CurrentMode.SupervisionPowerPosition;
            SupervisionBrakePosition = CurrentMode.SupervisionBrakePosition;
            TargetDistance = CurrentMode.TargetDistance;
            HasValidTarget = CurrentMode.HasValidTarget;
            IsReversing = CurrentMode.IsReversing;
            ActiveAntiSlipType = CurrentMode.ActiveAntiSlipType;
            AntiSlipCountdown = CurrentMode.AntiSlipCountdown;
            ServiceBrakeCurve = CurrentMode.ServiceBrakeCurve;

            return LastBrakeAction;
        }

        internal void OnSignalChange(CabSignal.CabSignalCode signal)
        {
            CurrentMode.OnSignalChange(signal);
        }

        internal void OnKeyDown(int keyIndex)
        {
            if (keyIndex == (int)AtsMain.AtsKey.C1)
            {
                if (AtsMain.vehicleState.Speed < 0.5f)
                {
                    SwitchMode(OperationMode.Degraded);
                }
                return;
            }
            if (keyIndex == (int)AtsMain.AtsKey.C2)
            {
                if (AtsMain.vehicleState.Speed < 0.5f)
                {
                    SwitchMode(OperationMode.Restricted);
                }
                return;
            }
            CurrentMode.OnKeyDown(keyIndex);
        }
    }
}
