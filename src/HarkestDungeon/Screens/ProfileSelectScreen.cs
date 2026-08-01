using System;
using System.Collections.Generic;
using Assets.Code.Game;
using Assets.Code.UI;
using Assets.Code.UI.Widgets;
using Assets.Code.Utils;
using DD2A11y.Core.Nav;
using DD2A11y.Elements;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

namespace DD2A11y.Screens {
    /// <summary>
    /// The profile-select panel under the title menu's profile button. Two phases, mirroring the
    /// game's own surfaces: the profile list (one row per slot - the profile's name plus its
    /// rename/delete buttons, an empty slot as the game's "Create New" - then the panel's Back
    /// button), and the create-profile window (title, name field, language dropdown, the GDPR
    /// text around the analytics toggle, Continue/Cancel). Escape drives the game's own close,
    /// which cancels a pending creation and drops the whole panel; the title menu re-announces.
    /// While a name field is being typed into (the create window's, or a row's rename - which
    /// the game does not report as text entry), the screen echoes keystrokes and speaks the
    /// accepted name when the edit ends. Elements are keyed to the profile guid, so focus
    /// survives the pooled row swap the game's every refresh performs.
    /// </summary>
    public sealed class ProfileSelectScreen : GameScreen {
        private static readonly AccessTools.FieldRef<ProfileSelectBhv, PlayableDirector> PanelField =
            AccessTools.FieldRefAccess<ProfileSelectBhv, PlayableDirector>("m_profileSelectPanel");
        private static readonly AccessTools.FieldRef<ProfileSelectBhv, ProfileCreationWidgetBhv> CreationField =
            AccessTools.FieldRefAccess<ProfileSelectBhv, ProfileCreationWidgetBhv>("m_creationWidgetBhv");
        private static readonly AccessTools.FieldRef<ProfileCreationWidgetBhv, TMP_InputField> CreationNameField =
            AccessTools.FieldRefAccess<ProfileCreationWidgetBhv, TMP_InputField>("m_nameInputLabel");
        private static readonly AccessTools.FieldRef<ProfileCreationWidgetBhv, LanguageWidgetBhv> LanguageField =
            AccessTools.FieldRefAccess<ProfileCreationWidgetBhv, LanguageWidgetBhv>("m_languageDropdownBhv");
        private static readonly AccessTools.FieldRef<ProfileCreationWidgetBhv, Toggle> AnalyticsToggleField =
            AccessTools.FieldRefAccess<ProfileCreationWidgetBhv, Toggle>("m_analyticsToggle");
        private static readonly AccessTools.FieldRef<ProfileCreationWidgetBhv, Button> CreateButtonField =
            AccessTools.FieldRefAccess<ProfileCreationWidgetBhv, Button>("m_createButton");
        private static readonly AccessTools.FieldRef<ProfileCreationWidgetBhv, TextMeshProUGUI> AnalyticsLabelField =
            AccessTools.FieldRefAccess<ProfileCreationWidgetBhv, TextMeshProUGUI>("m_analyticsCheckLabel");
        private static readonly AccessTools.FieldRef<ProfileSelectItemBhv, TMP_InputField> ItemNameField =
            AccessTools.FieldRefAccess<ProfileSelectItemBhv, TMP_InputField>("m_nameInputField");

        private readonly Action<string, bool> _speak;
        private readonly Core.Text.TypingEcho _echo;
        private ProfileSelectBhv _bhv;
        private Container _root;
        private int _builtSignature;
        private Dictionary<object, UIElement> _byKey = new Dictionary<object, UIElement>();
        // The name edit in flight: the creation window's field, or the renamed profile's guid -
        // where the accepted name is read back from once the edit ends (the rename commit swaps
        // every row instance, so the field itself dies with the edit).
        private bool _editingCreation;
        private uint _editingGuid;

        public ProfileSelectScreen(Action<string, bool> speak) {
            _speak = speak;
            _echo = new Core.Text.TypingEcho(() => EditedField() != null, TypedText, speak);
        }

        public override string Name => GameLoc.TryGet("profile_select_title") ?? S.ScreenProfileSelect;

