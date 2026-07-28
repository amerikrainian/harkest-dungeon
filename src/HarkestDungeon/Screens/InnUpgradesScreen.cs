using System;
using Assets.Code.Data;
using Assets.Code.Inn;
using Assets.Code.UI.Inn;
using Assets.Code.UI.Widgets;
using DD2A11y.Core.Nav;
using DD2A11y.Elements;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Screens {
    /// <summary>
    /// The inn hub's Inn Upgrades station (a <c>SubScreenInnUpgradeBhv</c> stack entry,
    /// Kingdoms), named by the inn header's station title. A tabbed screen: the category tab
    /// first (Left/Right switch through the game's own tab handlers - Barracks, Provisioner,
    /// Physician, Trainer, Wainwright), the game's materials line, then one element per node
    /// of the active category's tree - the same reader the map inn panel's tree uses: name,
    /// owned or the composed cost, description in the buffer, Enter purchasing through the
    /// node's own gated unlock. Escape closes through the station's own sub-screen flow.
    /// </summary>
    public sealed class InnUpgradesScreen : GameScreen {
        private static readonly AccessTools.FieldRef<SubScreenInnUpgradeBhv, DataContextBhv> ContextField =
            AccessTools.FieldRefAccess<SubScreenInnUpgradeBhv, DataContextBhv>("m_dataContextBhv");
        private static readonly AccessTools.FieldRef<SubScreenInnUpgradeBhv, TabGroupBhv> TabGroupField =
            AccessTools.FieldRefAccess<SubScreenInnUpgradeBhv, TabGroupBhv>("m_tabGroupBhv");
        private static readonly AccessTools.FieldRef<InnUpgradeCategoryWidgetBhv, SelectionInnUpgradeCategory> WidgetCategoryField =
            AccessTools.FieldRefAccess<InnUpgradeCategoryWidgetBhv, SelectionInnUpgradeCategory>("m_InnUpgradeCategory");

        private SubScreenInnUpgradeBhv _panel;
        private Container _root;
        private Container _nodes;
        private int _builtSignature;

        public override string Name => InnStations.Title() ?? S.ScreenGeneric;

        public override object ResolveTarget() {
            var top = StackTop.Object();
            _panel = top == null ? null : top.GetComponentInChildren<SubScreenInnUpgradeBhv>(false);
            return _panel;
        }

        public override Container BuildRoot(object target) {
            var panel = (SubScreenInnUpgradeBhv)target;
            _root = new RootContainer(ContainerShape.VerticalList, back: () => {
                if (panel.GoBack()) {
                    panel.CloseSubscreen();
                }
            });
            // The tab order comes from the tab group itself (the prefab's page order differs
            // from the panel's handler-method order), each page named by its category widget.
            var group = TabGroupField(panel);
            _root.Add(new TabSelectorElement(
                () => ActivePageIndex(group),
                () => group == null ? 0 : group.Count,
                index => {
                    var category = CategoryAt(group, index);
                    return category == null ? null : GameLoc.TryGet("inn_upgrade_" + category.GetName() + "_label");
                },
                index => {
                    // The group's own click handler: it swaps the category page AND runs the
                    // tab's callback (the panel's state update).
                    if (group != null && index >= 0 && index < group.Count) {
                        group.HandleTabButtonClick(index);
                    }
                }));
            var context = ContextField(panel);
            _root.Add(new ReadoutElement(
                () => context == null ? null : context.GetStringValue("materials_available")));
            // The node list rebuilds on a tab switch; the elements above persist so the tab
            // selector keeps focus through it.
            _nodes = new Container(ContainerShape.VerticalList);
            _root.Add(_nodes);
            PopulateNodes(panel);
            return _root;
        }

        public override bool OnUpdate(object target) {
            var panel = (SubScreenInnUpgradeBhv)target;
            if (Signature(panel) != _builtSignature) {
                _nodes.Clear();
                PopulateNodes(panel);
            }
            return false;
        }

        // The group's own m_activeIndex is not authoritative (its click loop overwrites it per
        // association); the visibly active page is.
        private static int ActivePageIndex(TabGroupBhv group) {
            if (group == null) {
                return 0;
            }
            var tabs = (System.Collections.IList)Traverse.Create(group).Field("m_tabs").GetValue();
            if (tabs == null) {
                return 0;
            }
            for (int i = 0; i < tabs.Count; i++) {
                var page = Traverse.Create(tabs[i]).Field("page").GetValue() as UnityEngine.GameObject;
                if (page != null && page.activeInHierarchy) {
                    return i;
                }
            }
            return 0;
        }

        private static InnUpgradeCategory CategoryAt(TabGroupBhv group, int index) {
            if (group == null) {
                return null;
            }
            var tabs = (System.Collections.IList)Traverse.Create(group).Field("m_tabs").GetValue();
            if (tabs == null || index < 0 || index >= tabs.Count) {
                return null;
            }
            var page = Traverse.Create(tabs[index]).Field("page").GetValue() as UnityEngine.GameObject;
            var widget = page == null ? null : page.GetComponentInChildren<InnUpgradeCategoryWidgetBhv>(includeInactive: true);
            if (widget == null) {
                return null;
            }
            var selection = WidgetCategoryField(widget);
            return selection == null ? null : selection.GetSelection();
        }

        private void PopulateNodes(SubScreenInnUpgradeBhv panel) {
            foreach (var node in panel.GetComponentsInChildren<InnUpgradeButtonBhv>(includeInactive: false)) {
                _nodes.Add(new InnUpgradeNodeElement(node));
            }
            _builtSignature = Signature(panel);
        }

        private static int Signature(SubScreenInnUpgradeBhv panel) {
            int signature = 17;
            var group = TabGroupField(panel);
            signature = signature * 31 + ActivePageIndex(group);
            foreach (var node in panel.GetComponentsInChildren<InnUpgradeButtonBhv>(includeInactive: false)) {
                signature = signature * 31 + node.GetInstanceID();
            }
            return signature;
        }
    }
}
