using System.Collections.Generic;
using DD2A11y.Core.Text;
using Xunit;

namespace DD2A11y.Tests {
    /// <summary>
    /// The shared-affix strip behind the combat resist glance: the repeated RESIST word drops
    /// out of a joined readout whichever side of the name a language puts it on, and labels
    /// come back whole whenever stripping would mangle one.
    /// </summary>
    public class CommonAffixTests {
        [Fact]
        public void SharedSuffix_IsStripped() {
            var terse = CommonAffix.Shorten(new List<string> { "STUN RESIST", "MOVE RESIST", "DEATHBLOW RESIST" });
            Assert.Equal(new List<string> { "STUN", "MOVE", "DEATHBLOW" }, terse);
        }

        [Fact]
        public void SharedPrefix_IsStripped() {
            var terse = CommonAffix.Shorten(new List<string> { "RES. STUN", "RES. MOVE" });
            Assert.Equal(new List<string> { "STUN", "MOVE" }, terse);
        }

        [Fact]
        public void UnspacedSuffix_IsStripped() {
            var terse = CommonAffix.Shorten(new List<string> { "スタン耐性", "移動耐性" });
            Assert.Equal(new List<string> { "スタン", "移動" }, terse);
        }

        [Fact]
        public void LongerSide_Wins() {
            // Both sides shared - "RES " (4 chars) in front, " X" (2) behind - so the front strips.
            var terse = CommonAffix.Shorten(new List<string> { "RES STUN X", "RES MOVE X" });
            Assert.Equal(new List<string> { "STUN X", "MOVE X" }, terse);
        }

        [Fact]
        public void NoSharedAffix_LeavesLabelsWhole() {
            var labels = new List<string> { "STUN", "MOVE", "DEBUFF" };
            Assert.Equal(labels, CommonAffix.Shorten(labels));
        }

        [Fact]
        public void AffixOfOneCharacter_IsKept() {
            var labels = new List<string> { "BLIGHT", "BURNT" };
            Assert.Equal(labels, CommonAffix.Shorten(labels));
        }

        [Fact]
        public void LabelThatIsOnlyTheAffix_KeepsAllWhole() {
            var labels = new List<string> { "RESIST", "STUN RESIST" };
            Assert.Equal(labels, CommonAffix.Shorten(labels));
        }

        [Fact]
        public void SingleLabel_IsUntouched() {
            var labels = new List<string> { "STUN RESIST" };
            Assert.Equal(labels, CommonAffix.Shorten(labels));
        }
    }
}
