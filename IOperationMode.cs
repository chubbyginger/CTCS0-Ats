namespace CTCS0_Ats
{
    internal enum OperationMode
    {
        Degraded,
        Normal,
        Shunting,
        Restricted
    }

    internal interface IOperationMode
    {
        void Enter();
        void Exit();
        BrakeAction Update(AtsMain.AtsVehicleState state, float deltaTime);
        void OnSignalChange(CabSignal.CabSignalCode signal);
        void OnKeyDown(int keyIndex);

        float CurrentLimitSpeed { get; }
        float CurrentServiceBrakeSpeed { get; }
        float CurrentEmergencySpeed { get; }
        int SupervisionPowerPosition { get; }
        int SupervisionBrakePosition { get; }
        float TargetDistance { get; }
        bool HasValidTarget { get; }
        bool IsReversing { get; }
        AntiSlipType ActiveAntiSlipType { get; }
        float AntiSlipCountdown { get; }
        SpeedCurve ServiceBrakeCurve { get; }
        SpeedCurve EmergencyBrakeCurve { get; }
    }
}
