using System.Collections.Generic;
using Assets.Code.Data;
using Assets.Code.Game;
using Assets.Code.Quirk;
using Assets.Code.UI.Screens;
using Assets.Code.Utils;
using DD2A11y.Core.Nav;
using DD2A11y.Elements;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine;
using UnityEngine.UI;

namespace DD2A11y.Screens {
    /// <summary>
    /// The Field Hospital (a road node) and the inn physician - the same
    /// <c>HospitalScreenBhv</c>, named by its own composed title ("Field Hospital: Triage").
    /// Layout: the hero pager (Left/Right page the party; HP and stress in the line, the
    /// status tooltip in the buffer), the tab selector (Triage / Wellness / Pharmacy by the
    /// tab buttons' own captions - the game disables the active tab's button, which is how
    /// the current one is read - Left/Right clicking the game's own buttons), then the
    /// active tab's rows: Triage's cure-disease and heal buttons (each reading all its own
    /// texts - amount and cost - with lock explanations in the buffer), Wellness's treatable
    /// quirks ("selected" on the one the commands would treat) with the lock/remove buttons,
    /// or the game's no-treatable notice. The Pharmacy tab hands the whole surface to the
    /// shared store screen (the embedded store), and this screen stands down while it shows.
    /// Escape closes through the widget's own close-button handler.
    /// </summary>
    public sealed class HospitalScreen : GameScreen {
        private static readonly AccessTools.FieldRef<HospitalScreenBhv, DataContextBhv> ContextField =
            AccessTools.FieldRefAccess<HospitalScreenBhv, DataContextBhv>("m_dataContextBhv");
        private static readonly AccessTools.FieldRef<HospitalScreenBhv, Button[]> TabsField =
            AccessTools.FieldRefAccess<HospitalScreenBhv, Button[]>("m_tabs");
        private static readonly AccessTools.FieldRef<HospitalScreenBhv, Button> MinorHealField =
            AccessTools.FieldRefAccess<HospitalScreenBhv, Button>("m_minorHealBtn");
        private static readonly AccessTools.FieldRef<HospitalScreenBhv, Button> FullHealField =
            AccessTools.FieldRefAccess<HospitalScreenBhv, Button>("m_fullHealBtn");
        private static readonly AccessTools.FieldRef<HospitalScreenBhv, Button> CureDiseaseField =
            AccessTools.FieldRefAccess<HospitalScreenBhv, Button>("m_cureDiseaseButton");
        private static readonly AccessTools.FieldRef<HospitalScreenBhv, List<Button>> NegativeQuirkButtonsField =
            AccessTools.FieldRefAccess<HospitalScreenBhv, List<Button>>("m_negativeQuirksButtons");
        private static readonly AccessTools.FieldRef<HospitalScreenBhv, List<Button>> PositiveQuirkButtonsField =
            AccessTools.FieldRefAccess<HospitalScreenBhv, List<Button>>("m_positiveQuirksButtons");
        private static readonly AccessTools.FieldRef<HospitalScreenBhv, List<QuirkInstance>> NegativeQuirksField =
            AccessTools.FieldRefAccess<HospitalScreenBhv, List<QuirkInstance>>("m_negativeQuirks");
        private static readonly AccessTools.FieldRef<HospitalScreenBhv, List<QuirkInstance>> PositiveQuirksField =
            AccessTools.FieldRefAccess<HospitalScreenBhv, List<QuirkInstance>>("m_positiveQuirks");
        private static readonly AccessTools.FieldRef<HospitalScreenBhv, QuirkInstance> SelectedQuirkField =
            AccessTools.FieldRefAccess<HospitalScreenBhv, QuirkInstance>("m_selectedQuirkToTreat");
        private static readonly AccessTools.FieldRef<HospitalScreenBhv, Button> LockQuirksField =
            AccessTools.FieldRefAccess<HospitalScreenBhv, Button>("m_lockQuirksButton");
        private static readonly AccessTools.FieldRef<HospitalScreenBhv, Button> RemoveQuirksField =
            AccessTools.FieldRefAccess<HospitalScreenBhv, Button>("m_removeQuirksButton");
        private static readonly AccessTools.FieldRef<HospitalScreenBhv, GameObject> NoTreatableField =
            AccessTools.FieldRefAccess<HospitalScreenBhv, GameObject>("m_noTreatableQuirksObj");
        private static readonly AccessTools.FieldRef<HospitalScreenBhv, float> MinorHealPercentField =
            AccessTools.FieldRefAccess<HospitalScreenBhv, float>("m_minorHealPercent");
        private static readonly System.Reflection.MethodInfo MinorHealCostMethod =
            AccessTools.Method(typeof(HospitalScreenBhv), "GetMinorHealCost");
        private static readonly System.Reflection.MethodInfo FullHealCostMethod =
            AccessTools.Method(typeof(HospitalScreenBhv), "GetFullHealCost");
        private static readonly System.Reflection.MethodInfo FullHealMultiplierMethod =
            AccessTools.Method(typeof(HospitalScreenBhv), "GetFullHealCostMultiplier");

