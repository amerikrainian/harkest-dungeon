using System;
using System.Collections.Generic;
using Assets.Code.Map;
using Assets.Code.Map.Minimap;
using Assets.Code.Utils;
using DD2A11y.Core.Text;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine;

namespace DD2A11y.Game {
    /// <summary>
    /// The road-map cursor: walks the minimap's own node/link graph with a path stack, one
    /// node per press. Up crosses the road ahead (a fork's first alternative is prefixed
    /// "choice"); Left/Right swap among that fork's alternatives; Down retraces the exact
    /// path, falling back to the traveled road when the stack runs dry. The cursor starts at
    /// the wagon - on its node when the coach stands at one, else a synthetic between-nodes
    /// position read live, since the coach keeps driving while the map is up. Home returns to
    /// the wagon, End jumps to the biome's destination. Every node and road reads fog-gated
    /// (an unscouted node stays "Unknown").
    /// </summary>
    public sealed class MapViewer {

        private static readonly AccessTools.FieldRef<MinimapMgrBhv, List<List<MinimapRow>>> RowsField =
            AccessTools.FieldRefAccess<MinimapMgrBhv, List<List<MinimapRow>>>("m_iconRows");
        private static readonly AccessTools.FieldRef<MinimapLink, Assets.Code.UI.Tooltips.LocalizedTextTooltipBhv> RouteTextField =
            AccessTools.FieldRefAccess<MinimapLink, Assets.Code.UI.Tooltips.LocalizedTextTooltipBhv>("m_routeTypeIconObjText");
        private static readonly AccessTools.FieldRef<MinimapLink, Assets.Code.UI.Tooltips.LocalizedTextTooltipBhv> UnknownRouteTextField =
            AccessTools.FieldRefAccess<MinimapLink, Assets.Code.UI.Tooltips.LocalizedTextTooltipBhv>("m_unknownObjText");
        private static readonly AccessTools.FieldRef<MinimapIcon, GameObject> CandleField =
            AccessTools.FieldRefAccess<MinimapIcon, GameObject>("m_candleObj");
        private static readonly AccessTools.FieldRef<MinimapIcon, GameObject> DoomField =
            AccessTools.FieldRefAccess<MinimapIcon, GameObject>("m_doomObj");
        private static readonly AccessTools.FieldRef<MinimapIcon, GameObject> KillContractField =
            AccessTools.FieldRefAccess<MinimapIcon, GameObject>("m_killContractObj");

        private struct NodeRef {
            public int Biome;
            public int Row;
            public int Index;
        }

        private readonly Action<string, bool> _speak;
        private NodeRef? _cursor; // null = the wagon
        private readonly List<(NodeRef From, MinimapLink Link)> _path = new List<(NodeRef, MinimapLink)>();

        public MapViewer(Action<string, bool> speak) {
            _speak = speak;
        }

        /// <summary>Home the cursor on the wagon: on the node itself when the coach stands at
        /// one, else the synthetic between-nodes position.</summary>
        public void Reset() {
            _cursor = null;
            _path.Clear();
            var progress = Progress();
            if (progress != null && progress.Value.IsAtNode()) {
                var info = progress.Value;
                _cursor = new NodeRef { Biome = info.GetBiomeIndex(), Row = info.GetRowIndex(), Index = info.GetIndex() };
            }
        }

        // ---- The spoken cursor (the screen's one element reads these live) ----

        public string CursorLine() {
            if (_cursor == null) {
                return WagonLine();
            }
            var icon = Icon(_cursor.Value);
            if (icon == null) {
                return null;
            }
            // The wagon's own node keeps the "at" framing, so the current location is
            // unmistakable from any later cursor stop.
            return IsWagonNode(_cursor.Value) ? S.MapWagonAt(NodeLine(icon)) : NodeLine(icon);
        }

        private static bool IsWagonNode(NodeRef at) {
            var progress = Progress();
            if (progress == null || !progress.Value.IsAtNode()) {
                return false;
            }
            var info = progress.Value;
            return info.GetBiomeIndex() == at.Biome && info.GetRowIndex() == at.Row && info.GetIndex() == at.Index;
        }

