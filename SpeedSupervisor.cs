using System;

namespace CTCS0_Ats
{
    internal struct SupervisionThresholds
    {
        internal float WarningOffset;
        internal float PowerCutOffset;
        internal float ServiceBrakeOffset;
        internal float EmergencyOffset;

        internal static SupervisionThresholds Normal()
        {
            float emergencyOffset = IsEMU() ? 10 : 8;
            float serviceBrakeOffset = HasServiceBrake() ? 5 : emergencyOffset;
            return new SupervisionThresholds
            {
                WarningOffset = 2,
                PowerCutOffset = 3,
                ServiceBrakeOffset = serviceBrakeOffset,
                EmergencyOffset = emergencyOffset
            };
        }

        internal static SupervisionThresholds Strict()
        {
            return new SupervisionThresholds
            {
                WarningOffset = 0,
                PowerCutOffset = 1,
                ServiceBrakeOffset = 3,
                EmergencyOffset = 5
            };
        }

        internal static SupervisionThresholds Shunting()
        {
            float emergencyOffset = IsEMU() ? 6 : 4;
            float serviceBrakeOffset = HasServiceBrake() ? 1 : emergencyOffset;
            return new SupervisionThresholds
            {
                WarningOffset = 2,
                PowerCutOffset = 1,
                ServiceBrakeOffset = serviceBrakeOffset,
                EmergencyOffset = emergencyOffset
            };
        }

        internal static SupervisionThresholds Limit20()
        {
            float emergencyOffset = IsEMU() ? 6 : 4;
            float serviceBrakeOffset = HasServiceBrake() ? 1 : emergencyOffset;
            return new SupervisionThresholds
            {
                WarningOffset = 2,
                PowerCutOffset = 1,
                ServiceBrakeOffset = serviceBrakeOffset,
                EmergencyOffset = emergencyOffset
            };
        }

        private static bool IsEMU()
        {
            return Config.VehicleType == Config.VehicleTypeEnum.EMU;
        }

        private static bool HasServiceBrake()
        {
            return Config.BrakeType == Config.BrakeTypeEnum.Straight;
        }
    }

    [Flags]
    internal enum BrakeAction
    {
        None = 0,
        Warning = 1,
        PowerCut = 2,
        ServiceBrake = 4,
        Emergency = 8
    }

    internal class SpeedSupervisor
    {
        internal SpeedCurve LimitCurve;
        internal SpeedCurve ServiceBrakeCurve;
        internal SpeedCurve EmergencyBrakeCurve;
        internal SupervisionThresholds Thresholds;
        internal float CurrentLimitSpeed;
        internal float CurrentServiceBrakeSpeed;
        internal float CurrentEmergencySpeed;

        internal int SupervisionPowerPosition;
        internal int SupervisionBrakePosition;

        private bool isInDecelerationZone;
        private bool isServiceBrakeActive;
        private bool serviceBrakeLatched;
        private bool emergencyBrakeLatched;
        private float decelerationTargetSpeed;