        private HospitalScreenBhv _hospital;
        private Container _root;
        private Container _content;
        private HospitalHeroElement _heroElement;
        private readonly Dictionary<Button, UIElement> _rows = new Dictionary<Button, UIElement>();
        private int _builtSignature;

        public override string Name {
            get {
                var context = _hospital == null ? null : ContextField(_hospital);
                string title = context == null ? null : context.GetStringValue("hospital_title");
                if (string.IsNullOrEmpty(title)) {
                    title = GameLoc.TryGet("hospital_title");
                }
                return string.IsNullOrEmpty(title) ? S.ScreenGeneric : title;
            }
        }

        public override object ResolveTarget() {
            var top = StackTop.Object();
            var hospital = top == null ? null : top.GetComponentInChildren<HospitalScreenBhv>(includeInactive: false);
            // The Pharmacy tab's embedded store is the shared store surface's to read.
            if (hospital != null && hospital.IsStoreScreenVisible) {
                hospital = null;
            }
            _hospital = hospital;
            return _hospital;
        }

        public override Container BuildRoot(object target) {
            var hospital = (HospitalScreenBhv)target;
            _root = new RootContainer(ContainerShape.VerticalList, back: hospital.HandleCloseButton);
            _rows.Clear();
            _heroElement = new HospitalHeroElement(hospital);
            _root.Add(_heroElement);
            _root.Add(new TabSelectorElement(CurrentTab, TabCount, TabName, SelectTab));
            _content = new Container(ContainerShape.VerticalList);
            Populate(hospital);
            _root.Add(_content);
            return _root;
        }

        public override bool OnUpdate(object target) {
            var hospital = (HospitalScreenBhv)target;
            if (Signature(hospital) != _builtSignature) {
                Populate(hospital);
            }
            return false;
        }

        /// <summary>Leave the Pharmacy by the game's own route - its first tab. The embedded
        /// store's done button is a husk here (it only hides the inventory panel), so the
        /// store screen's back comes through this instead.</summary>
        internal static void LeaveEmbeddedStore(HospitalScreenBhv hospital) {
            var tabs = TabsField(hospital);
            if (tabs != null && tabs.Length > 0 && tabs[0] != null) {
                tabs[0].onClick.Invoke();
            }
        }

        // ---- Tabs (the game disables the active tab's button) ----

        private List<Button> ActiveTabs() {
            var tabs = new List<Button>();
            if (_hospital != null) {
                foreach (var tab in TabsField(_hospital)) {
                    if (tab != null && tab.gameObject.activeInHierarchy) {
                        tabs.Add(tab);
                    }
                }
            }
            return tabs;
        }

        private int CurrentTab() {
            var tabs = ActiveTabs();
            for (int i = 0; i < tabs.Count; i++) {
                if (!tabs[i].interactable) {
                    return i;
                }
            }
            return 0;
        }

        private int TabCount() => ActiveTabs().Count;

        private string TabName(int index) {
            var tabs = ActiveTabs();
            return index < 0 || index >= tabs.Count ? null : UiText.FirstLabel(tabs[index].gameObject);
        }

        private void SelectTab(int index) {
            var tabs = ActiveTabs();
            if (index >= 0 && index < tabs.Count && tabs[index].interactable) {
                tabs[index].onClick.Invoke();
            }
        }

        // ---- The active tab's rows ----

        // Rows are reused per button across rebuilds (the game refreshes the lists on every
        // selection and hero page), so focus survives and a selection's own re-announce is
        // the only feedback.
        private void Populate(HospitalScreenBhv hospital) {
            _content.Clear();
            AddButton(CureDiseaseField(hospital));
            AddHealButton(hospital, MinorHealField(hospital), minor: true);
            AddHealButton(hospital, FullHealField(hospital), minor: false);
            AddQuirks(hospital, NegativeQuirkButtonsField(hospital), NegativeQuirksField(hospital));
            AddQuirks(hospital, PositiveQuirkButtonsField(hospital), PositiveQuirksField(hospital));
            AddCommandButton(LockQuirksField(hospital));
            AddCommandButton(RemoveQuirksField(hospital));
            var notice = NoTreatableField(hospital);
            if (notice != null && notice.activeInHierarchy) {
                _content.Add(new ReadoutElement(() => UiText.AllText(notice)));
            }
            _builtSignature = Signature(hospital);
        }