        public IEnumerable<string> DetailLines() {
            if (_cursor == null) {
                foreach (var line in WagonDetail()) {
                    yield return line;
                }
                yield break;
            }
            var at = _cursor.Value;
            var icon = Icon(at);
            if (icon == null) {
                yield break;
            }
            foreach (var line in TooltipReader.LinesOf(icon.GetTooltip())) {
                yield return line;
            }
            foreach (var line in MarkerDetail(icon)) {
                yield return line;
            }
            string state = ChosenWord(icon.GetChosenState());
            if (state != null) {
                yield return state;
            }
            if (_path.Count > 0 && Icon(at) == _path[_path.Count - 1].Link.ApproachingIcon) {
                yield return S.MapVia(RouteText(_path[_path.Count - 1].Link));
            }
            yield return S.MapRow(at.Row + 1, RowCount(at.Biome));
            foreach (var link in LinksFrom(at)) {
                yield return S.MapRoadTo(RouteText(link), NodeName(link.ApproachingIcon));
            }
        }

        // ---- Moves (each speaks its own result) ----

        public void Forward() {
            if (_cursor == null) {
                EnterFromWagon();
                return;
            }
            var links = LinksFrom(_cursor.Value);
            if (links.Count == 0) {
                _speak(S.MapTop, true);
                return;
            }
            bool fork = links.Count > 1;
            var link = links[0];
            var from = _cursor.Value;
            var target = RefOf(link.ApproachingIcon, from);
            if (target == null) {
                Plugin.Log.LogWarning("map: link target icon not found past biome " + from.Biome + " row " + from.Row);
                return;
            }
            _path.Add((from, link));
            _cursor = target;
            _speak(SpokenLine.Join(
                fork ? SpokenLine.Join(S.MapChoice, RouteText(link)) : RouteText(link),
                NodeLine(Icon(target.Value))), true);
        }

        public void Backward() {
            if (_cursor == null) {
                _speak(S.MapBottom, true);
                return;
            }
            if (_path.Count > 0) {
                var step = _path[_path.Count - 1];
                _path.RemoveAt(_path.Count - 1);
                _cursor = step.From;
                _speak(CursorLine(), true);
                return;
            }
            // The stack ran dry: step back onto the wagon when the cursor sits just ahead of
            // it, else follow the roads behind (the traveled one first).
            if (WagonLink() is MinimapLink wagonLink && Icon(_cursor.Value) == wagonLink.ApproachingIcon) {
                _cursor = null;
                _speak(WagonLine(), true);
                return;
            }
            var into = LinksInto(_cursor.Value);
            if (into.Count == 0) {
                _speak(S.MapBottom, true);
                return;
            }
            var back = into.Find(l => l.IsChosen()) ?? into[0];
            var origin = RefOf(back.FromIcon, _cursor.Value, backward: true);
            if (origin == null) {
                _speak(S.MapBottom, true);
                return;
            }
            _cursor = origin;
            _speak(CursorLine(), true);
        }

        /// <summary>Left/Right: swap among the alternatives of the fork the cursor stands on
        /// (the other roads out of the same origin). Silent when there is no fork to swap.</summary>
        public void CycleFork(int direction) {
            if (_cursor == null || _path.Count == 0) {
                return;
            }
            var top = _path[_path.Count - 1];
            var links = LinksFrom(top.From);
            if (links.Count <= 1) {
                return;
            }
            int index = links.IndexOf(top.Link) + direction;
            if (index < 0 || index >= links.Count) {
                return;
            }
            var link = links[index];
            var target = RefOf(link.ApproachingIcon, top.From);
            if (target == null) {
                return;
            }
            _path[_path.Count - 1] = (top.From, link);
            _cursor = target;
            // The alternative's own line only - its road rides in the buffer as the "via"
            // line, so cycling compares nodes without a leading road word.
            _speak(NodeLine(Icon(target.Value)), true);
        }

        public void JumpToWagon() {
            Reset();
            _speak(CursorLine(), true);
        }

        public void JumpToEnd() {
            var progress = Progress();
            if (progress == null) {
                return;
            }
            int biome = progress.Value.GetBiomeIndex();
            int lastRow = RowCount(biome) - 1;
            if (lastRow < 0) {
                return;
            }
            _path.Clear();
            _cursor = new NodeRef { Biome = biome, Row = lastRow, Index = 0 };
            _speak(CursorLine(), true);
        }

        // ---- Wagon (read live every time; the coach keeps moving) ----

        private void EnterFromWagon() {
            var progress = Progress();
            if (progress == null) {
                return;
            }
            var info = progress.Value;
            if (info.IsAtNode()) {
                _cursor = new NodeRef { Biome = info.GetBiomeIndex(), Row = info.GetRowIndex(), Index = info.GetIndex() };
                _speak(CursorLine(), true);
                return;
            }
            var link = WagonLink();
            if (link == null) {
                _speak(S.MapTop, true);
                return;
            }
            var from = new NodeRef { Biome = info.GetBiomeIndex(), Row = info.GetRowIndex(), Index = 0 };
            var target = RefOf(link.ApproachingIcon, from);
            if (target == null) {
                return;
            }
            _cursor = target;
            _speak(SpokenLine.Join(RouteText(link), NodeLine(Icon(target.Value))), true);
        }

