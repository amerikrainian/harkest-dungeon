using System;
using System.Collections.Generic;
using Assets.Code.Data;
using Assets.Code.UI;
using Assets.Code.UI.Widgets;
using DD2A11y.Core.Nav;
using DD2A11y.Elements;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine;
using UnityEngine.UI;

namespace DD2A11y.Screens {
    /// <summary>
    /// The mod panel (Workshop mod load order; opened by the modded menu's Confessions and
    /// Kingdoms flows and its Mods button). Layout: the game's own mod count, the Enable All
    /// and Disable All toggles, one row per installed mod (state, name, version; descriptions
    /// and validation errors in the buffer; Enter flips the mod, Space grab-and-place reorders
    /// through the game's own submit, the landing speaking the resulting load order), then the
    /// Browse Mods workshop button. Escape closes through the panel's own close, which saves
    /// the list.
    /// </summary>
    public sealed class ModPanelScreen : GameScreen {
        private static readonly AccessTools.FieldRef<ModScreenWidgetBhv, DataContextBhv> ContextField =
            AccessTools.FieldRefAccess<ModScreenWidgetBhv, DataContextBhv>("m_dataContextBhv");
        private static readonly AccessTools.FieldRef<ModScreenWidgetBhv, Toggle> EnableAllField =
            AccessTools.FieldRefAccess<ModScreenWidgetBhv, Toggle>("m_enableAllToggle");
        private static readonly AccessTools.FieldRef<ModScreenWidgetBhv, Toggle> DisableAllField =
            AccessTools.FieldRefAccess<ModScreenWidgetBhv, Toggle>("m_disableAllToggle");
        private static readonly AccessTools.FieldRef<ModScreenWidgetBhv, GameObject> WorkshopField =
            AccessTools.FieldRefAccess<ModScreenWidgetBhv, GameObject>("m_workshopButton");

        private readonly Action<string, bool> _speak;
        private ModScreenWidgetBhv _widget;
        private Container _root;
        private Container _rows;
        private int _builtRowsSignature;
        private readonly Dictionary<ModItemBhv, ModItemElement> _elements =
            new Dictionary<ModItemBhv, ModItemElement>();

        public ModPanelScreen(Action<string, bool> speak) {
            _speak = speak;
        }

        public override string Name {
            get {
                string title = GameLoc.TryGet("mod_panel_title_label");
                return title ?? UiText.FirstLabel(_widget != null ? _widget.gameObject : null);
            }
        }

        public override object ResolveTarget() {
            var top = StackTop.Object();
            _widget = top == null ? null : top.GetComponent<ModScreenWidgetBhv>();
            return _widget;
        }

        public override Container BuildRoot(object target) {
            var widget = (ModScreenWidgetBhv)target;
            _root = new RootContainer(ContainerShape.VerticalList, back: widget.OnCloseBtn);
            var context = ContextField(widget);
            _root.Add(new ReadoutElement(() => context == null ? null : context.GetStringValue("mod_count_label")));
            AddAllToggle(EnableAllField(widget), "mod_panel_enable_all_label");
            AddAllToggle(DisableAllField(widget), "mod_panel_disable_all_label");
            _rows = new Container(ContainerShape.VerticalList);
            _root.Add(_rows);
            _elements.Clear();
            PopulateRows(widget);
            var workshop = WorkshopField(widget);
            var browse = workshop == null ? null : workshop.GetComponent<Selectable>();
            if (browse != null) {
                _root.Add(new SelectableElement(browse));
            }
            return _root;
        }

        private void AddAllToggle(Toggle toggle, string locKey) {
            if (toggle == null) {
                return;
            }
            _root.Add(new ActionElement(
                () => GameLoc.TryGet(locKey),
                S.RoleToggle,
                () => toggle.isOn = !toggle.isOn,
                status: () => toggle.isOn ? S.StatusOn : S.StatusOff,
                reannounceOnActivate: true));
        }

        public override bool OnUpdate(object target) {
            var widget = (ModScreenWidgetBhv)target;
            if (RowsSignature(widget) != _builtRowsSignature) {
                // Workshop sync can add rows after the panel opened, and a reorder re-sorts
                // them; elements are reused per row so focus survives the re-sort.
                PopulateRows(widget);
            }
            return false;
        }

        // The rows in visual order. The pool parks recycled rows inactive, so the active
        // sweep sees exactly the shown list.
        private static List<ModItemBhv> Rows(ModScreenWidgetBhv widget) {
            var rows = new List<ModItemBhv>(widget.GetComponentsInChildren<ModItemBhv>(false));
            rows.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
            return rows;
        }

        private void PopulateRows(ModScreenWidgetBhv widget) {
            _rows.Clear();
            foreach (var row in Rows(widget)) {
                if (!_elements.TryGetValue(row, out var element)) {
                    element = new ModItemElement(row, GrabOrDrop);
                    _elements[row] = element;
                }
                _rows.Add(element);
            }
            _builtRowsSignature = RowsSignature(widget);
        }

        private static int RowsSignature(ModScreenWidgetBhv widget) {
            int signature = 17;
            foreach (var row in Rows(widget)) {
                signature = signature * 31 + row.GetInstanceID();
            }
            return signature;
        }

        // Space: the game's own reorder submit, two-phase. A grab announces itself, a second
        // press on the held row cancels, and a drop reads the resulting load order back.
        private void GrabOrDrop(ModItemElement element) {
            if (_widget == null) {
                return;
            }
            if (_widget.IsReordering()) {
                if (_widget.ModItemBeingReordered == element.Item) {
                    _widget.CancelReordering();
                    _speak(S.GrabCancelled, true);
                    return;
                }
                element.Item.OnDraggableSubmit();
                _speak(OrderLine(), true);
                return;
            }
            element.Item.OnDraggableSubmit();
            _speak(S.Grabbed(element.Item.ModName), true);
        }

        private string OrderLine() {
            var names = new List<string>();
            foreach (var row in Rows(_widget)) {
                names.Add(row.ModName);
            }
            return Core.Text.SpokenLine.Join(names.ToArray());
        }
    }
}
