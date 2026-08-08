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
            // Spoken once at game launch, as soon as the game's own language is known so the
            // line comes out translated; {0} = the mod version.
            D("ModLoaded", "Harkest Dungeon {0} loaded"),
            // Follows the loaded line when the newest release outranks the running build;
            // {0} = that newer version. Up to date (or ahead) stays silent.
            D("UpdateAvailable", "update {0} available"),

            // Screen names, spoken when the mod takes over a screen. Match the game's own word for
            // the screen where it has one (the settings screen's title, the pause header).
            D("ScreenMainMenu", "main menu"),
            // The Kingdoms campaign menu opened from the title menu, when the game's own
            // "Kingdoms" caption is unavailable. Noun.
            D("ScreenKingdoms", "kingdoms"),
            D("ScreenSettings", "settings"),
            // The profile-select panel under the title menu's profile button, when the game's
            // own "Select Profile" title is unavailable.
            D("ScreenProfileSelect", "select profile"),
            // The settings screen's key-bindings panel, when the game's own "Bindings" caption
            // is unavailable.
            D("ScreenKeyBindings", "key bindings"),
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
            // The academic view modeled as its own screen.
            D("ScreenInspector", "inspector"),
            // The standalone player-inventory screen (road, crossroads, loot).
            D("ScreenInventory", "inventory"),
            // The free-driving screen (the road HUD as Tab panels). Biome, distance, and
            // hero lines come from the game; these are the mod's own frame words.
            D("ScreenDriving", "driving"),
            // The driving area's label while the game shows no biome name to reuse.
            D("DrivingRoad", "road"),
            // The flame meter; {0} = the current value. The game captions it with the glyph only.
            D("DrivingFlame", "Flame {0}"),
            // The node-arrival prompt (every roadside stop halts the coach on it).
            D("ScreenNodePrompt", "road stop"),
            // Appended to the prompt's button when entering also feeds a hero goal; the game
            // shows a candle icon only.
            D("NodeCandleReward", "candle reward"),
            // The advance-or-escape dialog between a lair's battles: section label over the
            // reward items its cleared battles already secured. Adjective.
            D("LairLooted", "looted"),
            // Same dialog: section label over the reward the next battle offers. Noun phrase.
            D("LairNextLoot", "next battle"),
            // The road map (M while driving) and its cursor lines. Node and road names come
            // from the game's own fog-gated tooltips; these frame them.
            D("ScreenMap", "map"),
            D("MapClosed", "map closed"),
            // The wagon's live position; {0}/{1} = the road's endpoints (fog-gated names).
            D("MapWagon", "on the road, {0} to {1}"),
            D("MapWagonAt", "at {0}"),
            // Prefixed to the road crossed into a fork's first alternative.
            D("MapChoice", "choice"),
            // The cursor hit the last node ahead / the first node behind.
            D("MapTop", "no road forward"),
            D("MapBottom", "no road back"),
            // Node markers: carries a Candle of Hope; visiting advances Loathing; a Kingdoms
            // kill contract.
            D("MapCandle", "candle"),
            D("MapDoom", "loathing"),
            D("MapContract", "contract"),
            // Chosen-state of a node or road already passed.
            D("MapTraveled", "traveled"),
            D("MapNotTaken", "not taken"),
            // Buffer line: the cursor's row position; {0} = row, {1} = the biome's row count.
            D("MapRow", "row {0} of {1}"),
            // Buffer line per road out of the focused node; {0} = the road, {1} = where it leads.
            D("MapRoadTo", "{0}, to {1}"),
            // Buffer line: the road the cursor arrived by; {0} = the road.
            D("MapVia", "via {0}"),
            // A safe road (the game ships no string of its own for one).
            D("MapSafeRoad", "safe road"),
            // A tooltip that yielded no text (the game's shape changed).
            D("MapUnknown", "unknown"),

            // The kingdoms overworld map cursor. Cell names, day labels, siege durations, and
            // tooltips come from the game; these are the mod's own overlay words.
            D("ScreenKingdomMap", "kingdom map"),
            D("KingdomStagecoach", "stagecoach here"),
            D("KingdomReachable", "reachable"),
            D("KingdomTravelScheduled", "travel scheduled"),
            D("KingdomBoss", "boss"),
            D("KingdomSiege", "siege"),
            D("KingdomSiegeMedium", "medium strength"),
            D("KingdomSiegeHigh", "high strength"),
            D("KingdomTreasure", "treasure"),
            D("KingdomReward", "reward offered"),
            D("KingdomUpgraded", "upgraded"),
            D("KingdomCursed", "cursed"),
            D("KingdomQuest", "quest"),
            // Grid coordinates, bare numbers; {0} = row, {1} = column.
            D("KingdomCell", "{0}, {1}"),
            D("KingdomMovingHero", "moving {0}, activate a destination inn"),
            D("ScreenKingdomEvent", "kingdom event"),
            // The road-fork route menu, shown while the coach waits at a junction.
            D("ScreenFork", "fork"),
            // The inn hub, when the inn's own name is unavailable. Noun.
            D("ScreenInn", "inn"),
            // The inn's stationed-hero portrait strip (Kingdoms), a section name. Noun.
            D("InnStationedHeroes", "stationed heroes"),
            // A replacement candidate already at the current inn, where the game shows a bare
            // inn icon on the row. Spoken as the row's state.
            D("InnAtThisInn", "at this inn"),
            // The Altar of Hope hub, when the game's own title is unavailable. Noun.
            D("ScreenAltar", "altar"),
            // The embark staging scene between the crossroads (or an inn) and the drive, where
            // hero relationships reveal before the coach departs. Noun.
            D("ScreenEmbark", "departure"),
            // An embark relationship row before its reveal press, where the game shows only
            // a question mark over the two heroes' portraits. Spoken as the row's value.
            D("RelationshipUnrevealed", "unrevealed relationship"),
            // The candle cost on an altar unlock button, where the game shows a candle icon
            // and a bare number; {0} = the number.
            D("AltarCandleCost", "{0} candle|{0} candles"),
            // The altar's item reveal modal, spoken before the unlocked item's name.
            D("AltarUnlocked", "unlocked"),
            // A filled memory slot the profile can reroll, as the slot's state; {0} = the
            // candle cost. The game shows only a button-prompt icon and the cost.
            D("AltarMemoryReroll", "reroll {0} candle|reroll {0} candles"),
            // A skill the hero has already mastered: the state word on the inn's Mastery
            // Trainer, and the spoken form of the laurel on the hero sheet's and combat bar's
            // skill rows. Adjective.
            D("SkillMastered", "mastered"),
            // The upgrade buffer's only line when the focused skill has no mastery preview to
            // show: the skill is already mastered, or no mastered variant exists.
            D("SkillNoUpgrade", "no upgrade available"),
            // The Mastery Trainer's remaining points readout; {0} = the number.
            D("MasteryPoints", "mastery points {0}"),
            // A repair button on the Wainwright's stagecoach sheet, where the game shows only
            // a wrench icon and the cost; {0} = the game's own cost text ("baubles 8").
            D("StationRepair", "repair, {0}"),
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
            // The results screens' run total (end expedition, game over), which the game shows
            // as a bare number beside a candle icon; {0} = the number.
            D("ResultsTotal", "total {0}"),

            // Control type words, spoken after a control's label ("Continue, button"). Nouns.
            D("RoleButton", "button"),
            D("RoleToggle", "toggle"),
            D("RoleSlider", "slider"),
            D("RoleDropdown", "dropdown"),
            D("RoleTab", "tab"),
            // The hero sheet's header line (the hero's name); Left/Right there switch heroes.
            D("RoleHero", "hero"),
            // A text entry field (the kingdom name). Noun.
            D("RoleEdit", "edit"),

            // Control state words.
            // A toggle that is checked / unchecked.
            D("StatusOn", "on"),
            D("StatusOff", "off"),
            // Spoken when adjusting a slider that is already at its end.
            D("StatusMinimum", "minimum"),
            D("StatusMaximum", "maximum"),
            // The currently chosen entry (a tab, a hero already in the party).
            D("StatusSelected", "selected"),
            D("StatusOwned", "owned"),
            D("RequiresUpgrade", "needs {0}"),
            // An ordained enemy (carrying the confession boss's blessing).
            D("StatusBlessed", "blessed"),
            // A control present but not usable right now (a grayed-out button). Adjective.
            D("StatusUnavailable", "unavailable"),
            // A slider value; {0} = the number.
            D("ValuePercent", "{0} percent"),

            // Text entry. Spoken when a field enters typing mode: every key then goes into the
            // field until Enter accepts or Escape cancels.
            D("EditStarted", "editing, enter when done"),
            // Echo of a typed space (a bare space is inaudible).
            D("EditSpace", "space"),
            // Echo of an erased character; {0} = the character.
            D("EditDeleted", "{0} deleted"),

            // The mod's own tab on the game's settings screen.
            D("TabModSettings", "mod settings"),
            // The separator joining the parts of a spoken line ("Exit game, button"). Noun phrase.
            D("SettingSeparator", "announcement separator"),
            // How far away the road's pickup pings are audible, in the road's own distance
            // units. Noun phrase.
            D("SettingSensingRange", "sensing range"),
            // Toggle: passed roadside pickups collect themselves, no steering needed; the
            // pickup ping stays quiet while it is on. Noun phrase.
            D("SettingAutoCollect", "auto collect pickups"),
            // The baseline volume of every mod sound, the slider heading the sounds glossary
            // tab; the per-sound volumes below ride on it as offsets. Noun phrase.
            D("SettingMasterVolume", "master volume"),
            // Spoken when committing an empty value returns a setting to its default.
            D("SettingReset", "reset to default"),
            // The mod's own announcements tab on the game's settings screen: one toggle per
            // optional mod announcement.
            D("TabModAnnouncements", "mod announcements"),
            // Toggle: whether a corpse's own destruction speaks a died line in battle. Noun phrase.
            D("SettingCorpseDeaths", "corpse deaths"),
            // The settings graphics tab's gamma reset button, icon-only in the game's own UI.
            D("OptionsGammaReset", "Reset Gamma Correction"),

            // The mod's own sounds glossary tab on the game's settings screen: one row per sound
            // the mod plays. Enter plays the row's sound once, Space loops it, Left/Right step
            // its volume.
            D("TabModSounds", "mod sounds"),
            // The glossary's group tabs after the master volume row, one per sound family
            // (the assets/audio folders). Nouns.
            D("SoundTabRoad", "road"),
            D("SoundTabCombat", "combat"),
            // State word leading the glossary row whose sound is looping right now.
            D("StatusPlaying", "playing"),
            // Glossary row labels: when the mod plays each sound. Road driving events first.
            // The repeating positional ping while a roadside pickup is in sensing range.
            D("SoundRoadPickup", "pickup nearby"),
            // The coach is drifting off the road's edge.
            D("SoundRoadEdgeBump", "road edge"),
            // Loops while the coach turns; its end cue marks the settle back to straight.
            D("SoundRoadTurning", "coach turning"),
            D("SoundRoadTurnEnd", "turn ended"),
            // Combat target-selection beeps: focus landed on a valid / invalid target.
            D("SoundCombatTargetValid", "target valid"),
            D("SoundCombatTargetInvalid", "target invalid"),

            // The mod's own key-rebinding tab on the game's settings screen: one row per mod
            // command showing its current keys. Enter opens the row's menu (add a key, delete
            // one), Shift+Enter restores the default keys.
            D("TabModKeys", "mod keys"),
            // The row-menu choice that starts listening for a key to add. Verb phrase.
            D("KeyAddBinding", "add key"),
            // The row-menu choice that starts listening for a controller button to add, shown
            // only while a gamepad is connected. Verb phrase.
            D("KeyAddPadBinding", "add button"),
            // The row-menu choice swapping one of the command's keys for a newly listened one;
            // {0} = the key combo being replaced.
            D("KeyReplaceBinding", "replace {0}"),
            // The row-menu choice deleting one of the command's keys; {0} = the key combo.
            D("KeyDeleteBinding", "delete {0}"),
            // Spoken while listening: the next key pressed (with any Ctrl/Shift/Alt held) is
            // added to the command; Escape keeps things as they are.
            D("KeyPressNew", "press the new key"),
            // Spoken while listening for a controller input: the next button RELEASED is added
            // (a trigger can be held first as the combo's modifier); Escape keeps things as
            // they are.
            D("KeyPressNewPad", "press the new button"),
            // A command with every key deleted.
            D("KeyNotSet", "not set"),
            // A captured key refused because another command holds it (delete it there first);
            // spoken after the key's name. {0} = the holding command's name.
            D("KeyAlreadyBound", "already bound to {0}"),
            // Buffer line naming the command's authored default key or keys; {0} = the keys.
            D("KeyDefault", "default {0}"),

            // Crossroads (the pre-run hub). Section names for the two hero strips; the game shows
            // these visually with no header string to reuse. Nouns.
            D("CrossroadsParty", "party"),
            D("CrossroadsRoster", "roster"),
            // The crossroads path-select overlay, when the game's own panel title is
            // unavailable. Noun phrase.
            D("ScreenPathSelect", "hero path"),
            // The readout carrying the previewed path's full card (flavour, rank and target
            // lines, effects) in its buffer. Noun phrase naming that section.
            D("PathDetails", "path details"),
            // The crossroads party-loadout overlay, when the game's own panel title is
            // unavailable. Noun phrase.
            D("ScreenPartyLoadouts", "party loadouts"),
            // The icon-only buttons on a saved loadout's row (no text or tooltip in the game's
            // own UI): rename the loadout, delete it. Verb phrases.
            D("LoadoutRename", "rename"),
            D("LoadoutDelete", "delete"),
            // Spoken before a hero's given name when the name itself is the news (a rename
            // committed, a reroll landed). Noun phrase.
            D("HeroNameField", "hero name"),
            // The icon-only button restoring the shown hero's cosmetics and memories to their
            // defaults (the game asks for confirmation). Verb phrase.
            D("HeroReset", "reset hero"),
            // A hero slot with no hero in it.
            D("CrossroadsEmptySlot", "empty slot"),
            // A party slot's battle position, spoken before the occupant ("rank 1, Highwayman");
            // rank 1 is the front line, as in combat. {0} = the number.
            D("CrossroadsRank", "rank {0}"),
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
            // Section name for the combat item slots row, used when the game's item-type
            // string ("item_type_combat") is missing. Noun.
            D("SheetCombatItems", "combat items"),
            // A tab or panel with nothing in it yet (the relationships tab before a run), where
            // the game shows blank space. Adjective.
            D("PanelEmpty", "empty"),
            // Prefix on an entry the game marks with its unviewed notification icon (an
            // archive tutorial, a collection item). Adjective.
            D("TutorialNew", "New"),

            // Combat. The battle status line, spoken on turn changes and as the header readout;
            // {0} = the round number, {1} = the acting combatant's name.
            D("CombatHeader", "round {0}, {1}"),
            // Section name for the enemy strip. Noun (the party strip reuses CrossroadsParty).
            D("CombatEnemies", "enemies"),
            // The torch/flame meter readout; {0} = its value. The game shows it as a bare icon.
            D("CombatTorch", "torch {0}"),
            // An ordained (blessed) combatant's line word - the game's blessed portrait icon
            // as a word, using the game's own term for the state. The blessing's effects ride
            // in the buffer as the icon's tooltip.
            D("CombatOrdained", "ordained"),
            // Why a combatant cannot take the chosen skill, prepended to its line during
            // target-select (validity itself rides as the high/low beep). Mirrors the game's
            // own target checks, which it shows only as dimming.
            D("TargetOutOfRange", "out of range"),
            D("TargetAlliesOnly", "allies only"),
            D("TargetEnemiesOnly", "enemies only"),
            D("TargetSelfOnly", "self only"),
            D("TargetNotSelf", "not self"),
            D("TargetStealthed", "stealthed"),
            D("TargetBlocked", "blocked"),
            D("TargetUntargetable", "untargetable"),
            D("TargetConditionNotMet", "condition not met"),
            // The game's per-target preview on a valid target; {0} = the percent / heal range.
            D("CombatHitChance", "{0}% hit"),
            D("CombatCritChance", "{0}% crit"),
            D("CombatHealPreview", "heals {0}"),
            // The pick would strip these tokens from the hit combatant - the removals the
            // game previews by flashing the recipient's tray icons; {0} = the names, joined.
            D("CombatRemoves", "removes {0}"),
            // The pick would move these tokens to the acting hero; {0} = the names.
            D("CombatSteals", "steals {0}"),
            // The pick would turn these tokens into others; {0} = the names.
            D("CombatConverts", "converts {0}"),
            // A guardian will absorb the hit aimed at this target; {0} = the guardian.
            D("CombatIntercepted", "intercepted by {0}"),
            // Picking this target draws a counter-attack; {0} = its damage range.
            D("CombatRiposte", "riposte {0}"),
            // Buffer line on a skill the hero holds twice: once equipped by the player, once as
            // a granted always-equipped copy. The game's bar shows two identical buttons that
            // select the same skill; the mod reads one button and notes the grant here.
            D("CombatSkillAlsoGranted", "also granted as a bonus skill"),
            // A stacked token in the Shift-glance line - the game's own x-count with a space a
            // reader can speak; {0} = the token's name, {1} = the stack count (2 or more).
            D("CombatTokenCount", "{0} x{1}"),
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
            // A combatant whose name several living teammates share (a pack of Lost Souls):
            // each speaks with its rank so the turn order tells them apart, the way the game's
            // portrait hover highlights the specific one; {0} = the name, {1} = the rank.
            D("CombatantNumbered", "{0} {1}"),
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
            // Inspector section names, used where the game has no reusable header string.
            D("InspectorTokens", "tokens"),
            D("InspectorDots", "damage over time"),
            D("InspectorBuffs", "buffs"),
            D("InspectorDebuffs", "debuffs"),
            D("InspectorConditions", "conditions"),
            // A studied skill still cooling down; {0} = rounds remaining.
            D("InspectorCooldown", "cooldown {0}"),
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
            // The faction currency's cost glyph (the Wainwright's repair prices); the game
            // calls the currency Baubles but spells it only in a tooltip.
            D("SpriteBaubles", "baubles"),
            // Replaces the game's "???" placeholder glyph (a locked confession, an unexplored
            // node's rewards), which a synthesizer voices as nothing. Adjective.
            D("TextUnknown", "unknown"),

            // Buffer review (Ctrl plus arrows). The buffer holding the focused control's detail
            // lines (its tooltips). Noun naming that buffer.
            D("BufferControl", "control"),
            // The battle-event log buffer, non-empty only during combat. Noun naming that buffer.
            D("BufferCombat", "combat"),
            // The skill-upgrade preview buffer's name, used only when the game's own upgrade
            // header string is missing. Noun naming that buffer.
            D("BufferMastery", "mastery"),
            // The buffer holding the vitals of the hero the focused control concerns (a skill's
            // owner, a story choice's hero). Noun naming that buffer.
            D("BufferHero", "hero"),
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
            // The advertised-hotkey buttons ("Map (M)", "Inventory (I)") on captured screens.
            D("InputHotkeyMap", "Map button"),
            D("InputHotkeyInventory", "Inventory button"),
            // The combat inspector keys (the game's academic view).
            D("InputInspector", "Toggle inspector"),
            D("InputInspectorPrev", "Inspector previous combatant"),
            D("InputInspectorNext", "Inspector next combatant"),
            D("InputDiscard", "Discard item"),
            // The hero-name keys at the crossroads, acting on the focused hero.
            D("InputRename", "Rename hero"),
            D("InputReroll", "Roll a new hero name"),
            // The combat glance hotkeys, one action per battlefield slot in rank order;
            // {0} = the slot number. Bare key = name and health, Shift = buffs and debuffs,
            // Ctrl = resistances.
            D("InputCombatEnemy", "Enemy {0} status"),
            D("InputCombatEnemyEffects", "Enemy {0} buffs and debuffs"),
            D("InputCombatEnemyResists", "Enemy {0} resistances"),
            D("InputCombatHero", "Hero {0} status"),
            D("InputCombatHeroEffects", "Hero {0} buffs and debuffs"),
            D("InputCombatHeroResists", "Hero {0} resistances"),
            D("InputCombatActor", "Acting combatant status"),
            // The valid-targets glance, live while a skill is focused.
            D("InputCombatTargets", "Focused skill's valid targets"),
            D("InputCombatTurnOrder", "Turn order"),
            // The road-story glance, live on the story screen's choices.
            D("InputStoryHero", "Focused choice's hero status"),
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
            sb.Append("# Harkest Dungeon translation template. Copy to <language>.txt and translate the values.\n");
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
        public static string UpdateAvailable(string version) => F("UpdateAvailable", version);

        public static string ScreenMainMenu => T("ScreenMainMenu");
        public static string ScreenKingdoms => T("ScreenKingdoms");
        public static string ScreenSettings => T("ScreenSettings");
        public static string ScreenProfileSelect => T("ScreenProfileSelect");
        public static string ScreenKeyBindings => T("ScreenKeyBindings");
        public static string ScreenPauseMenu => T("ScreenPauseMenu");
        public static string ScreenCrossroads => T("ScreenCrossroads");
        public static string ScreenDialog => T("ScreenDialog");
        public static string ScreenGeneric => T("ScreenGeneric");
        public static string ScreenHeroSheet => T("ScreenHeroSheet");
        public static string ScreenCombat => T("ScreenCombat");
        public static string ScreenInspector => T("ScreenInspector");
        public static string ScreenInventory => T("ScreenInventory");
        public static string ScreenDriving => T("ScreenDriving");
        public static string DrivingRoad => T("DrivingRoad");
        public static string DrivingFlame(string value) => F("DrivingFlame", value);
        public static string ScreenNodePrompt => T("ScreenNodePrompt");
        public static string NodeCandleReward => T("NodeCandleReward");
        public static string LairLooted => T("LairLooted");
        public static string LairNextLoot => T("LairNextLoot");
        public static string ScreenMap => T("ScreenMap");
        public static string MapClosed => T("MapClosed");
        public static string MapWagon(string from, string to) => F("MapWagon", from, to);
        public static string MapWagonAt(string node) => F("MapWagonAt", node);
        public static string MapChoice => T("MapChoice");
        public static string MapTop => T("MapTop");
        public static string MapBottom => T("MapBottom");
        public static string MapCandle => T("MapCandle");
        public static string MapDoom => T("MapDoom");
        public static string MapContract => T("MapContract");
        public static string MapTraveled => T("MapTraveled");
        public static string MapNotTaken => T("MapNotTaken");
        public static string MapRow(int row, int count) => F("MapRow", row, count);
        public static string MapRoadTo(string route, string node) => F("MapRoadTo", route, node);
        public static string MapVia(string route) => F("MapVia", route);
        public static string MapSafeRoad => T("MapSafeRoad");
        public static string MapUnknown => T("MapUnknown");
        public static string ScreenKingdomMap => T("ScreenKingdomMap");
        public static string KingdomStagecoach => T("KingdomStagecoach");
        public static string KingdomReachable => T("KingdomReachable");
        public static string KingdomTravelScheduled => T("KingdomTravelScheduled");
        public static string KingdomBoss => T("KingdomBoss");
        public static string KingdomSiege => T("KingdomSiege");
        public static string KingdomSiegeMedium => T("KingdomSiegeMedium");
        public static string KingdomSiegeHigh => T("KingdomSiegeHigh");
        public static string KingdomTreasure => T("KingdomTreasure");
        public static string KingdomReward => T("KingdomReward");
        public static string KingdomUpgraded => T("KingdomUpgraded");
        public static string KingdomCursed => T("KingdomCursed");
        public static string KingdomQuest => T("KingdomQuest");
        public static string KingdomCell(int row, int col) => F("KingdomCell", row, col);
        public static string KingdomMovingHero(string hero) => F("KingdomMovingHero", hero);
        public static string ScreenKingdomEvent => T("ScreenKingdomEvent");
        public static string ScreenFork => T("ScreenFork");
        public static string ScreenInn => T("ScreenInn");
        public static string InnStationedHeroes => T("InnStationedHeroes");
        public static string InnAtThisInn => T("InnAtThisInn");
        public static string ScreenAltar => T("ScreenAltar");
        public static string ScreenEmbark => T("ScreenEmbark");
        public static string RelationshipUnrevealed => T("RelationshipUnrevealed");
        public static string AltarCandleCost(int count) => P("AltarCandleCost", count);
        public static string AltarUnlocked => T("AltarUnlocked");
        public static string AltarMemoryReroll(int count) => P("AltarMemoryReroll", count);
        public static string SkillMastered => T("SkillMastered");
        public static string SkillNoUpgrade => T("SkillNoUpgrade");
        public static string MasteryPoints(int count) => F("MasteryPoints", count);
        public static string StationRepair(string cost) => F("StationRepair", cost);
        public static string InventorySlots(string count) => F("InventorySlots", count);
        public static string InventoryEmptySlots(int count) => P("InventoryEmptySlots", count);
        public static string InventorySorted => T("InventorySorted");
        public static string ItemDiscarded(string item) => F("ItemDiscarded", item);
        public static string ItemSold(string item) => F("ItemSold", item);
        public static string ResultsTotal(string number) => F("ResultsTotal", number);

        public static string RoleButton => T("RoleButton");
        public static string RoleToggle => T("RoleToggle");
        public static string RoleSlider => T("RoleSlider");
        public static string RoleEdit => T("RoleEdit");
        public static string EditStarted => T("EditStarted");
        public static string EditSpace => T("EditSpace");
        public static string EditDeleted(string character) => F("EditDeleted", character);
        public static string TabModSettings => T("TabModSettings");
        public static string SettingSeparator => T("SettingSeparator");
        public static string SettingSensingRange => T("SettingSensingRange");
        public static string SettingAutoCollect => T("SettingAutoCollect");
        public static string SettingMasterVolume => T("SettingMasterVolume");
        public static string SettingReset => T("SettingReset");
        public static string TabModAnnouncements => T("TabModAnnouncements");
        public static string SettingCorpseDeaths => T("SettingCorpseDeaths");
        public static string OptionsGammaReset => T("OptionsGammaReset");
        public static string TabModSounds => T("TabModSounds");
        public static string SoundTabRoad => T("SoundTabRoad");
        public static string SoundTabCombat => T("SoundTabCombat");
        public static string StatusPlaying => T("StatusPlaying");

        /// <summary>The glossary label of one mod sound, keyed "Sound{cue}" by convention; a test
        /// pins every <see cref="Audio.AudioCue"/> value to a table entry.</summary>
        public static string SoundLabel(Audio.AudioCue cue) => T("Sound" + cue);

        public static string TabModKeys => T("TabModKeys");
        public static string KeyAddBinding => T("KeyAddBinding");
        public static string KeyAddPadBinding => T("KeyAddPadBinding");
        public static string KeyPressNewPad => T("KeyPressNewPad");
        public static string KeyReplaceBinding(string keys) => F("KeyReplaceBinding", keys);
        public static string KeyDeleteBinding(string keys) => F("KeyDeleteBinding", keys);
        public static string KeyPressNew => T("KeyPressNew");
        public static string KeyNotSet => T("KeyNotSet");
        public static string KeyAlreadyBound(string label) => F("KeyAlreadyBound", label);
        public static string KeyDefault(string keys) => F("KeyDefault", keys);
        public static string RoleDropdown => T("RoleDropdown");
        public static string RoleTab => T("RoleTab");
        public static string RoleHero => T("RoleHero");

        public static string StatusOn => T("StatusOn");
        public static string StatusOff => T("StatusOff");
        public static string StatusMinimum => T("StatusMinimum");
        public static string StatusMaximum => T("StatusMaximum");
        public static string StatusSelected => T("StatusSelected");
        public static string StatusOwned => T("StatusOwned");
        public static string RequiresUpgrade(string names) => F("RequiresUpgrade", names);
        public static string StatusBlessed => T("StatusBlessed");
        public static string StatusUnavailable => T("StatusUnavailable");
        public static string ValuePercent(int value) => F("ValuePercent", value);

        public static string CrossroadsParty => T("CrossroadsParty");
        public static string CrossroadsRoster => T("CrossroadsRoster");
        public static string ScreenPathSelect => T("ScreenPathSelect");
        public static string PathDetails => T("PathDetails");
        public static string ScreenPartyLoadouts => T("ScreenPartyLoadouts");
        public static string LoadoutRename => T("LoadoutRename");
        public static string LoadoutDelete => T("LoadoutDelete");
        public static string HeroNameField => T("HeroNameField");
        public static string HeroReset => T("HeroReset");
        public static string CrossroadsEmptySlot => T("CrossroadsEmptySlot");
        public static string CrossroadsRank(int rank) => F("CrossroadsRank", rank);
        public static string CrossroadsInParty => T("CrossroadsInParty");
        public static string Grabbed(string what) => F("Grabbed", what);
        public static string GrabCancelled => T("GrabCancelled");
        public static string CannotPlace => T("CannotPlace");

        public static string SheetSpeed(int value) => F("SheetSpeed", value);
        public static string SheetCombatItems => T("SheetCombatItems");
        public static string TutorialNew => T("TutorialNew");
        public static string PanelEmpty => T("PanelEmpty");

        public static string CombatHeader(int round, string actor) => F("CombatHeader", round, actor);
        public static string CombatEnemies => T("CombatEnemies");
        public static string CombatTorch(int value) => F("CombatTorch", value);
        public static string CombatOrdained => T("CombatOrdained");
        public static string CombatSkillAlsoGranted => T("CombatSkillAlsoGranted");
        public static string CombatTokenCount(string name, int count) => F("CombatTokenCount", name, count);
        public static string TargetOutOfRange => T("TargetOutOfRange");
        public static string TargetAlliesOnly => T("TargetAlliesOnly");
        public static string TargetEnemiesOnly => T("TargetEnemiesOnly");
        public static string TargetSelfOnly => T("TargetSelfOnly");
        public static string TargetNotSelf => T("TargetNotSelf");
        public static string TargetStealthed => T("TargetStealthed");
        public static string TargetBlocked => T("TargetBlocked");
        public static string TargetUntargetable => T("TargetUntargetable");
        public static string TargetConditionNotMet => T("TargetConditionNotMet");
        public static string CombatHitChance(int percent) => F("CombatHitChance", percent);
        public static string CombatCritChance(int percent) => F("CombatCritChance", percent);
        public static string CombatHealPreview(string range) => F("CombatHealPreview", range);
        public static string CombatRemoves(string names) => F("CombatRemoves", names);
        public static string CombatSteals(string names) => F("CombatSteals", names);
        public static string CombatConverts(string names) => F("CombatConverts", names);
        public static string CombatIntercepted(string guardian) => F("CombatIntercepted", guardian);
        public static string CombatRiposte(string damage) => F("CombatRiposte", damage);
        public static string CombatTookDamage(string name, int damage) => F("CombatTookDamage", name, damage);
        public static string CombatTookDamageOne(string name) => F("CombatTookDamageOne", name);
        public static string CombatDied(string name) => F("CombatDied", name);
        public static string CombatDeathsDoor(string name) => F("CombatDeathsDoor", name);
        public static string CombatUsedSkill(string name, string skill, string target) => F("CombatUsedSkill", name, skill, target);
        public static string CombatGained(string name, string what) => F("CombatGained", name, what);
        public static string CombatResisted(string name, string what) => F("CombatResisted", name, what);
        public static string CombatTurnOrder(string names) => F("CombatTurnOrder", names);
        public static string CombatantNumbered(string name, int rank) => F("CombatantNumbered", name, rank);
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
        public static string InspectorTokens => T("InspectorTokens");
        public static string InspectorDots => T("InspectorDots");
        public static string InspectorBuffs => T("InspectorBuffs");
        public static string InspectorDebuffs => T("InspectorDebuffs");
        public static string InspectorConditions => T("InspectorConditions");
        public static string InspectorCooldown(int rounds) => F("InspectorCooldown", rounds);
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
        public static string SpriteBaubles => T("SpriteBaubles");
        public static string TextUnknown => T("TextUnknown");

        public static string BufferControl => T("BufferControl");
        public static string BufferCombat => T("BufferCombat");
        public static string BufferMastery => T("BufferMastery");
        public static string BufferHero => T("BufferHero");
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
        public static string InputHotkeyMap => T("InputHotkeyMap");
        public static string InputHotkeyInventory => T("InputHotkeyInventory");
        public static string InputInspector => T("InputInspector");
        public static string InputInspectorPrev => T("InputInspectorPrev");
        public static string InputInspectorNext => T("InputInspectorNext");
        public static string InputDiscard => T("InputDiscard");
        public static string InputRename => T("InputRename");
        public static string InputReroll => T("InputReroll");
        public static string InputCombatEnemy(int slot) => F("InputCombatEnemy", slot);
        public static string InputCombatEnemyEffects(int slot) => F("InputCombatEnemyEffects", slot);
        public static string InputCombatEnemyResists(int slot) => F("InputCombatEnemyResists", slot);
        public static string InputCombatHero(int slot) => F("InputCombatHero", slot);
        public static string InputCombatHeroEffects(int slot) => F("InputCombatHeroEffects", slot);
        public static string InputCombatHeroResists(int slot) => F("InputCombatHeroResists", slot);
        public static string InputCombatActor => T("InputCombatActor");
        public static string InputCombatTargets => T("InputCombatTargets");
        public static string InputCombatTurnOrder => T("InputCombatTurnOrder");
        public static string InputStoryHero => T("InputStoryHero");
    }
}
