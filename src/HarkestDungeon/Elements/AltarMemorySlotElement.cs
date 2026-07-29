using System.Collections.Generic;
using Assets.Code.Actor;
using Assets.Code.Boss;
using Assets.Code.Data;
using Assets.Code.Item;
using Assets.Code.Library;
using Assets.Code.UI;
using Assets.Code.Utils;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Speech;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Elements {
    /// <summary>
    /// One memory slot on a hero's row of the altar's Timeless Wood panel. Each slot is keyed
    /// to a confession boss (the identity the sighted slot carries as the boss sprite), so an
    /// unfilled slot is named by the game's own boss-choice label ("I. Denial"): "empty" when
    /// a memory can be chosen (Enter opens the game's selection list), "unavailable" while the
    /// hero has not survived that confession (Enter shows the game's own explanation dialog).
    /// A filled slot is named by its memory item, the item tooltip in the buffer, and - once
    /// the track's reroll milestone is bought - offers the reroll on Enter at the game's own
    /// candle cost, spoken as the slot's state; a paid reroll opens the replacement choices.
    /// </summary>
    public sealed class AltarMemorySlotElement : UIElement {
        private static readonly AccessTools.FieldRef<AltarMemoryBhv, ActorInstance> ActorField =
            AccessTools.FieldRefAccess<AltarMemoryBhv, ActorInstance>("m_actor");
        private static readonly AccessTools.FieldRef<AltarMemoryBhv, int> IndexField =
            AccessTools.FieldRefAccess<AltarMemoryBhv, int>("m_index");
        private static readonly AccessTools.FieldRef<AltarMemoryBhv, DataContextBhv> ContextField =
            AccessTools.FieldRefAccess<AltarMemoryBhv, DataContextBhv>("m_dataContextBhv");
        private static readonly AccessTools.FieldRef<AltarMemoryBhv, AltarMemorySelectionPanelBhv> PanelField =
            AccessTools.FieldRefAccess<AltarMemoryBhv, AltarMemorySelectionPanelBhv>("m_selectionPanel");

        private readonly AltarMemoryBhv _slot;

        public AltarMemorySlotElement(AltarMemoryBhv slot) {
            _slot = slot;
        }

        /// <summary>The live slot widget, for restoring focus after a selection round-trip.</summary>
        internal AltarMemoryBhv Slot => _slot;

        public override bool CanFocus => _slot != null && _slot.gameObject.activeInHierarchy;

        private Assets.Code.Item.IReadOnlyItemInstance Item
            => ActorField(_slot).GetMemoryInventory().GetItem(IndexField(_slot));

        /// <summary>The confession boss this slot index is keyed to - the same filtered
        /// library read the widget itself does - named by the game's boss-choice label.</summary>
        private string ConfessionName {
            get {
                var bosses = SingletonMonoBehaviour<Library<string, BossDefinition>>.Instance
                    .GetLibraryElements((BossDefinition d) => d.m_EndBiomeType != null);
                int index = IndexField(_slot);
                if (index >= bosses.Count) {
                    return null;
                }
                return GameLoc.TryGet("boss_choice_" + bosses[index].m_Id + "_label");
            }
        }

        public override string Label {
            get {
                var item = Item;
                return item != null ? ItemDescription.GetTitle(item.GetItemDefinition()) : ConfessionName;
            }
        }

        public override string Role => S.RoleButton;

        public override string Value {
            get {
                var context = ContextField(_slot);
                if (context.GetBoolValue("memory_locked")) {
                    return S.StatusUnavailable;
                }
                if (context.GetBoolValue("memory_selectable")) {
                    return S.PanelEmpty;
                }
                if (_slot.CanReroll) {
                    return S.AltarMemoryReroll(RerollCost);
                }
                return null;
            }
        }

        private static int RerollCost {
            get {
                var cost = SingletonMonoBehaviour<Assets.Code.AltarOfHope.AltarOfHopeBhv>.Instance
                    .GetRepeatableItemCost(Assets.Code.Item.ItemType.MEMORY_REROLL);
                return cost == null ? 0 : (int)cost.GetProfileValueValue(1f);
            }
        }

        public override IEnumerable<ElementAction> GetActions() {
            var context = ContextField(_slot);
            if (context.GetBoolValue("memory_selectable") || context.GetBoolValue("memory_locked")) {
                // The game's own click: open the selection list, or the run-locked dialog.
                yield return new ElementAction(ActionIds.Activate, _slot.OnClick);
            } else if (_slot.CanReroll) {
                yield return new ElementAction(ActionIds.Activate, () => {
                    _slot.OnAltarReroll();
                    // The purchase validates itself; no opened panel means candles were short.
                    if (!PanelField(_slot).IsActive) {
                        SpeechPipeline.Instance?.Speak(S.StatusUnavailable, interrupt: true);
                    }
                });
            }
        }

        public override IEnumerable<string> GetBufferLines() {
            yield return GetFocusText();
            // A filled slot's confession identity moves to the buffer behind the item name.
            string label = Label;
            if (Item != null) {
                string confession = ConfessionName;
                if (!string.IsNullOrEmpty(confession) && confession != label) {
                    yield return confession;
                }
            }
            foreach (var line in TooltipReader.Lines(_slot.gameObject)) {
                if (line != label) {
                    yield return line;
                }
            }
        }
    }
}
