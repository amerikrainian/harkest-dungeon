using System.Collections.Generic;
using Assets.Code.Actor;
using Assets.Code.Item;
using Assets.Code.Quirk;
using Assets.Code.UI;
using Assets.Code.UI.Managers;
using Assets.Code.UI.Screens;
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
    /// full card as buffer lines, the Skill Loadouts button (where the game offers it), the
    /// combat item and trinket slots. Resistances, quirks,
    /// skills, combat items and trinkets are one horizontal row each (Left/Right within,
    /// Up/Down across). The Relationships tab reads
    /// each partner row with its affinity readout on the focus line; the other tabs read as a
    /// generic sweep of their panel's labeled selectables. When the bag stands open beneath the
    /// sheet (the inn hub keeps it there; the game shows both side by side), it reads inline as
    /// a final section - the same shared panel the hub shows - and Enter on an empty equip slot
    /// follows the game's own response into it: the bag filters to the slot's type and focus
    /// lands on its first item. Escape closes through the sheet's own
    /// teardown. While the game's pick-a-slot holds a bag item for this sheet, the sheet keeps
    /// the surface over the locked bag with focus on the destination slot, Enter places through
    /// the slot's own submit, and Escape aborts the pick instead of closing.
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
        private static readonly AccessTools.FieldRef<CharacterSheetCosmeticsBhv, List<CharacterSheetCosmeticButtonBhv>> PaletteButtonsField =
            AccessTools.FieldRefAccess<CharacterSheetCosmeticsBhv, List<CharacterSheetCosmeticButtonBhv>>("m_paletteButtonsAdded");
        private static readonly AccessTools.FieldRef<CharacterSheetCosmeticsBhv, List<CharacterSheetCosmeticButtonBhv>> KitButtonsField =
            AccessTools.FieldRefAccess<CharacterSheetCosmeticsBhv, List<CharacterSheetCosmeticButtonBhv>>("m_kitButtonsAdded");
        private static readonly AccessTools.FieldRef<CharacterSheetCosmeticsBhv, List<CharacterSheetCosmeticButtonBhv>> SkinButtonsField =
            AccessTools.FieldRefAccess<CharacterSheetCosmeticsBhv, List<CharacterSheetCosmeticButtonBhv>>("m_skinButtonsAdded");
        private static readonly AccessTools.FieldRef<CharacterSheetCosmeticsBhv, IList<ResourceActorPalette>> PalettesField =
            AccessTools.FieldRefAccess<CharacterSheetCosmeticsBhv, IList<ResourceActorPalette>>("m_spawnedActorPalettes");
        private static readonly AccessTools.FieldRef<CharacterSheetCosmeticsBhv, IList<ResourceActorWeaponKit>> KitsField =
            AccessTools.FieldRefAccess<CharacterSheetCosmeticsBhv, IList<ResourceActorWeaponKit>>("m_spawnedActorWeaponKits");
        private static readonly AccessTools.FieldRef<CharacterSheetCosmeticsBhv, IList<ResourceActorSkin>> SkinsField =
            AccessTools.FieldRefAccess<CharacterSheetCosmeticsBhv, IList<ResourceActorSkin>>("m_SpawnedActorSkins");
        private static readonly AccessTools.FieldRef<CharacterSheetConditionsUiBhv, List<GameObject>> ConditionRowsField =
            AccessTools.FieldRefAccess<CharacterSheetConditionsUiBhv, List<GameObject>>("m_objectsAdded");
        private static readonly AccessTools.FieldRef<CharacterSheetConditionsUiBhv, GameObject> GoalContainerField =
            AccessTools.FieldRefAccess<CharacterSheetConditionsUiBhv, GameObject>("m_goalContainer");
        private static readonly AccessTools.FieldRef<CharacterSheetConditionsUiBhv, TMPro.TextMeshProUGUI> GoalLabelField =
            AccessTools.FieldRefAccess<CharacterSheetConditionsUiBhv, TMPro.TextMeshProUGUI>("m_goalLabel");
        private static readonly AccessTools.FieldRef<CharacterSheetConditionsUiBhv, GameObject> MemoriesContainerField =
            AccessTools.FieldRefAccess<CharacterSheetConditionsUiBhv, GameObject>("m_memoriesContainer");
        private static readonly AccessTools.FieldRef<CharacterSheetUiBhv, Button> SkillLoadoutButtonField =
            AccessTools.FieldRefAccess<CharacterSheetUiBhv, Button>("m_skillLoadoutButton");
        private static readonly System.Reflection.MethodInfo FindPlayerInventoryMethod =
            AccessTools.Method(typeof(CommonUiBhv), "FindPlayerInventoryInstance");

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

        // Frames a pressed slot's bag-browse may wait for the game's filter to land (it is
        // applied through a post-start callback) before the press falls back to a re-read.
        private const int PendingBagFrames = 120;

        private readonly TraditionalNavigator _navigator;
        private readonly InventoryPanel _bagPanel;
        private CharacterSheetUiBhv _sheet;
        private Container _root;
        private Container _items;
        private Container _trinkets;
        private Container _combatItems;
        private Container _bag;
        private InventoryUiBhv _bagInventory;
        private ItemType _pendingBagType; // the pressed slot's type while its browse is settling
        private int _pendingBagHeld;
        private bool _wasArmed;
        private readonly List<int> _tabIndices = new List<int>(); // our tab position -> top-bar index
        private uint _builtGuid;
        private int _builtTab = -1;
        private int _builtCount = -1;
        private int _builtTabsSignature;

        public CharacterSheetScreen(TraditionalNavigator navigator, System.Action<string, bool> speak) {
            _navigator = navigator;
            _bagPanel = new InventoryPanel(speak, navigator);
        }

        /// <summary>The grab key (Space / Shift+Space), routed here while this screen stands.</summary>
        public void ToggleGrab(UIElement current, bool takeOne) => _bagPanel.ToggleGrab(current, takeOne);

        public override string Name => S.ScreenHeroSheet;

        public override object ResolveTarget() {
            var top = StackTop.Object();
            _sheet = top == null ? null : top.GetComponent<CharacterSheetUiBhv>();
            // While the game's pick holds an item for this sheet, the bag above it is locked
            // whole (every row non-interactable) and the destinations are the sheet's own
            // slots - the sheet keeps the surface even though the bag is the stack top.
            if (_sheet == null && Game.SlotSelect.ArmedForSheet) {
                _sheet = Game.SlotSelect.Sheet();
            }
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
            // The bag standing open beneath the sheet reads inline as the last section, the
            // same shared panel the inn hub shows - the game displays both side by side.
            _bag = new Container(ContainerShape.VerticalList, S.ScreenInventory);
            _root.Add(_bag);
            _pendingBagType = null;
            RebuildBag();
            // Entered under an armed pick: the entry lands on the slot the held item is for,
            // the same first-empty-else-first choice the game's own arming selects.
            _wasArmed = Game.SlotSelect.ArmedForSheet;
            if (_wasArmed) {
                SeedFocus(PickDestinationSlot());
            }
            return _root;
        }

        // Route the entry descent (root -> items -> row -> slot) to the element, so the
        // attach lands there and the entry announcement reads it.
        private void SeedFocus(UIElement element) {
            if (element == null) {
                return;
            }
            foreach (var row in new[] { _trinkets, _combatItems }) {
                if (row == null) {
                    continue;
                }
                foreach (var child in row.Children) {
                    if (child == element) {
                        _root.SetFocusedChild(_items);
                        _items.SetFocusedChild(row);
                        row.SetFocusedChild(element);
                        return;
                    }
                }
            }
        }

        // The slot the armed pick is for: the game hands the held item to the matching row's
        // container as its SelectedItem and selects its first empty slot, else its first.
        private UIElement PickDestinationSlot() {
            UIElement first = null;
            foreach (var row in new[] { _trinkets, _combatItems }) {
                if (row == null) {
                    continue;
                }
                foreach (var child in row.Children) {
                    if (child is EquipSlotElement slot && slot.PickDestination) {
                        if (!slot.Occupied) {
                            return slot;
                        }
                        if (first == null) {
                            first = slot;
                        }
                    }
                }
            }
            return first;
        }

        public override bool OnUpdate(object target) {
            var sheet = (CharacterSheetUiBhv)target;
            // A pick arming while the sheet already stands (Enter on a bag item with this
            // sheet beneath) moves focus to the destination slot; the landing read is the
            // announcement. Before the entry announcement the move is silent - the entry
            // reads the landed path itself.
            bool armed = Game.SlotSelect.ArmedForSheet;
            if (armed && !_wasArmed) {
                var destination = PickDestinationSlot();
                if (destination != null) {
                    _navigator.Focus(destination, announce: EntryAnnounced);
                }
            }
            _wasArmed = armed;
            // A tab arriving or leaving late (the cosmetics tab activates a beat after the
            // sheet opens) refreshes the selector's index list; the selector reads it live.
            if (TabsSignature(sheet) != _builtTabsSignature) {
                RebuildTabIndices(sheet);
            }
            // A hero switch, a mouse tab click, or a content change (a quirk reroll) rebuilds the
            // items; announcing is left to whoever moved (the header/tab adjust spoke already, and
            // the router re-homes and re-announces an orphaned focus). The exception is the hero
            // ARRIVING: some open paths stamp the sheet's guid a frame after our entry (the road's
            // C key), so the announced header was a bare "hero" - re-announce with the hero in it.
            bool heroArrived = false;
            if (sheet.ActorGuid != _builtGuid || ActiveTabIndex(sheet) != _builtTab || ContentCount(sheet) != _builtCount) {
                heroArrived = _builtGuid == 0 && sheet.ActorGuid != 0;
                RebuildTabIndices(sheet);
                RebuildItems(sheet);
            }
            UpdateBag();
            return heroArrived;
        }

        // ---- The bag beneath ----

        // The open bag standing UNDER the sheet on the stack. The sheet resolves only as the
        // stack top, so an open bag is never above it - except during an armed pick, when the
        // game locks the bag whole and the destination slots take the arrows: the section
        // empties for that stretch and returns on disarm.
        private InventoryUiBhv BagBeneath() {
            if (Game.SlotSelect.ArmedForSheet) {
                return null;
            }
            var common = SingletonMonoBehaviour<CommonUiBhv>.Instance;
            if (!common.IsInventoryActive) {
                return null;
            }
            var screen = (UiScreenBhv)FindPlayerInventoryMethod.Invoke(common, null);
            return screen == null ? null : screen.GetWidget<InventoryUiBhv>();
        }

        private void RebuildBag() {
            _bag.Clear();
            _bagInventory = BagBeneath();
            if (_bagInventory != null) {
                _bagPanel.BuildInto(_bag, _bagInventory);
            }
        }

        // Per frame: the section follows the live stack (bag opening/closing, a pick arming),
        // the panel tracks its pooled slots, and a pressed slot's pending browse lands focus in
        // the bag once the game's filter has settled on the slot's type.
        private void UpdateBag() {
            if (BagBeneath() != _bagInventory) {
                RebuildBag();
            }
            if (_bagInventory != null) {
                _bagPanel.Update();
            }
            if (_pendingBagType == null) {
                return;
            }
            if (_bagInventory == null) {
                _pendingBagType = null;
                Plugin.Log.LogWarning("CharacterSheetScreen: bag left the stack before the slot's browse landed");
                return;
            }
            var filter = _bagInventory.FindNonDefaultFilterWithSubType(_pendingBagType);
            if (filter == null || _bagInventory.CurrentFilter != filter) {
                if (++_pendingBagHeld < PendingBagFrames) {
                    return;
                }
                // The filter never landed (the game refused for a reason the press's guards
                // missed): re-read the slot so the press is not silent, and log the miss.
                _pendingBagType = null;
                Plugin.Log.LogWarning("CharacterSheetScreen: bag filter never matched the pressed slot");
                if (_navigator.Current != null) {
                    _navigator.Focus(_navigator.Current, announce: true);
                }
                return;
            }
            _pendingBagType = null;
            // The landing the game's own flow selects: the first item under the applied
            // filter, else the filter tab (which reads the applied filter's name).
            var target = _bagPanel.FirstItemElement() ?? _bagPanel.FilterTab;
            if (target != null) {
                _navigator.Focus(target, announce: EntryAnnounced);
            }
        }

        private void BeginBagBrowse(ItemType type) {
            _pendingBagType = type;
            _pendingBagHeld = 0;
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
            _builtTabsSignature = TabsSignature(sheet);
        }

        private static int TabsSignature(CharacterSheetUiBhv sheet) {
            var topBar = TopBarField(sheet);
            int signature = 0;
            for (int i = 0; i < topBar.TabCount; i++) {
                var tab = topBar.GetTab(i);
                if (tab.gameObject.activeSelf && tab.Button.interactable) {
                    signature |= 1 << i;
                }
            }
            return signature;
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
            _trinkets = null;
            _combatItems = null;
            _items.Clear();
            if (sheet.ActiveTab == CharacterSheetUiBhv.Tab.Skills) {
                BuildSkillsTab(sheet);
            } else if (sheet.ActiveTab == CharacterSheetUiBhv.Tab.Cosmetic) {
                BuildCosmeticsTab(sheet);
            } else if (sheet.ActiveTab == CharacterSheetUiBhv.Tab.Conditions) {
                BuildConditionsTab(sheet);
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
            if (sheet.ActiveTab == CharacterSheetUiBhv.Tab.Conditions) {
                var panel = sheet.GetComponentInChildren<CharacterSheetConditionsUiBhv>(includeInactive: true);
                if (panel == null) {
                    return 0;
                }
                var goal = GoalContainerField(panel);
                int rows = ConditionRowsField(panel).Count + (goal != null && goal.activeInHierarchy ? 1 : 0);
                foreach (var memory in MemoryRows(panel)) {
                    rows++;
                }
                return rows;
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
            // The game shows the button only where loadouts apply (editable skills at the
            // crossroads/inn); its caption lives on the container beside it.
            var loadoutButton = SkillLoadoutButtonField(sheet);
            if (loadoutButton != null && loadoutButton.gameObject.activeInHierarchy) {
                _items.Add(new SelectableElement(loadoutButton, null, loadoutButton.transform.parent.gameObject));
            }
            var combatItems = new Container(ContainerShape.HorizontalList,
                GameLoc.TryGet("item_type_combat") ?? S.SheetCombatItems);
            foreach (var holder in CombatItemsField(stats)) {
                AddSlotButton(combatItems, holder.GetComponent<Assets.Code.UI.Items.CombatInventoryItemContainerBhv>()?.GetElement(0)?.gameObject, holder);
            }
            if (!combatItems.IsEmptyContainer) {
                _items.Add(combatItems);
                _combatItems = combatItems;
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
                _trinkets = trinkets;
            }
        }

        private void AddSlotButton(Container container, GameObject slot, GameObject rowScope) {
            if (slot == null || !slot.activeInHierarchy) {
                return;
            }
            var button = slot.GetComponent<Button>();
            if (button == null) {
                return;
            }
            var itemSlot = slot.GetComponent<Assets.Code.UI.Items.InventoryItemBhv>();
            container.Add(itemSlot != null
                ? new EquipSlotElement(itemSlot, button, rowScope, BeginBagBrowse)
                : new SelectableElement(button, null, rowScope));
        }

        // The cosmetics rows, each titled by the game's own section label: the palettes, the
        // weapon kits, and any hero skins, one named swatch per unlocked cosmetic (skins list
        // locked ones too, with the unlock hint in the buffer). The game offers this tab only
        // at the inn and the crossroads.
        private void BuildCosmeticsTab(CharacterSheetUiBhv sheet) {
            var cosmetics = sheet.GetComponentInChildren<CharacterSheetCosmeticsBhv>(includeInactive: true);
            if (cosmetics == null) {
                BuildGenericTab(sheet);
                return;
            }
            AddCosmeticRow(GameLoc.TryGet("hero_palette_label"), PaletteButtonsField(cosmetics),
                index => CosmeticName((System.Collections.IList)PalettesField(cosmetics), index));
            AddCosmeticRow(GameLoc.TryGet("weapon_kit_label"), KitButtonsField(cosmetics),
                index => CosmeticName((System.Collections.IList)KitsField(cosmetics), index));
            AddCosmeticRow(GameLoc.TryGet("hero_skin_label"), SkinButtonsField(cosmetics),
                index => CosmeticName((System.Collections.IList)SkinsField(cosmetics), index));
        }

        private void AddCosmeticRow(string title, List<CharacterSheetCosmeticButtonBhv> buttons,
                System.Func<int, string> nameOf) {
            var row = new Container(ContainerShape.HorizontalList, title);
            foreach (var button in buttons) {
                if (button != null && button.gameObject.activeInHierarchy) {
                    row.Add(new CosmeticButtonElement(button, nameOf));
                }
            }
            if (!row.IsEmptyContainer) {
                _items.Add(row);
            }
        }

        // A cosmetic's display name: the loc string keyed by the resource asset's own name,
        // the same lookup the game's tooltip does.
        private static string CosmeticName(System.Collections.IList resources, int index) {
            if (resources == null || index < 0 || index >= resources.Count) {
                return null;
            }
            var resource = resources[index] as Object;
            return resource == null ? null : GameLoc.TryGet(resource.name) ?? resource.name;
        }

        // The conditions tab: one row per condition (its source - the granting inn, the
        // trophy - is the tooltip in the buffer), then the hero's run goal and memories under
        // the game's own section titles. Empty sections vanish rather than reading as stops,
        // matching the inspector's conditions row.
        private void BuildConditionsTab(CharacterSheetUiBhv sheet) {
            var panel = sheet.GetComponentInChildren<CharacterSheetConditionsUiBhv>(includeInactive: true);
            if (panel == null) {
                BuildGenericTab(sheet);
                return;
            }
            foreach (var row in ConditionRowsField(panel)) {
                var captured = row;
                _items.Add(new ReadoutElement(
                    () => captured == null ? null : UiText.AllText(captured),
                    detail: () => TooltipReader.Lines(captured)));
            }
            var goalContainer = GoalContainerField(panel);
            if (goalContainer != null && goalContainer.activeInHierarchy) {
                var goals = new Container(ContainerShape.VerticalList, GameLoc.TryGet("hero_objectives_title_label"));
                var label = GoalLabelField(panel);
                goals.Add(new HeroGoalElement(() => Actors.Get(sheet.ActorGuid), label.gameObject, nameHero: false));
                _items.Add(goals);
            }
            var memories = new Container(ContainerShape.VerticalList, GameLoc.TryGet("character_sheet_memories_label"));
            foreach (var row in MemoryRows(panel)) {
                var captured = row;
                memories.Add(new ReadoutElement(
                    () => captured == null ? null : UiText.AllText(captured),
                    detail: () => TooltipReader.Lines(captured)));
            }
            if (!memories.IsEmptyContainer) {
                _items.Add(memories);
            }
            if (_items.IsEmptyContainer) {
                _items.Add(new StaticTextElement(() => S.PanelEmpty));
            }
        }

        // The memory rows live under the memories section's inner container, filled by their
        // own binder; the section's title and separators are not rows.
        private static IEnumerable<GameObject> MemoryRows(CharacterSheetConditionsUiBhv panel) {
            var container = MemoriesContainerField(panel);
            if (container == null || !container.activeInHierarchy) {
                yield break;
            }
            var inner = FindChild(container.transform, "Container");
            if (inner == null) {
                yield break;
            }
            foreach (Transform row in inner) {
                if (row.gameObject.activeInHierarchy && UiText.HasAnyTextSource(row.gameObject)) {
                    yield return row.gameObject;
                }
            }
        }

        private static Transform FindChild(Transform root, string name) {
            foreach (var child in root.GetComponentsInChildren<Transform>(includeInactive: false)) {
                if (child.name == name) {
                    return child;
                }
            }
            return null;
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

        private void Close() {
            // An armed grab in the inline bag drops first, matching the other bag surfaces.
            if (_bagPanel.GrabArmed) {
                _bagPanel.CancelGrab();
                return;
            }
            // An armed pick with the bag standing above the sheet: Escape aborts the pick
            // (the game's own back does the same) and the unlocked bag takes the surface -
            // its re-announce is the feedback. When the pick opened the sheet itself (it is
            // the stack top), the game's hide below cancels the pick as part of closing.
            if (Game.SlotSelect.ArmedForSheet
                && StackTop.Object()?.GetComponent<CharacterSheetUiBhv>() == null) {
                Game.SlotSelect.Cancel();
                return;
            }
            SingletonMonoBehaviour<CommonUiBhv>.Instance.HideCharacterSheet();
        }
    }
}
