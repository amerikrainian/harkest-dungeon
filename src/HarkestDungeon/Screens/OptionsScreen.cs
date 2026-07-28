using System.Collections.Generic;
using Assets.Code.UI.Options;
using Assets.Code.UI.Screens;
using DD2A11y.Core.Nav;
using DD2A11y.Elements;
using DD2A11y.Game;
using DD2A11y.Input;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine;
using UnityEngine.UI;

namespace DD2A11y.Screens {
    /// <summary>
    /// The settings screen. Layout: a tab selector first (Left/Right switch tabs, remembered
    /// across close/reopen), then the active tab's rows in one vertical flow. Rows are the
    /// spawned <see cref="OptionsItemBhv"/> toggles/sliders plus the tab page's bespoke widgets
    /// (resolution/window/language dropdowns, keybind rows), read generically.
    /// </summary>
    public sealed class OptionsScreen : GameScreen {
        private static readonly AccessTools.FieldRef<OptionsMenuUiBhv, List<OptionsMenuUiBhv.OptionsTab>> TabsField =
            AccessTools.FieldRefAccess<OptionsMenuUiBhv, List<OptionsMenuUiBhv.OptionsTab>>("m_tabs");
        private static readonly AccessTools.FieldRef<OptionsMenuUiBhv, int> ButtonIndexField =
            AccessTools.FieldRefAccess<OptionsMenuUiBhv, int>("m_buttonIndex");

        // The tab the player was on last time the screen was open, restored on reopen. The mod
        // tab is remembered separately: it has no game tab index behind it.
        private static int s_rememberedTab;
        private static bool s_rememberedModTab;

        private readonly Core.Settings.ModSettings _settings;
        private readonly ModTextEdit _textEdit;
        private readonly System.Action<string, bool> _speak;

        private OptionsMenuUiBhv _options;
        private Container _root;
        private Container _items;
        private readonly List<int> _tabIndices = new List<int>(); // our position -> m_tabs index
        private int _builtTab = -1;
        private bool _restoring;
        private int _entryTab; // the tab the entry announcement read, to detect a late restore
        // The mod settings tab, appended after the game's own tabs; its rows are mod elements,
        // not swept game widgets, and the game's tab state is left untouched while it is up.
        private bool _onModTab;

        public OptionsScreen(Core.Settings.ModSettings settings, ModTextEdit textEdit,
                             System.Action<string, bool> speak) {
            _settings = settings;
            _textEdit = textEdit;
            _speak = speak;
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
            _onModTab = s_rememberedModTab;
            _restoring = true;
            EnforceRememberedTab(options);

            if (_tabIndices.Count > 0) {
                _root.Add(new TabSelectorElement(
                    () => CurrentPosition(options),
                    () => _tabIndices.Count + 1,
                    position => TabName(options, position),
                    position => {
                        SelectTab(options, position);
                        RebuildItems(options);
                    }));
            }

            _items = new Container(ContainerShape.VerticalList);
            _root.Add(_items);
            RebuildItems(options);
            _entryTab = CurrentPosition(options);
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
                    announce = CurrentPosition(options) != _entryTab;
                }
            }

            // A mouse click on a tab button changes the game's index under us; a change during
            // the close sequence is the game's teardown, not the player's tab choice. On the mod
            // tab the same signal means the player clicked back onto a game tab.
            if (ButtonIndexField(options) != _builtTab) {
                if (_onModTab) {
                    _onModTab = false;
                    s_rememberedModTab = false;
                }
                RebuildItems(options);
                if (!_restoring && options.ScreenState == UiScreenState.Open) {
                    s_rememberedTab = CurrentPosition(options);
                }
            }
            return announce;
        }

        private void EnforceRememberedTab(OptionsMenuUiBhv options) {
            RebuildTabIndices(options); // tab buttons fade in during the open animation
            if (_tabIndices.Count == 0) {
                return;
            }
            int wanted = s_rememberedTab < _tabIndices.Count ? s_rememberedTab : 0;
            if (ButtonIndexField(options) != _tabIndices[wanted]) {
                SelectTab(options, wanted);
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
            if (_onModTab) {
                return _tabIndices.Count;
            }
            int actual = ButtonIndexField(options);
            int position = _tabIndices.IndexOf(actual);
            return position < 0 ? 0 : position;
        }

        private string TabName(OptionsMenuUiBhv options, int position) {
            if (position == _tabIndices.Count) {
                return S.TabModSettings;
            }
            var tabs = TabsField(options);
            int actual = _tabIndices[position];
            return UiText.FirstLabel(tabs[actual].m_button.gameObject) ?? tabs[actual].m_group.ToString();
        }

        private void SelectTab(OptionsMenuUiBhv options, int position) {
            if (position == _tabIndices.Count) {
                _onModTab = true;
                s_rememberedModTab = true;
                return;
            }
            _onModTab = false;
            s_rememberedModTab = false;
            var tabs = TabsField(options);
            int actual = _tabIndices[position];
            tabs[actual].m_button.onClick.Invoke(); // the game's own tab switch (page + navigation)
            s_rememberedTab = position;
        }

        private void RebuildItems(OptionsMenuUiBhv options) {
            _builtTab = ButtonIndexField(options);
            _items.Clear();

            if (_onModTab) {
                foreach (var setting in _settings.All) {
                    if (setting is Core.Settings.TextSetting text) {
                        _items.Add(new TextEntryElement(
                            () => text.Label,
                            () => Core.Text.SpokenChars.Spell(text.Value),
                            typed => {
                                if (typed.Length == 0) {
                                    _speak(S.SettingReset, true);
                                    text.Reset();
                                } else {
                                    text.Set(typed);
                                }
                            },
                            _textEdit, _speak,
                            hint: () => Core.Text.SpokenChars.Spell(text.DefaultValue)));
                    }
                }
                return;
            }

            var tabs = TabsField(options);
            if (_builtTab < 0 || _builtTab >= tabs.Count) {
                return;
            }
            var tab = tabs[_builtTab];

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
