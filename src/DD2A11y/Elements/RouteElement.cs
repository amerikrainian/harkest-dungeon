using System.Collections.Generic;
using Assets.Code.Game;
using Assets.Code.Map.Generation;
using Assets.Code.Roster;
using Assets.Code.UI;
using Assets.Code.Utils;
using DD2A11y.Core.Audio;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Text;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Elements {
    /// <summary>
    /// One route at a road fork: its direction and destination (the game's own road-indicator
    /// title; "Unknown" while unrevealed - what the sighted banner shows, never the hidden
    /// type). Focus plays the destination's identity tick panned to the route's side. The
    /// buffer carries the destination's description, which heroes prefer this route, and the
    /// banner's tooltips. Enter commits through the game's own selection (audio and narration
    /// included), after which the coach drives itself.
    /// </summary>
    public sealed class RouteElement : UIElement {
        private static readonly AccessTools.FieldRef<RoadIndicatorUIBhv, RoadIndicatorUIBhv.IndicatorDirection> DirectionField =
            AccessTools.FieldRefAccess<RoadIndicatorUIBhv, RoadIndicatorUIBhv.IndicatorDirection>("m_indicatorDirection");

        private readonly RoadIndicatorUIBhv _indicator;
        private readonly IAudioEngine _audio;

        public RouteElement(RoadIndicatorUIBhv indicator, IAudioEngine audio) {
            _indicator = indicator;
            _audio = audio;
        }

        public override bool CanFocus => _indicator != null && _indicator.gameObject.activeInHierarchy;

        public override string Label => SpokenLine.Join(DirectionWord(), DestinationTitle());

        public override string Role => S.RoleButton;

        private string DirectionWord() {
            switch (DirectionField(_indicator)) {
                case RoadIndicatorUIBhv.IndicatorDirection.Left: return S.RouteLeft;
                case RoadIndicatorUIBhv.IndicatorDirection.Forward: return S.RouteForward;
                case RoadIndicatorUIBhv.IndicatorDirection.Right: return S.RouteRight;
                default: return null;
            }
        }

        // The destination as the sighted banner shows it: its road-indicator title when
        // revealed, the game's own "Unknown" otherwise. The hidden type is never leaked.
        private string DestinationTitle() {
            if (!_indicator.IsRevealed()) {
                return GameLoc.TryGet("road_indicator_unknown_title");
            }
            var type = _indicator.GetNodeType();
            return GameLoc.TryGet(type.m_roadIndicatorNameKey) ?? type.GetName();
        }

        public override IEnumerable<ElementAction> GetActions() {
            yield return new ElementAction(ActionIds.Activate, _indicator.OnClick);
        }

        public override void OnFocused() {
            _audio?.PlayCue(IdentityCue(), 1f, Pan());
        }

        private float Pan() {
            switch (DirectionField(_indicator)) {
                case RoadIndicatorUIBhv.IndicatorDirection.Left: return -0.6f;
                case RoadIndicatorUIBhv.IndicatorDirection.Right: return 0.6f;
                default: return 0f;
            }
        }

        private AudioCue IdentityCue() {
            if (!_indicator.IsRevealed()) {
                return AudioCue.NodeUnknown;
            }
            var type = _indicator.GetNodeType();
            if (type == NodeType.CACHE) return AudioCue.NodeCache;
            if (type == NodeType.HOSPITAL) return AudioCue.NodeHospital;
            if (type == NodeType.STORE) return AudioCue.NodeStore;
            if (type == NodeType.WATCH_TOWER) return AudioCue.NodeWatchtower;
            if (type == NodeType.OASIS) return AudioCue.NodeOasis;
            if (type == NodeType.DUNGEON) return AudioCue.NodeDungeon;
            if (type == NodeType.GUARDIAN) return AudioCue.NodeGuardian;
            if (type == NodeType.CREATURE_DEN) return AudioCue.NodeDen;
            if (type == NodeType.GATE) return AudioCue.NodeGate;
            if (type == NodeType.BRIDGE) return AudioCue.NodeBridge;
            if (type == NodeType.INN || type == NodeType.KINGDOM_INN) return AudioCue.NodeInn;
            if (type == NodeType.STORY_CULTIST || type == NodeType.STORY_ASSIST || type == NodeType.STORY_RESIST
                || type == NodeType.STORY_COSMIC || type == NodeType.STORY_HERO || type == NodeType.STORY_HERO_REPLACEMENT) {
                return AudioCue.NodeStory;
            }
            return AudioCue.NodeUnknown;
        }

        public override IEnumerable<string> GetBufferLines() {
            yield return GetFocusText();
            if (_indicator.IsRevealed()) {
                string description = GameLoc.TryGet(_indicator.GetNodeType().m_roadIndicatorDescKey);
                if (description != null) {
                    yield return description;
                }
            }
            string preferrers = Preferrers();
            if (preferrers != null) {
                yield return S.RoutePreferredBy(preferrers);
            }
            foreach (var line in TooltipReader.Lines(_indicator.gameObject)) {
                yield return line;
            }
        }

        // Heroes whose calculated preference matches what this banner PUBLICLY shows (the game
        // computes preferences against the same revealed-or-unknown list).
        private string Preferrers() {
            var rosterManager = Singleton<GameTypeMgr>.Instance?.RosterManager;
            if (rosterManager == null) {
                return null;
            }
            var publicType = _indicator.IsRevealed() ? _indicator.GetNodeType() : NodeType.UNKNOWN;
            List<string> names = null;
            foreach (uint guid in rosterManager.GetActorGuids(RosterStatusType.PARTY)) {
                var actor = Actors.Get(guid);
                if (actor?.CalculatedRouteChoice != publicType) {
                    continue;
                }
                (names = names ?? new List<string>()).Add(Actors.Name(actor));
            }
            return names == null ? null : string.Join(", ", names.ToArray());
        }
    }
}
