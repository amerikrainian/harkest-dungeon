using System.Collections.Generic;
using Assets.Code.UI;
using Assets.Code.UI.Screens;
using DD2A11y.Core.Nav;
using DD2A11y.Elements;
using DD2A11y.Game;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Screens {
    /// <summary>
    /// The altar's Living City panel (<c>AltarClassSubScreenBhv</c>): the candle balance,
    /// then one horizontal row per hero - the hero's icon button first (Enter spends one
    /// candle into the track), then the track's milestone diamonds left to right (Enter buys
    /// up to the milestone). Up/Down move between heroes, each row remembering its column and
    /// announcing the hero's name as context; Left/Right walk a track. The panel spawns hero
    /// rows over several frames on open, so the tree follows the live set, keeping built rows
    /// so focus stands. Escape closes through the panel's own flow (a raw stack pop would
    /// leave the altar's region markers disabled).
    /// </summary>
    public sealed class AltarClassScreen : GameScreen {
        private AltarClassSubScreenBhv _panel;
        private Container _root;
        private Container _heroes;
        private int _builtSignature;
        private Dictionary<AltarClassHeroBhv, Container> _rows =
            new Dictionary<AltarClassHeroBhv, Container>();

        public override string Name {
            get {
                string title = UiText.ChildLabel(_panel != null ? _panel.gameObject : null, "exit_anchor");
                return string.IsNullOrEmpty(title) ? S.ScreenGeneric : title;
            }
        }

        public override object ResolveTarget() {
            var top = StackTop.Object();
            _panel = top == null ? null : top.GetComponent<AltarClassSubScreenBhv>();
            return _panel;
        }

        public override Container BuildRoot(object target) {
            var panel = (AltarClassSubScreenBhv)target;
            _root = new RootContainer(ContainerShape.VerticalList, back: () => panel.CloseSubscreen());
            _root.Add(AltarScreen.CandleBalance());
            _heroes = new Container(ContainerShape.VerticalList);
            _root.Add(_heroes);
            _rows.Clear();
            Populate(panel);
            return _root;
        }

        public override bool OnUpdate(object target) {
            var panel = (AltarClassSubScreenBhv)target;
            if (Signature(panel) != _builtSignature) {
                Populate(panel);
            }
            return false;
        }

        // Rows are keyed to their live track widget and reused across rebuilds, so the rows
        // already built (and the focus inside them) survive the panel's frame-by-frame spawn.
        private void Populate(AltarClassSubScreenBhv panel) {
            var previous = _rows;
            _rows = new Dictionary<AltarClassHeroBhv, Container>();
            _heroes.Clear();
            foreach (var hero in panel.GetComponentsInChildren<AltarClassHeroBhv>(includeInactive: false)) {
                if (!previous.TryGetValue(hero, out var row)) {
                    row = BuildRow(hero);
                }
                if (row != null) {
                    _rows[hero] = row;
                    _heroes.Add(row);
                }
            }
            _builtSignature = Signature(panel);
        }

        private static Container BuildRow(AltarClassHeroBhv hero) {
            var button = hero.GetButton();
            if (button == null) {
                return null;
            }
            var row = new Container(ContainerShape.HorizontalList, AltarHeroTrackElement.HeroName(hero));
            row.Add(new AltarHeroTrackElement(hero, button));
            foreach (var milestone in hero.GetComponentsInChildren<ProgressTrackMilestoneBhv>(includeInactive: false)) {
                var selectable = milestone.GetComponent<UnityEngine.UI.Selectable>();
                if (selectable != null) {
                    row.Add(new AltarMilestoneElement(milestone, selectable));
                }
            }
            return row;
        }

        // An instance-id signature, not a count: the pooled row widgets recycle into brand-new
        // instances on reopen, which a count reads as unchanged while every reference dies.
        private static int Signature(AltarClassSubScreenBhv panel) {
            int signature = 17;
            foreach (var hero in panel.GetComponentsInChildren<AltarClassHeroBhv>(includeInactive: false)) {
                signature = signature * 31 + hero.GetInstanceID();
            }
            return signature;
        }
    }
}
