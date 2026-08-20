# Buffers

Buffers are review lists for extra information. Moving focus chooses what information is available; buffer commands let you inspect it without moving focus away. The focus announcement stays short ("Continue, button"), and everything longer the element carries - tooltips, stat blocks, skill text - lands in a buffer, one line per tooltip.

## Default Buffer Controls

- Ctrl+Right: next buffer. Ctrl+Left: previous buffer.
- Ctrl+Up: next line. Ctrl+Down: previous line.
- The right stick performs the same actions on controller.

Switching buffers speaks the buffer's name and its current line. Buffers repopulate from the live element on every press, so they never go stale.

## The Roster

- **UI**: the focused element's own details - its line, then one line per tooltip, then a glossary line for each token glyph the text mentions (the same "name: description" a sighted player gets on mouse-over).
- **Upgrade**: the focused skill's mastery preview, under the game's own upgrade header. A skill with nothing left to learn says so once instead of listing nothing.
- **Hero**: the vitals of the hero the focused element concerns - whoever's trinket, skill, or chair you are on.
- **Enemies** and **Party**: one overview line per combatant, filled only in combat.
- **Combat**: the battle-event log. It follows the latest entry, so Ctrl+Down walks backwards through what just happened.
- **Subtitles**: the on-screen subtitle history, kept only while the game's own Subtitles setting is on. Cinematics narrate themselves; this is how you read them.

Only buffers with something to say are available; the review keys skip empty ones, so each screen cycles only the buffers that answer there.