        internal BrakeAction Evaluate(float currentLocation, float currentSpeed)
        {
            if (LimitCurve == null || LimitCurve.Points.Count == 0)
            {
                CurrentLimitSpeed = 0;
                CurrentServiceBrakeSpeed = 0;
                CurrentEmergencySpeed = 0;
                SupervisionPowerPosition = AtsMain.vehicleSpec.PowerNotches;
                SupervisionBrakePosition = 0;
                isServiceBrakeActive = false;
                serviceBrakeLatched = false;
                emergencyBrakeLatched = false;
                return BrakeAction.None;
            }

            CurrentLimitSpeed = LimitCurve.GetSpeedAt(currentLocation);

            isInDecelerationZone = IsDecelerationZone(currentLocation);

            if (isInDecelerationZone)
            {
                if (ServiceBrakeCurve != null && ServiceBrakeCurve.Points.Count > 0)
                {
                    CurrentServiceBrakeSpeed = ServiceBrakeCurve.GetSpeedAt(currentLocation);
                }
                else
                {
                    CurrentServiceBrakeSpeed = CurrentLimitSpeed + Thresholds.ServiceBrakeOffset;
                }

                if (EmergencyBrakeCurve != null && EmergencyBrakeCurve.Points.Count > 0)
                {
                    CurrentEmergencySpeed = EmergencyBrakeCurve.GetSpeedAt(currentLocation);
                }
                else
                {
                    CurrentEmergencySpeed = CurrentLimitSpeed + Thresholds.EmergencyOffset;
                }
            }
            else
            {
                CurrentServiceBrakeSpeed = CurrentLimitSpeed + Thresholds.ServiceBrakeOffset;
                CurrentEmergencySpeed = CurrentLimitSpeed + Thresholds.EmergencyOffset;
            }

            var action = BrakeAction.None;

            if (currentSpeed >= CurrentEmergencySpeed)
            {
                emergencyBrakeLatched = true;
            }

            if (emergencyBrakeLatched)
            {
                action |= BrakeAction.Emergency;
                if (currentSpeed < 0.5f)
                {
                    emergencyBrakeLatched = false;
                }
            }

            if (HasServiceBrake() && !emergencyBrakeLatched)
            {
                if (currentSpeed >= CurrentServiceBrakeSpeed)
                {
                    if (isInDecelerationZone)
                    {
                        serviceBrakeLatched = true;
                    }
                    action |= BrakeAction.ServiceBrake;
                }
                else if (serviceBrakeLatched)
                {
                    if (currentSpeed < decelerationTargetSpeed)
                    {
                        serviceBrakeLatched = false;
                    }
                    else
                    {
                        action |= BrakeAction.ServiceBrake;
                    }
                }
            }

            if (!isInDecelerationZone)
            {
                serviceBrakeLatched = false;
            }

            if (!emergencyBrakeLatched)
            {
                if (isInDecelerationZone)
                {
                    if (currentSpeed >= CurrentServiceBrakeSpeed - 1)
                    {
                        action |= BrakeAction.PowerCut;
                    }
                    if (currentSpeed >= CurrentServiceBrakeSpeed - 5)
                    {
                        action |= BrakeAction.Warning;
                    }
                }
                else
                {
                    if (currentSpeed >= CurrentServiceBrakeSpeed - 2)
                    {
                        action |= BrakeAction.PowerCut;
                    }
                    if (currentSpeed >= CurrentServiceBrakeSpeed - 3)
                    {
                        action |= BrakeAction.Warning;
                    }
                }
            }

            if ((action & (BrakeAction.ServiceBrake | BrakeAction.Emergency)) != 0)
            {
                action |= BrakeAction.PowerCut;
            }

            isServiceBrakeActive = (action & BrakeAction.ServiceBrake) != 0;

            SupervisionPowerPosition = AtsMain.vehicleSpec.PowerNotches;
            SupervisionBrakePosition = 0;

            if (emergencyBrakeLatched)
            {
                SupervisionBrakePosition = AtsMain.vehicleSpec.BrakeNotches + 1;
                SupervisionPowerPosition = 0;
            }
            else if (HasServiceBrake() && isServiceBrakeActive)
            {
                SupervisionBrakePosition = AtsMain.vehicleSpec.BrakeNotches;
                SupervisionPowerPosition = 0;
            }
            else if ((action & BrakeAction.PowerCut) != 0)
            {
                SupervisionPowerPosition = 0;
            }

            return action;
        }

        internal void BuildBrakeCurves(float deceleration, float emptyRunTime)
        {
            if (LimitCurve == null || LimitCurve.Points.Count == 0) return;

            bool isStopZone = IsStopZone();

            if (HasServiceBrake())
            {
                bool svcIncludeEmptyRun = true;
                float svcBrakeCoeff = 0.8f;
                float svcSafetyDist = isStopZone ? 100 : 0;

                ServiceBrakeCurve = LimitCurve.GenerateBrakeCurve(
                    deceleration, emptyRunTime, svcIncludeEmptyRun, svcBrakeCoeff, svcSafetyDist);
            }
            else
            {
                ServiceBrakeCurve = null;
            }

            bool emgIncludeEmptyRun = isStopZone;
            float emgBrakeCoeff = 1.0f;
            float emgSafetyDist = isStopZone ? 70 : 0;

            EmergencyBrakeCurve = LimitCurve.GenerateBrakeCurve(
                deceleration, emptyRunTime, emgIncludeEmptyRun, emgBrakeCoeff, emgSafetyDist);

            Tool.DebugWriteLine(string.Format(
                "制动曲线生成: {0}, 紧急制动曲线{1}点, 停车区={2}",
                HasServiceBrake() ? "常用制动曲线" + ServiceBrakeCurve.Points.Count + "点" : "无常用制动",
                EmergencyBrakeCurve.Points.Count,
                isStopZone));
        }

        private bool IsDecelerationZone(float currentLocation)
        {
            if (LimitCurve == null || LimitCurve.Points.Count < 2) return false;

            for (int i = 0; i < LimitCurve.Points.Count - 1; i++)
            {
                if (currentLocation >= LimitCurve.Points[i].Location
                    && currentLocation <= LimitCurve.Points[i + 1].Location)
                {
                    bool isDecel = LimitCurve.Points[i].Speed > LimitCurve.Points[i + 1].Speed + 0.5f;
                    if (isDecel)
                    {
                        decelerationTargetSpeed = LimitCurve.Points[i + 1].Speed;
                    }
                    return isDecel;
                }
            }
            return false;
        }

        private bool IsStopZone()
        {
            if (LimitCurve == null || LimitCurve.Points.Count == 0) return false;
            return LimitCurve.Points[LimitCurve.Points.Count - 1].Speed < 0.5f;
        }

        private static bool HasServiceBrake()
        {
            return Config.BrakeType == Config.BrakeTypeEnum.Straight;
        }
    }
}
