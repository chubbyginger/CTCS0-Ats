using System;
using System.Collections.Generic;

namespace CTCS0_Ats
{
    internal struct SpeedPoint
    {
        internal float Location;
        internal float Speed;

        internal SpeedPoint(float location, float speed)
        {
            Location = location;
            Speed = speed;
        }
    }

    internal class SpeedCurve
    {
        internal List<SpeedPoint> Points;

        internal SpeedCurve()
        {
            Points = new List<SpeedPoint>();
        }

        internal SpeedCurve(List<SpeedPoint> points)
        {
            Points = points;
        }

        internal static SpeedCurve Constant(float fromLocation, float toLocation, float speed)
        {
            var curve = new SpeedCurve();
            curve.Points.Add(new SpeedPoint(fromLocation, speed));
            curve.Points.Add(new SpeedPoint(toLocation, speed));
            return curve;
        }

        internal static SpeedCurve StepDown(float fromLocation, float fromSpeed, float toLocation, float toSpeed)
        {
            var curve = new SpeedCurve();
            curve.Points.Add(new SpeedPoint(fromLocation, fromSpeed));
            curve.Points.Add(new SpeedPoint(toLocation, toSpeed));
            return curve;
        }

        internal float GetSpeedAt(float location)
        {
            if (Points.Count == 0) return 0;
            if (Points.Count == 1) return Points[0].Speed;
            if (location <= Points[0].Location) return Points[0].Speed;
            if (location >= Points[Points.Count - 1].Location) return Points[Points.Count - 1].Speed;

            for (int i = 0; i < Points.Count - 1; i++)
            {
                if (location >= Points[i].Location && location <= Points[i + 1].Location)
                {
                    float ratio = (location - Points[i].Location) / (Points[i + 1].Location - Points[i].Location);
                    return Points[i].Speed + ratio * (Points[i + 1].Speed - Points[i].Speed);
                }
            }
            return Points[Points.Count - 1].Speed;
        }

        internal static SpeedCurve Min(SpeedCurve a, SpeedCurve b)
        {
            if (a.Points.Count == 0) return b;
            if (b.Points.Count == 0) return a;

            var result = new SpeedCurve();
            var allLocations = new SortedSet<float>();

            foreach (var p in a.Points) allLocations.Add(p.Location);
            foreach (var p in b.Points) allLocations.Add(p.Location);

            foreach (float loc in allLocations)
            {
                float speedA = a.GetSpeedAt(loc);
                float speedB = b.GetSpeedAt(loc);
                result.Points.Add(new SpeedPoint(loc, Math.Min(speedA, speedB)));
            }

            result.InsertIntersections(a, b);
            return result;
        }

        private void InsertIntersections(SpeedCurve a, SpeedCurve b)
        {
            if (Points.Count < 2) return;

            var insertions = new List<SpeedPoint>();

            for (int i = 0; i < Points.Count - 1; i++)
            {
                float loc0 = Points[i].Location;
                float loc1 = Points[i + 1].Location;

                float a0 = a.GetSpeedAt(loc0);
                float a1 = a.GetSpeedAt(loc1);
                float b0 = b.GetSpeedAt(loc0);
                float b1 = b.GetSpeedAt(loc1);

                float diff0 = a0 - b0;
                float diff1 = a1 - b1;

                if ((diff0 > 0 && diff1 < 0) || (diff0 < 0 && diff1 > 0))
                {
                    float da0 = a1 - a0;
                    float db0 = b1 - b0;
                    float denom = da0 - db0;
                    if (Math.Abs(denom) > 0.0001f)
                    {
                        float t = (b0 - a0) / denom;
                        if (t > 0.001f && t < 0.999f)
                        {
                            float interLoc = loc0 + t * (loc1 - loc0);
                            float interSpeed = a0 + t * da0;
                            insertions.Add(new SpeedPoint(interLoc, interSpeed));
                        }
                    }
                }
            }

            foreach (var p in insertions)
            {
                for (int i = 0; i < Points.Count - 1; i++)
                {
                    if (p.Location >= Points[i].Location && p.Location <= Points[i + 1].Location)
                    {
                        Points.Insert(i + 1, p);
                        break;
                    }
                }
            }
        }