        private string WagonLine() {
            var progress = Progress();
            if (progress == null) {
                return null;
            }
            var info = progress.Value;
            if (info.IsAtNode()) {
                var icon = Icon(new NodeRef { Biome = info.GetBiomeIndex(), Row = info.GetRowIndex(), Index = info.GetIndex() });
                return icon == null ? null : S.MapWagonAt(NodeName(icon));
            }
            var link = WagonLink();
            if (link == null) {
                return null;
            }
            return S.MapWagon(NodeName(link.FromIcon), NodeName(link.ApproachingIcon));
        }

        private IEnumerable<string> WagonDetail() {
            string line = WagonLine();
            if (line != null) {
                yield return line;
            }
            var progress = Progress();
            if (progress == null) {
                yield break;
            }
            var info = progress.Value;
            if (!info.IsAtNode() && WagonLink() is MinimapLink link) {
                yield return RouteText(link);
            }
            yield return S.MapRow(info.GetRowIndex() + 1, info.GetRowCount());
        }

        private MinimapLink WagonLink() {
            var progress = Progress();
            if (progress == null || progress.Value.IsAtNode()) {
                return null;
            }
            var info = progress.Value;
            var row = RowAt(info.GetBiomeIndex(), info.GetRowIndex());
            if (row == null) {
                return null;
            }
            var links = row.GetLinks();
            int index = info.GetIndex();
            return index >= 0 && index < links.Count ? links[index] : null;
        }

        // ---- Text (always through the game's own fog-gated tooltips) ----

        // A revealed node's name comes from the model's own loc keys (the same composition the
        // icon's OnGenerate applies) - the widget tooltip gets its key only on the icon's first
        // Update after the map opens, so reading it at entry races into the game's "EMPTY KEY"
        // placeholder. Unrevealed nodes read their prefab-baked unknown tooltip.
        private static string NodeName(MinimapIcon icon) {
            if (icon == null) {
                return null;
            }
            if (!icon.IsRevealed()) {
                foreach (var line in TooltipReader.LinesOf(icon.GetTooltip())) {
                    return line;
                }
                return S.MapUnknown;
            }
            string key = icon.GetNodeType().m_minimapIconTooltipLocKey;
            string subType = icon.GetNodeSubType();
            string text = string.IsNullOrEmpty(subType) ? null : GameLoc.TryGet(key + "_" + subType + "_subtype_label");
            if (text == null) {
                text = GameLoc.TryGet(key);
            }
            return text ?? S.MapUnknown;
        }

        private string NodeLine(MinimapIcon icon) {
            if (icon == null) {
                return null;
            }
            return SpokenLine.Join(NodeName(icon), MarkerWords(icon), ChosenWord(icon.GetChosenState()));
        }

        private static string MarkerWords(MinimapIcon icon) {
            var parts = new List<string>();
            if (IsShown(CandleField(icon))) {
                parts.Add(S.MapCandle);
            }
            if (IsShown(DoomField(icon))) {
                parts.Add(S.MapDoom);
            }
            if (IsShown(KillContractField(icon))) {
                parts.Add(S.MapContract);
            }
            return parts.Count == 0 ? null : SpokenLine.Join(parts.ToArray());
        }

        private static IEnumerable<string> MarkerDetail(MinimapIcon icon) {
            foreach (var marker in new[] { CandleField(icon), DoomField(icon), KillContractField(icon) }) {
                if (!IsShown(marker)) {
                    continue;
                }
                foreach (var line in TooltipReader.Lines(marker)) {
                    yield return line;
                }
            }
        }

        private static bool IsShown(GameObject marker) => marker != null && marker.activeSelf;

        private static string RouteText(MinimapLink link) {
            var tooltip = link.IsRevealed() ? RouteTextField(link) : UnknownRouteTextField(link);
            foreach (var line in TooltipReader.LinesOf(tooltip)) {
                if (!line.Contains("EMPTY KEY")) {
                    return line;
                }
                break;
            }
            // The game ships no string for a safe road (its icon tooltip is the missing-key
            // marker); everything else falling through is a shape change worth hearing about.
            if (link.IsRevealed() && link.RouteDef != null
                && link.RouteDef.m_RouteType == Assets.Code.Map.Generation.Route.RouteType.SAFE) {
                return S.MapSafeRoad;
            }
            return S.MapUnknown;
        }

