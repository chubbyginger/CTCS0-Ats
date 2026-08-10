using System.Collections.Generic;
using Newtonsoft.Json;

#pragma warning disable CS0649

namespace CTCS0_Ats
{
    internal class SpeedByGauge
    {
        [JsonProperty("p4")]
        internal float P4;
        [JsonProperty("p3")]
        internal float P3;
        [JsonProperty("p2")]
        internal float P2;
        [JsonProperty("p1")]
        internal float P1;
        [JsonProperty("f4")]
        internal float F4;
        [JsonProperty("f3")]
        internal float F3;
        [JsonProperty("f2")]
        internal float F2;
        [JsonProperty("f1")]
        internal float F1;

        internal float GetValue()
        {
            float maxSpeed = Config.MaxSpeed;

            if (Config.PassengerFreight == Config.PassengerFreightEnum.Passenger)
            {
                if (maxSpeed > 140f) return P1;
                if (maxSpeed > 120f) return P2;
                if (maxSpeed > 100f) return P3;
                return P4;
            }
            else
            {
                if (maxSpeed >= 120f) return F1;
                if (maxSpeed >= 90f) return F2;
                if (maxSpeed > 80f) return F3;
                return F4;
            }
        }

        internal static float GetValueOrNull(SpeedByGauge gauge)
        {
            if (gauge == null) return 0f;
            return gauge.GetValue();
        }

        public override string ToString()
        {
            return string.Format("p4={0} p3={1} p2={2} p1={3} f4={4} f3={5} f2={6} f1={7}",
                P4, P3, P2, P1, F4, F3, F2, F1);
        }
    }

    internal class RouteMeta
    {
        [JsonProperty("name")]
        internal string Name;
        [JsonProperty("totalLength")]
        internal float TotalLength;
        [JsonProperty("kmAtOrigin")]
        internal float KmAtOrigin;
        [JsonProperty("defaultSpeedLimit")]
        internal SpeedByGauge DefaultSpeedLimit;
    }

    internal class GradientRecord
    {
        [JsonProperty("start")]
        internal float Start;
        [JsonProperty("end")]
        internal float End;
        [JsonProperty("value")]
        internal float Value;
    }

    internal class CurveRecord
    {
        [JsonProperty("start")]
        internal float Start;
        [JsonProperty("end")]
        internal float End;
        [JsonProperty("radius")]
        internal float Radius;
        [JsonProperty("direction")]
        internal string Direction;
    }

    internal class PermanentSpeedLimitRecord
    {
        [JsonProperty("start")]
        internal float Start;
        [JsonProperty("end")]
        internal float End;
        [JsonProperty("speed")]
        internal SpeedByGauge Speed;
        [JsonProperty("reason")]
        internal string Reason;
    }

    internal class BridgeRecord
    {
        [JsonProperty("location")]
        internal float Location;
        [JsonProperty("name")]
        internal string Name;
        [JsonProperty("length")]
        internal float Length;
    }

    internal class TunnelRecord
    {
        [JsonProperty("location")]
        internal float Location;
        [JsonProperty("name")]
        internal string Name;
        [JsonProperty("length")]
        internal float Length;
    }

    internal class LevelCrossingRecord
    {
        [JsonProperty("location")]
        internal float Location;
        [JsonProperty("name")]
        internal string Name;
    }

    internal class StructuresGroup
    {
        [JsonProperty("bridges")]
        internal List<BridgeRecord> Bridges;
        [JsonProperty("tunnels")]
        internal List<TunnelRecord> Tunnels;
        [JsonProperty("levelCrossings")]
        internal List<LevelCrossingRecord> LevelCrossings;
    }

    internal class CatenaryLimitRecord
    {
        [JsonProperty("start")]
        internal float Start;
        [JsonProperty("end")]
        internal float End;
        [JsonProperty("speed")]
        internal float Speed;
    }

    internal class CatenarySectionRecord
    {
        [JsonProperty("start")]
        internal float Start;
        [JsonProperty("end")]
        internal float End;
        [JsonProperty("center")]
        internal float Center;
        [JsonProperty("structure")]
        internal string Structure;
    }

    internal class CatenaryGroup
    {
        [JsonProperty("limits")]
        internal List<CatenaryLimitRecord> Limits;
        [JsonProperty("sections")]
        internal List<CatenarySectionRecord> CatenarySections;
    }

    internal class LineData
    {
        [JsonProperty("meta")]
        internal RouteMeta Meta;
        [JsonProperty("gradients")]
        internal List<GradientRecord> Gradients;
        [JsonProperty("curves")]
        internal List<CurveRecord> Curves;
        [JsonProperty("permanentSpeedLimits")]
        internal List<PermanentSpeedLimitRecord> PermanentSpeedLimits;
        [JsonProperty("structures")]
        internal StructuresGroup Structures;
        [JsonProperty("catenary")]
        internal CatenaryGroup Catenary;
    }