        /// <summary>Whether a profile name field is capturing keystrokes right now. The create
        /// window reports its edit through the game's IsInputtingText, but a row rename does NOT -
        /// the input manager asks this so its keys pause for both.</summary>
        public bool EditingName => EditedField() != null;

        public override object ResolveTarget() {
            if (GameModeMgr.CurrentMode != GameModeType.MAIN_MENU || Singleton<GameModeMgr>.Instance.IsChangingState()) {
                return null;
            }
            if (_bhv == null) {
                _bhv = UnityEngine.Object.FindObjectOfType<ProfileSelectBhv>();
            }
            if (_bhv == null || !_bhv.IsProfileSelectWindowActive()) {
                return null;
            }
            return _bhv;
        }

        public override Container BuildRoot(object target) {
            _bhv = (ProfileSelectBhv)target;
            // The game's own Escape: cancels a pending creation and closes the whole panel.
            _root = new RootContainer(ContainerShape.VerticalList, back: () => _bhv.OnExitProfileSelectButton());
            _byKey.Clear();
            Populate();
            return _root;
        }

        public override bool OnUpdate(object target) {
            var edited = EditedField();
            if (edited != null) {
                var item = edited.GetComponentInParent<ProfileSelectItemBhv>();
                _editingCreation = item == null;
                _editingGuid = item != null && !item.IsEmpty && item.GetProfileInstance() != null
                    ? item.GetProfileInstance().ProfileGuid : 0;
            }
            if (_echo.Tick()) {
                _speak(AcceptedName(), true);
            }
            if (Signature() != _builtSignature) {
                _root.Clear();
                Populate();
            }
            return false;
        }

        // The focused name field, if any: the create window's, else a row's rename edit.
        private TMP_InputField EditedField() {
            if (_bhv == null || !_bhv.IsProfileSelectWindowActive()) {
                return null;
            }
            var creation = CreationField(_bhv);
            if (creation != null && creation.gameObject.activeInHierarchy) {
                var field = CreationNameField(creation);
                if (field != null && field.isFocused) {
                    return field;
                }
            }
            foreach (var item in Items()) {
                var field = ItemNameField(item);
                if (field != null && field.isFocused) {
                    return field;
                }
            }
            return null;
        }

        private string TypedText() {
            var field = EditedField();
            return field != null ? field.text : "";
        }

        // The committed name once an edit ends. The rename commit refreshes every row, so the
        // name is re-read from the model-backed row matching the edited guid.
        private string AcceptedName() {
            if (_editingCreation) {
                var creation = CreationField(_bhv);
                var field = creation != null ? CreationNameField(creation) : null;
                return field != null ? field.text : null;
            }
            var item = FindByGuid(_editingGuid);
            if (item == null) {
                return null;
            }
            var nameField = ItemNameField(item);
            return nameField != null ? nameField.text : null;
        }

        private List<ProfileSelectItemBhv> Items() {
            var items = new List<ProfileSelectItemBhv>();
            if (_bhv != null) {
                items.AddRange(_bhv.GetComponentsInChildren<ProfileSelectItemBhv>(includeInactive: false));
            }
            return items;
        }

        private ProfileSelectItemBhv FindByGuid(uint guid) {
            foreach (var item in Items()) {
                if (!item.IsEmpty && item.GetProfileInstance() != null
                    && item.GetProfileInstance().ProfileGuid == guid) {
                    return item;
                }
            }
            return null;
        }

