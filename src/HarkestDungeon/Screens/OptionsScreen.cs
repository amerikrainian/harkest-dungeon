using System.Collections.Generic;
using Assets.Code.UI.Options;
using Assets.Code.UI.Screens;
using DD2A11y.Core.Nav;
using DD2A11y.Elements;
using DD2A11y.Game;
using DD2A11y.Screens.Options;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine;
using UnityEngine.UI;

namespace DD2A11y.Screens {
    /// <summary>
    /// The settings screen. Layout: a tab selector first (Left/Right switch tabs, remembered
    /// across close/reopen), then the active tab's rows in one vertical flow. Rows are the
    /// spawned <see cref="OptionsItemBhv"/> toggles/sliders plus the tab page's bespoke widgets
    /// (resolution/window/language dropdowns, keybind rows), read generically. The mod's own
    /// tabs (<see cref="ModTab"/>: settings, the sounds glossary) are appended after the game's;
    /// their rows are mod elements, not swept game widgets, and the game's tab state is left
    /// untouched while one is up.
    /// </summary>
    public sealed class OptionsScreen : GameScreen {
        private static readonly AccessTools.FieldRef<OptionsMenuUiBhv, List<OptionsMenuUiBhv.OptionsTab>> TabsField =
            AccessTools.FieldRefAccess<OptionsMenuUiBhv, List<OptionsMenuUiBhv.OptionsTab>>("m_tabs");
        private static readonly AccessTools.FieldRef<OptionsMenuUiBhv, int> ButtonIndexField =
            AccessTools.FieldRefAccess<OptionsMenuUiBhv, int>("m_buttonIndex");
        private static readonly AccessTools.FieldRef<OptionsMenuUiBhv, GammaCorrectionOptionBhv> GammaOptionField =
            AccessTools.FieldRefAccess<OptionsMenuUiBhv, GammaCorrectionOptionBhv>("gammaCorrectionOptionBhv");
        private static readonly AccessTools.FieldRef<GammaCorrectionOptionBhv, Button> GammaResetField =
            AccessTools.FieldRefAccess<GammaCorrectionOptionBhv, Button>("resetButton");

        // The tab the player was on last time the screen was open, restored on reopen. A mod tab
        // is remembered separately (by its index, -1 for none): it has no game tab index behind
        // it, and the game-side memory keeps the last game tab for when the player returns.
        private static int s_rememberedTab;
        private static int s_rememberedModTab = -1;

        private readonly IReadOnlyList<ModTab> _modTabs;

        private OptionsMenuUiBhv _options;
        private Container _root;
        private Container _items;
        private Button _gammaResetButton;
        private readonly List<int> _tabIndices = new List<int>(); // our position -> m_tabs index
        private int _builtTab = -1;
        private bool _restoring;
        private string _entryTabName; // the tab the entry announcement read, to detect a late restore
        private int _settledFrames; // consecutive Open frames the game's tab index held still
        // The active mod tab's index into _modTabs, -1 while a game tab is up.
        private int _modTab = -1;
        private ModTab _shownModTab;

        public OptionsScreen(IReadOnlyList<ModTab> modTabs) {
            _modTabs = modTabs;
        }

        public override string Name => S.ScreenSettings;

        public override object ResolveTarget() {
            var top = StackTop.Object();
            _options = top == null ? null : top.GetComponent<OptionsMenuUiBhv>();
            return _options;
        }

        public override Container BuildRoot(object target) {
            var options = (OptionsMenuUiBhv)target;
            _root = new RootContainer(ContainerShape.VerticalList, back: () => Close(options));
            RebuildTabIndices(options);
            _modTab = s_rememberedModTab < _modTabs.Count ? s_rememberedModTab : -1;
            _restoring = true;
            _settledFrames = 0;
            EnforceRememberedTab(options);

            if (_tabIndices.Count > 0) {
                _root.Add(new TabSelectorElement(
                    () => CurrentPosition(options),
                    () => _tabIndices.Count + _modTabs.Count,
                    position => TabName(options, position),
                    position => {
                        SelectTab(options, position);
                        RebuildItems(options);
                    }));
            }

            _items = new Container(ContainerShape.VerticalList);
            _root.Add(_items);
            RebuildItems(options);
            // By NAME: the tab buttons fade in during the open animation, so a position captured
            // now can point at a different tab once the full set is active.
            _entryTabName = TabName(options, CurrentPosition(options));
            return _root;
        }

