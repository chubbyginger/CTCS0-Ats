using System;
using System.Collections.Generic;

namespace CTCS0_Ats
{
    internal class SpeedTrailBuffer
    {
        private List<SpeedPoint> trail;
        private int maxPoints;
        private float recordInterval;
        private float lastRecordLocation;
        private const float NO_LOCATION = -999999f;

        internal SpeedTrailBuffer()
        {
            trail = new List<SpeedPoint>();
            maxPoints = 500;
            recordInterval = 5f;
            lastRecordLocation = NO_LOCATION;
        }

        internal void Record(float location, float speed, bool isReversing)
        {
            if (isReversing) return;

            float absSpeed = Math.Abs(speed);

            if (lastRecordLocation == NO_LOCATION)
            {
                trail.Add(new SpeedPoint(location, absSpeed));
                lastRecordLocation = location;
                return;
            }

            if (location - lastRecordLocation < recordInterval) return;

            trail.Add(new SpeedPoint(location, absSpeed));
            lastRecordLocation = location;

            while (trail.Count > maxPoints)
            {
                trail.RemoveAt(0);
            }
        }

        internal List<SpeedPoint> GetTrailInRange(float fromLocation, float toLocation)
        {
            var result = new List<SpeedPoint>();
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
        }
    }
}