        internal SpeedCurve GenerateBrakeCurve(float decelerationKmhPerS, float emptyRunTimeS,
            bool includeEmptyRun, float safetyDistance)
        {
            if (Points.Count == 0) return new SpeedCurve();

            float decelMs2 = decelerationKmhPerS / 3.6f;
            float tk = includeEmptyRun ? emptyRunTimeS : 0;

            var result = new SpeedCurve();

            for (int i = Points.Count - 1; i >= 1; i--)
            {
                float targetLoc = Points[i].Location;
                float targetSpeedKmh = Points[i].Speed;
                float targetSpeedMs = targetSpeedKmh / 3.6f;

                if (Points[i - 1].Speed <= targetSpeedKmh + 0.5f) continue;

                float startLoc = Points[i - 1].Location;
                float step = 20f;
                float d = 0;

                while (targetLoc - d - safetyDistance >= startLoc)
                {
                    float loc = targetLoc - d - safetyDistance;
                    float maxSpeedMs;

                    if (tk > 0)
                    {
                        float discriminant = decelMs2 * decelMs2 * tk * tk
                            + targetSpeedMs * targetSpeedMs
                            + 2 * decelMs2 * d;
                        if (discriminant < 0) discriminant = 0;
                        maxSpeedMs = -decelMs2 * tk + (float)Math.Sqrt(discriminant);
                    }
                    else
                    {
                        maxSpeedMs = (float)Math.Sqrt(targetSpeedMs * targetSpeedMs + 2 * decelMs2 * d);
                    }

                    float maxSpeedKmh = maxSpeedMs * 3.6f;
                    if (maxSpeedKmh > Config.MaxSpeed) maxSpeedKmh = Config.MaxSpeed;

                    result.Points.Add(new SpeedPoint(loc, maxSpeedKmh));
                    d += step;
                }

                float startSpeedKmh;
                float startSpeedMs;
                float totalD = targetLoc - startLoc - safetyDistance;
                if (tk > 0)
                {
                    float discriminant = decelMs2 * decelMs2 * tk * tk
                        + targetSpeedMs * targetSpeedMs
                        + 2 * decelMs2 * totalD;
                    if (discriminant < 0) discriminant = 0;
                    startSpeedMs = -decelMs2 * tk + (float)Math.Sqrt(discriminant);
                }
                else
                {
                    startSpeedMs = (float)Math.Sqrt(targetSpeedMs * targetSpeedMs + 2 * decelMs2 * totalD);
                }
                startSpeedKmh = startSpeedMs * 3.6f;
                if (startSpeedKmh > Config.MaxSpeed) startSpeedKmh = Config.MaxSpeed;
                result.Points.Add(new SpeedPoint(startLoc, startSpeedKmh));
            }

            result.Points.Sort((a, b) => a.Location.CompareTo(b.Location));

            var deduped = new SpeedCurve();
            for (int i = 0; i < result.Points.Count; i++)
            {
                if (i == 0 || Math.Abs(result.Points[i].Location - result.Points[i - 1].Location) > 0.1f)
                {
                    deduped.Points.Add(result.Points[i]);
                }
            }

            return deduped;
        }

        internal SpeedCurve AddOffset(float offset)
        {
            var result = new SpeedCurve();
            foreach (var p in Points)
            {
                result.Points.Add(new SpeedPoint(p.Location, p.Speed + offset));
            }
            return result;
        }

        internal SpeedCurve GetRelativeSlice(float currentLocation, float displayDistance)
        {
            var result = new SpeedCurve();
            float endLocation = currentLocation + displayDistance;

            foreach (var p in Points)
            {
                if (p.Location >= currentLocation && p.Location <= endLocation)
                {
                    result.Points.Add(new SpeedPoint(p.Location - currentLocation, p.Speed));
                }
            }

            if (result.Points.Count == 0)
            {
                float speed = GetSpeedAt(currentLocation);
                result.Points.Add(new SpeedPoint(0, speed));
                result.Points.Add(new SpeedPoint(displayDistance, speed));
            }
            else
            {
                if (result.Points[0].Location > 0.1f)
                {
                    float speed = GetSpeedAt(currentLocation);
                    result.Points.Insert(0, new SpeedPoint(0, speed));
                }
                if (result.Points[result.Points.Count - 1].Location < displayDistance - 0.1f)
                {
                    float speed = GetSpeedAt(endLocation);
                    result.Points.Add(new SpeedPoint(displayDistance, speed));
                }
            }

            return result;
        }
    }
}
