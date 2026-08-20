# Combat

Combat is the most overhauled surface in the mod. It is worth reading this section in full before your first fight; the Darkest Dungeon franchise does not grade on a curve.

## How It All Normally Works

Both sides field up to four combatants in a line of ranks, 1 at the front, 4 at the back. Position is everything: each skill can only be used from certain ranks and can only reach certain ranks, so a hero shoved out of position may find their whole kit greyed out until they move back. Combatants act one at a time in an initiative order that interleaves the two sides.

Instead of quietly stacking numbers, most effects in this game are **tokens**: block, dodge, strength, blind, and their many friends, each a discrete thing a combatant holds and spends. Damage over time - bleed, blight, burn - ticks on the victim's turns. Heroes additionally accumulate **stress**; enough of it causes a meltdown, which hurts them and sours their relationships. At zero HP a hero lands on Death's Door, where any further hit kills them unless their deathblow resistance says otherwise. Enemies skip the drama and simply die.

## The Layout

When a battle starts, focus lands on the round title. Below it, the battlefield is **one row laid out like the screen**: your party right-to-left - rank 4 leftmost, rank 1 at the front - then the enemies rank 1 through 4. The two front lines meet in the middle, so the combatant most likely to be hit next is standing next to the first enemy in the list. Below the battlefield sits the acting hero's skill bar.

Duplicate enemies are numbered 1 to N in the order you first meet them, and the numbers stay put through shuffles and compact when someone dies. No guessing which of three Lost Souls just gained a taste for your Vestal.

The round header's buffer carries the flame state and its current effects for both sides.

## Skills

Left and Right browse the acting hero's skills. The focus line is terse - name and what makes it immediately relevant - and the full card lives in the UI buffer: rank requirements, targets, effects, token glyphs each explained by a trailing glossary line. The **upgrade buffer** holds the skill's mastery preview, so you can check what mastering it would change without leaving the fight.

A skill that would move party relationships plays a soft cue when you land on it; press A to hear the affinity consequences spelled out. The base game only telegraphs the negative ones - positive swings arrive as pleasant surprises, which is the most optimistic design decision in the entire product.

## Targeting

Enter on a skill drops the cursor straight onto its first valid target. Arrows browse the rest of the field - a high beep marks a valid target, a low beep an invalid one - and each landing reads the target with the skill's preview against them: hit chance, crit, damage, and what the tokens will do about it. Enter executes; Escape backs out to the skill bar with nothing spent.

## Glances

The glance keys speak in place and never move your cursor. Slots count from the front line on both sides.

- 1-4: that enemy's name and health. Q/W/E/R: the same for your party.
- Shift+slot: their tokens, buffs, debuffs, and dots.
- Ctrl+slot: their resistances.
- S: the acting combatant. T: the focused skill's targets. Shift+T: the turn order.
- F, H, B: the flame, coach, and wallet, as everywhere.

Controller equivalents live in [Controls](controls.md); the short version is that LeftTrigger chords glance and RightTrigger chords read resistances.

## The Inspector

I toggles the game's combatant dossier - the thing sighted players hold Alt for - on the focused combatant, and A and D cycle combatants while it is open. Inside you get the full academic view: stats, skills, tokens, quirks, the works, all buffered.

## Events

The mod speaks the fight as it happens, using the game's own pop text: damage and crits, token gains and losses, dots, resists, wounds, heals, stress, meltdowns, deaths, and kills. Everything spoken also lands in the **combat buffer**, a running log that follows the latest line; walk backwards through it when three things happened at once and you would like them one at a time.

Corpses crumbling on their timer speak a died line too; if hearing the same enemy die twice offends you, the **corpse deaths** toggle in mod announcements is yours.
