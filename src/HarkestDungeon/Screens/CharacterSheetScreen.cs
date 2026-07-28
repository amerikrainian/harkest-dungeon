using System.Collections.Generic;
using Assets.Code.Actor;
using Assets.Code.Quirk;
using Assets.Code.UI;
using Assets.Code.UI.Managers;
using Assets.Code.UI.Tooltips;
using Assets.Code.UI.Widgets;
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
    /// The hero sheet (character sheet). Layout: the hero header (Left/Right page through the
    /// party's heroes), then the sheet's tab selector, then the active tab's content. The
    /// Skills tab is read fully from the game model: health/stress/speed, the resistances, the
    /// quirks, each combat skill (Enter equips/unequips through the game's own button) with its
    /// full card as buffer lines, the combat item and trinket slots. Resistances, quirks,
    /// skills, combat items and trinkets are one horizontal row each (Left/Right within,
    /// Up/Down across). The Relationships tab reads
    /// each partner row with its affinity readout on the focus line; the other tabs read as a
    /// generic sweep of their panel's labeled selectables. Escape closes through the sheet's own
    /// teardown.
    /// </summary>
    public sealed class CharacterSheetScreen : GameScreen {
        private static readonly AccessTools.FieldRef<CharacterSheetUiBhv, CharacterSheetTopBarUiBhv> TopBarField =
            AccessTools.FieldRefAccess<CharacterSheetUiBhv, CharacterSheetTopBarUiBhv>("m_characterSheetTopBarBhv");
        private static readonly AccessTools.FieldRef<CharacterSheetUiBhv, CharacterSheetStatsUiBhv> StatsField =
            AccessTools.FieldRefAccess<CharacterSheetUiBhv, CharacterSheetStatsUiBhv>("m_characterSheetStatsBhv");
        private static readonly AccessTools.FieldRef<CharacterSheetStatsUiBhv, TextTooltipBhv> HealthTipField =
            AccessTools.FieldRefAccess<CharacterSheetStatsUiBhv, TextTooltipBhv>("m_healthTooltip");
        private static readonly AccessTools.FieldRef<CharacterSheetStatsUiBhv, TextTooltipBhv> StressTipField =
            AccessTools.FieldRefAccess<CharacterSheetStatsUiBhv, TextTooltipBhv>("m_stressTooltip");
        private static readonly AccessTools.FieldRef<CharacterSheetStatsUiBhv, TextTooltipBhv> SpeedTipField =
            AccessTools.FieldRefAccess<CharacterSheetStatsUiBhv, TextTooltipBhv>("m_speedTooltip");
        private static readonly AccessTools.FieldRef<CharacterSheetStatsUiBhv, List<GameObject>> CombatItemsField =
            AccessTools.FieldRefAccess<CharacterSheetStatsUiBhv, List<GameObject>>("m_combatItemsAdded");
        private static readonly AccessTools.FieldRef<CharacterSheetStatsUiBhv, Assets.Code.UI.Items.TrinketInventoryItemContainerBhv> TrinketsField =
            AccessTools.FieldRefAccess<CharacterSheetStatsUiBhv, Assets.Code.UI.Items.TrinketInventoryItemContainerBhv>("m_trinketContainerBhv");

        private struct ResistanceRow {
            public AccessTools.FieldRef<CharacterSheetStatsUiBhv, TextTooltipBhv> Tip;
            public string LocKey;
        }

        // The game's own pairing of resistance tooltip widget and name key (its PopulateTooltips).
        private static readonly ResistanceRow[] Resistances = {
            Res("m_resBleedTooltip", "resistance_bleed"),
            Res("m_resBlightTooltip", "resistance_blight"),
            Res("m_resBurnTooltip", "resistance_burn"),
            Res("m_resStunTooltip", "resistance_stun"),
            Res("m_resMoveTooltip", "resistance_move"),
            Res("m_resDebuffTooltip", "resistance_debuff"),
            Res("m_resDiseaseTooltip", "resistance_disease"),
            Res("m_resDeathsDoorTooltip", "resistance_deaths_door"),
            Res("m_resStressTooltip", "resistance_stress"),
        };

        private static ResistanceRow Res(string field, string locKey) => new ResistanceRow {
            Tip = AccessTools.FieldRefAccess<CharacterSheetStatsUiBhv, TextTooltipBhv>(field),
            LocKey = locKey,
        };

        private CharacterSheetUiBhv _sheet;
        private Container _root;
        private Container _items;
        private readonly List<int> _tabIndices = new List<int>(); // our tab position -> top-bar index
        private uint _builtGuid;
        private int _builtTab = -1;
        private int _builtCount = -1;

        public override string Name => S.ScreenHeroSheet;

        public override object ResolveTarget() {
            var top = StackTop.Object();
            _sheet = top == null ? null : top.GetComponent<CharacterSheetUiBhv>();
            return _sheet;
        }

        public override Container BuildRoot(object target) {
            var sheet = (CharacterSheetUiBhv)target;
            _root = new RootContainer(ContainerShape.VerticalList, back: Close);
            _root.Add(new HeroHeaderElement(sheet));

            RebuildTabIndices(sheet);
            if (_tabIndices.Count > 0) {
                _root.Add(new TabSelectorElement(
                    () => CurrentPosition(sheet),
                    () => _tabIndices.Count,
                    position => TabName(sheet, position),
                    position => {
                        sheet.SelectTabByIndex(_tabIndices[position]);
                        RebuildItems(sheet);
                    }));
            }

            _items = new Container(ContainerShape.VerticalList);
            _root.Add(_items);
            RebuildItems(sheet);
            return _root;
        }

        public override bool OnUpdate(object target) {
            var sheet = (CharacterSheetUiBhv)target;
            // A hero switch, a mouse tab click, or a content change (a quirk reroll) rebuilds the
            // items; announcing is left to whoever moved (the header/tab adjust spoke already, and
            // the router re-homes and re-announces an orphaned focus).
            if (sheet.ActorGuid != _builtGuid || ActiveTabIndex(sheet) != _builtTab || ContentCount(sheet) != _builtCount) {
                RebuildTabIndices(sheet);
                RebuildItems(sheet);
            }
            return false;
        }

        // ---- Tabs ----

        private void RebuildTabIndices(CharacterSheetUiBhv sheet) {
            _tabIndices.Clear();
            var topBar = TopBarField(sheet);
            for (int i = 0; i < topBar.TabCount; i++) {
                var tab = topBar.GetTab(i);
                if (tab.gameObject.activeSelf && tab.Button.interactable) {
                    _tabIndices.Add(i);
                }
            }
        }

        private int ActiveTabIndex(CharacterSheetUiBhv sheet) {
            var topBar = TopBarField(sheet);
            var active = sheet.ActiveTab;
            for (int i = 0; i < topBar.TabCount; i++) {
                if (topBar.GetTab(i).Tab == active) {
                    return i;
                }
            }
            return -1;
        }

        private int CurrentPosition(CharacterSheetUiBhv sheet) {
            int position = _tabIndices.IndexOf(ActiveTabIndex(sheet));
            return position < 0 ? 0 : position;
        }

        private string TabName(CharacterSheetUiBhv sheet, int position) {
            var tab = TopBarField(sheet).GetTab(_tabIndices[position]);
            // The same key the game composes for the sheet's title binding. The tab's own label
            // text is an unbound placeholder, so the tooltip is the visual caption fallback.
            string name = GameLoc.TryGet("character_sheet_tab_" + tab.Tab.ToString().ToLowerInvariant());
            if (name != null) {
                return name;
            }
            foreach (var line in TooltipReader.Lines(tab.gameObject)) {
                return line;
            }
            return tab.Tab.ToString();
        }

        // ---- Content ----

        private void RebuildItems(CharacterSheetUiBhv sheet) {
            _builtGuid = sheet.ActorGuid;
            _builtTab = ActiveTabIndex(sheet);
            _items.Clear();
            if (sheet.ActiveTab == CharacterSheetUiBhv.Tab.Skills) {
                BuildSkillsTab(sheet);
            } else {
                BuildGenericTab(sheet);
            }
            _builtCount = ContentCount(sheet);
        }

        // The variable-size piece of the built tree, checked per frame to catch rebuild triggers
        // the tab/hero checks miss (a quirk reroll, a panel repopulating).
        private int ContentCount(CharacterSheetUiBhv sheet) {
            if (sheet.ActiveTab == CharacterSheetUiBhv.Tab.Skills) {
                var actor = Actors.Get(sheet.ActorGuid);
                return actor?.QuirkContainer == null ? 0 : actor.QuirkContainer.GetInstances().Count;
            }
            int count = 0;
            foreach (var selectable in SweepPanel(sheet)) {
                count++;
            }
            return count;
        }

        private void BuildSkillsTab(CharacterSheetUiBhv sheet) {
            var stats = StatsField(sheet);

            _items.Add(new ReadoutElement(
                () => {
                    var actor = Actors.Get(sheet.ActorGuid);
                    return actor == null ? null : StatLine("status_bar_health", (int)actor.DisplayedHp, (int)actor.DisplayedHpMax);
                },
                detail: () => TooltipReader.LinesOf(HealthTipField(stats))));
            _items.Add(new ReadoutElement(
                () => {
                    var actor = Actors.Get(sheet.ActorGuid);
                    return actor == null ? null : StatLine("status_bar_stress", (int)actor.Stress, (int)actor.StressMax);
                },
                detail: () => TooltipReader.LinesOf(StressTipField(stats))));
            _items.Add(new ReadoutElement(
                () => {
                    var actor = Actors.Get(sheet.ActorGuid);
                    return actor == null ? null : S.SheetSpeed((int)actor.GetClampedStatValue(ActorStatType.SPEED));
                },
                detail: () => TooltipReader.LinesOf(SpeedTipField(stats))));

            var resistances = new Container(ContainerShape.HorizontalList, GameLoc.TryGet("character_sheet_resistances_title"));
            foreach (var row in Resistances) {
                var tipRef = row.Tip;
                string locKey = row.LocKey;
                resistances.Add(new ReadoutElement(
                    () => GameLoc.TryGet(locKey),
                    () => RowValue(tipRef(stats)),
                    () => TooltipReader.LinesOf(tipRef(stats))));
            }
            _items.Add(resistances);

            BuildQuirks(sheet);
            BuildSkills(sheet, stats);
            BuildTrinkets(stats);
        }

        private void BuildQuirks(CharacterSheetUiBhv sheet) {
            var actor = Actors.Get(sheet.ActorGuid);
            if (actor?.QuirkContainer == null) {
                return;
            }
            var quirks = new Container(ContainerShape.HorizontalList, GameLoc.TryGet("character_sheet_quirks_title"));
            // Elements address a quirk by category and index, re-reading the live container on
            // every speak, so a reroll never leaves a stale name behind.
            for (int kind = 0; kind < 3; kind++) {
                int count = 0;
                foreach (var quirk in QuirksOf(actor, kind)) {
                    int index = count++;
                    int quirkKind = kind;
                    quirks.Add(new ReadoutElement(
                        () => {
                            var live = QuirkAt(sheet.ActorGuid, quirkKind, index);
                            return live == null ? null
                                : QuirkDescription.GetNameString(live.Definition, Actors.Get(sheet.ActorGuid), appendRareIcon: false);
                        },
                        detail: () => QuirkDetail(sheet.ActorGuid, quirkKind, index)));
                }
            }
            if (!quirks.IsEmptyContainer) {
                _items.Add(quirks);
            }
        }

        private static IEnumerable<QuirkInstance> QuirksOf(ActorInstance actor, int kind) {
            foreach (var quirk in actor.QuirkContainer.GetInstances()) {
                var definition = quirk.Definition;
                // The disease/curse quirk has its own slot on the sheet; the rest split by the
                // game's own positive/negative tags.
                bool special = definition.IsDisease || definition.IsCurse;
                bool matches = kind == 0 ? !special && definition.IsPositive
                    : kind == 1 ? !special && definition.IsNegative
                    : special;
                if (matches) {
                    yield return quirk;
                }
            }
        }

        private static QuirkInstance QuirkAt(uint actorGuid, int kind, int index) {
            var actor = Actors.Get(actorGuid);
            if (actor?.QuirkContainer == null) {
                return null;
            }
            int i = 0;
            foreach (var quirk in QuirksOf(actor, kind)) {
                if (i++ == index) {
                    return quirk;
                }
            }
            return null;
        }

        private static IEnumerable<string> QuirkDetail(uint actorGuid, int kind, int index) {
            var quirk = QuirkAt(actorGuid, kind, index);
            if (quirk == null) {
                yield break;
            }
            string description = QuirkDescription.GetDescriptionString(quirk.Definition, Actors.Get(actorGuid));
            if (string.IsNullOrWhiteSpace(description)) {
                yield break;
            }
            foreach (var line in description.Split('\n')) {
                if (!string.IsNullOrWhiteSpace(line)) {
                    yield return line;
                }
            }
        }

        private void BuildSkills(CharacterSheetUiBhv sheet, CharacterSheetStatsUiBhv stats) {
            var actor = Actors.Get(sheet.ActorGuid);
            if (actor == null) {
                return;
            }
            var skills = new Container(ContainerShape.HorizontalList, GameLoc.TryGet("character_sheet_tab_skills"));
            foreach (var skillId in actor.GetUnlockedCharacterSheetCombatSkillIds()) {
                skills.Add(new SkillEquipElement(sheet, stats, skillId));
            }
            if (!skills.IsEmptyContainer) {
                _items.Add(skills);
            }
            var combatItems = new Container(ContainerShape.HorizontalList,
                GameLoc.TryGet("item_type_combat") ?? S.SheetCombatItems);
            foreach (var holder in CombatItemsField(stats)) {
                AddSlotButton(combatItems, holder.GetComponent<Assets.Code.UI.Items.CombatInventoryItemContainerBhv>()?.GetElement(0)?.gameObject, holder);
            }
            if (!combatItems.IsEmptyContainer) {
                _items.Add(combatItems);
            }
        }

        private void BuildTrinkets(CharacterSheetStatsUiBhv stats) {
            var container = TrinketsField(stats);
            if (container == null) {
                return;
            }
            var trinkets = new Container(ContainerShape.HorizontalList, GameLoc.TryGet("character_sheet_trinkets_title"));
            for (int i = 0; i < container.GetElementCount(); i++) {
                var slot = container.GetElement(i);
                AddSlotButton(trinkets, slot == null ? null : slot.gameObject, slot == null ? null : slot.gameObject);
            }
            if (!trinkets.IsEmptyContainer) {
                _items.Add(trinkets);
            }
        }

        private static void AddSlotButton(Container container, GameObject slot, GameObject rowScope) {
            if (slot == null || !slot.activeInHierarchy) {
                return;
            }
            var button = slot.GetComponent<Button>();
            if (button == null) {
                return;
            }
            var itemSlot = slot.GetComponent<Assets.Code.UI.Items.InventoryItemBhv>();
            container.Add(itemSlot != null
                ? new EquipSlotElement(itemSlot, button, rowScope)
                : new SelectableElement(button, null, rowScope));
        }

        // Any other tab: the panel's labeled selectables, with the panel's own text as the floor
        // when it has none (a tab that is purely informational). Relationship rows get their
        // dedicated element so the affinity readout rides the focus line.
        private void BuildGenericTab(CharacterSheetUiBhv sheet) {
            foreach (var selectable in SweepPanel(sheet)) {
                var relationship = selectable.GetComponent<CharacterSheetRelationshipActorUiBhv>();
                _items.Add(relationship != null
                    ? new RelationshipRowElement(relationship, selectable)
                    : (UIElement)new SelectableElement(selectable));
            }
            if (_items.IsEmptyContainer) {
                var panel = sheet.GetTabPanel(sheet.ActiveTab);
                _items.Add(new StaticTextElement(() => UiText.AllText(panel) ?? S.PanelEmpty));
            }
        }

        private IEnumerable<Selectable> SweepPanel(CharacterSheetUiBhv sheet) {
            var panel = sheet.GetTabPanel(sheet.ActiveTab);
            if (panel == null) {
                yield break;
            }
            foreach (var selectable in panel.GetComponentsInChildren<Selectable>(includeInactive: false)) {
                if (selectable is Scrollbar || selectable.GetComponent<SelectOnEmptyFallbackBhv>() != null) {
                    continue;
                }
                if (UiText.HasAnyTextSource(selectable.gameObject)) {
                    yield return selectable;
                }
            }
        }

        // ---- Reads ----

        private static string StatLine(string locKey, int current, int max) {
            string format = GameLoc.TryGet(locKey);
            return format == null ? current + "/" + max : string.Format(format, current, max);
        }

        // The row's displayed number (the game's own clamped/immune/dashed rendering), read from
        // the widget the tooltip sits on.
        private static string RowValue(TextTooltipBhv tip) {
            if (tip == null) {
                return null;
            }
            foreach (var tmp in tip.GetComponentsInChildren<TMPro.TMP_Text>(includeInactive: false)) {
                if (tmp.gameObject.name.StartsWith("Value", System.StringComparison.Ordinal)) {
                    return tmp.text;
                }
            }
            return null;
        }

        private static void Close() {
            SingletonMonoBehaviour<CommonUiBhv>.Instance.HideCharacterSheet();
        }
    }
}
