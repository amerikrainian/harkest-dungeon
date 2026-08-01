using System;
using System.Collections.Generic;
using Assets.Code.Game;
using Assets.Code.Kingdom;
using Assets.Code.Kingdom.UI;
using Assets.Code.UI;
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
    /// The Kingdoms campaign menu, an additively loaded scene shown over the title menu (the mode
    /// stays MAIN_MENU and nothing lands on the screen stack). Three phases, mirroring the game's
    /// own surfaces: the entry menu (Continue Kingdom where a save exists, New Kingdom, back, the
    /// game-type description), the save-select list, and the creation wizard read one game step at
    /// a time. Escape drives the game's own back handler, which unwinds one surface at a time and
    /// finally returns to the title menu. While the name field is being typed into, the screen
    /// echoes keystrokes and re-reads the field when the edit ends. Elements are reused across
    /// rebuilds keyed to their live widget, so focus survives the wizard's staggered spawns.
    /// </summary>
    public sealed class KingdomMenuScreen : GameScreen {
        private static readonly AccessTools.FieldRef<KingdomMainMenuUIBhv, Button> ContinueButtonField =
            AccessTools.FieldRefAccess<KingdomMainMenuUIBhv, Button>("m_ContinueKingdomButton");
        private static readonly AccessTools.FieldRef<KingdomMainMenuUIBhv, Button> NewButtonField =
            AccessTools.FieldRefAccess<KingdomMainMenuUIBhv, Button>("m_NewKingdomButton");
        private static readonly AccessTools.FieldRef<KingdomMainMenuUIBhv, Button> ExitButtonField =
            AccessTools.FieldRefAccess<KingdomMainMenuUIBhv, Button>("m_ExitButton");
        private static readonly AccessTools.FieldRef<KingdomMainMenuUIBhv, KingdomSaveSelectUIBhv> SaveSelectField =
            AccessTools.FieldRefAccess<KingdomMainMenuUIBhv, KingdomSaveSelectUIBhv>("m_KingdomSaveSelectUIBhv");
        private static readonly AccessTools.FieldRef<KingdomMainMenuUIBhv, KingdomCreationFlowBhv> CreationFlowField =
            AccessTools.FieldRefAccess<KingdomMainMenuUIBhv, KingdomCreationFlowBhv>("m_KingdomCreationFlowBhv");
        private static readonly AccessTools.FieldRef<KingdomSaveSelectUIBhv, Button> LoadButtonField =
            AccessTools.FieldRefAccess<KingdomSaveSelectUIBhv, Button>("m_loadSaveBtn");
        private static readonly AccessTools.FieldRef<KingdomSaveSelectUIBhv, Button> SaveExitButtonField =
            AccessTools.FieldRefAccess<KingdomSaveSelectUIBhv, Button>("m_exitBtn");
        private static readonly AccessTools.FieldRef<KingdomCreationFlowBhv, Button> NextButtonField =
            AccessTools.FieldRefAccess<KingdomCreationFlowBhv, Button>("m_nextButton");
        private static readonly AccessTools.FieldRef<KingdomCreationFlowBhv, GameObject> PreviousButtonField =
            AccessTools.FieldRefAccess<KingdomCreationFlowBhv, GameObject>("m_previousButtonGO");

        private static KingdomMainMenuUIBhv _instance;

        /// <summary>The kingdoms menu component while its scene owns the title menu, else null.
        /// MainMenuScreen consults this so it stands down for the overlaying scene.</summary>
        internal static KingdomMainMenuUIBhv LiveInstance() {
            if (_instance == null) {
                _instance = UnityEngine.Object.FindObjectOfType<KingdomMainMenuUIBhv>(includeInactive: true);
            }
            return _instance != null && _instance.gameObject.activeInHierarchy ? _instance : null;
        }

        private readonly Core.Text.TypingEcho _echo;
        private KingdomMainMenuUIBhv _ui;
        private Container _root;
        private int _builtSignature;
        private KingdomNameElement _nameElement;
        private Dictionary<object, UIElement> _byWidget = new Dictionary<object, UIElement>();

        public KingdomMenuScreen(Action<string, bool> speak) {
            _echo = new Core.Text.TypingEcho(
                () => TextEntry.IsTyping && _nameElement != null, FieldText, speak);
        }

        public override string Name => GameLoc.TryGet("main_menu_kingdoms_label") ?? S.ScreenKingdoms;

        public override object ResolveTarget() {
            if (GameModeMgr.CurrentMode != GameModeType.MAIN_MENU || Singleton<GameModeMgr>.Instance.IsChangingState()) {
                return null;
            }
            return LiveInstance();
        }

        public override Container BuildRoot(object target) {
            _ui = (KingdomMainMenuUIBhv)target;
            _root = new RootContainer(ContainerShape.VerticalList,
                back: () => _ui.TryGoBack(usedEscStartButton: false));
            _byWidget.Clear();
            Populate();
            return _root;
        }

        public override bool OnUpdate(object target) {
            // The game's edit flow owns the keyboard while typing (the input manager pauses on
            // IsTyping); the echo speaks what that flow changes, and the edit's end requests a
            // re-announce so the name element reads back the accepted name.
            bool announce = _echo.Tick();
            if (Signature() != _builtSignature) {
                _root.Clear();
                Populate();
            }
            return announce;
        }

        private string FieldText() {
            var field = _nameElement != null ? _nameElement.Field : null;
            return field != null ? field.text : "";
        }

        private void Populate() {
            _builtSignature = Signature();
            _nameElement = null;
            var previous = _byWidget;
            _byWidget = new Dictionary<object, UIElement>();
            var flow = CreationFlowField(_ui);
            if (flow != null && flow.gameObject.activeInHierarchy) {
                PopulateWizard(flow, previous);
                return;
            }
            var saveSelect = SaveSelectField(_ui);
            if (saveSelect != null && saveSelect.gameObject.activeInHierarchy) {
                foreach (var item in saveSelect.GetComponentsInChildren<KingdomSaveItemBhv>(includeInactive: false)) {
                    var saveItem = item;
                    Add(previous, saveItem, () => new KingdomSaveElement(saveItem));
                }
                var load = LoadButtonField(saveSelect);
                Add(previous, load, () => new SelectableElement(load));
                var exit = SaveExitButtonField(saveSelect);
                Add(previous, exit, () => Labeled(exit, "Button_Back"));
                return;
            }
            var continueButton = ContinueButtonField(_ui);
            if (continueButton != null && continueButton.gameObject.activeInHierarchy) {
                Add(previous, continueButton, () => new SelectableElement(continueButton));
            }
            var newButton = NewButtonField(_ui);
            Add(previous, newButton, () => new SelectableElement(newButton));
            var exitButton = ExitButtonField(_ui);
            Add(previous, exitButton, () => Labeled(exitButton, "Button_Back"));
            Add(previous, "description", () => new StaticTextElement(() => GameLoc.TryGet("kingdom_game_type_description")));
        }

        private static readonly Dictionary<object, UIElement> NoReuse = new Dictionary<object, UIElement>();
        private HashSet<int> _builtSteps = new HashSet<int>();

        // One wizard step is active at a time (two only mid-transition: the incoming step shows
        // before the outgoing hides): the name step reads as a field, the disclaimer as its text,
        // the difficulty presets by their id's own name, the preset stat rows as readouts (bare
        // hover Selectables are never controls), and every remaining labeled selectable as
        // itself; the wizard's own Next/Previous close the list. Steps disable Next until the
        // step's choice is made, which reads as "unavailable".
        // Element reuse stops when the outgoing step disappears: the persistent Next/Previous
        // must orphan focus exactly once per step change, so the landing announces the new step
        // instead of staying mute on the button (and instead of announcing per transition frame).
        private void PopulateWizard(KingdomCreationFlowBhv flow, Dictionary<object, UIElement> previous) {
            var stepIds = new HashSet<int>();
            foreach (var step in flow.GetComponentsInChildren<IKingdomCreationFlowStep>(includeInactive: false)) {
                stepIds.Add(((Component)step).gameObject.GetInstanceID());
            }
            foreach (int id in _builtSteps) {
                if (!stepIds.Contains(id)) {
                    previous = NoReuse;
                    break;
                }
            }
            _builtSteps = stepIds;
            foreach (var step in flow.GetComponentsInChildren<IKingdomCreationFlowStep>(includeInactive: false)) {
                var stepObject = ((Component)step).gameObject;
                if (step is KingdomSelectNameInputBhv nameStep) {
                    _nameElement = (KingdomNameElement)Add(previous, nameStep, () => new KingdomNameElement(nameStep));
                } else if (step is KingdomGangDisclaimerStep) {
                    Add(previous, step, () => new StaticTextElement(() => UiText.AllText(stepObject)));
                }
                foreach (var selectable in stepObject.GetComponentsInChildren<Selectable>(includeInactive: false)) {
                    var widget = selectable;
                    if (widget is TMP_InputField || widget.GetComponent<SelectOnEmptyFallbackBhv>() != null) {
                        continue;
                    }
                    var difficulty = widget.GetComponent<KingdomDifficultyToggleBhv>();
                    if (difficulty != null) {
                        Add(previous, widget, () => new SelectableElement(widget, () => DifficultyLabel(difficulty)));
                        continue;
                    }
                    if (!UiText.HasAnyTextSource(widget.gameObject)) {
                        continue;
                    }
                    if (widget is KingdomSelectGangItem gangItem) {
                        Add(previous, widget, () => new KingdomGangElement(gangItem));
                        continue;
                    }
                    if (widget.GetType() == typeof(Selectable)) {
                        // A bare Selectable is a hover target over a labeled value, never a control.
                        Add(previous, widget, () => new ReadoutElement(
                            () => UiText.AllText(widget.gameObject),
                            detail: () => TooltipReader.Lines(widget.gameObject)));
                        continue;
                    }
                    Add(previous, widget, () => new SelectableElement(widget));
                }
            }
            var next = NextButtonField(flow);
            Add(previous, next, () => Labeled(next, "continue_label"));
            var previousObject = PreviousButtonField(flow);
            var previousButton = previousObject != null ? previousObject.GetComponent<Button>() : null;
            Add(previous, previousButton, () => Labeled(previousButton, "Button_Back"));
        }

        // A preset toggle's name from its definition id. The step's last toggle is the custom
        // slot, holding a COPY of the default definition rather than the library instance - it
        // takes the game's own "Custom" caption.
        private static string DifficultyLabel(KingdomDifficultyToggleBhv toggle) {
            var definition = toggle.Value;
            if (definition == null) {
                return null;
            }
            var library = SingletonMonoBehaviour<Assets.Code.Library.Library<string, KingdomDifficultyDefinition>>.Instance;
            bool custom = library == null || !ReferenceEquals(library.GetLibraryElement(definition.m_Id), definition);
            return GameLoc.TryGet("kingdom_difficulty_" + (custom ? "custom" : definition.m_Id));
        }

        private UIElement Add(Dictionary<object, UIElement> previous, object key, Func<UIElement> make) {
            if (!previous.TryGetValue(key, out var element)) {
                element = make();
            }
            _byWidget[key] = element;
            _root.Add(element);
            return element;
        }

        // The wizard's Next/Previous and the exit buttons are bare arrow icons; the game's own
        // caption for the motion stands in where the button carries no text.
        private static SelectableElement Labeled(Button button, string fallbackLocKey) {
            return new SelectableElement(button, () => {
                string label = UiText.FirstLabel(button != null ? button.gameObject : null);
                return string.IsNullOrEmpty(label) ? GameLoc.TryGet(fallbackLocKey) : label;
            });
        }

        // Phase and the live widgets it reads (pooled lists and step swaps replace instances at
        // equal count, so ids, not counts).
        private int Signature() {
            int signature = 17;
            var flow = CreationFlowField(_ui);
            if (flow != null && flow.gameObject.activeInHierarchy) {
                foreach (var step in flow.GetComponentsInChildren<IKingdomCreationFlowStep>(includeInactive: false)) {
                    var stepObject = ((Component)step).gameObject;
                    signature = signature * 31 + stepObject.GetInstanceID();
                    foreach (var selectable in stepObject.GetComponentsInChildren<Selectable>(includeInactive: false)) {
                        signature = signature * 31 + selectable.GetInstanceID();
                    }
                }
                return signature;
            }
            var saveSelect = SaveSelectField(_ui);
            if (saveSelect != null && saveSelect.gameObject.activeInHierarchy) {
                signature = signature * 31 + 1;
                foreach (var item in saveSelect.GetComponentsInChildren<KingdomSaveItemBhv>(includeInactive: false)) {
                    signature = signature * 31 + item.GetInstanceID();
                }
                return signature;
            }
            var continueButton = ContinueButtonField(_ui);
            return signature * 31 + (continueButton != null && continueButton.gameObject.activeInHierarchy ? 3 : 2);
        }
    }
}
