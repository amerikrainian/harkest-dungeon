using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DD2A11y.Core.Strings {
    /// <summary>
    /// Central table for text the MOD itself authors and speaks (never game content, which is read
    /// live and already localized). Every authored word lives in <see cref="Defaults"/> as a key and
    /// its English value; the members below are typed accessors reading through
    /// <see cref="Translation"/>, so a loaded translation file overrides any value at speak time.
    /// Grammar lives in the values: "{0}"-style slots carry word order, and '|'-separated forms carry
    /// plurals (picked by the translation's plural rule - see <see cref="PluralRules"/>). Game-content
    /// reading must never route through here.
    /// </summary>
    public static class Strings {
        private static KeyValuePair<string, string> D(string key, string value)
            => new KeyValuePair<string, string>(key, value);

        /// <summary>Every authored string, in template order: the key a translation file addresses
        /// and its English default. The comments are the translation context: where the line is
        /// spoken, what fills each {0} slot, and the part of speech where a bare English word is
        /// ambiguous. Values are spoken by a screen reader: terse, lowercase unless shown otherwise,
        /// no decorative punctuation.</summary>
        internal static readonly KeyValuePair<string, string>[] Defaults = {
            // Spoken once when the mod finishes initializing at game launch; {0} = the mod version.
            D("ModLoaded", "DD2A11y {0} loaded"),

            // Screen names, spoken when the mod takes over a screen. Match the game's own word for
            // the screen where it has one (the settings screen's title, the pause header).
            D("ScreenMainMenu", "main menu"),
            D("ScreenSettings", "settings"),
            D("ScreenPauseMenu", "pause menu"),
            D("ScreenCrossroads", "crossroads"),
            // A generic confirmation dialog with no title of its own.
            D("ScreenDialog", "dialog"),
            // A game screen the mod reads generically, when it shows no title text to reuse.
            D("ScreenGeneric", "screen"),
            // A hero's character sheet (stats, quirks, skills). The game calls it "hero sheet".
            D("ScreenHeroSheet", "hero sheet"),
            // A battle. Noun.
            D("ScreenCombat", "combat"),
            // The road-fork route menu, shown while the coach waits at a junction.
            D("ScreenFork", "fork"),
            // The inn hub, when the inn's own name is unavailable. Noun.
            D("ScreenInn", "inn"),
            // The inventory's used-slot readout; {0} = the game's own count text ("5 / 20").
            D("InventorySlots", "slots {0}"),
            // The free bag capacity, collapsed to one line; {0} = how many slots are empty.
            D("InventoryEmptySlots", "{0} empty slot|{0} empty slots"),
            // Spoken after the sort button runs; the game's one sort orders by item type,
            // then name.
            D("InventorySorted", "sorted by type"),
            // Outcome of the discard key: the whole focused stack was thrown away; {0} = the
            // item's name.
            D("ItemDiscarded", "discarded {0}"),
            // Outcome of the same key while a seller is open (the game sells one item per
            // press instead); {0} = the item's name.
            D("ItemSold", "sold {0}"),

            // Control type words, spoken after a control's label ("Continue, button"). Nouns.
            D("RoleButton", "button"),
            D("RoleToggle", "toggle"),
            D("RoleSlider", "slider"),
            D("RoleDropdown", "dropdown"),
            D("RoleTab", "tab"),
            // The hero sheet's header line (the hero's name); Left/Right there switch heroes.
            D("RoleHero", "hero"),

            // Control state words.
            // A toggle that is checked / unchecked.
            D("StatusOn", "on"),
            D("StatusOff", "off"),
            // Spoken when adjusting a slider that is already at its end.
            D("StatusMinimum", "minimum"),
            D("StatusMaximum", "maximum"),
            // The currently chosen entry (a tab, a hero already in the party).
            D("StatusSelected", "selected"),
            // A control present but not usable right now (a grayed-out button). Adjective.
            D("StatusUnavailable", "unavailable"),
            // A slider value; {0} = the number.
            D("ValuePercent", "{0} percent"),

            // Crossroads (the pre-run hub). Section names for the two hero strips; the game shows
            // these visually with no header string to reuse. Nouns.
            D("CrossroadsParty", "party"),
            D("CrossroadsRoster", "roster"),
            // A hero slot with no hero in it.
            D("CrossroadsEmptySlot", "empty slot"),
            // A roster hero currently placed in the party, appended to their readout.
            D("CrossroadsInParty", "in party"),

            // Grab-and-place, shared by the crossroads hero move and the inventory stack move.
            // Spoken when something is picked up to move; {0} = the hero's or item's name.
            D("Grabbed", "grabbed {0}"),
            // Spoken when a grab is dropped without placing (the same slot again, Escape, or
            // the source changed underneath).
            D("GrabCancelled", "grab cancelled"),
            // Spoken when the grabbed hero or item cannot be placed on the focused target.
            D("CannotPlace", "cannot place here"),

            // Hero sheet. The speed stat readout; {0} = the number. The game shows this stat as a
            // bare icon with no name string to reuse.
            D("SheetSpeed", "speed {0}"),
            // A tab or panel with nothing in it yet (the relationships tab before a run), where
            // the game shows blank space. Adjective.
            D("PanelEmpty", "empty"),

            // Combat. The battle status line, spoken on turn changes and as the header readout;
            // {0} = the round number, {1} = the acting combatant's name.
            D("CombatHeader", "round {0}, {1}"),
            // Section name for the enemy strip. Noun (the party strip reuses CrossroadsParty).
            D("CombatEnemies", "enemies"),
            // The torch/flame meter readout; {0} = its value. The game shows it as a bare icon.
            D("CombatTorch", "torch {0}"),
            // Spoken when a chosen skill starts waiting for its target.
            D("CombatSelectTarget", "select target"),
            // Appended to a combatant the chosen skill can hit / cannot hit.
            D("CombatTargetValid", "valid target"),
            D("CombatTargetInvalid", "invalid target"),
            // Spoken when target selection is cancelled back to skill choice.
            D("CombatTargetCancelled", "target cancelled"),
            // Battle events, announced as they happen and kept in the combat buffer.
            // Damage to any combatant; {0} = who, {1} = the amount (2 or more).
            D("CombatTookDamage", "{0} took {1} damage"),
            // Damage of exactly 1, where the number is noise.
            D("CombatTookDamageOne", "{0} took damage"),
            // A combatant died; {0} = who.
            D("CombatDied", "{0} died"),
            // A hero fell to death's door; {0} = who.
            D("CombatDeathsDoor", "{0} at death's door"),
            // An enemy acted; {0} = the enemy, {1} = the skill's name, {2} = its target.
            D("CombatUsedSkill", "{0} used {1} on {2}"),
            // A combatant received a token or a damage-over-time; {0} = who, {1} = what (the
            // game's own token/dot name, with its own count format when stacked).
            D("CombatGained", "{0} gained {1}"),
            // A combatant shrugged off an applied effect; {0} = who, {1} = what was resisted.
            D("CombatResisted", "{0} resisted {1}"),
            // The upcoming acting order, current actor first; {0} = the combatant names, joined.
            D("CombatTurnOrder", "turn order, {0}"),
            // Multi-wave fights; {0} = the current battle number, {1} = the total.
            D("CombatBattleCount", "battle {0} of {1}"),
            // Damage that was a critical hit; {0} = who was hit, {1} = the amount.
            D("CombatTookDamageCrit", "{0} took {1} damage, crit"),
            // A combatant recovered HP; {0} = who, {1} = the amount.
            D("CombatHealed", "{0} healed {1}"),
            // A critical heal; {0} = who, {1} = the amount.
            D("CombatHealedCrit", "{0} healed {1}, crit"),
            // An attack missed; {0} = the attacker, {1} = the intended target.
            D("CombatMissed", "{0} missed {1}"),
            // The target evaded an attack that would have hit; {0} = the target.
            D("CombatDodged", "{0} dodged"),
            // Stress gained; {0} = the hero, {1} = the amount.
            D("CombatStressed", "{0} gained {1} stress"),
            // Stress relieved; {0} = the hero, {1} = the amount.
            D("CombatStressHealed", "{0} lost {1} stress"),
            // A hero at death's door survived a hit that would have killed; {0} = the hero.
            D("CombatDeathBlowResisted", "{0} resisted the death blow"),
            // A token was used up powering its effect; {0} = who, {1} = the token's name.
            D("CombatSpent", "{0} spent {1}"),
            // A token was destroyed by an enemy effect; {0} = who, {1} = the token's name.
            D("CombatLost", "{0} lost {1}"),
            // A combatant was wounded / had a wound healed; {0} = who.
            D("CombatWounded", "{0} wounded"),
            D("CombatWoundHealed", "{0} wound healed"),
            // The relationship meter between two heroes moved; {0} and {1} = the heroes,
            // {2} = the signed change ("+1").
            D("CombatAffinity", "{0} and {1}, affinity {2}"),
            // A speech-bubble line a combatant says; {0} = the speaker, {1} = the game's line.
            D("BarkLine", "{0}: {1}"),

            // Toasts (corner notifications). A tutorial toast; {0} = the game's tutorial title.
            D("ToastTutorial", "tutorial, {0}"),
            // A hero completed their run objective; {0} = the hero.
            D("ToastObjective", "{0} objective complete"),

            // Driving. Spoken (with the fork cue) when a junction comes into range; the route
            // menu follows when the coach stops there.
            D("RoadForkAhead", "fork ahead"),
            // Route directions at a fork. The game shows arrows with no words to reuse.
            D("RouteLeft", "left"),
            D("RouteForward", "forward"),
            D("RouteRight", "right"),
            // Heroes whose route preference matches this route; {0} = their names, joined.
            D("RoutePreferredBy", "preferred by {0}"),

            // Words for the game's inline effect glyphs in skill and tooltip text, where the icon
            // itself carries the meaning. Nouns.
            // A healing effect.
            D("SpriteHeal", "heal"),
            // A positive status effect.
            D("SpriteBuff", "buff"),
            // A negative status effect.
            D("SpriteDebuff", "debuff"),
            // Stress damage.
            D("SpriteStress", "stress"),
            // A disease effect.
            D("SpriteDisease", "disease"),
            // The speed stat (trinket and buff lines show it as a bare icon).
            D("SpriteSpeed", "speed"),
            // The health stat, as the game abbreviates it in its own status text.
            D("SpriteHealth", "HP"),

            // Buffer review (Ctrl plus arrows). The buffer holding the focused control's detail
            // lines (its tooltips). Noun naming that buffer.
            D("BufferControl", "control"),
            // The battle-event log buffer, non-empty only during combat. Noun naming that buffer.
            D("BufferCombat", "combat"),
            // Spoken when a buffer key is pressed and every buffer is empty.
            D("BufferNone", "no buffer lines"),
            // Switching to a buffer: {0} = the buffer's name, {1} = its current line.
            D("BufferLine", "{0}: {1}"),

            // Input action names, for a future keybindings reader. Short imperative phrases.
            D("InputNavigateUp", "Navigate up"),
            D("InputNavigateDown", "Navigate down"),
            D("InputNavigateLeft", "Navigate left"),
            D("InputNavigateRight", "Navigate right"),
            D("InputNextPanel", "Next panel"),
            D("InputPrevPanel", "Previous panel"),
            D("InputActivate", "Activate control"),
            D("InputBack", "Back"),
            D("InputJumpFirst", "Jump to first"),
            D("InputJumpLast", "Jump to last"),
            D("InputBufferNext", "Next buffer"),
            D("InputBufferPrev", "Previous buffer"),
            D("InputBufferLineNext", "Next buffer line"),
            D("InputBufferLinePrev", "Previous buffer line"),
            D("InputGrab", "Grab or place"),
            D("InputPlaceOne", "Place one from a grabbed stack"),
            D("InputInspect", "Open hero sheet"),
            D("InputDiscard", "Discard item"),
        };

        private static readonly Dictionary<string, string> English = BuildEnglish();

        private static Dictionary<string, string> BuildEnglish() {
            var map = new Dictionary<string, string>(Defaults.Length, System.StringComparer.Ordinal);
            foreach (var entry in Defaults) {
                map[entry.Key] = entry.Value;
            }
            return map;
        }

        /// <summary>Whether the table defines this key (used by <see cref="Translation.Load"/> to
        /// reject typo'd entries).</summary>
        public static bool DefinesKey(string key) => English.ContainsKey(key);

        /// <summary>The full translator template: a header, the plural rule, then every key and its
        /// English default in table order. lang/en.txt is pinned to this by a test.</summary>
        public static string DumpTemplate() {
            var sb = new StringBuilder();
            sb.Append("# DD2A11y translation template. Copy to <language>.txt and translate the values.\n");
            sb.Append("# Lines starting with # are comments. Format: key = value.\n");
            sb.Append("# {0}-style slots are filled at runtime; keep them, reorder freely.\n");
            sb.Append("# '|' separates plural forms, chosen by the _plural rule below.\n");
            sb.Append("_plural = english\n");
            foreach (var entry in Defaults) {
                sb.Append(entry.Key).Append(" = ").Append(entry.Value).Append('\n');
            }
            return sb.ToString();
        }

        private static string T(string key) => Translation.Get(key, English[key]);

        private static string F(string key, params object[] args)
            => string.Format(CultureInfo.InvariantCulture, T(key), args);

        private static string P(string key, int count) {
            string value = T(key);
            string[] forms = value.Split('|');
            int index = Translation.Overrides(key) ? Translation.PluralIndex(count) : PluralRules.English(count);
            if (index >= forms.Length) {
                index = forms.Length - 1;
            }
            return string.Format(CultureInfo.InvariantCulture, forms[index], count);
        }

        public static string ModLoaded(string version) => F("ModLoaded", version);

        public static string ScreenMainMenu => T("ScreenMainMenu");
        public static string ScreenSettings => T("ScreenSettings");
        public static string ScreenPauseMenu => T("ScreenPauseMenu");
        public static string ScreenCrossroads => T("ScreenCrossroads");
        public static string ScreenDialog => T("ScreenDialog");
        public static string ScreenGeneric => T("ScreenGeneric");
        public static string ScreenHeroSheet => T("ScreenHeroSheet");
        public static string ScreenCombat => T("ScreenCombat");
        public static string ScreenFork => T("ScreenFork");
        public static string ScreenInn => T("ScreenInn");
        public static string InventorySlots(string count) => F("InventorySlots", count);
        public static string InventoryEmptySlots(int count) => P("InventoryEmptySlots", count);
        public static string InventorySorted => T("InventorySorted");
        public static string ItemDiscarded(string item) => F("ItemDiscarded", item);
        public static string ItemSold(string item) => F("ItemSold", item);

        public static string RoleButton => T("RoleButton");
        public static string RoleToggle => T("RoleToggle");
        public static string RoleSlider => T("RoleSlider");
        public static string RoleDropdown => T("RoleDropdown");
        public static string RoleTab => T("RoleTab");
        public static string RoleHero => T("RoleHero");

        public static string StatusOn => T("StatusOn");
        public static string StatusOff => T("StatusOff");
        public static string StatusMinimum => T("StatusMinimum");
        public static string StatusMaximum => T("StatusMaximum");
        public static string StatusSelected => T("StatusSelected");
        public static string StatusUnavailable => T("StatusUnavailable");
        public static string ValuePercent(int value) => F("ValuePercent", value);

        public static string CrossroadsParty => T("CrossroadsParty");
        public static string CrossroadsRoster => T("CrossroadsRoster");
        public static string CrossroadsEmptySlot => T("CrossroadsEmptySlot");
        public static string CrossroadsInParty => T("CrossroadsInParty");
        public static string Grabbed(string what) => F("Grabbed", what);
        public static string GrabCancelled => T("GrabCancelled");
        public static string CannotPlace => T("CannotPlace");

        public static string SheetSpeed(int value) => F("SheetSpeed", value);
        public static string PanelEmpty => T("PanelEmpty");

        public static string CombatHeader(int round, string actor) => F("CombatHeader", round, actor);
        public static string CombatEnemies => T("CombatEnemies");
        public static string CombatTorch(int value) => F("CombatTorch", value);
        public static string CombatSelectTarget => T("CombatSelectTarget");
        public static string CombatTargetValid => T("CombatTargetValid");
        public static string CombatTargetInvalid => T("CombatTargetInvalid");
        public static string CombatTargetCancelled => T("CombatTargetCancelled");
        public static string CombatTookDamage(string name, int damage) => F("CombatTookDamage", name, damage);
        public static string CombatTookDamageOne(string name) => F("CombatTookDamageOne", name);
        public static string CombatDied(string name) => F("CombatDied", name);
        public static string CombatDeathsDoor(string name) => F("CombatDeathsDoor", name);
        public static string CombatUsedSkill(string name, string skill, string target) => F("CombatUsedSkill", name, skill, target);
        public static string CombatGained(string name, string what) => F("CombatGained", name, what);
        public static string CombatResisted(string name, string what) => F("CombatResisted", name, what);
        public static string CombatTurnOrder(string names) => F("CombatTurnOrder", names);
        public static string CombatBattleCount(int current, int total) => F("CombatBattleCount", current, total);
        public static string CombatTookDamageCrit(string name, int damage) => F("CombatTookDamageCrit", name, damage);
        public static string CombatHealed(string name, int amount) => F("CombatHealed", name, amount);
        public static string CombatHealedCrit(string name, int amount) => F("CombatHealedCrit", name, amount);
        public static string CombatMissed(string attacker, string target) => F("CombatMissed", attacker, target);
        public static string CombatDodged(string name) => F("CombatDodged", name);
        public static string CombatStressed(string name, int amount) => F("CombatStressed", name, amount);
        public static string CombatStressHealed(string name, int amount) => F("CombatStressHealed", name, amount);
        public static string CombatDeathBlowResisted(string name) => F("CombatDeathBlowResisted", name);
        public static string CombatSpent(string name, string what) => F("CombatSpent", name, what);
        public static string CombatLost(string name, string what) => F("CombatLost", name, what);
        public static string CombatWounded(string name) => F("CombatWounded", name);
        public static string CombatWoundHealed(string name) => F("CombatWoundHealed", name);
        public static string CombatAffinity(string first, string second, string change) => F("CombatAffinity", first, second, change);
        public static string BarkLine(string speaker, string text) => F("BarkLine", speaker, text);

        public static string ToastTutorial(string title) => F("ToastTutorial", title);
        public static string ToastObjective(string name) => F("ToastObjective", name);

        public static string RoadForkAhead => T("RoadForkAhead");
        public static string RouteLeft => T("RouteLeft");
        public static string RouteForward => T("RouteForward");
        public static string RouteRight => T("RouteRight");
        public static string RoutePreferredBy(string names) => F("RoutePreferredBy", names);

        public static string SpriteHeal => T("SpriteHeal");
        public static string SpriteBuff => T("SpriteBuff");
        public static string SpriteDebuff => T("SpriteDebuff");
        public static string SpriteStress => T("SpriteStress");
        public static string SpriteDisease => T("SpriteDisease");
        public static string SpriteSpeed => T("SpriteSpeed");
        public static string SpriteHealth => T("SpriteHealth");

        public static string BufferControl => T("BufferControl");
        public static string BufferCombat => T("BufferCombat");
        public static string BufferNone => T("BufferNone");
        public static string BufferLine(string buffer, string line) => F("BufferLine", buffer, line);

        public static string InputNavigateUp => T("InputNavigateUp");
        public static string InputNavigateDown => T("InputNavigateDown");
        public static string InputNavigateLeft => T("InputNavigateLeft");
        public static string InputNavigateRight => T("InputNavigateRight");
        public static string InputNextPanel => T("InputNextPanel");
        public static string InputPrevPanel => T("InputPrevPanel");
        public static string InputActivate => T("InputActivate");
        public static string InputBack => T("InputBack");
        public static string InputJumpFirst => T("InputJumpFirst");
        public static string InputJumpLast => T("InputJumpLast");
        public static string InputBufferNext => T("InputBufferNext");
        public static string InputBufferPrev => T("InputBufferPrev");
        public static string InputBufferLineNext => T("InputBufferLineNext");
        public static string InputBufferLinePrev => T("InputBufferLinePrev");
        public static string InputGrab => T("InputGrab");
        public static string InputPlaceOne => T("InputPlaceOne");
        public static string InputInspect => T("InputInspect");
        public static string InputDiscard => T("InputDiscard");
    }
}