        private static string ChosenWord(MinimapState state) {
            switch (state) {
                case MinimapState.CHOSEN: return S.MapTraveled;
                case MinimapState.UNCHOSEN: return S.MapNotTaken;
                default: return null;
            }
        }

        // ---- Graph access over the live minimap ----

        private static MinimapMgrBhv Mgr() {
            if (!SingletonMonoBehaviour<MapMgrBhv>.HasInstance()) {
                return null;
            }
            return SingletonMonoBehaviour<MapMgrBhv>.Instance.GetMinimapMgr();
        }

        private static Assets.Code.Map.Generation.ProgressInfo? Progress() {
            if (!SingletonMonoBehaviour<MapMgrBhv>.HasInstance()) {
                return null;
            }
            var info = SingletonMonoBehaviour<MapMgrBhv>.Instance.GetProgress();
            return info.IsValid ? info : (Assets.Code.Map.Generation.ProgressInfo?)null;
        }

        private static MinimapRow RowAt(int biome, int row) {
            var mgr = Mgr();
            if (mgr == null) {
                return null;
            }
            var rows = RowsField(mgr);
            if (biome < 0 || biome >= rows.Count || row < 0 || row >= rows[biome].Count) {
                return null;
            }
            return rows[biome][row];
        }

        private static int RowCount(int biome) {
            var mgr = Mgr();
            if (mgr == null) {
                return 0;
            }
            var rows = RowsField(mgr);
            return biome >= 0 && biome < rows.Count ? rows[biome].Count : 0;
        }

        private static MinimapIcon Icon(NodeRef at) {
            var row = RowAt(at.Biome, at.Row);
            if (row == null) {
                return null;
            }
            var icons = row.GetMinimapIcons();
            return at.Index >= 0 && at.Index < icons.Count ? icons[at.Index] : null;
        }

        private static List<MinimapLink> LinksFrom(NodeRef at) {
            var result = new List<MinimapLink>();
            var icon = Icon(at);
            var row = RowAt(at.Biome, at.Row);
            if (icon == null || row == null) {
                return result;
            }
            foreach (var link in row.GetLinks()) {
                if (link.FromIcon == icon) {
                    result.Add(link);
                }
            }
            return result;
        }

        private static List<MinimapLink> LinksInto(NodeRef at) {
            var result = new List<MinimapLink>();
            var icon = Icon(at);
            if (icon == null) {
                return result;
            }
            foreach (var row in RowsBehind(at)) {
                foreach (var link in row.GetLinks()) {
                    if (link.ApproachingIcon == icon) {
                        result.Add(link);
                    }
                }
            }
            return result;
        }

        // Rows are chained per biome; the previous row is usually in the same biome, the biome
        // boundary hands over to the previous biome's last row.
        private static IEnumerable<MinimapRow> RowsBehind(NodeRef at) {
            if (at.Row > 0) {
                var row = RowAt(at.Biome, at.Row - 1);
                if (row != null) {
                    yield return row;
                }
            } else if (at.Biome > 0) {
                var row = RowAt(at.Biome - 1, RowCount(at.Biome - 1) - 1);
                if (row != null) {
                    yield return row;
                }
            }
        }

        // Resolves an icon back to its coordinates by searching the rows a link can reach from
        // `near` (the next row, or across the biome boundary; the previous ones going backward).
        private NodeRef? RefOf(MinimapIcon icon, NodeRef near, bool backward = false) {
            var candidates = new List<NodeRef>();
            if (backward) {
                if (near.Row > 0) {
                    candidates.Add(new NodeRef { Biome = near.Biome, Row = near.Row - 1 });
                } else if (near.Biome > 0) {
                    candidates.Add(new NodeRef { Biome = near.Biome - 1, Row = RowCount(near.Biome - 1) - 1 });
                }
            } else {
                if (near.Row + 1 < RowCount(near.Biome)) {
                    candidates.Add(new NodeRef { Biome = near.Biome, Row = near.Row + 1 });
                } else {
                    candidates.Add(new NodeRef { Biome = near.Biome + 1, Row = 0 });
                }
            }
            foreach (var candidate in candidates) {
                var row = RowAt(candidate.Biome, candidate.Row);
                if (row == null) {
                    continue;
                }
                var icons = row.GetMinimapIcons();
                for (int i = 0; i < icons.Count; i++) {
                    if (icons[i] == icon) {
                        return new NodeRef { Biome = candidate.Biome, Row = candidate.Row, Index = i };
                    }
                }
            }
            return null;
        }
    }
}