        public override bool OnUpdate(object target) {
            var options = (OptionsMenuUiBhv)target;
            bool announce = false;

            // The game's own open sequence stomps the tab back to 0; keep enforcing the
            // remembered tab until the screen settles Open, then re-announce if the settled tab
            // is not the one the entry announcement read out.
            if (_restoring) {
                EnforceRememberedTab(options);
                if (options.ScreenState == UiScreenState.Open) {
                    _restoring = false;
                    announce = TabName(options, CurrentPosition(options)) != _entryTabName;
                }
            }

            // A mouse click on a tab button changes the game's index under us - but so do the
            // game's own open moves, some landing after the screen already reports Open. Only a
            // change that interrupts a settled index is the player's; the game's moves come in
            // the opening burst, before the index has ever held still. On a mod tab a player's
            // click means they moved back onto a game tab.
            if (ButtonIndexField(options) != _builtTab) {
                bool playerDriven = !_restoring && options.ScreenState == UiScreenState.Open
                    && _settledFrames >= 2;
                _settledFrames = 0;
                if (playerDriven && _modTab >= 0) {
                    _modTab = -1;
                    s_rememberedModTab = -1;
                }
                if (_modTab >= 0) {
                    _builtTab = ButtonIndexField(options); // mod rows stand; just resync the index
                } else {
                    RebuildItems(options);
                }
                if (playerDriven) {
                    s_rememberedTab = CurrentPosition(options);
                }
            } else if (options.ScreenState == UiScreenState.Open) {
                _settledFrames++;
            }
            return announce;
        }

        public override void OnLeave() {
            if (_shownModTab != null) {
                _shownModTab.OnHidden();
                _shownModTab = null;
            }
        }

        // Re-asserts the remembered game-side tab against the open sequence's stomp, without
        // touching which tab the player chose - a remembered mod tab rides above the game tab.
        private void EnforceRememberedTab(OptionsMenuUiBhv options) {
            RebuildTabIndices(options); // tab buttons fade in during the open animation
            if (_tabIndices.Count == 0) {
                return;
            }
            int wanted = s_rememberedTab < _tabIndices.Count ? s_rememberedTab : 0;
            if (ButtonIndexField(options) != _tabIndices[wanted]) {
                TabsField(options)[_tabIndices[wanted]].m_button.onClick.Invoke();
            }
        }

        private void RebuildTabIndices(OptionsMenuUiBhv options) {
            _tabIndices.Clear();
            var tabs = TabsField(options);
            for (int i = 0; i < tabs.Count; i++) {
                var button = tabs[i].m_button;
                if (button != null && button.gameObject.activeInHierarchy) {
                    _tabIndices.Add(i);
                }
            }
        }

        private int CurrentPosition(OptionsMenuUiBhv options) {
            if (_modTab >= 0) {
                return _tabIndices.Count + _modTab;
            }
            int actual = ButtonIndexField(options);
            int position = _tabIndices.IndexOf(actual);
            return position < 0 ? 0 : position;
        }

        private string TabName(OptionsMenuUiBhv options, int position) {
            if (position >= _tabIndices.Count) {
                return _modTabs[position - _tabIndices.Count].Name;
            }
            var tabs = TabsField(options);
            int actual = _tabIndices[position];
            return UiText.FirstLabel(tabs[actual].m_button.gameObject) ?? tabs[actual].m_group.ToString();
        }

