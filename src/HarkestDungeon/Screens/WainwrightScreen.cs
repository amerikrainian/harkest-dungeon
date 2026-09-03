using Assets.Code.Data;
using Assets.Code.Game;
using Assets.Code.UI;
using Assets.Code.UI.Managers;
using Assets.Code.UI.Screens;
using Assets.Code.Utils;
using DD2A11y.Core.Nav;
using DD2A11y.Elements;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DD2A11y.Screens {
    /// <summary>
    /// The stagecoach sheet (<c>StageCoachConfigUiBhv</c>) in both its contexts: the inn's
    /// Wainwright station, and the read-only view the road opens on the Z hotkey.
    /// Named by the sheet's own title value, which the game sets per context ("The
    /// Wainwright" / "The Stagecoach"). Layout: the coach's name (from the model; renaming is
    /// unmodeled), the wallet, the cargo/armor/wheel stat lines the game composes ("Armor:
    /// 1/2", damage explanations in the buffer), a repair button per damaged stat ("repair,
    /// faction 8" - the game's own transaction, its insufficient-funds line on failure), the
    /// livery cycler, then the upgrade slots (equip/unequip through the shared slot flow;
    /// altar-locked slots carry their lock text in the buffer). The road variant carries no
    /// wallet, repairs, or livery cycler, and its slots refuse edits through the widget's own
    /// editable gate. While the game's equip pick is armed (a coach item's press opened the
    /// sheet holding it), entry lands on the slot the held item is for - the hero sheet's
    /// guided-pick landing - with a transient first line reading "Equipping" plus the item's
    /// name one Home above, the spoken form of the item riding the cursor. Escape closes the
    /// sheet, first cancelling that pick.
    /// </summary>
    public sealed class WainwrightScreen : GameScreen {
        private static readonly AccessTools.FieldRef<StageCoachConfigUiBhv, DataContextBhv> ContextField =
            AccessTools.FieldRefAccess<StageCoachConfigUiBhv, DataContextBhv>("m_DataContextBhv");
        private static readonly AccessTools.FieldRef<StageCoachConfigUiBhv, TMP_InputField> NameFieldRef =
            AccessTools.FieldRefAccess<StageCoachConfigUiBhv, TMP_InputField>("m_coachNameInputLabel");
        // The rename key is live here: the coach's name and, at a Kingdoms inn, its pet.
        private static readonly Core.Input.InputCategory[] Categories =
            { Core.Input.InputCategory.Roster, Core.Input.InputCategory.UI };

        private readonly System.Action<string, bool> _speak;
        private readonly Core.Text.TypingEcho _echo;
        private StageCoachConfigUiBhv _sheet;
        private Container _root;
        private int _builtSignature;

        public WainwrightScreen(System.Action<string, bool> speak) {
            _speak = speak;
            // The coach rename is the sheet's own inline edit (its "Rename" action): the field
            // takes the keyboard and the sheet reports IsInputtingText, so the mod's keys
            // already pause; this echoes what the field takes and reads the accepted name.
            _echo = new Core.Text.TypingEcho(() => _sheet != null && _sheet.IsInputtingText, CoachFieldText, speak);
        }

        public override Core.Input.InputCategory[] InputCategories => Categories;

        private string CoachFieldText() {
            var field = _sheet == null ? null : NameFieldRef(_sheet);
            return field == null ? "" : field.text;
        }

        /// <summary>The coach's name: Enter or the rename key starts the sheet's own inline
        /// edit (it clears the field and takes typing until Return; Escape restores).</summary>
        private sealed class CoachNameElement : UIElement {
            private readonly StageCoachConfigUiBhv _sheet;

            public CoachNameElement(StageCoachConfigUiBhv sheet) {
                _sheet = sheet;
            }

            public override string Label => Singleton<GameTypeMgr>.Instance.StageCoach.GetStageCoachName();

            public override string Role => S.RoleEdit;

            public override System.Collections.Generic.IEnumerable<ElementAction> GetActions() {
                yield return new ElementAction(ActionIds.Activate, _sheet.OnEditNameButtonPressed);
                yield return new ElementAction("rename", _sheet.OnEditNameButtonPressed);
            }
        }

        // The game stamps the sheet's per-context title ("The Wainwright" / "The Stagecoach")
        // into its DataContext in OnScreenPushed, after the object already tops the stack; on
        // the entry frame the same title is derived from the game's own keys by the condition
        // OnScreenPushed uses.
        public override string Name {
            get {
                var context = _sheet == null ? null : ContextField(_sheet);
                var title = context == null ? null : context.GetStringValue("stagecoach_title");
                if (string.IsNullOrEmpty(title)) {
                    title = GameLoc.TryGet(GameModeMgr.CurrentMode == GameModeType.INN
                        ? "inn_screen_name_stage_coach" : "stagecoach_sheet_driving_title");
                }
                return string.IsNullOrEmpty(title) ? S.ScreenGeneric : title;
            }
        }

        public override object ResolveTarget() {
            var top = StackTop.Object();
            _sheet = top == null ? null : top.GetComponent<StageCoachConfigUiBhv>();
            return _sheet;
        }

        // The equip pick that opened the sheet arms in the push step, a beat after the object
        // tops the stack; announcing before then would build without the equipping line and
        // read the coach name as the landing.
        public override bool EntrySettled => _sheet == null || _sheet.ScreenState == UiScreenState.Open;

        public override Container BuildRoot(object target) {
            var sheet = (StageCoachConfigUiBhv)target;
            _root = new RootContainer(ContainerShape.VerticalList, back: () => Close(sheet));
            Populate(sheet);
            return _root;
        }

        // Enter on a coach item in the bag opens this sheet with the game's equip pick armed.
        // The game's own Escape ends that pick before anything closes (CommonUiBhv.GoBack),
        // and the sheet's close path never does - a plain close would strand the picked item
        // locked and reading unavailable in the bag. Cancel the pick, then close: one press
        // backs out of the whole flow, and the inn re-announce is the feedback.
        private static void Close(StageCoachConfigUiBhv sheet) {
            if (SingletonMonoBehaviour<CommonUiBhv>.Instance.IsSelectingItemSlot) {
                SlotSelect.Cancel();
            }
            sheet.CloseSubscreen();
        }

        public override bool OnUpdate(object target) {
            var sheet = (StageCoachConfigUiBhv)target;
            if (_echo.Tick()) {
                // The rename ended: read the name the coach now carries (the game restores
                // the previous one when the field was left empty or the edit cancelled).
                _speak(Singleton<GameTypeMgr>.Instance.StageCoach.GetStageCoachName(), true);
            }
            if (Signature(sheet) != _builtSignature) {
                _root.Clear();
                Populate(sheet);
            }
            return false;
        }

        private void Populate(StageCoachConfigUiBhv sheet) {
            // The armed equip pick (Enter on a coach item in the bag opened this sheet holding
            // it): sighted players see the picked item ride the cursor and the accepting slots
            // glow, with no text anywhere. A standing first line carries the same state - entry
            // lands on it, so the opening reads "The Wainwright, Equipping Battered Helm" - and
            // the signature drops it the moment the pick ends.
            if (SingletonMonoBehaviour<CommonUiBhv>.Instance.IsSelectingItemSlot) {
                _root.Add(new ReadoutElement(SlotSelect.EquippingLine));
            }
            _root.Add(new CoachNameElement(sheet));

            var bag = Object.FindObjectOfType<Assets.Code.UI.Screens.InventoryUiBhv>();
            var currencies = bag == null ? null : FindChild(bag.transform, "Currencies");
            if (currencies != null) {
                foreach (Transform row in currencies) {
                    var captured = row;
                    _root.Add(new ReadoutElement(() => InventoryPanel.CurrencyLine(captured)));
                }
            }

            // The game's own composed stat lines; each stat container's tooltip (what damages
            // it) is the buffer, and its repair button follows when the game shows one.
            AddStat(sheet, "stagecoach_cargo_slots", null);
            AddStat(sheet, "armor_stat_label", "ArmorContainer");
            AddStat(sheet, "wheel_stat_label", "WheelContainer");

            var skin = FindChild(sheet.transform, "CycleSkinButton");
            if (skin != null && skin.gameObject.activeInHierarchy) {
                var button = skin.GetComponent<Button>();
                if (button != null) {
                    _root.Add(new CoachLiveryElement(button, skin.parent.gameObject));
                }
            }

            var slots = new Container(ContainerShape.VerticalList);
            foreach (var slot in sheet.GetComponentsInChildren<Assets.Code.UI.Items.InventoryItemBhv>(includeInactive: false)) {
                var selectable = slot.GetComponent<Selectable>();
                if (selectable != null) {
                    slots.Add(new EquipSlotElement(slot, selectable, slot.gameObject, rename: PetRename(slot)));
                }
            }
            if (!slots.IsEmptyContainer) {
                _root.Add(slots);
                // The armed pick lands on the slot the held item is for, the same
                // first-empty-else-first choice the hero sheet's picks make; the equipping
                // line stays one Home above.
                if (SingletonMonoBehaviour<CommonUiBhv>.Instance.IsSelectingItemSlot) {
                    SeedFocus(slots, PickDestinationSlot(slots));
                }
            }
            _builtSignature = Signature(sheet);
        }

        private void SeedFocus(Container slots, UIElement element) {
            if (element == null) {
                return;
            }
            _root.SetFocusedChild(slots);
            slots.SetFocusedChild(element);
        }

        // The slot the armed pick is for: the game hands the held item to the accepting
        // container as its SelectedItem; altar-locked slots share that container and are
        // skipped.
        private static UIElement PickDestinationSlot(Container slots) {
            UIElement first = null;
            foreach (var child in slots.Children) {
                if (child is EquipSlotElement slot && slot.PickDestination && !slot.Locked) {
                    if (!slot.Occupied) {
                        return slot;
                    }
                    if (first == null) {
                        first = slot;
                    }
                }
            }
            return first;
        }

        // The pet slot's rename, where the game offers its own edit-name affordance: a
        // Kingdoms inn (the game's RenamePet action and the slot's edit button share the
        // gate). Opens the game's name-input dialog, read by its own screen.
        private static System.Action PetRename(Assets.Code.UI.Items.InventoryItemBhv slot) {
            if (!(slot is Assets.Code.UI.Items.InventoryItemStageCoachUpgradeBhv upgrade)
                || !(slot.ItemContainer is Assets.Code.UI.Items.InventoryItemContainerStageCoachUpgradeBhv container)
                || container.SlotType != Assets.Code.Item.ItemSlotType.PET
                || Singleton<GameTypeMgr>.Instance.CurrentGameType != GameType.KINGDOM
                || GameModeMgr.CurrentMode != GameModeType.INN) {
                return null;
            }
            return upgrade.OnEditNamePressed;
        }

        private void AddStat(StageCoachConfigUiBhv sheet, string bindingKey, string containerName) {
            var container = containerName == null ? null : FindChild(sheet.transform, containerName);
            _root.Add(new ReadoutElement(
                () => {
                    var live = ContextField(sheet);
                    return live == null ? null : live.GetStringValue(bindingKey);
                },
                detail: () => TooltipReader.Lines(container == null ? null : container.gameObject)));
            if (container == null) {
                return;
            }
            var repair = container.GetComponentInChildren<RunValueTransactionButtonBhv>(includeInactive: false);
            if (repair != null) {
                var cost = repair.GetComponentInChildren<TMP_Text>(includeInactive: false);
                _root.Add(new ActionElement(
                    () => S.StationRepair(cost == null ? "" : cost.text), S.RoleButton, repair.OnClick));
            }
        }

        // Repair buttons and slot widgets appear and vanish with the coach's state, and the
        // equipping line with the armed pick.
        private static int Signature(StageCoachConfigUiBhv sheet) {
            int signature = SingletonMonoBehaviour<CommonUiBhv>.Instance.IsSelectingItemSlot ? 19 : 17;
            foreach (var repair in sheet.GetComponentsInChildren<RunValueTransactionButtonBhv>(includeInactive: false)) {
                signature = signature * 31 + repair.GetInstanceID();
            }
            foreach (var slot in sheet.GetComponentsInChildren<Assets.Code.UI.Items.InventoryItemBhv>(includeInactive: false)) {
                signature = signature * 31 + slot.GetInstanceID();
            }
            return signature;
        }

        private static Transform FindChild(Transform root, string name) {
            if (root == null) {
                return null;
            }
            foreach (var child in root.GetComponentsInChildren<Transform>(includeInactive: false)) {
                if (child.name == name) {
                    return child;
                }
            }
            return null;
        }
    }
}
