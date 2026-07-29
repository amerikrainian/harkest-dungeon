using System.Collections.Generic;
using Assets.Code.Data;
using Assets.Code.UI;
using Assets.Code.UI.Screens;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Speech;
using DD2A11y.Elements;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine;

namespace DD2A11y.Screens {
    /// <summary>
    /// The altar's memory panel (<c>AltarMemorySubScreenBhv</c> - "The Timeless Wood"), two
    /// states in one tree. Browsing: the candle balance, the Memory unlock track as a
    /// horizontal row (icon button then milestone diamonds, the shared track elements), the
    /// game's own "heroes with memories are required" notice when no hero qualifies, then one
    /// horizontal row per memoried hero holding that hero's memory slots. While the game's
    /// selection list is open (Enter on an empty slot, or a paid reroll) the tree swaps to
    /// the memory offers alone, modal-style: Enter commits through the game's own
    /// select-and-buy, Escape closes the list back to the slot - except mid-reroll, which the
    /// game itself refuses to cancel (the reroll was already paid), answering "unavailable".
    /// Focus returns to the opened slot after either round-trip, so the landed line reads
    /// the slot's new state. Escape while browsing closes through the panel's own GoBack
    /// flow (a raw stack pop would leave the altar's region markers disabled).
    /// </summary>
    public sealed class AltarMemoryScreen : GameScreen {
        private static readonly AccessTools.FieldRef<AltarMemorySubScreenBhv, AltarProgressTrackBaseBhv> TrackField =
            AccessTools.FieldRefAccess<AltarMemorySubScreenBhv, AltarProgressTrackBaseBhv>("m_memoryProgressTrack");
        private static readonly AccessTools.FieldRef<AltarMemorySubScreenBhv, AltarMemorySelectionPanelBhv> SelectionField =
            AccessTools.FieldRefAccess<AltarMemorySubScreenBhv, AltarMemorySelectionPanelBhv>("m_selectionPanel");
        private static readonly AccessTools.FieldRef<AltarMemorySubScreenBhv, GameObject> NoActorsField =
            AccessTools.FieldRefAccess<AltarMemorySubScreenBhv, GameObject>("m_noMemoriedActorsObj");
        private static readonly AccessTools.FieldRef<AltarMemorySubScreenBhv, GameObject> OpenedSlotField =
            AccessTools.FieldRefAccess<AltarMemorySubScreenBhv, GameObject>("m_previouslyOpenedMemorySlot");
        private static readonly AccessTools.FieldRef<AltarMemoryActorBhv, DataContextBhv> ActorContextField =
            AccessTools.FieldRefAccess<AltarMemoryActorBhv, DataContextBhv>("m_dataContextBhv");

        private AltarMemorySubScreenBhv _panel;
        private Container _root;
        private ReadoutElement _balance;
        private int _builtSignature;
        private bool _builtChoices;
        private Dictionary<AltarMemoryActorBhv, Container> _rows =
            new Dictionary<AltarMemoryActorBhv, Container>();

        public override string Name {
            get {
                string title = UiText.ChildLabel(_panel != null ? _panel.gameObject : null, "exit_anchor");
                return string.IsNullOrEmpty(title) ? S.ScreenGeneric : title;
            }
        }

        public override object ResolveTarget() {
            var top = StackTop.Object();
            _panel = top == null ? null : top.GetComponent<AltarMemorySubScreenBhv>();
            return _panel;
        }

        public override Container BuildRoot(object target) {
            var panel = (AltarMemorySubScreenBhv)target;
            _root = new RootContainer(ContainerShape.VerticalList, back: () => Back(panel));
            // One instance across populates, so the entry landing survives the milestones
            // spawning a beat after the stack entry (that rebuild must not re-announce).
            _balance = AltarScreen.CandleBalance();
            _rows.Clear();
            _builtChoices = false;
            Populate(panel);
            return _root;
        }

        public override bool OnUpdate(object target) {
            var panel = (AltarMemorySubScreenBhv)target;
            if (Signature(panel) != _builtSignature) {
                Populate(panel);
            }
            return false; // state swaps orphan the focus; the navigator's re-landing announces
        }

        private static void Back(AltarMemorySubScreenBhv panel) {
            var selection = SelectionField(panel);
            if (selection.IsActive && selection.IsRerolling) {
                // A paid reroll must pick one of its offers; the game refuses the close.
                SpeechPipeline.Instance?.Speak(S.StatusUnavailable, interrupt: true);
                return;
            }
            if (panel.GoBack()) {
                panel.CloseSubscreen();
            }
        }

