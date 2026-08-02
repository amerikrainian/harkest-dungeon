using System.Collections.Generic;
using Assets.Code.Campaign;
using Assets.Code.Game;
using Assets.Code.UI;
using Assets.Code.Utils;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Text;
using DD2A11y.Elements;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DD2A11y.Screens {
    /// <summary>
    /// The crossroads party-loadout overlay: a canvas panel like the path select, matched off
    /// the game's own panel flag and registered above the crossroads. One row per saved
    /// loadout - its name, the heroes it holds as buffer lines - with Enter applying it to the
    /// party (the game's own <c>OnClickSubmit</c>), Shift+Enter deleting it, and the row's
    /// rename button reading as its own control. Save Loadout stores the current party; Escape
    /// closes through the game's own toggle.
    /// </summary>
    public sealed class PartyLoadoutScreen : GameScreen {
        private static readonly AccessTools.FieldRef<HeroSelectBhv, bool> PanelOpenField =
            AccessTools.FieldRefAccess<HeroSelectBhv, bool>("m_partyLoadoutOpen");
        private static readonly AccessTools.FieldRef<HeroSelectBhv, UnityEngine.Playables.PlayableDirector> PanelField =
            AccessTools.FieldRefAccess<HeroSelectBhv, UnityEngine.Playables.PlayableDirector>("m_partyLoadoutPanelDirector");
        private static readonly AccessTools.FieldRef<HeroSelectBhv, Button> SaveButtonField =
            AccessTools.FieldRefAccess<HeroSelectBhv, Button>("m_saveLoadoutButton");
        private static readonly AccessTools.FieldRef<LoadoutSelectBhv, TMP_InputField> NameField =
            AccessTools.FieldRefAccess<LoadoutSelectBhv, TMP_InputField>("m_nameInputLabel");
        private static readonly AccessTools.FieldRef<LoadoutSelectBhv, Transform> PartyContainerField =
            AccessTools.FieldRefAccess<LoadoutSelectBhv, Transform>("m_partyContainer");

        private readonly System.Action<string, bool> _speak;
        private readonly TypingEcho _echo;
        private HeroSelectBhv _heroSelect;
        private Container _root;
        private int _builtSignature;
        private LoadoutSelectBhv _renaming;

        public PartyLoadoutScreen(System.Action<string, bool> speak) {
            _speak = speak;
            _echo = new TypingEcho(() => Renaming() != null, RenamedText, speak);
        }

        public override string Name {
            get {
                var panel = _heroSelect == null ? null : PanelField(_heroSelect);
                string title = UiText.FirstLabel(panel == null ? null : panel.gameObject);
                return string.IsNullOrEmpty(title) ? S.ScreenPartyLoadouts : title;
            }
        }

        /// <summary>Whether a loadout's name field is capturing keystrokes (the row sets the
        /// game's own typing flag too, but the mod's keys must pause either way).</summary>
        public bool EditingName => Renaming() != null;

        public override object ResolveTarget() {
            if (GameModeMgr.CurrentMode != GameModeType.HERO_SELECT
                || Singleton<GameModeMgr>.Instance.IsChangingState()) {
                _heroSelect = null;
                return null;
            }
            if (_heroSelect == null) {
                _heroSelect = UnityEngine.Object.FindObjectOfType<HeroSelectBhv>();
            }
            return _heroSelect != null && PanelOpenField(_heroSelect) ? _heroSelect : null;
        }

        public override Container BuildRoot(object target) {
            var heroSelect = (HeroSelectBhv)target;
            _root = new RootContainer(ContainerShape.VerticalList,
                back: () => heroSelect.TogglePartyLoadoutPanel());
            Populate(heroSelect);
            return _root;
        }

        public override bool OnUpdate(object target) {
            var heroSelect = (HeroSelectBhv)target;
            var renaming = Renaming();
            if (renaming != null) {
                _renaming = renaming;
            }
            if (_echo.Tick()) {
                _speak(RenamedText(), true); // the accepted name
            }
            // Rows are pooled: adding or deleting a loadout hands back brand-new instances.
            if (Signature(heroSelect) != _builtSignature) {
                _root.Clear();
                Populate(heroSelect);
            }
            return false;
        }

        private void Populate(HeroSelectBhv heroSelect) {
            _builtSignature = Signature(heroSelect);
            foreach (var row in Rows(heroSelect)) {
                var loadout = row;
                var block = new Container(ContainerShape.HorizontalList);
                block.Add(new ActionElement(
                    () => LoadoutName(loadout), S.RoleButton,
                    loadout.OnClickSubmit,
                    extraBufferLines: () => HeroLines(loadout)));
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

            var save = SaveButtonField(heroSelect);
            if (save != null && save.gameObject.activeInHierarchy) {
                _root.Add(new SelectableElement(save));
            }
            var panel = PanelField(heroSelect);
            if (panel != null) {
                foreach (var button in panel.gameObject.GetComponentsInChildren<Button>(includeInactive: false)) {
                    if (button.gameObject.name == "CloseBtn") {
                        _root.Add(new SelectableElement(button));
                    }
                }
            }
        }

        private static IEnumerable<LoadoutSelectBhv> Rows(HeroSelectBhv heroSelect) {
            var panel = PanelField(heroSelect);
            if (panel == null) {
                yield break;
            }
            foreach (var row in panel.gameObject.GetComponentsInChildren<LoadoutSelectBhv>(includeInactive: false)) {
                yield return row;
            }
        }

        private static string LoadoutName(LoadoutSelectBhv row) {
            var field = row == null ? null : NameField(row);
            return field == null ? null : field.text;
        }

        // The party the loadout holds: each hero portrait carries the class name in its own
        // tooltip, the only place the row spells the roster out.
        private static IEnumerable<string> HeroLines(LoadoutSelectBhv row) {
            var container = row == null ? null : PartyContainerField(row);
            if (container == null) {
                yield break;
            }
            foreach (var line in TooltipReader.Lines(container.gameObject)) {
                yield return line;
            }
        }

        private LoadoutSelectBhv Renaming() {
            if (_heroSelect == null || !PanelOpenField(_heroSelect)) {
                return null;
            }
            foreach (var row in Rows(_heroSelect)) {
                if (row.IsInputtingText) {
                    return row;
                }
            }
            return null;
        }

        private string RenamedText() => LoadoutName(_renaming) ?? "";

        private static int Signature(HeroSelectBhv heroSelect) {
            int signature = 17;
            foreach (var row in Rows(heroSelect)) {
                signature = signature * 31 + row.GetInstanceID();
            }
            return signature;
        }
    }
}
