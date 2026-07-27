using System.Collections.Generic;
using Assets.Code.Data;
using Assets.Code.Inn;
using Assets.Code.Kingdom.UI;
using Assets.Code.UI.Inn;
using Assets.Code.UI.Screens;
using Assets.Code.Utils;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Text;
using DD2A11y.Elements;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine;
using UnityEngine.UI;

namespace DD2A11y.Screens {
    /// <summary>
    /// The kingdom map's inn (and camp) cell panel (a <c>ScreenKingdomMapInnPanel</c> widget on
    /// a Map layer stack entry), named by the inn. The main view: the garrison - one element per
    /// stationed hero (name and class, travel status in the buffer) or militia defender - then
    /// the panel's live buttons (travel or fast travel, engage siege, storage), the five upgrade
    /// tabs each carrying its category's purchase percentage, and the treasure rewards when the
    /// cell holds any. Enter on a tab opens that category's upgrade tree, which reads instead:
    /// one element per node - the upgrade's name, its owned/cost state, description in the
    /// buffer; Enter purchases through the game's own gated unlock. Escape folds the tree
    /// first, then closes (the screen's own two-stage back).
    /// </summary>
    public sealed class KingdomInnPanelScreen : GameScreen {
        private static readonly AccessTools.FieldRef<ScreenKingdomMapInnPanel, DataContextBhv> ContextField =
            AccessTools.FieldRefAccess<ScreenKingdomMapInnPanel, DataContextBhv>("m_dataContextBhv");
        private static readonly AccessTools.FieldRef<ScreenKingdomMapInnPanel, InnUpgradeCategoryWidgetBhv> ActiveCategoryField =
            AccessTools.FieldRefAccess<ScreenKingdomMapInnPanel, InnUpgradeCategoryWidgetBhv>("m_activeCategoryBhv");
        private static readonly AccessTools.FieldRef<ScreenKingdomMapInnPanel, Button> TravelButtonField =
            AccessTools.FieldRefAccess<ScreenKingdomMapInnPanel, Button>("m_travelButton");
        private static readonly AccessTools.FieldRef<ScreenKingdomMapInnPanel, Button> FastTravelButtonField =
            AccessTools.FieldRefAccess<ScreenKingdomMapInnPanel, Button>("m_fastTravelButton");
        private static readonly AccessTools.FieldRef<ScreenKingdomMapInnPanel, Button> EngageSiegeButtonField =
            AccessTools.FieldRefAccess<ScreenKingdomMapInnPanel, Button>("m_engageSiegeButton");
        private static readonly AccessTools.FieldRef<ScreenKingdomMapInnPanel, Button> DefenselessSiegeButtonField =
            AccessTools.FieldRefAccess<ScreenKingdomMapInnPanel, Button>("m_defenselessSiegeButton");
        private static readonly AccessTools.FieldRef<ScreenKingdomMapInnPanel, GameObject> StorageButtonField =
            AccessTools.FieldRefAccess<ScreenKingdomMapInnPanel, GameObject>("m_innStorageBtn");
        private static readonly AccessTools.FieldRef<ScreenKingdomMapInnPanel, GameObject> DefenselessLabelField =
            AccessTools.FieldRefAccess<ScreenKingdomMapInnPanel, GameObject>("m_defenselessLabel");

        private static readonly (string Tab, string Context, string Loc)[] Tabs = {
            ("m_defenseTab", "defense_upgrades", "inn_upgrade_defense_label"),
            ("m_provisionerTab", "provisioner_upgrades", "inn_upgrade_provisioner_label"),
            ("m_physicianTab", "physician_upgrades", "inn_upgrade_physician_label"),
            ("m_trainerTab", "trainer_upgrades", "inn_upgrade_trainer_label"),
            ("m_wainwrightTab", "wainwright_upgrades", "inn_upgrade_wainwright_label"),
        };

        private ScreenKingdomMapInnPanel _panel;
        private Container _root;
        private int _builtSignature;

        // The model name, which is set before the panel's own populate runs - the bound
        // inn_name value arrives a beat after our entry announce.
        public override string Name {
            get {
                var cell = _panel == null ? null : _panel.SelectedCell;
                if (cell == null) {
                    return S.ScreenGeneric;
                }
                if (cell.CellType == Assets.Code.Kingdom.KingdomMapCellType.CAMP) {
                    return GameLoc.TryGet("inn_title_kingdom_camp") ?? S.ScreenGeneric;
                }
                if (cell.InnInstance != null && !string.IsNullOrEmpty(cell.InnInstance.Name)) {
                    return cell.InnInstance.Name;
                }
                return S.ScreenGeneric;
            }
        }

        public override object ResolveTarget() {
            var top = StackTop.Object();
            _panel = top == null ? null : top.GetComponentInChildren<ScreenKingdomMapInnPanel>(false);
            return _panel;
        }

        public override Container BuildRoot(object target) {
            var panel = (ScreenKingdomMapInnPanel)target;
            var screen = panel.GetComponentInParent<UiScreenBhv>();
            // TryCloseScreen honours the panel's own two-stage back: an open upgrade tree
            // blocks the close and folds instead.
            _root = new RootContainer(ContainerShape.VerticalList, back: () => screen.TryCloseScreen());
            Populate(panel);
            return _root;
        }

        public override bool OnUpdate(object target) {
            var panel = (ScreenKingdomMapInnPanel)target;
            if (Signature(panel) != _builtSignature) {
                _root.Clear();
                Populate(panel);
                return true;
            }
            return false;
        }

