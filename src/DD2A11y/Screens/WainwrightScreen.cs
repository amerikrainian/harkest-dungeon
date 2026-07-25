using Assets.Code.Data;
using Assets.Code.Game;
using Assets.Code.UI;
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
    /// The inn's Wainwright (the <c>StageCoachConfigUiBhv</c> stagecoach sheet), named by the
    /// inn header's station title. Layout: the coach's name (from the model; renaming is
    /// unmodeled), the wallet, the cargo/armor/wheel stat lines the game composes ("Armor:
    /// 1/2", damage explanations in the buffer), a repair button per damaged stat ("repair,
    /// faction 8" - the game's own transaction, its insufficient-funds line on failure), the
    /// livery cycler, then the upgrade slots (equip/unequip through the shared slot flow;
    /// altar-locked slots carry their lock text in the buffer). Escape closes the sheet.
    /// </summary>
    public sealed class WainwrightScreen : GameScreen {
        private static readonly AccessTools.FieldRef<StageCoachConfigUiBhv, DataContextBhv> ContextField =
            AccessTools.FieldRefAccess<StageCoachConfigUiBhv, DataContextBhv>("m_DataContextBhv");

        private StageCoachConfigUiBhv _sheet;
        private Container _root;
        private int _builtSignature;

        public override string Name => InnStations.Title() ?? S.ScreenGeneric;

        public override object ResolveTarget() {
            var top = StackTop.Object();
            _sheet = top == null ? null : top.GetComponent<StageCoachConfigUiBhv>();
            return _sheet;
        }

        public override Container BuildRoot(object target) {
            var sheet = (StageCoachConfigUiBhv)target;
            _root = new RootContainer(ContainerShape.VerticalList, back: sheet.CloseSubscreen);
            Populate(sheet);
            return _root;
        }

        public override bool OnUpdate(object target) {
            var sheet = (StageCoachConfigUiBhv)target;
            if (Signature(sheet) != _builtSignature) {
                _root.Clear();
                Populate(sheet);
            }
            return false;
        }

        private void Populate(StageCoachConfigUiBhv sheet) {
            _root.Add(new ReadoutElement(
                () => Singleton<GameTypeMgr>.Instance.StageCoach.GetStageCoachName()));

            var bag = Object.FindObjectOfType<Assets.Code.UI.Screens.InventoryUiBhv>();
            var currencies = bag == null ? null : FindChild(bag.transform, "Currencies");
            if (currencies != null) {
                foreach (Transform row in currencies) {
                    var captured = row;
                    _root.Add(new ReadoutElement(() => InnScreen.CurrencyLine(captured)));
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
                    _root.Add(new SelectableElement(button, null, skin.parent.gameObject));
                }
            }

            var slots = new Container(ContainerShape.VerticalList);
            foreach (var slot in sheet.GetComponentsInChildren<Assets.Code.UI.Items.InventoryItemBhv>(includeInactive: false)) {
                var selectable = slot.GetComponent<Selectable>();
                if (selectable != null) {
                    slots.Add(new EquipSlotElement(slot, selectable, slot.gameObject));
                }
            }
            if (!slots.IsEmptyContainer) {
                _root.Add(slots);
            }
            _builtSignature = Signature(sheet);
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

        // Repair buttons and slot widgets appear and vanish with the coach's state.
        private static int Signature(StageCoachConfigUiBhv sheet) {
            int signature = 17;
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
