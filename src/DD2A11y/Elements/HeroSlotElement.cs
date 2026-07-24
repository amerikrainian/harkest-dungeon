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
    /// in the selectable pool. The label is the hero's class name from the game's own loc key;
    /// the slot's tooltip detail (a locked hero's flavor and traits) lives in the buffer. Enter
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

        public HeroSlotElement(HeroSelectActorUIBhv slot, Button button)
            : base(button, null, slot.gameObject) {
            Slot = slot;
        }

        /// <summary>Whether the grab-and-place move can pick this slot up (the game's own
        /// draggability rule: a real, unlocked hero).</summary>
        public bool CanGrab => Slot.IsOccupied && !Slot.IsLocked();

        public override string Label {
            get {
                if (Slot.IsOccupied) {
                    var instance = Slot.ActorInstance;
                    string name = instance == null ? null : GameLoc.TryGet(instance.ActorDataId);
                    if (!string.IsNullOrEmpty(name)) {
                        return name;
                    }
                }
                if (Slot.IsLocked()) {
                    return LockedClassName();
                }
                if (!Slot.IsOccupied) {
                    return S.CrossroadsEmptySlot;
                }
                return UiText.FirstLabel(Slot.gameObject);
            }
        }

        public override string Value {
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

        // Enter toggles party membership in place; speaking the value afterwards reads
        // "in party" when a pool hero landed in the party.
        public override bool ReannounceOnActivate => true;

        public override IEnumerable<ElementAction> GetActions() {
            foreach (var action in base.GetActions()) {
                yield return action;
            }
            if (Slot.IsOccupied && !Slot.IsLocked()) {
                yield return new ElementAction("inspect", OpenSheet);
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
        private string LockedClassName() {
            var resource = ResourceActorField(Slot);
            if (resource == null || !SingletonMonoBehaviour<Library<string, ActorDataClass>>.HasInstance()) {
                return null;
            }
            var dataClass = SingletonMonoBehaviour<Library<string, ActorDataClass>>.Instance
                .GetLibraryElement(resource.name);
            return dataClass == null ? null : GameLoc.TryGet(dataClass.Id);
        }

        public override IEnumerable<string> GetBufferLines() {
            yield return GetFocusText();
            string label = TextFilter.Clean(Label);
            string hoverHint = GameLoc.TryGet("hero_select_actor_hover_label");
            foreach (var tooltip in new[] { TooltipField(Slot), LockedTooltipField(Slot) }) {
                string text = tooltip == null ? null : TooltipReader.TextOf(tooltip);
                if (string.IsNullOrEmpty(text)) {
                    continue;
                }
                foreach (var line in text.Split('\n')) {
                    string clean = TextFilter.Clean(line);
                    if (clean.Length == 0 || clean == label) {
                        continue; // the name line already leads the focus text
                    }
                    if (hoverHint != null && clean == TextFilter.Clean(hoverHint)) {
                        continue; // "click to select" mouse instructions are noise here
                    }
                    yield return clean;
                }
            }
        }
    }
}
