using Assets.Code.Item;
using Assets.Code.Item.Events;
using Assets.Code.UI.Canvases;
using Assets.Code.UI.Items;
using Assets.Code.UI.Managers;
using Assets.Code.UI.Widgets;
using Assets.Code.Utils;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Game {
    /// <summary>
    /// The game's pick-a-slot mode (drag-canvas slot-select): Enter on a bag trinket with the
    /// hero sheet open arms it - the whole bag panel locks, the held item rides the drag
    /// canvas, and the game waits for a press on one of the sheet's slots. While armed for the
    /// sheet, the sheet reader takes the surface even though the bag is the stack top, so the
    /// destination slots are the ones under the arrows.
    /// </summary>
    internal static class SlotSelect {
        public static bool ArmedForSheet {
            get {
                if (!SingletonMonoBehaviour<CommonUiBhv>.HasInstance()) {
                    return false;
                }
                var common = SingletonMonoBehaviour<CommonUiBhv>.Instance;
                return common.IsSelectingItemSlot && common.IsCharacterSheetActiveAndNotClosing;
            }
        }

        /// <summary>The open sheet widget - the armed resolve target while the bag stands
        /// above the sheet on the stack.</summary>
        public static CharacterSheetUiBhv Sheet() {
            var screen = SingletonMonoBehaviour<CommonUiBhv>.Instance.GetCharacterSheetInstance();
            return screen == null ? null : screen.GetWidget<CharacterSheetUiBhv>();
        }

        /// <summary>Abort the armed pick, the same calls the game's own Escape makes; every
        /// surface stays standing and the bag unlocks.</summary>
        public static void Cancel() {
            EventEndInventoryItemSlotSelect.Trigger();
            SingletonMonoBehaviour<DragCanvasUiBhv>.Instance.EndSlotSelect();
        }

        /// <summary>The armed pick's spoken line ("Equipping Battered Helm") - the held item
        /// rides the drag canvas for sighted players until a slot is pressed, and this is its
        /// spoken form. Read live; null while no pick is armed, so an element carrying it
        /// vanishes from the walk on its own.</summary>
        public static string EquippingLine() {
            if (!SingletonMonoBehaviour<CommonUiBhv>.HasInstance()
                || !SingletonMonoBehaviour<CommonUiBhv>.Instance.IsSelectingItemSlot) {
                return null;
            }
            var held = SingletonMonoBehaviour<DragCanvasUiBhv>.Instance.DragElement as InventoryItemBhv;
            var item = held == null ? null : held.Item;
            return ItemUtils.IsValid(item)
                ? S.Equipping(ItemDescription.GetTitle(item.GetItemDefinition())) : null;
        }
    }
}