        private void SelectTab(OptionsMenuUiBhv options, int position) {
            if (position >= _tabIndices.Count) {
                _modTab = position - _tabIndices.Count;
                s_rememberedModTab = _modTab;
                return;
            }
            _modTab = -1;
            s_rememberedModTab = -1;
            var tabs = TabsField(options);
            int actual = _tabIndices[position];
            tabs[actual].m_button.onClick.Invoke(); // the game's own tab switch (page + navigation)
            s_rememberedTab = position;
        }

        private void RebuildItems(OptionsMenuUiBhv options) {
            _builtTab = ButtonIndexField(options);
            var shown = _modTab >= 0 ? _modTabs[_modTab] : null;
            if (_shownModTab != null && _shownModTab != shown) {
                _shownModTab.OnHidden();
            }
            _shownModTab = shown;
            _items.Clear();

            if (shown != null) {
                shown.Populate(_items);
                return;
            }

            var tabs = TabsField(options);
            if (_builtTab < 0 || _builtTab >= tabs.Count) {
                return;
            }
            var tab = tabs[_builtTab];
            _gammaResetButton = GammaResetField(GammaOptionField(options));

            var seen = new HashSet<Selectable>();
            if (tab.m_layout != null) {
                foreach (Transform child in tab.m_layout) {
                    if (!child.gameObject.activeInHierarchy) {
                        continue;
                    }
                    AddRow(child.gameObject, seen);
                }
            }
            // Bespoke widgets living on the page outside the spawned layout (resolution, window
            // mode, language, gamma...).
            if (tab.m_page != null) {
                foreach (var selectable in tab.m_page.GetComponentsInChildren<Selectable>(includeInactive: false)) {
                    AddSelectable(selectable, selectable.transform.parent != null
                        ? selectable.transform.parent.gameObject : selectable.gameObject, seen);
                }
            }
        }

        private void AddRow(GameObject row, HashSet<Selectable> seen) {
            var optionsItem = row.GetComponent<OptionsItemBhv>();
            if (optionsItem != null) {
                var element = OptionsItemElement.TryCreate(optionsItem);
                if (element != null) {
                    foreach (var selectable in row.GetComponentsInChildren<Selectable>(includeInactive: false)) {
                        seen.Add(selectable);
                    }
                    _items.Add(element);
                }
                return;
            }
            foreach (var selectable in row.GetComponentsInChildren<Selectable>(includeInactive: false)) {
                AddSelectable(selectable, row, seen);
            }
        }

        private void AddSelectable(Selectable selectable, GameObject rowScope, HashSet<Selectable> seen) {
            if (!seen.Add(selectable)) {
                return;
            }
            // Scroll plumbing and invisible anchors are not controls.
            if (selectable is Scrollbar || selectable.GetComponent<SelectOnEmptyFallbackBhv>() != null) {
                return;
            }
            // A selectable nested inside a slider is its drag handle (a Button in the game's
            // prefabs); the slider element itself adjusts the value with Left/Right.
            if (!(selectable is Slider) && selectable.GetComponentInParent<Slider>() != null) {
                return;
            }
            // The gamma reset button is icon-only in the game's own UI, with no text or tooltip
            // anywhere under it or its row; it gets the one mod-authored label on this screen.
            if (selectable == _gammaResetButton) {
                _items.Add(new SelectableElement(selectable, () => S.OptionsGammaReset, rowScope));
                return;
            }
            // A row can hold a second control beside the one its title names (the resolution
            // row's Update button); a button carrying its own caption reads that caption, not
            // the row title.
            if (selectable is Button && UiText.HasAnyTextSource(selectable.gameObject)) {
                rowScope = selectable.gameObject;
            }
            _items.Add(new SelectableElement(selectable, null, rowScope));
        }

        // The game's own Escape is two-stage on mouse+keyboard (deselect the tab, then close);
        // for a screen-reader user the deselect stage is noise, so run both in one press.
        // GoBack() still does the game's selection housekeeping; TryCloseScreen respects
        // IsAllowedToClose.
        private static void Close(OptionsMenuUiBhv options) {
            options.GoBack();
            options.TryCloseScreen();
        }
    }
}