    internal class SignalRecord
    {
        [JsonProperty("location")]
        internal float Location;
        [JsonProperty("type")]
        internal string Type;
        [JsonProperty("number")]
        internal int Number;
        [JsonProperty("nextDist")]
        internal float NextDist;
        [JsonProperty("speedLimit")]
        internal SpeedByGauge SpeedLimit;
        [JsonProperty("yellowSpeed")]
        internal SpeedByGauge YellowSpeed;
        [JsonProperty("flags")]
        internal List<string> Flags;
        [JsonProperty("stationId")]
        internal int StationId;

        internal bool HasFlag(string flag)
        {
            return Flags != null && Flags.Contains(flag);
        }
    }

    internal class StationRecord
    {
        [JsonProperty("id")]
        internal int Id;
        [JsonProperty("name")]
        internal string Name;
        [JsonProperty("type")]
        internal string Type;
        [JsonProperty("line")]
        internal string Line;
        [JsonProperty("direction")]
        internal string Direction;
        [JsonProperty("entrySignalLocation")]
        internal float EntrySignalLocation;
        [JsonProperty("centerDist")]
        internal float CenterDist;
        [JsonProperty("exitDist")]
        internal float ExitDist;
        [JsonProperty("exitSpeedLimit")]
        internal SpeedByGauge ExitSpeedLimit;
        [JsonProperty("mainTrackNo")]
        internal int MainTrackNo;
        [JsonProperty("blockMethod")]
        internal string BlockMethod;
        [JsonProperty("startAlignmentDist")]
        internal float StartAlignmentDist;
        [JsonProperty("flags")]
        internal List<string> Flags;
        [JsonProperty("sidings")]
        internal List<SidingRecord> Sidings;
        [JsonProperty("turnouts")]
        internal List<TurnoutRecord> Turnouts;
        [JsonProperty("sidingSelectionTriggerDist")]
        internal float SidingSelectionTriggerDist;

        internal float EffectiveSidingTriggerDist
        {
            get { return SidingSelectionTriggerDist > 0 ? SidingSelectionTriggerDist : 1500f; }
        }

        internal bool HasFlag(string flag)
        {
            return Flags != null && Flags.Contains(flag);
        }
    }

    internal class SidingRecord
    {
        [JsonProperty("trackNo")]
        internal int TrackNo;
        [JsonProperty("entryDist")]
        internal float EntryDist;
        [JsonProperty("exitDist")]
        internal float ExitDist;
        [JsonProperty("correction")]
        internal float Correction;
        [JsonProperty("entrySpeedLimit")]
        internal SpeedByGauge EntrySpeedLimit;
        [JsonProperty("exitSpeedLimit")]
        internal SpeedByGauge ExitSpeedLimit;
        [JsonProperty("flags")]
        internal List<string> Flags;

        internal bool HasFlag(string flag)
        {
            return Flags != null && Flags.Contains(flag);
        }
    }

    internal class TurnoutRecord
    {
        [JsonProperty("location")]
        internal float Location;
        [JsonProperty("speedLimitStraight")]
        internal float SpeedLimitStraight;
        [JsonProperty("speedLimitDiverging")]
        internal float SpeedLimitDiverging;
        [JsonProperty("isEntry")]
        internal bool IsEntry;
    }

    internal class TransferOption
    {
        [JsonProperty("id")]
        internal int Id;
        [JsonProperty("name")]
        internal string Name;
        [JsonProperty("dataDir")]
        internal string DataDir;
    }

    internal class TransferRecord
    {
        [JsonProperty("location")]
        internal float Location;
        [JsonProperty("description")]
        internal string Description;
        [JsonProperty("options")]
        internal List<TransferOption> Options;
        [JsonProperty("autoTriggerBeacon")]
        internal int AutoTriggerBeacon;
    }

    internal class ControlData
    {
        [JsonProperty("signals")]
        internal List<SignalRecord> Signals;
        [JsonProperty("stations")]
        internal List<StationRecord> Stations;
        [JsonProperty("transfers")]
        internal List<TransferRecord> Transfers;
    }

    internal class TempSpeedRestriction
    {
        [JsonProperty("start")]
        internal float Start;
        [JsonProperty("end")]
        internal float End;
        [JsonProperty("speed")]
        internal SpeedByGauge Speed;
        [JsonProperty("reason")]
        internal string Reason;
    }

    internal class BlockChange
    {
        [JsonProperty("location")]
        internal float Location;
        [JsonProperty("blockMethod")]
        internal string BlockMethod;
    }

    internal class GreenLicense
    {
        [JsonProperty("signalNumber")]
        internal int SignalNumber;
        [JsonProperty("enabled")]
        internal bool Enabled;
    }

    internal class TemporaryData
    {
        [JsonProperty("speedRestrictions")]
        internal List<TempSpeedRestriction> SpeedRestrictions;
        [JsonProperty("blockChanges")]
        internal List<BlockChange> BlockChanges;
        [JsonProperty("greenLicenses")]
        internal List<GreenLicense> GreenLicenses;
    }
}

#pragma warning restore CS0649
