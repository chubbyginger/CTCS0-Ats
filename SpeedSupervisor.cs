using System;

namespace CTCS0_Ats
{
    internal struct SupervisionThresholds
    {
        internal float ServiceBrakeOffset;
        internal float EmergencyOffset;
        internal float ConstWarningOffset;
        internal float ConstPowerCutOffset;
        internal float DecelWarningOffset;
        internal float DecelPowerCutOffset;

        internal static SupervisionThresholds Normal()
        {
            float emergencyOffset = IsEMU() ? 10 : 8;
            float serviceBrakeOffset = HasServiceBrake() ? 5 : emergencyOffset;
            return new SupervisionThresholds
            {
                ServiceBrakeOffset = serviceBrakeOffset,
                EmergencyOffset = emergencyOffset,
                ConstWarningOffset = 3,
                ConstPowerCutOffset = 2,
                DecelWarningOffset = 5,
                DecelPowerCutOffset = 1
            };
        }

        internal static SupervisionThresholds Strict()
        {
            return new SupervisionThresholds
            {
                ServiceBrakeOffset = 3,
                EmergencyOffset = 5,
                ConstWarningOffset = 3,
                ConstPowerCutOffset = 2,
                DecelWarningOffset = 5,
                DecelPowerCutOffset = 1
            };
        }

        internal static SupervisionThresholds Shunting()
        {
            float emergencyOffset = IsEMU() ? 6 : 4;
            float serviceBrakeOffset = HasServiceBrake() ? 1 : emergencyOffset;
            return new SupervisionThresholds
            {
                ServiceBrakeOffset = serviceBrakeOffset,
                EmergencyOffset = emergencyOffset,
                ConstWarningOffset = 3,
                ConstPowerCutOffset = 2,
                DecelWarningOffset = 5,
                DecelPowerCutOffset = 1
            };
        }

        internal static SupervisionThresholds Limit20()
        {
            float emergencyOffset = IsEMU() ? 6 : 4;
            float serviceBrakeOffset = HasServiceBrake() ? 1 : emergencyOffset;
            return new SupervisionThresholds
            {
                ServiceBrakeOffset = serviceBrakeOffset,
                EmergencyOffset = emergencyOffset,
                ConstWarningOffset = 3,
                ConstPowerCutOffset = 2,
                DecelWarningOffset = 5,
                DecelPowerCutOffset = 1
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
        private bool isStationStop;

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

            BuildBrakeCurves(Math.Abs(currentSpeed));

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
                float refSpeed = HasServiceBrake()
                    ? CurrentServiceBrakeSpeed
                    : CurrentEmergencySpeed;
                float warningOffset = isInDecelerationZone
                    ? Thresholds.DecelWarningOffset
                    : Thresholds.ConstWarningOffset;
                float powerCutOffset = isInDecelerationZone
                    ? Thresholds.DecelPowerCutOffset
                    : Thresholds.ConstPowerCutOffset;

                if (currentSpeed >= refSpeed - powerCutOffset)
                {
                    action |= BrakeAction.PowerCut;
                }
                if (currentSpeed >= refSpeed - warningOffset)
                {
                    action |= BrakeAction.Warning;
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

        internal void BuildBrakeCurves(float currentSpeedKmh)
        {
            if (LimitCurve == null || LimitCurve.Points.Count == 0) return;

            bool isStopZone = IsStopZone();
            float rangeStart = LimitCurve.Points[0].Location;
            float rangeEnd = LimitCurve.Points[LimitCurve.Points.Count - 1].Location;

            if (HasServiceBrake())
            {
                float svcSafetyDist = 0;
                if (isStopZone)
                {
                    float aBase = isStationStop
                        ? Config.SafetyDistanceBaseServiceStation
                        : Config.SafetyDistanceBaseServiceSection;
                    svcSafetyDist = aBase + 0.5f * currentSpeedKmh;
                }

                var rawSvc = LimitCurve.GenerateBrakeCurve(
                    Config.ServiceBrakeDeceleration, Config.EmptyRunTime, true, svcSafetyDist);

                var svcBase = SpeedCurve.Constant(rangeStart, rangeEnd,
                    LimitCurve.Points[0].Speed + Thresholds.ServiceBrakeOffset);
                var svcWithOffset = rawSvc.AddOffset(Thresholds.ServiceBrakeOffset);
                ServiceBrakeCurve = SpeedCurve.Min(svcBase, svcWithOffset);
            }
            else
            {
                ServiceBrakeCurve = null;
            }

            bool emgIncludeEmptyRun;
            if (isStopZone)
            {
                emgIncludeEmptyRun = true;
            }
            else
            {
                emgIncludeEmptyRun = !HasServiceBrake();
            }

            float emgSafetyDist = 0;
            if (isStopZone)
            {
                float aBase = isStationStop
                    ? Config.SafetyDistanceBaseEmergencyStation
                    : Config.SafetyDistanceBaseEmergencySection;
                emgSafetyDist = aBase + 0.5f * currentSpeedKmh;
            }

            var rawEmg = LimitCurve.GenerateBrakeCurve(
                Config.EmergencyBrakeDeceleration, Config.EmptyRunTime, emgIncludeEmptyRun, emgSafetyDist);

            var emgBase = SpeedCurve.Constant(rangeStart, rangeEnd,
                LimitCurve.Points[0].Speed + Thresholds.EmergencyOffset);
            var emgWithOffset = rawEmg.AddOffset(Thresholds.EmergencyOffset);
            EmergencyBrakeCurve = SpeedCurve.Min(emgBase, emgWithOffset);
        }

        internal void SetStationStop(bool stationStop)
        {
            isStationStop = stationStop;
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
