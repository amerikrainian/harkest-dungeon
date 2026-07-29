using System.Collections.Generic;
using Assets.Code.UI;
using Assets.Code.UI.Screens;
using DD2A11y.Core.Nav;
using DD2A11y.Elements;
using DD2A11y.Game;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Screens {
    /// <summary>
    /// The altar's stat-upgrade panel (<c>AltarGeneralSubScreenBhv</c> - "The Intrepid
    /// Coast"): the candle balance, then one horizontal row per upgrade track - the track's
    /// icon button first (Enter spends one candle into the track), then the milestone
    /// diamonds left to right (Enter buys up to the milestone). Up/Down move between tracks,
    /// each row remembering its column and announcing the track's name as context;
    /// Left/Right walk a track. Escape closes through the panel's own flow (a raw stack pop
    /// would leave the altar's region markers disabled).
    /// </summary>
    public sealed class AltarGeneralScreen : GameScreen {
        private AltarGeneralSubScreenBhv _panel;
        private Container _root;
        private Container _tracks;
        private int _builtSignature;
        private Dictionary<AltarGeneralObjectBhv, Container> _rows =
            new Dictionary<AltarGeneralObjectBhv, Container>();

        public override string Name {
            get {
                string title = UiText.ChildLabel(_panel != null ? _panel.gameObject : null, "exit_anchor");
                return string.IsNullOrEmpty(title) ? S.ScreenGeneric : title;
            }
        }

        public override object ResolveTarget() {
            var top = StackTop.Object();
            _panel = top == null ? null : top.GetComponent<AltarGeneralSubScreenBhv>();
            return _panel;
        }

        public override Container BuildRoot(object target) {
            var panel = (AltarGeneralSubScreenBhv)target;
            _root = new RootContainer(ContainerShape.VerticalList, back: panel.CloseSubscreen);
            _root.Add(AltarScreen.CandleBalance());
            _tracks = new Container(ContainerShape.VerticalList);
            _root.Add(_tracks);
            _rows.Clear();
            Populate(panel);
            return _root;
        }

        public override bool OnUpdate(object target) {
            var panel = (AltarGeneralSubScreenBhv)target;
            if (Signature(panel) != _builtSignature) {
                Populate(panel);
            }
            return false;
        }

        // Rows are keyed to their live track widget and reused across rebuilds, so focus
        // survives the pool re-spawning the widgets on reopen.
        private void Populate(AltarGeneralSubScreenBhv panel) {
            var previous = _rows;
            _rows = new Dictionary<AltarGeneralObjectBhv, Container>();
            _tracks.Clear();
            foreach (var track in panel.GetComponentsInChildren<AltarGeneralObjectBhv>(includeInactive: false)) {
                if (!previous.TryGetValue(track, out var row)) {
                    row = BuildRow(track);
                }
                if (row != null) {
                    _rows[track] = row;
                    _tracks.Add(row);
                }
            }
            _builtSignature = Signature(panel);
        }

        private static Container BuildRow(AltarGeneralObjectBhv track) {
            var button = track.GetButton();
            if (button == null) {
                return null;
            }
            var row = new Container(ContainerShape.HorizontalList, TrackName(track));
            row.Add(new AltarTrackElement(track, button, () => TrackName(track)));
            foreach (var milestone in track.GetComponentsInChildren<ProgressTrackMilestoneBhv>(includeInactive: false)) {
                var selectable = milestone.GetComponent<UnityEngine.UI.Selectable>();
                if (selectable != null) {
                    row.Add(new AltarMilestoneElement(milestone, selectable));
                }
            }
            return row;
        }

        /// <summary>The track's name: the game binds the loc KEY ("altar_upgrade_&lt;id&gt;")
        /// as the raw context value, so localize it; the row's own label is the fallback.</summary>
        private static string TrackName(AltarGeneralObjectBhv track) {
            string key = AltarTrackElement.ContextField(track).GetStringValue("stat_name");
            string name = string.IsNullOrEmpty(key) ? null : GameLoc.TryGet(key);
            return string.IsNullOrEmpty(name) ? UiText.FirstLabel(track.gameObject) : name;
        }

        // An instance-id signature, not a count: the pooled widgets recycle into brand-new
        // instances on reopen, which a count reads as unchanged while every reference dies.
        private static int Signature(AltarGeneralSubScreenBhv panel) {
            int signature = 17;
            foreach (var track in panel.GetComponentsInChildren<AltarGeneralObjectBhv>(includeInactive: false)) {
                signature = signature * 31 + track.GetInstanceID();
            }
            return signature;
        }
    }
}