        private void Populate(AltarMemorySubScreenBhv panel) {
            var selection = SelectionField(panel);
            _root.Clear();
            if (selection.IsActive) {
                BuildChoices(selection);
            } else {
                BuildBrowse(panel);
            }
            _builtChoices = selection.IsActive;
            _builtSignature = Signature(panel);
        }

        private void BuildBrowse(AltarMemorySubScreenBhv panel) {
            _root.Add(_balance);

            var track = TrackField(panel);
            var button = track.GetButton();
            if (button != null) {
                var row = new Container(ContainerShape.HorizontalList,
                    AltarTrackElement.TrackName(track));
                row.Add(new AltarTrackElement(track, button, () => AltarTrackElement.TrackName(track)));
                foreach (var milestone in track.GetComponentsInChildren<ProgressTrackMilestoneBhv>(includeInactive: false)) {
                    var selectable = milestone.GetComponent<UnityEngine.UI.Selectable>();
                    if (selectable != null) {
                        row.Add(new AltarMilestoneElement(milestone, selectable));
                    }
                }
                _root.Add(row);
            }

            var noActors = NoActorsField(panel);
            if (noActors != null && noActors.activeInHierarchy) {
                _root.Add(new ReadoutElement(() => UiText.AllText(noActors)));
            }

            // Hero rows are keyed to their live widget and reused across rebuilds, so the
            // remembered column survives a selection round-trip.
            var previous = _rows;
            _rows = new Dictionary<AltarMemoryActorBhv, Container>();
            var heroes = new Container(ContainerShape.VerticalList);
            foreach (var actor in panel.GetComponentsInChildren<AltarMemoryActorBhv>(includeInactive: false)) {
                if (!previous.TryGetValue(actor, out var row)) {
                    row = BuildHeroRow(actor);
                }
                if (row != null) {
                    _rows[actor] = row;
                    heroes.Add(row);
                }
            }
            _root.Add(heroes);

            // After a selection round-trip, land back on the slot that opened the list, so
            // its updated state (the chosen memory) is the landing line.
            if (!_builtChoices) {
                return;
            }
            var openedSlot = OpenedSlotField(panel);
            var opened = openedSlot == null ? null : openedSlot.GetComponent<AltarMemoryBhv>();
            if (opened != null) {
                foreach (var row in _rows.Values) {
                    foreach (var child in row.Children) {
                        if (child is AltarMemorySlotElement element && element.Slot == opened) {
                            _root.SetFocusedChild(heroes);
                            heroes.SetFocusedChild(row);
                            row.SetFocusedChild(child);
                            return;
                        }
                    }
                }
            }
        }

        private static Container BuildHeroRow(AltarMemoryActorBhv actor) {
            var row = new Container(ContainerShape.HorizontalList,
                ActorContextField(actor).GetStringValue("actor_name"));
            foreach (var slot in actor.GetComponentsInChildren<AltarMemoryBhv>(includeInactive: false)) {
                row.Add(new AltarMemorySlotElement(slot));
            }
            return row.Children.Count == 0 ? null : row;
        }

        private void BuildChoices(AltarMemorySelectionPanelBhv selection) {
            var list = new Container(ContainerShape.VerticalList);
            foreach (var choice in selection.GetComponentsInChildren<AltarSelectMemoryBhv>(includeInactive: false)) {
                list.Add(new AltarMemoryChoiceElement(choice));
            }
            _root.Add(list);
        }

        // An instance-id signature over whichever widget set the state shows: the pooled
        // widgets recycle into brand-new instances, which a count reads as unchanged while
        // every reference dies. The state flag keeps the two sets from colliding.
        private static int Signature(AltarMemorySubScreenBhv panel) {
            var selection = SelectionField(panel);
            int signature = selection.IsActive ? 23 : 17;
            if (selection.IsActive) {
                foreach (var choice in selection.GetComponentsInChildren<AltarSelectMemoryBhv>(includeInactive: false)) {
                    signature = signature * 31 + choice.GetInstanceID();
                }
            } else {
                foreach (var actor in panel.GetComponentsInChildren<AltarMemoryActorBhv>(includeInactive: false)) {
                    signature = signature * 31 + actor.GetInstanceID();
                }
                // The track object itself is prefab-static, so its id never changes; the
                // milestones spawn into it a beat after the stack entry appears.
                foreach (var milestone in panel.GetComponentsInChildren<ProgressTrackMilestoneBhv>(includeInactive: false)) {
                    signature = signature * 31 + milestone.GetInstanceID();
                }
                var noActors = NoActorsField(panel);
                if (noActors != null && noActors.activeInHierarchy) {
                    signature = signature * 31 + 1;
                }
            }
            return signature;
        }
    }
}