        // The panel's own blocking flag IS "the upgrade tree is open" (it gates its two-stage
        // back on exactly this).
        private static bool TreeOpen(ScreenKingdomMapInnPanel panel) => panel.IsBlockingCloseAction;

        private void Populate(ScreenKingdomMapInnPanel panel) {
            if (TreeOpen(panel)) {
                PopulateTree(panel);
            } else {
                PopulateMain(panel);
            }
            _builtSignature = Signature(panel);
        }

        private void PopulateMain(ScreenKingdomMapInnPanel panel) {
            foreach (var actor in panel.GetComponentsInChildren<KingdomInnPanelActorBhv>(includeInactive: false)) {
                var captured = actor;
                _root.Add(new ReadoutElement(
                    () => GarrisonLine(captured),
                    detail: () => TooltipReader.Lines(captured.gameObject)));
            }
            var defenseless = DefenselessLabelField(panel);
            if (defenseless != null && defenseless.activeInHierarchy) {
                _root.Add(new ReadoutElement(() => UiText.AllText(defenseless)));
            }
            AddButton(TravelButtonField(panel));
            AddButton(FastTravelButtonField(panel));
            AddButton(EngageSiegeButtonField(panel));
            AddButton(DefenselessSiegeButtonField(panel));
            var storage = StorageButtonField(panel);
            if (storage != null && storage.activeInHierarchy) {
                AddButton(storage.GetComponent<Button>());
            }
            var context = ContextField(panel);
            var traverse = Traverse.Create(panel);
            foreach (var (tabField, contextKey, locKey) in Tabs) {
                var tab = traverse.Field<Button>(tabField).Value;
                if (tab == null || !tab.gameObject.activeInHierarchy) {
                    continue;
                }
                string key = contextKey;
                string loc = locKey;
                _root.Add(new SelectableElement(tab,
                    () => GameLoc.TryGet(loc)) {
                });
            }
            foreach (var reward in panel.GetComponentsInChildren<Assets.Code.UI.Items.UninteractableRewardItemBhv>(includeInactive: false)) {
                var captured = reward;
                _root.Add(new ReadoutElement(
                    () => UiText.AllText(captured.gameObject),
                    detail: () => TooltipReader.Lines(captured.gameObject)));
            }
        }

        // A stationed hero reads name and class from the model behind its portrait; a militia
        // filler slot has no actor and reads its class tooltip alone.
        private static string GarrisonLine(KingdomInnPanelActorBhv widget) {
            if (widget == null) {
                return null;
            }
            if (widget.ActorGuid == 0) {
                string militia = null;
                foreach (var line in TooltipReader.Lines(widget.gameObject)) {
                    militia = line;
                    break;
                }
                return militia;
            }
            var actor = Actors.Get(widget.ActorGuid);
            if (actor == null) {
                return null;
            }
            return SpokenLine.Join(Actors.Name(actor), GameLoc.TryGet(actor.ActorDataClass.Id));
        }

        private void AddButton(Button button) {
            if (button != null && button.gameObject.activeInHierarchy) {
                _root.Add(new SelectableElement(button));
            }
        }

        private void PopulateTree(ScreenKingdomMapInnPanel panel) {
            var context = ContextField(panel);
            _root.Add(new ReadoutElement(() => {
                string title = context == null ? null : GameLoc.TryGet(context.GetStringValue("filter_title"));
                string materials = context == null ? null : context.GetStringValue("material_qty");
                return SpokenLine.Join(title, materials);
            }));
            var category = ActiveCategoryField(panel);
            if (category == null) {
                return;
            }
            foreach (var node in category.GetComponentsInChildren<InnUpgradeButtonBhv>(includeInactive: false)) {
                _root.Add(new InnUpgradeNodeElement(node));
            }
        }

        private static int Signature(ScreenKingdomMapInnPanel panel) {
            int signature = 17;
            signature = signature * 31 + (TreeOpen(panel) ? 1 : 0);
            var cell = panel.SelectedCell;
            if (cell != null) {
                signature = signature * 31 + cell.Coordinates.x;
                signature = signature * 31 + cell.Coordinates.y;
            }
            if (TreeOpen(panel)) {
                var category = ActiveCategoryField(panel);
                signature = signature * 31 + (category == null ? 0 : category.GetInstanceID());
                if (category != null) {
                    foreach (var node in category.GetComponentsInChildren<InnUpgradeButtonBhv>(includeInactive: false)) {
                        signature = signature * 31 + node.GetInstanceID();
                    }
                }
                return signature;
            }
            foreach (var actor in panel.GetComponentsInChildren<KingdomInnPanelActorBhv>(includeInactive: false)) {
                signature = signature * 31 + actor.GetInstanceID();
                signature = signature * 31 + (int)actor.ActorGuid;
            }
            foreach (var reward in panel.GetComponentsInChildren<Assets.Code.UI.Items.UninteractableRewardItemBhv>(includeInactive: false)) {
                signature = signature * 31 + reward.GetInstanceID();
            }
            var travel = TravelButtonField(panel);
            signature = signature * 31 + (travel != null && travel.gameObject.activeInHierarchy ? 1 : 0);
            var fast = FastTravelButtonField(panel);
            signature = signature * 31 + (fast != null && fast.gameObject.activeInHierarchy ? 1 : 0);
            var siege = EngageSiegeButtonField(panel);
            signature = signature * 31 + (siege != null && siege.gameObject.activeInHierarchy ? 1 : 0);
            return signature;
        }
    }
}
