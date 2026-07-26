using System;
using System.Collections.Generic;

namespace CTCS0_Ats
{
    internal struct BrakeCurveTrailPoint
    {
        internal float Location;
        internal float ServiceBrakeSpeed;
        internal float EmergencyBrakeSpeed;

        internal BrakeCurveTrailPoint(float location, float svcSpeed, float emgSpeed)
        {
            Location = location;
            ServiceBrakeSpeed = svcSpeed;
            EmergencyBrakeSpeed = emgSpeed;
        }
    }

    internal class BrakeCurveTrailBuffer
    {
        private List<BrakeCurveTrailPoint> trail;
        private int maxPoints;
        private float recordInterval;
        private float lastRecordLocation;
        private const float NO_LOCATION = -999999f;
        private bool forceNext;

        internal BrakeCurveTrailBuffer()
        {
            trail = new List<BrakeCurveTrailPoint>();
            maxPoints = 500;
            recordInterval = 5f;
            lastRecordLocation = NO_LOCATION;
            forceNext = false;
        }

        internal void Record(float location, float svcSpeed, float emgSpeed, bool isReversing)
        {
            if (isReversing) return;

            if (lastRecordLocation == NO_LOCATION)
            {
                trail.Add(new BrakeCurveTrailPoint(location, svcSpeed, emgSpeed));
                lastRecordLocation = location;
                forceNext = false;
                return;
            }

            if (!forceNext && location - lastRecordLocation < recordInterval) return;

            trail.Add(new BrakeCurveTrailPoint(location, svcSpeed, emgSpeed));
            lastRecordLocation = location;
            forceNext = false;

            while (trail.Count > maxPoints)
            {
                trail.RemoveAt(0);
            }
        }

        internal void ForceRecord(float location, float svcSpeed, float emgSpeed)
        {
            trail.Add(new BrakeCurveTrailPoint(location, svcSpeed, emgSpeed));
            lastRecordLocation = location;
            forceNext = false;

            while (trail.Count > maxPoints)
            {
                trail.RemoveAt(0);
            }
        }

        internal void ForceNext()
        {
            forceNext = true;
        }

        internal List<BrakeCurveTrailPoint> GetTrailInRange(float fromLocation, float toLocation)
        {
            var result = new List<BrakeCurveTrailPoint>();
            foreach (var p in trail)
            {
                if (p.Location >= fromLocation && p.Location <= toLocation)
                {
                    result.Add(p);
                }
            }
            return result;
        }

        internal void Reset()
        {
            trail.Clear();
            lastRecordLocation = NO_LOCATION;
            forceNext = false;
        }
    }
}
