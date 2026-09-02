using System.Collections.Generic;
using Assets.Code.Actor;
using Assets.Code.Library;
using Assets.Code.UI.HeroSelect;
using Assets.Code.UI.Managers;
using Assets.Code.UI.Tooltips;
using Assets.Code.UI.Widgets;
using Assets.Code.Utils;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Text;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine;
using UnityEngine.UI;

namespace DD2A11y.Elements {
    /// <summary>
    /// A hero slot at the crossroads: a party rank (the game calls these roster slots) or a hero
    /// in the selectable pool. The label is the hero's name then class ("Dismas, Highwayman" -
    /// the class from the game's own loc key, the name from the shown-hero panel the landing
    /// drives); the slot's tooltip detail (a locked hero's flavor and traits) lives in the
    /// buffer. Enter
    /// runs the game's own quick-transfer (into the party / back out); the hero sheet opens via
    /// the inspect action, and precise moves go through the screen's grab-and-place, which
    /// drives the game's drop logic.
    /// </summary>
    public sealed class HeroSlotElement : SelectableElement {
        private static readonly AccessTools.FieldRef<HeroSelectActorUIBhv, TextTooltipBhv> TooltipField =
            AccessTools.FieldRefAccess<HeroSelectActorUIBhv, TextTooltipBhv>("m_tooltipBhv");
        private static readonly AccessTools.FieldRef<HeroSelectActorUIBhv, TextTooltipBhv> LockedTooltipField =
            AccessTools.FieldRefAccess<HeroSelectActorUIBhv, TextTooltipBhv>("m_lockedClassTooltip");
        private static readonly AccessTools.FieldRef<HeroSelectActorUIBhv, ResourceActor> ResourceActorField =
            AccessTools.FieldRefAccess<HeroSelectActorUIBhv, ResourceActor>("m_ResourceActor");
        private static readonly AccessTools.FieldRef<HeroSelectActorUIBhv, GameObject> RosteredOutlineField =
            AccessTools.FieldRefAccess<HeroSelectActorUIBhv, GameObject>("m_RosteredOutline");

        public HeroSelectActorUIBhv Slot { get; }

        private readonly System.Action<HeroSlotElement> _display;
        private readonly System.Action<HeroSlotElement> _toggleGrab;
        private readonly System.Action _rename;
        private readonly System.Action _reroll;

        /// <param name="display">Makes this slot's hero the one the scene shows (the game's own
        /// selection). The name, path, and reroll controls all act on the SHOWN hero, so the
        /// hero-targeted actions here call it first.</param>
        /// <param name="toggleGrab">Picks this hero up, or places a held one here. BOTH Enter
        /// and the grab key run it, so the two are one move with one state.</param>
        public HeroSlotElement(HeroSelectActorUIBhv slot, Button button,
                               System.Action<HeroSlotElement> display = null,
                               System.Action<HeroSlotElement> toggleGrab = null,
                               System.Action rename = null, System.Action reroll = null)
            : base(button, null, slot.gameObject) {
            Slot = slot;
            _display = display;
            _toggleGrab = toggleGrab;
            _rename = rename;
            _reroll = reroll;
        }

        /// <summary>Whether this slot holds a hero the scene can display.</summary>
        public bool HasHero => Slot.IsOccupied && !Slot.IsLocked() && Slot.ActorInstance != null;

        // Landing on a hero shows them: the canvas model, the stat block, and the targets of
        // the name/path/reroll controls all follow the game's own selection, which our browsing
        // otherwise never moves. Display-only - it never touches the party.
        public override void OnFocused() {
            if (HasHero) {
                _display?.Invoke(this);
            }
        }

        /// <summary>Whether the grab-and-place move can pick this slot up (the game's own
        /// draggability rule: a real, unlocked hero).</summary>
        public bool CanGrab => Slot.IsOccupied && !Slot.IsLocked();

        /// <summary>The occupant's identity (or the empty/locked stand-in), without the rank
        /// prefix - what a grab announces and what the buffer dedupes tooltip lines against.
        /// A hero reads name then class ("Dismas, Highwayman"), the name combat leads with;
        /// landing shows the hero, so the display panel holds that name (the rename field's
        /// text) for a sighted player.</summary>
        public string HeroName {
            get {
                if (Slot.IsOccupied) {
                    var instance = Slot.ActorInstance;
                    string className = instance == null ? null : GameLoc.TryGet(instance.ActorDataId);
                    if (!string.IsNullOrEmpty(className)) {
                        string name = instance.ActorName;
                        return string.IsNullOrEmpty(name) || name == className
                            ? className : SpokenLine.Join(name, className);
                    }
                }
                if (Slot.IsLocked()) {
                    return LockedClassName();
                }
                if (!Slot.IsOccupied) {
                    return S.EmptySlot;
                }
                return UiText.FirstLabel(Slot.gameObject);
            }
        }

        // A party slot leads with its battle position ("rank 1, Dismas, Highwayman"): rank 1
        // is the front line, the same numbering combat uses, and it is what tells the four
        // otherwise identical empty slots apart. Pool heroes have no rank.
        public override string Label
            => Slot.IsRosterSlot
                ? SpokenLine.Join(S.CrossroadsRank(Slot.RosterIndex + 1), HeroName)
                : HeroName;

