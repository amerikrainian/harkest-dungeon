using Assets.Code.Kingdom.UI;
using DD2A11y.Core.Text;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Elements {
    /// <summary>A gang card in the kingdom creation wizard: single-select like a radio row, so it
    /// reads its chosen state and re-announces after picking (the game's own feedback is the
    /// gang's narration).</summary>
    public sealed class KingdomGangElement : SelectableElement {
        private readonly KingdomSelectGangItem _item;

        public KingdomGangElement(KingdomSelectGangItem item) : base(item) {
            _item = item;
        }

        public override string Value => _item.IsActivated
            ? SpokenLine.Join(S.StatusSelected, base.Value)
            : base.Value;

        public override bool ReannounceOnActivate => true;
    }
}
