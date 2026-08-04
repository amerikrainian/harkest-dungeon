using System.Collections.Generic;
using static DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Core.Nav {
    /// <summary>
    /// A navigable element. Leaves yield the parts that compose into the spoken focus message and
    /// advertise actions (activate, back, ...); they do NOT handle keys or navigation - the Navigator
    /// does. The focus message is terse (label, role, value); anything longer the element carries
    /// (tooltips, descriptions) goes into <see cref="GetBufferLines"/> for on-demand review.
    /// </summary>
    public abstract class UIElement {
        public Container? Parent { get; internal set; }

        public virtual bool CanFocus => true;

        /// <summary>A state word spoken before everything else ("selected"), or null. Selection
        /// state leads the line so a list scan lands on the current choice instantly.</summary>
        public virtual string? Status => null;

        /// <summary>The element's name/text. Read live at announce time (never cached).</summary>
        public virtual string? Label => null;

        /// <summary>A short type word spoken after the label (e.g. "button", "toggle"), or null.</summary>
        public virtual string? Role => null;

        /// <summary>The element's current value/state (e.g. "on", "50 percent"), or null.</summary>
        public virtual string? Value => null;

        /// <summary>
        /// True if activating changes this element's value in place (a toggle, a stepper) so the
        /// navigator re-announces it. False for buttons that open another screen (the screen change
        /// announces itself).
        /// </summary>
        public virtual bool ReannounceOnActivate => false;

        /// <summary>The actions this element advertises. Navigators invoke them by id.</summary>
        public virtual IEnumerable<ElementAction> GetActions() { yield break; }

        /// <summary>The option menu activating this element opens (a dropdown's choices), built
        /// fresh per open, or null for an element that activates in place. When non-null, the
        /// navigator opens it instead of invoking the activate action.</summary>
        public virtual Popup? BuildPopup() => null;

        /// <summary>Find an advertised action by id and run it. Returns true if found.</summary>
        public bool InvokeAction(string id) {
            foreach (var a in GetActions()) {
                if (a.Id == id) {
                    a.Execute();
                    return true;
                }
            }
            return false;
        }

        /// <summary>The composed spoken focus message: status, label, role, value, joined by ", "
        /// (non-empty only). Virtual so an element with a richer composition can override the
        /// default join.</summary>
        public virtual string GetFocusText()
            => Text.SpokenLine.Join(Status, Label, Role, Value);

        /// <summary>What a post-activation re-announce speaks. Virtual for elements whose
        /// activation changes their label rather than a value (an equip slot).</summary>
        public virtual string GetValueText() => Text.SpokenLine.Join(Status, Value);

        /// <summary>What to announce after an in-place adjust action (increase/decrease) just ran;
        /// <paramref name="changed"/> is whether the value text actually moved. A move reads the new
        /// value; an adjust that moved nothing hit a bound, so the default names it ("minimum" /
        /// "maximum") rather than read the same value back. An element overrides this for a control
        /// whose ends are not a magnitude (a dropdown re-reads its choice).</summary>
        public virtual string GetAdjustText(string actionId, bool changed) {
            if (changed) {
                return GetValueText();
            }
            return actionId == ActionIds.Increase ? StatusMaximum : StatusMinimum;
        }

        /// <summary>The buffer's own line for this element: the focus message without the role
        /// word - the buffer reviews content, and the control type is not content.</summary>
        public virtual string GetBufferHeadText()
            => Text.SpokenLine.Join(Status, Label, Value);

        /// <summary>The element's review lines for the buffer system: its role-less own line
        /// first (<see cref="GetBufferHeadText"/>), then one line per detail the focus message
        /// leaves out - each tooltip, each stat block line - dropping blanks and details that
        /// only repeat the head's label or value (compared through the speech filter, so a
        /// markup-carrying tooltip title still folds into the plain label). Read live on every
        /// buffer keypress, so lines never go stale.</summary>
        public IEnumerable<string> GetBufferLines() {
            yield return GetBufferHeadText();
            string label = Text.TextFilter.Clean(Label);
            string value = Text.TextFilter.Clean(Value);
            foreach (var line in GetDetailLines()) {
                string clean = Text.TextFilter.Clean(line);
                if (string.IsNullOrWhiteSpace(clean) || clean == label || clean == value) {
                    continue;
                }
                yield return line;
            }
        }

        /// <summary>The detail lines behind the head line (tooltips, stat blocks, descriptions);
        /// the base carries none.</summary>
        protected virtual IEnumerable<string> GetDetailLines() {
            yield break;
        }

        /// <summary>Called by the navigator when this element becomes the focused leaf after a move.
        /// The default does nothing; an engine-coupled element overrides it to sync the game's own
        /// cursor to our focus where that is side-effect-free.</summary>
        public virtual void OnFocused() { }
    }
}