        public override string Status {
            get {
                if (Slot.IsLocked()) {
                    return S.StatusUnavailable;
                }
                if (!Slot.IsRosterSlot && Slot.IsOccupied) {
                    var outline = RosteredOutlineField(Slot);
                    if (outline != null && outline.activeSelf) {
                        return S.CrossroadsInParty;
                    }
                }
                return null;
            }
        }

        // Every grab path speaks its own outcome (grabbed, cancelled, cannot place, or the
        // landing slot read live), so the navigator must not re-announce over it.
        public override bool ReannounceOnActivate => false;

        public override IEnumerable<ElementAction> GetActions() {
            // Enter and the grab key are the SAME move: pick this hero up, or place the held
            // one here, through the game's own drop rules. The game's Enter is a two-step that
            // arms hidden selection state and moves the game's own cursor, which desynced from
            // our focus; this path holds all its state mod-side and commits in one call.
            if (_toggleGrab != null) {
                yield return new ElementAction(ActionIds.Activate, () => _toggleGrab(this));
                yield return new ElementAction("grab", () => _toggleGrab(this));
            }
            if (Slot.IsOccupied && !Slot.IsLocked()) {
                yield return new ElementAction("inspect", OpenSheet);
            }
            // Rename and reroll act on the SHOWN hero, so each shows this one first - the
            // hero under focus is always the one they affect, from a party rank or the roster
            // alike.
            if (HasHero && _rename != null) {
                yield return new ElementAction("rename", () => {
                    _display?.Invoke(this);
                    _rename();
                });
            }
            if (HasHero && _reroll != null) {
                yield return new ElementAction("reroll", () => {
                    _display?.Invoke(this);
                    _reroll();
                });
            }
        }

        private void OpenSheet() {
            SingletonMonoBehaviour<CommonUiBhv>.Instance.ToggleCharacterSheet(
                CharacterSheetUiBhv.Tab.Skills, Slot.ActorGuid,
                isSkillsEditable: true, isInventoryEditable: false,
                autoSelectTrinketSlot: false, heroSelectFilterParty: Slot.IsRosterSlot);
        }

        // The class name for a hero not yet unlocked (no ActorInstance exists): resolve the
        // actor data class from the slot's resource and localize its id, the same key the game
        // uses for unlocked names.
        private string LockedClassName() => GameLoc.TryGet(ClassId());

        private string ClassId() {
            if (Slot.IsOccupied) {
                var instance = Slot.ActorInstance;
                return instance == null ? null : instance.ActorDataId;
            }
            var resource = ResourceActorField(Slot);
            if (resource == null || !SingletonMonoBehaviour<Library<string, ActorDataClass>>.HasInstance()) {
                return null;
            }
            var dataClass = SingletonMonoBehaviour<Library<string, ActorDataClass>>.Instance
                .GetLibraryElement(resource.name);
            return dataClass == null ? null : dataClass.Id;
        }

        protected override IEnumerable<string> GetDetailLines() {
            string label = TextFilter.Clean(HeroName);
            // The card tooltip's own first line is the bare class name, already inside the
            // head's name-then-class identity.
            string className = TextFilter.Clean(GameLoc.TryGet(ClassId()) ?? string.Empty);
            string hoverHint = GameLoc.TryGet("hero_select_actor_hover_label");
            foreach (var tooltip in new[] { TooltipField(Slot), LockedTooltipField(Slot) }) {
                string text = tooltip == null ? null : TooltipReader.TextOf(tooltip);
                if (string.IsNullOrEmpty(text)) {
                    continue;
                }
                foreach (var line in text.Split('\n')) {
                    string clean = TextFilter.Clean(line);
                    if (clean.Length == 0 || clean == label || (className.Length > 0 && clean == className)) {
                        continue; // the identity line already leads the focus text
                    }
                    if (hoverHint != null && clean == TextFilter.Clean(hoverHint)) {
                        continue; // "click to select" mouse instructions are noise here
                    }
                    yield return clean;
                }
            }
            foreach (var line in ClassDescription.Lines(ClassId())) {
                yield return line;
            }
            // After the class blurb: the panel's aggregate rank-pip rows as exact counts,
            // ascending - the numbers behind the landing tones (same templates as the path
            // comparison readout) - then the Rank row's glow, the ranks the hero's
            // ally-targeting skills can reach, spoken only when the hero has any.
            if (HasHero) {
                var actor = Slot.ActorInstance;
                yield return S.PathLaunchSkills(CountList(RankCoverage.LaunchCounts(actor)));
                yield return S.PathTargetSkills(CountList(RankCoverage.TargetCounts(actor)));
                string reach = RankList(RankCoverage.AllyReachRanks(actor));
                if (reach.Length > 0) {
                    yield return S.AllySkillReach(reach);
                }
            }
        }

        private static string CountList(int[] counts) {
            var parts = new string[counts.Length];
            for (int i = 0; i < counts.Length; i++) {
                parts[i] = counts[i].ToString();
            }
            return SpokenLine.Join(parts);
        }

        private static string RankList(bool[] active) {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < active.Length; i++) {
                if (!active[i]) {
                    continue;
                }
                if (sb.Length > 0) {
                    sb.Append(' ');
                }
                sb.Append(i + 1);
            }
            return sb.ToString();
        }

        public override IEnumerable<string> GetSideBufferLines(string bufferKey)
            => bufferKey == Core.Buffers.BufferKeys.Hero && HasHero
                ? HeroStatus.Lines(Slot.ActorInstance) : base.GetSideBufferLines(bufferKey);
    }
}