        private void Populate() {
            _builtSignature = Signature();
            var previous = _byKey;
            _byKey = new Dictionary<object, UIElement>();

            if (_bhv.IsCreateProfileWindowActive()) {
                PopulateCreation();
                return;
            }

            foreach (var item in Items()) {
                if (item.IsEmpty) {
                    // Empty slots die namelessly on every refresh; keyed to the instance, the
                    // element goes with them (creating lands in the create window anyway).
                    var captured = item;
                    Add(previous, captured.GetInstanceID(),
                        () => new ProfileItemElement(_bhv, () => captured));
                    continue;
                }
                uint guid = item.GetProfileInstance().ProfileGuid;
                Add(previous, "p:" + guid, () => new ProfileItemElement(_bhv, () => FindByGuid(guid)));
                // The row's side buttons (rename, delete, and whatever else the game shows),
                // resolved live by name so the elements survive the pooled row swap.
                foreach (var button in item.GetComponentsInChildren<Button>(includeInactive: false)) {
                    if (button.GetComponentInChildren<TMP_InputField>(includeInactive: true) != null) {
                        continue; // the name button, already the row element above
                    }
                    if (!UiText.HasAnyTextSource(button.gameObject)) {
                        continue;
                    }
                    string buttonName = button.name;
                    Add(previous, "p:" + guid + ":" + buttonName, () => new ActionElement(
                        () => UiText.FirstLabel(RowButton(guid, buttonName)?.gameObject),
                        S.RoleButton,
                        () => RowButton(guid, buttonName)?.onClick.Invoke()));
                }
            }

            var exit = ExitButton();
            if (exit != null) {
                Add(previous, "exit", () => new SelectableElement(exit));
            }
        }

        private Button RowButton(uint guid, string buttonName) {
            var item = FindByGuid(guid);
            if (item == null) {
                return null;
            }
            foreach (var button in item.GetComponentsInChildren<Button>(includeInactive: false)) {
                if (button.name == buttonName) {
                    return button;
                }
            }
            return null;
        }

        private Button ExitButton() {
            var panel = PanelField(_bhv);
            if (panel == null) {
                return null;
            }
            foreach (var button in panel.GetComponentsInChildren<Button>(includeInactive: false)) {
                if (button.GetComponentInParent<ProfileSelectItemBhv>() == null
                    && UiText.HasAnyTextSource(button.gameObject)) {
                    return button;
                }
            }
            return null;
        }

        // The create window, top to bottom as the game lays it out. Its name edit is already
        // active when the window opens (the game activates it), so entry lands on the title
        // while the echo reports the edit.
        private void PopulateCreation() {
            var creation = CreationField(_bhv);
            _root.Add(new StaticTextElement(() => GameLoc.TryGet("first_time_profile_creation_title")));
            _root.Add(new ActionElement(
                () => GameLoc.TryGet("profile_creation_name_label"),
                S.RoleEdit,
                creation.OnEditNameButtonPressed,
                extraBufferLines: () => BufferLine(GameLoc.TryGet("profile_creation_name_char_limit_label")),
                value: () => {
                    var field = CreationNameField(creation);
                    return field != null ? field.text : null;
                }));
            var language = LanguageField(creation);
            var dropdown = language != null
                ? language.GetComponentInChildren<TMP_Dropdown>(includeInactive: true) : null;
            if (dropdown != null) {
                _root.Add(new SelectableElement(dropdown,
                    () => GameLoc.TryGet("profile_creation_language_label")));
            }
            _root.Add(new StaticTextElement(() => GameLoc.TryGet("gdpr_dialog_desc_1")));
            var toggle = AnalyticsToggleField(creation);
            _root.Add(new SelectableElement(toggle, () => {
                var label = AnalyticsLabelField(creation);
                return label != null ? label.text : null;
            }));
            _root.Add(new StaticTextElement(() => GameLoc.TryGet("gdpr_dialog_desc_2")));
            var create = CreateButtonField(creation);
            _root.Add(new SelectableElement(create));
            foreach (var button in create.transform.parent.GetComponentsInChildren<Button>(includeInactive: false)) {
                if (button != create) {
                    _root.Add(new SelectableElement(button));
                }
            }
        }

        private static IEnumerable<string> BufferLine(string line) {
            if (!string.IsNullOrEmpty(line)) {
                yield return line;
            }
        }

        private UIElement Add(Dictionary<object, UIElement> previous, object key, Func<UIElement> make) {
            if (!previous.TryGetValue(key, out var element)) {
                element = make();
            }
            _byKey[key] = element;
            _root.Add(element);
            return element;
        }

        // Phase plus the live row instances (the game's refresh recycles every row through a
        // pool at equal count, so ids, not counts).
        private int Signature() {
            int signature = 17;
            if (_bhv.IsCreateProfileWindowActive()) {
                return signature * 31 + 1;
            }
            foreach (var item in Items()) {
                signature = signature * 31 + item.GetInstanceID();
            }
            return signature;
        }
    }
}