        // Amount and cost live in separate labels on one button, so the row reads all of its
        // own texts; lock explanations ("Upgrade Physician to unlock") ride the tooltip.
        private void AddButton(Button button) {
            if (button == null || !button.gameObject.activeInHierarchy) {
                return;
            }
            if (!_rows.TryGetValue(button, out var row)) {
                var scope = button.gameObject;
                row = new SelectableElement(button, () => UiText.AllText(scope));
                _rows[button] = row;
            }
            _content.Add(row);
        }

        // A heal row: its own amount label ("+8 HP", "+MAX"), then the price composed from
        // the model the way the store composes it - the game's own bound text carries a
        // strikethrough original price that would read as two numbers.
        private void AddHealButton(HospitalScreenBhv hospital, Button button, bool minor) {
            if (button == null || !button.gameObject.activeInHierarchy) {
                return;
            }
            if (!_rows.TryGetValue(button, out var row)) {
                var scope = button.gameObject;
                row = new SelectableElement(button, () => Core.Text.SpokenLine.Join(
                    UiText.ChildLabel(scope, "PctLabel"), HealCostText(hospital, minor)));
                _rows[button] = row;
            }
            _content.Add(row);
        }

        // The same inputs the game's own UpdateHealCost feeds CostDescription, minus the
        // strikethrough.
        private static string HealCostText(HospitalScreenBhv hospital, bool minor) {
            var cost = (Assets.Code.Cost.CostDefinition)(minor
                ? MinorHealCostMethod.Invoke(hospital, null) : FullHealCostMethod.Invoke(hospital, null));
            if (cost == null) {
                return null;
            }
            float multiplier;
            if (minor) {
                multiplier = Singleton<GameTypeMgr>.Instance.RunDataManager
                    .GetStatValue(Assets.Code.Run.RunStatType.STORE_COST_BUY_MULTIPLIER, cost.m_Id);
                multiplier *= MinorHealPercentField(hospital);
            } else {
                multiplier = (float)FullHealMultiplierMethod.Invoke(hospital, new object[] { cost });
            }
            return Assets.Code.Cost.CostDescription.GetStoreBuyDescription(cost, multiplier, showStrikethrough: false);
        }

        // The lock/remove commands caption themselves only in their tooltips (the visible row
        // is icon plus cost), so the verb leads and the cost follows.
        private void AddCommandButton(Button button) {
            if (button == null || !button.gameObject.activeInHierarchy) {
                return;
            }
            if (!_rows.TryGetValue(button, out var row)) {
                var scope = button.gameObject;
                row = new SelectableElement(button, () => Core.Text.SpokenLine.Join(
                    FirstLine(TooltipReader.Lines(scope)), UiText.AllText(scope)));
                _rows[button] = row;
            }
            _content.Add(row);
        }

        private void AddQuirks(HospitalScreenBhv hospital, List<Button> buttons, List<QuirkInstance> quirks) {
            if (buttons == null) {
                return;
            }
            for (int i = 0; i < buttons.Count; i++) {
                var button = buttons[i];
                if (button == null || !button.gameObject.activeInHierarchy) {
                    continue;
                }
                if (!_rows.TryGetValue(button, out var row)) {
                    int index = i;
                    row = new HospitalQuirkElement(button, () => {
                        var selected = SelectedQuirkField(hospital);
                        var list = quirks;
                        return selected != null && list != null && index < list.Count
                            && ReferenceEquals(selected, list[index]);
                    });
                    _rows[button] = row;
                }
                _content.Add(row);
            }
        }

        private static string FirstLine(IEnumerable<string> lines) {
            foreach (var line in lines) {
                if (!string.IsNullOrWhiteSpace(line)) {
                    return line;
                }
            }
            return null;
        }

        private int Signature(HospitalScreenBhv hospital) {
            int signature = 17;
            signature = signature * 31 + CurrentTab();
            if (_heroElement != null) {
                signature = signature * 31 + (int)_heroElement.ActorGuid;
            }
            signature = Accumulate(signature, CureDiseaseField(hospital));
            signature = Accumulate(signature, MinorHealField(hospital));
            signature = Accumulate(signature, FullHealField(hospital));
            signature = AccumulateAll(signature, NegativeQuirkButtonsField(hospital));
            signature = AccumulateAll(signature, PositiveQuirkButtonsField(hospital));
            signature = Accumulate(signature, LockQuirksField(hospital));
            signature = Accumulate(signature, RemoveQuirksField(hospital));
            var notice = NoTreatableField(hospital);
            signature = signature * 31 + (notice != null && notice.activeInHierarchy ? 1 : 0);
            return signature;
        }

        private static int Accumulate(int signature, Button button) {
            bool active = button != null && button.gameObject.activeInHierarchy;
            return signature * 31 + (active ? button.GetInstanceID() : 0);
        }

        private static int AccumulateAll(int signature, List<Button> buttons) {
            if (buttons == null) {
                return signature;
            }
            foreach (var button in buttons) {
                signature = Accumulate(signature, button);
            }
            return signature;
        }
    }
}
