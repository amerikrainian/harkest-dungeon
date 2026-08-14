using System.Collections.Generic;
using Assets.Code.UI;
using Assets.Code.UI.Managers;
using Assets.Code.UI.Screens;
using Assets.Code.UI.Widgets;
using Assets.Code.Utils;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Text;
using DD2A11y.Elements;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using TMPro;
using UnityEngine.UI;

namespace DD2A11y.Screens {
    /// <summary>
    /// The hero sheet's skill-loadout screen (saved skill sets per hero, opened by the sheet's
    /// Skill Loadouts button): one row per saved loadout - Enter applies it to the hero through
    /// the game's own submit, with the row's rename and delete buttons as their own controls -
    /// then Save Loadout (stores the hero's current skills) and the game's Continue. Escape
    /// closes through the game's own close, which also persists the loadouts.
    /// </summary>
    public sealed class SkillLoadoutScreen : GameScreen {
        private static readonly AccessTools.FieldRef<LoadoutSelectBhv, TMP_InputField> NameField =
            AccessTools.FieldRefAccess<LoadoutSelectBhv, TMP_InputField>("m_nameInputLabel");
        private static readonly AccessTools.FieldRef<SkillLoadoutWidgetBhv, Button> SaveButtonField =
            AccessTools.FieldRefAccess<SkillLoadoutWidgetBhv, Button>("m_saveLoadoutButton");

        private readonly System.Action<string, bool> _speak;
        private readonly TypingEcho _echo;
        private SkillLoadoutWidgetBhv _widget;
        private Container _root;
        private int _builtSignature;
        private LoadoutSelectBhv _renaming;

        public SkillLoadoutScreen(System.Action<string, bool> speak) {
            _speak = speak;
            _echo = new TypingEcho(() => Renaming() != null, RenamedText, speak);
        }

        public override string Name => GameLoc.TryGet("skill_loadout_title_label") ?? S.ScreenSkillLoadouts;

        /// <summary>Whether a loadout's name field is capturing keystrokes (the game reports it
        /// through the widget, but the mod's keys must pause either way).</summary>
        public bool EditingName => Renaming() != null;

        public override object ResolveTarget() {
            var top = StackTop.Object();
            _widget = top == null ? null : top.GetComponentInChildren<SkillLoadoutWidgetBhv>(includeInactive: false);
            return _widget;
        }

        // The rows spawn in the screen's open step, a beat after the object tops the stack;
        // announcing before then would land past them.
        public override bool EntrySettled {
            get {
                var screen = _widget == null ? null : _widget.GetComponentInParent<UiScreenBhv>();
                return screen == null || screen.ScreenState == UiScreenState.Open;
            }
        }

        public override Container BuildRoot(object target) {
            var widget = (SkillLoadoutWidgetBhv)target;
            _root = new RootContainer(ContainerShape.VerticalList,
                back: () => SingletonMonoBehaviour<CommonUiBhv>.Instance.CloseSkillLoadoutScreen());
            Populate(widget);
            return _root;
        }

        public override bool OnUpdate(object target) {
            var widget = (SkillLoadoutWidgetBhv)target;
            var renaming = Renaming();
            if (renaming != null) {
                _renaming = renaming;
            }
            if (_echo.Tick()) {
                _speak(RenamedText(), true); // the accepted name
            }
            // Rows are pooled: saving or deleting a loadout hands back brand-new instances.
            if (Signature(widget) != _builtSignature) {
                _root.Clear();
                Populate(widget);
            }
            return false;
        }

        private void Populate(SkillLoadoutWidgetBhv widget) {
            _builtSignature = Signature(widget);
            foreach (var row in Rows(widget)) {
                var loadout = row;
                var block = new Container(ContainerShape.HorizontalList);
                block.Add(new ActionElement(
                    () => LoadoutName(loadout), S.RoleButton,
                    loadout.OnClickSubmit));
                foreach (var button in loadout.GetComponentsInChildren<Button>(includeInactive: false)) {
                    // The row's own submit is the block's first element. Its side buttons are
                    // icon-only game-wide (no text, no tooltip), so they take authored labels.
                    if (button.gameObject == loadout.gameObject) {
                        continue;
                    }
                    string name = button.gameObject.name;
                    if (name == "EditNameButton") {
                        block.Add(new SelectableElement(button, () => S.LoadoutRename));
                    } else if (name == "DeleteButton") {
                        block.Add(new SelectableElement(button, () => S.LoadoutDelete));
                    } else {
                        block.Add(new SelectableElement(button));
                    }
                }
                _root.Add(block);
            }

            var save = SaveButtonField(widget);
            if (save != null && save.gameObject.activeInHierarchy) {
                _root.Add(new SelectableElement(save));
            }
            foreach (var button in widget.GetComponentsInChildren<Button>(includeInactive: false)) {
                if (button.gameObject.name == "CloseBtn") {
                    _root.Add(new SelectableElement(button));
                }
            }
        }

        private static IEnumerable<LoadoutSelectBhv> Rows(SkillLoadoutWidgetBhv widget) {
            foreach (var row in widget.GetComponentsInChildren<LoadoutSelectBhv>(includeInactive: false)) {
                yield return row;
            }
        }

        private static string LoadoutName(LoadoutSelectBhv row) {
            var field = row == null ? null : NameField(row);
            return field == null ? null : field.text;
        }

        private LoadoutSelectBhv Renaming() {
            if (_widget == null) {
                return null;
            }
            foreach (var row in Rows(_widget)) {
                if (row.IsInputtingText) {
                    return row;
                }
            }
            return null;
        }

        private string RenamedText() => LoadoutName(_renaming) ?? "";

        private static int Signature(SkillLoadoutWidgetBhv widget) {
            int signature = 17;
            foreach (var row in Rows(widget)) {
                signature = signature * 31 + row.GetInstanceID();
            }
            return signature;
        }
    }
}
