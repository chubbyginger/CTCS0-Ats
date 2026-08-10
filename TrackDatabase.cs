using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace CTCS0_Ats
{
    internal class TrackDatabase
    {
        internal RouteMeta Meta;
        internal List<GradientRecord> Gradients;
        internal List<CurveRecord> Curves;
        internal List<PermanentSpeedLimitRecord> PermSpeedLimits;
        internal List<BridgeRecord> Bridges;
        internal List<TunnelRecord> Tunnels;
        internal List<LevelCrossingRecord> LevelCrossings;
        internal List<CatenaryLimitRecord> CatenaryLimits;
        internal List<CatenarySectionRecord> CatenarySections;
        internal List<SignalRecord> Signals;
        internal List<StationRecord> Stations;
        internal List<TransferRecord> Transfers;
        internal List<TempSpeedRestriction> TempRestrictions;

        private int _nextSignalIdx;
        private int _nextStationIdx;
        private int _currentGradientIdx;
        private int _currentCurveIdx;
        private int _nextCatenarySectionIdx;
        private int _nextPermSpeedLimitIdx;
        private int _nextCatenaryLimitIdx;
        private int _nextTransferIdx;

        private TrackDatabase _activeBranchDb;
        private float _branchStartLocation;
        private float _branchEndLocation;
        private bool _branchActive;

        internal int SelectedSidingNo;
        internal int SelectedBranchId;

        internal bool IsLoaded;
        internal string TrackDataDir;

        internal void Load(string trackDataDir)
        {
            TrackDataDir = trackDataDir;
            try
            {
                string lineFile = Path.Combine(trackDataDir, "line.json");
                string controlFile = Path.Combine(trackDataDir, "control.json");

                if (!File.Exists(lineFile) || !File.Exists(controlFile))
                {
                    Tool.DebugWriteLine("TrackDatabase: line.json或control.json不存在, 路径=" + trackDataDir);
                    IsLoaded = false;
                    return;
                }

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Populate
                };

                string lineJson = File.ReadAllText(lineFile);
                string controlJson = File.ReadAllText(controlFile);

                var lineData = JsonConvert.DeserializeObject<LineData>(lineJson, settings);
                var controlData = JsonConvert.DeserializeObject<ControlData>(controlJson, settings);

                if (lineData?.Meta == null || controlData?.Signals == null)
                {
                    Tool.DebugWriteLine("TrackDatabase: JSON解析失败, 数据为null");
                    IsLoaded = false;
                    return;
                }

                Meta = lineData.Meta;
                Gradients = lineData.Gradients ?? new List<GradientRecord>();
                Curves = lineData.Curves ?? new List<CurveRecord>();
                PermSpeedLimits = lineData.PermanentSpeedLimits ?? new List<PermanentSpeedLimitRecord>();
                Bridges = lineData.Structures?.Bridges ?? new List<BridgeRecord>();
                Tunnels = lineData.Structures?.Tunnels ?? new List<TunnelRecord>();
                LevelCrossings = lineData.Structures?.LevelCrossings ?? new List<LevelCrossingRecord>();
                CatenaryLimits = lineData.Catenary?.Limits ?? new List<CatenaryLimitRecord>();
                CatenarySections = lineData.Catenary?.CatenarySections ?? new List<CatenarySectionRecord>();
                Signals = controlData.Signals ?? new List<SignalRecord>();
                Stations = controlData.Stations ?? new List<StationRecord>();
                Transfers = controlData.Transfers ?? new List<TransferRecord>();
                TempRestrictions = new List<TempSpeedRestriction>();

                SortAndIndex();

                IsLoaded = true;
                Tool.DebugWriteLine("TrackDatabase: 加载完成, 交路=" + Meta.Name
                    + ", 信号机=" + Signals.Count
                    + ", 车站=" + Stations.Count
                    + ", 坡段=" + Gradients.Count
                    + ", 曲线=" + Curves.Count);
            }
            catch (Exception ex)
            {
                Tool.DebugWriteLine("TrackDatabase: 加载异常 " + ex.Message);
                IsLoaded = false;
            }
        }

        internal void LoadTemporary(string tempFile)
        {
            if (!File.Exists(tempFile)) return;
            try
            {
                string json = File.ReadAllText(tempFile);
                var data = JsonConvert.DeserializeObject<TemporaryData>(json);
                if (data?.SpeedRestrictions != null)
                {
                    TempRestrictions = data.SpeedRestrictions;
                    TempRestrictions.Sort((a, b) => a.Start.CompareTo(b.Start));
                }
                Tool.DebugWriteLine("TrackDatabase: 运行揭示加载完成, 限速=" + TempRestrictions.Count);
            }
            catch (Exception ex)
            {
                Tool.DebugWriteLine("TrackDatabase: 运行揭示加载异常 " + ex.Message);
            }
        }

        private void SortAndIndex()
        {
            Gradients.Sort((a, b) => a.Start.CompareTo(b.Start));
            Curves.Sort((a, b) => a.Start.CompareTo(b.Start));
            PermSpeedLimits.Sort((a, b) => a.Start.CompareTo(b.Start));
            Signals.Sort((a, b) => a.Location.CompareTo(b.Location));
            Stations.Sort((a, b) => a.EntrySignalLocation.CompareTo(b.EntrySignalLocation));
            CatenaryLimits.Sort((a, b) => a.Start.CompareTo(b.Start));
            CatenarySections.Sort((a, b) => a.Start.CompareTo(b.Start));
            Transfers.Sort((a, b) => a.Location.CompareTo(b.Location));
            Bridges.Sort((a, b) => a.Location.CompareTo(b.Location));
            Tunnels.Sort((a, b) => a.Location.CompareTo(b.Location));
            LevelCrossings.Sort((a, b) => a.Location.CompareTo(b.Location));
        }

        internal void InitializeAtDeparture(float startLocation)
        {
            _nextSignalIdx = BinarySearchNext(Signals, startLocation, s => s.Location);
            _nextStationIdx = BinarySearchNext(Stations, startLocation, s => s.EntrySignalLocation);
            _currentGradientIdx = FindContainingIndex(Gradients, startLocation, g => g.Start, g => g.End);
            _currentCurveIdx = FindContainingIndex(Curves, startLocation, c => c.Start, c => c.End);
            _nextCatenarySectionIdx = BinarySearchNext(CatenarySections, startLocation, p => p.Start);
            _nextPermSpeedLimitIdx = BinarySearchNext(PermSpeedLimits, startLocation, p => p.Start);
            _nextCatenaryLimitIdx = BinarySearchNext(CatenaryLimits, startLocation, c => c.Start);
            _nextTransferIdx = BinarySearchNext(Transfers, startLocation, t => t.Location);

            SelectedSidingNo = 0;
            SelectedBranchId = 0;
            _branchActive = false;
            _activeBranchDb = null;

            Tool.DebugWriteLine("TrackDatabase: 开车对标完成, Location=" + startLocation
                + ", 前方信号机Idx=" + _nextSignalIdx
                + ", 前方车站Idx=" + _nextStationIdx);
        }

        internal SignalRecord GetNextSignal(float location)
        {
            if (_branchActive && _activeBranchDb != null
                && location >= _branchStartLocation && location <= _branchEndLocation)
            {
                var sig = _activeBranchDb.GetNextSignal(location);
                if (sig != null) return sig;
            }

            AdvanceCursor(ref _nextSignalIdx, Signals, location, s => s.Location);
            if (_nextSignalIdx < Signals.Count)
                return Signals[_nextSignalIdx];
            return null;
        }

        internal List<SignalRecord> GetNextSignals(float location, int count)
        {
            var result = new List<SignalRecord>();

            if (_branchActive && _activeBranchDb != null
                && location >= _branchStartLocation && location <= _branchEndLocation)
            {
                result.AddRange(_activeBranchDb.GetNextSignals(location, count));
                if (result.Count >= count) return result.GetRange(0, count);
            }

            AdvanceCursor(ref _nextSignalIdx, Signals, location, s => s.Location);
            for (int i = _nextSignalIdx; i < Signals.Count && result.Count < count; i++)
            {
                result.Add(Signals[i]);
            }
            return result;
        }

        internal float GetSectionSpeedLimit(float location)
        {
            if (_branchActive && _activeBranchDb != null
                && location >= _branchStartLocation && location <= _branchEndLocation)
            {
                float branchLimit = _activeBranchDb.GetSectionSpeedLimit(location);
                if (branchLimit > 0) return branchLimit;
            }

            AdvanceCursor(ref _nextSignalIdx, Signals, location, s => s.Location);
            int currentIdx = _nextSignalIdx - 1;
            if (currentIdx < 0) currentIdx = 0;
            if (currentIdx < Signals.Count)
            {
                float limit = SpeedByGauge.GetValueOrNull(Signals[currentIdx].SpeedLimit);
                if (limit > 0) return limit;
            }
            return SpeedByGauge.GetValueOrNull(Meta?.DefaultSpeedLimit);
        }

        internal float GetGradientAt(float location)
        {
            if (Gradients.Count == 0) return 0f;

            while (_currentGradientIdx > 0
                && location < Gradients[_currentGradientIdx].Start)
                _currentGradientIdx--;
            while (_currentGradientIdx < Gradients.Count - 1
                && location >= Gradients[_currentGradientIdx].End)
                _currentGradientIdx++;

            if (_currentGradientIdx >= 0 && _currentGradientIdx < Gradients.Count)
            {
                var g = Gradients[_currentGradientIdx];
                if (location >= g.Start && location < g.End)
                    return g.Value;
            }

            for (int i = 0; i < Gradients.Count; i++)
            {
                if (location >= Gradients[i].Start && location < Gradients[i].End)
                {
                    _currentGradientIdx = i;
                    return Gradients[i].Value;
                }
            }
            return 0f;
        }

        internal CurveRecord GetCurveAt(float location)
        {
            if (Curves.Count == 0) return null;

            while (_currentCurveIdx > 0
                && location < Curves[_currentCurveIdx].Start)
                _currentCurveIdx--;
            while (_currentCurveIdx < Curves.Count - 1
                && location >= Curves[_currentCurveIdx].End)
                _currentCurveIdx++;

            if (_currentCurveIdx >= 0 && _currentCurveIdx < Curves.Count)
            {
                var c = Curves[_currentCurveIdx];
                if (location >= c.Start && location < c.End)
                    return c;
            }
            return null;
        }

        internal float GetFixedModeLimitSpeed(float location)
        {
            float limit = SpeedByGauge.GetValueOrNull(Meta?.DefaultSpeedLimit);
            if (limit <= 0) limit = Config.MaxSpeed;

            float sectionLimit = GetSectionSpeedLimit(location);
            if (sectionLimit > 0 && sectionLimit < limit) limit = sectionLimit;

            for (int i = 0; i < PermSpeedLimits.Count; i++)
            {
                var psl = PermSpeedLimits[i];
                if (location >= psl.Start && location < psl.End)
                {
                    float pslVal = SpeedByGauge.GetValueOrNull(psl.Speed);
                    if (pslVal > 0 && pslVal < limit) limit = pslVal;
                }
            }

            for (int i = 0; i < CatenaryLimits.Count; i++)
            {
                var cl = CatenaryLimits[i];
                if (location >= cl.Start && location < cl.End)
                {
                    if (cl.Speed > 0 && cl.Speed < limit) limit = cl.Speed;
                }
            }

            for (int i = 0; i < TempRestrictions.Count; i++)
            {
                var tr = TempRestrictions[i];
                if (location >= tr.Start && location < tr.End)
                {
                    float trVal = SpeedByGauge.GetValueOrNull(tr.Speed);
                    if (trVal > 0 && trVal < limit) limit = trVal;
                }
            }

            var curve = GetCurveAt(location);
            if (curve != null && curve.Radius > 0 && curve.Radius <= 400f)
            {
                limit = Math.Max(0, limit - 2f);
            }

            if (Config.MaxSpeed > 0 && Config.MaxSpeed < limit) limit = Config.MaxSpeed;

            return limit;
        }

        internal CatenarySectionRecord GetNextCatenarySection(float location)
        {
            AdvanceCursor(ref _nextCatenarySectionIdx, CatenarySections, location, p => p.Start);
            if (_nextCatenarySectionIdx < CatenarySections.Count)
                return CatenarySections[_nextCatenarySectionIdx];
            return null;
        }

        internal StationRecord GetNextStation(float location)
        {
            AdvanceCursor(ref _nextStationIdx, Stations, location, s => s.EntrySignalLocation);
            if (_nextStationIdx < Stations.Count)
                return Stations[_nextStationIdx];
            return null;
        }

        internal StationRecord GetStationAt(float location)
        {
            for (int i = 0; i < Stations.Count; i++)
            {
                var st = Stations[i];
                float entryLoc = st.EntrySignalLocation;
                float centerLoc = entryLoc + st.CenterDist;
                float exitLoc = centerLoc + st.ExitDist + 200f;
                if (location >= entryLoc - 100f && location <= exitLoc)
                    return st;
            }
            return null;
        }

        internal SidingRecord GetSelectedSiding(int stationId)
        {
            StationRecord station = Stations.Find(s => s.Id == stationId);
            if (station == null || station.Sidings == null) return null;
            if (SelectedSidingNo <= 0) return null;
            return station.Sidings.Find(s => s.TrackNo == SelectedSidingNo);
        }

        internal TransferRecord GetNextTransfer(float location)
        {
            AdvanceCursor(ref _nextTransferIdx, Transfers, location, t => t.Location);
            if (_nextTransferIdx < Transfers.Count)
                return Transfers[_nextTransferIdx];
            return null;
        }

        internal void ActivateBranch(int branchId)
        {
            if (Transfers == null || string.IsNullOrEmpty(TrackDataDir)) return;

            foreach (var transfer in Transfers)
            {
                if (transfer.Options == null) continue;
                foreach (var option in transfer.Options)
                {
                    if (option.Id == branchId && !string.IsNullOrEmpty(option.DataDir))
                    {
                        string branchDir = Path.Combine(TrackDataDir, option.DataDir);
                        var branchDb = new TrackDatabase();
                        branchDb.Load(branchDir);
                        if (branchDb.IsLoaded)
                        {
                            _activeBranchDb = branchDb;
                            _branchStartLocation = transfer.Location;
                            _branchEndLocation = transfer.Location + branchDb.Meta.TotalLength;
                            _branchActive = true;
                            SelectedBranchId = branchId;
                            Tool.DebugWriteLine("TrackDatabase: 支线激活, id=" + branchId
                                + ", dir=" + option.DataDir
                                + ", 区间=" + _branchStartLocation + "~" + _branchEndLocation);
                        }
                        return;
                    }
                }
            }
            Tool.DebugWriteLine("TrackDatabase: 支线激活失败, 未找到id=" + branchId);
        }

        internal void DeactivateBranch()
        {
            _activeBranchDb = null;
            _branchActive = false;
            SelectedBranchId = 0;
            Tool.DebugWriteLine("TrackDatabase: 支线已退回主线");
        }

        internal bool CheckBranchExpiry(float location)
        {
            if (_branchActive && location > _branchEndLocation)
            {
                Tool.DebugWriteLine("TrackDatabase: 支线数据走完, 自动退回主线, location=" + location);
                DeactivateBranch();
                return true;
            }
            return false;
        }

        internal void OnBeacon(int type, int data)
        {
            if (type == 12300)
            {
                Tool.DebugWriteLine("TrackDatabase: Beacon 12300 交路选择, data=" + data);
            }
            else if (type == 12301)
            {
                foreach (var transfer in Transfers)
                {
                    if (transfer.AutoTriggerBeacon == data)
                    {
                        ActivateBranch(data);
                        return;
                    }
                }
                Tool.DebugWriteLine("TrackDatabase: Beacon 12301 未匹配转移, data=" + data);
            }
            else if (type == 12302)
            {
                Tool.DebugWriteLine("TrackDatabase: Beacon 12302 监控交路号, data=" + data);
            }
        }

        private static void AdvanceCursor<T>(ref int cursor, List<T> list, float location, Func<T, float> getKey)
        {
            while (cursor < list.Count && getKey(list[cursor]) <= location)
                cursor++;
        }

        private static int BinarySearchNext<T>(List<T> list, float location, Func<T, float> getKey)
        {
            int lo = 0, hi = list.Count;
            while (lo < hi)
            {
                int mid = lo + (hi - lo) / 2;
                if (getKey(list[mid]) <= location)
                    lo = mid + 1;
                else
                    hi = mid;
            }
            return lo;
        }

        private static int FindContainingIndex<T>(List<T> list, float location,
            Func<T, float> getStart, Func<T, float> getEnd)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (location >= getStart(list[i]) && location < getEnd(list[i]))
                    return i;
            }
            return list.Count > 0 ? list.Count - 1 : 0;
        }

        internal void DumpInfo()
        {
            if (!IsLoaded)
            {
                Tool.DebugWriteLine("线路数据库未加载");
                return;
            }

            Tool.DebugWriteLine("线路数据库加载报告:");

            Tool.DebugWriteLine("[交路信息]");
            Tool.DebugWriteLine("  名称: " + Meta.Name);
            Tool.DebugWriteLine("  总长: " + FmtDist(Meta.TotalLength));
            Tool.DebugWriteLine("  起点公里标: " + Meta.KmAtOrigin + " km");
            if (Meta.DefaultSpeedLimit != null)
            {
                Tool.DebugWriteLine("  默认限速: " + Meta.DefaultSpeedLimit.ToString()
                    + " → 当前取值=" + Meta.DefaultSpeedLimit.GetValue() + " km/h");
            }

            DumpGradients();
            DumpCurves();
            DumpPermSpeedLimits();
            DumpStructures();
            DumpCatenary();
            DumpSignals();
            DumpStations();
            DumpTransfers();
            DumpTempRestrictions();

            Tool.DebugWriteLine("════════════════════════════════════════");
        }

        private void DumpGradients()
        {
            Tool.DebugWriteLine("[坡度] " + Gradients.Count + "段");
            for (int i = 0; i < Gradients.Count; i++)
            {
                var g = Gradients[i];
                Tool.DebugWriteLine(string.Format("  #{0}: {1}~{2}  {3}{4}‰",
                    i, FmtDist(g.Start), FmtDist(g.End),
                    g.Value >= 0 ? "+" : "", g.Value));
            }
        }

        private void DumpCurves()
        {
            Tool.DebugWriteLine("[曲线] " + Curves.Count + "段");
            for (int i = 0; i < Curves.Count; i++)
            {
                var c = Curves[i];
                string dir = string.IsNullOrEmpty(c.Direction) ? "" : DirName(c.Direction);
                Tool.DebugWriteLine(string.Format("  #{0}: {1}~{2}  R={3}m {4}",
                    i, FmtDist(c.Start), FmtDist(c.End), c.Radius, dir));
            }
        }

        private void DumpPermSpeedLimits()
        {
            Tool.DebugWriteLine("[永久限速] " + PermSpeedLimits.Count + "段");
            for (int i = 0; i < PermSpeedLimits.Count; i++)
            {
                var p = PermSpeedLimits[i];
                float val = SpeedByGauge.GetValueOrNull(p.Speed);
                Tool.DebugWriteLine(string.Format("  #{0}: {1}~{2}  {3} → {4} km/h  原因={5}",
                    i, FmtDist(p.Start), FmtDist(p.End),
                    p.Speed?.ToString() ?? "null", val, p.Reason ?? ""));
            }
        }

        private void DumpStructures()
        {
            Tool.DebugWriteLine("[桥梁] " + Bridges.Count + "座");
            for (int i = 0; i < Bridges.Count; i++)
            {
                var b = Bridges[i];
                Tool.DebugWriteLine(string.Format("  #{0}: {1}  \"{2}\"  长{3}m",
                    i, FmtDist(b.Location), b.Name, b.Length));
            }

            Tool.DebugWriteLine("[隧道] " + Tunnels.Count + "座");
            for (int i = 0; i < Tunnels.Count; i++)
            {
                var t = Tunnels[i];
                Tool.DebugWriteLine(string.Format("  #{0}: {1}  \"{2}\"  长{3}m",
                    i, FmtDist(t.Location), t.Name, t.Length));
            }

            Tool.DebugWriteLine("[道口] " + LevelCrossings.Count + "处");
            for (int i = 0; i < LevelCrossings.Count; i++)
            {
                var l = LevelCrossings[i];
                Tool.DebugWriteLine(string.Format("  #{0}: {1}  \"{2}\"",
                    i, FmtDist(l.Location), l.Name));
            }
        }

        private void DumpCatenary()
        {
            Tool.DebugWriteLine("[接触网限速] " + CatenaryLimits.Count + "段");
            for (int i = 0; i < CatenaryLimits.Count; i++)
            {
                var c = CatenaryLimits[i];
                Tool.DebugWriteLine(string.Format("  #{0}: {1}~{2}  {3} km/h",
                    i, FmtDist(c.Start), FmtDist(c.End), c.Speed));
            }

            Tool.DebugWriteLine("[接触网分相] " + CatenarySections.Count + "处");
            for (int i = 0; i < CatenarySections.Count; i++)
            {
                var p = CatenarySections[i];
                Tool.DebugWriteLine(string.Format("  #{0}: {1}~{2}  中心{3}  类型={4}",
                    i, FmtDist(p.Start), FmtDist(p.End), FmtDist(p.Center), p.Structure ?? ""));
            }
        }

        private void DumpSignals()
        {
            Tool.DebugWriteLine("[信号机] " + Signals.Count + "架");
            for (int i = 0; i < Signals.Count; i++)
            {
                var s = Signals[i];
                float spdVal = SpeedByGauge.GetValueOrNull(s.SpeedLimit);
                float yelVal = SpeedByGauge.GetValueOrNull(s.YellowSpeed);
                string flags = s.Flags != null ? string.Join(",", s.Flags) : "";
                Tool.DebugWriteLine(string.Format(
                    "  #{0}: Loc={1}  {2}#{3}  间距{4}m  限速{{{5}}}→{6}  黄灯{{{7}}}→{8}  标志[{9}]  站={10}",
                    i, FmtDist(s.Location), SignalTypeName(s.Type), s.Number,
                    s.NextDist,
                    s.SpeedLimit?.ToString() ?? "null", spdVal,
                    s.YellowSpeed?.ToString() ?? "null", yelVal,
                    flags, s.StationId));
            }
        }

        private void DumpStations()
        {
            Tool.DebugWriteLine("[车站] " + Stations.Count + "站");
            for (int i = 0; i < Stations.Count; i++)
            {
                var st = Stations[i];
                Tool.DebugWriteLine(string.Format(
                    "  #{0}: id={1} \"{2}\" {3}  {4} {5}",
                    i, st.Id, st.Name, st.Type ?? "",
                    st.Line ?? "", DirName(st.Direction)));
                Tool.DebugWriteLine(string.Format(
                    "       进站信号Loc={0}  中心距={1}m  出岔距={2}m",
                    FmtDist(st.EntrySignalLocation), st.CenterDist, st.ExitDist));
                Tool.DebugWriteLine(string.Format(
                    "       正线={0}  闭塞={1}  对标距={2}m  侧线触发距={3}m",
                    st.MainTrackNo, st.BlockMethod ?? "",
                    st.StartAlignmentDist, st.EffectiveSidingTriggerDist));
                float exitVal = SpeedByGauge.GetValueOrNull(st.ExitSpeedLimit);
                Tool.DebugWriteLine(string.Format(
                    "       出岔限速{{{0}}}→{1} km/h  标志[{2}]",
                    st.ExitSpeedLimit?.ToString() ?? "null", exitVal,
                    st.Flags != null ? string.Join(",", st.Flags) : ""));

                if (st.Sidings != null && st.Sidings.Count > 0)
                {
                    Tool.DebugWriteLine("       侧线: " + st.Sidings.Count + "条");
                    foreach (var sd in st.Sidings)
                    {
                        float entryVal = SpeedByGauge.GetValueOrNull(sd.EntrySpeedLimit);
                        float sdExitVal = SpeedByGauge.GetValueOrNull(sd.ExitSpeedLimit);
                        Tool.DebugWriteLine(string.Format(
                            "         股道{0}: 进岔{1}m 出岔{2}m 修正{3}m  进岔限速→{4}  出岔限速→{5}  标志[{6}]",
                            sd.TrackNo, sd.EntryDist, sd.ExitDist, sd.Correction,
                            entryVal, sdExitVal,
                            sd.Flags != null ? string.Join(",", sd.Flags) : ""));
                    }
                }

                if (st.Turnouts != null && st.Turnouts.Count > 0)
                {
                    Tool.DebugWriteLine("       道岔: " + st.Turnouts.Count + "组");
                    foreach (var tn in st.Turnouts)
                    {
                        Tool.DebugWriteLine(string.Format(
                            "         Loc={0}  直向{1}km/h  侧向{2}km/h  {3}",
                            FmtDist(tn.Location), tn.SpeedLimitStraight,
                            tn.SpeedLimitDiverging, tn.IsEntry ? "进站道岔" : "出站道岔"));
                    }
                }
            }
        }

        private void DumpTransfers()
        {
            Tool.DebugWriteLine("[支线转移] " + Transfers.Count + "处");
            for (int i = 0; i < Transfers.Count; i++)
            {
                var t = Transfers[i];
                Tool.DebugWriteLine(string.Format("  #{0}: Loc={1}  \"{2}\"",
                    i, FmtDist(t.Location), t.Description));
                if (t.Options != null)
                {
                    foreach (var opt in t.Options)
                    {
                        string dirInfo = string.IsNullOrEmpty(opt.DataDir) ? "(主线)" : "→ " + opt.DataDir;
                        Tool.DebugWriteLine(string.Format("    选项{0}: {1} {2}",
                            opt.Id, opt.Name, dirInfo));
                    }
                }
                if (t.AutoTriggerBeacon != 0)
                    Tool.DebugWriteLine("    自动触发Beacon=" + t.AutoTriggerBeacon);
            }
        }

        private void DumpTempRestrictions()
        {
            Tool.DebugWriteLine("[运行揭示] " + TempRestrictions.Count + "条");
            for (int i = 0; i < TempRestrictions.Count; i++)
            {
                var tr = TempRestrictions[i];
                float val = SpeedByGauge.GetValueOrNull(tr.Speed);
                Tool.DebugWriteLine(string.Format("  #{0}: {1}~{2}  {3} → {4} km/h  原因={5}",
                    i, FmtDist(tr.Start), FmtDist(tr.End),
                    tr.Speed?.ToString() ?? "null", val, tr.Reason ?? ""));
            }
        }

        private static string FmtDist(float m)
        {
            if (Math.Abs(m) >= 10000f)
                return string.Format("{0:F1}km", m / 1000f);
            return string.Format("{0:F0}m", m);
        }

        private static string SignalTypeName(string type)
        {
            if (type == null) return "??";
            switch (type)
            {
                case "Through": return "通过";
                case "Entry": return "进站";
                case "Exit": return "出站";
                case "EntryExit": return "进出站";
                case "Approach": return "预告";
                case "Permissive": return "容许";
                default: return type;
            }
        }

        private static string DirName(string dir)
        {
            if (string.IsNullOrEmpty(dir)) return "";
            switch (dir)
            {
                case "Up": return "上行";
                case "Down": return "下行";
                case "Left": return "左";
                case "Right": return "右";
                default: return dir;
            }
        }
    }
}
