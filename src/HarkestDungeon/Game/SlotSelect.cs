using Assets.Code.Item.Events;
using Assets.Code.UI.Canvases;
using Assets.Code.UI.Managers;
using Assets.Code.UI.Widgets;
using Assets.Code.Utils;

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
    }
}
