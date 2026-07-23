using DD2A11y.Core.Text;
using Xunit;

namespace DD2A11y.Tests {
    public class TextFilterTests {
        [Fact]
        public void StripsRichTextTags()
            => Assert.Equal("Continue", TextFilter.Clean("<color=#FF0000><b>Continue</b></color>"));

        [Fact]
        public void FoldsTypographicPunctuation()
            => Assert.Equal("wait - 'go'...", TextFilter.Clean("wait — ‘go’…"));

        [Fact]
        public void LineBreakBecomesSentenceBreak()
            => Assert.Equal("First. Second", TextFilter.Clean("First\nSecond"));

        [Fact]
        public void LineBreakAfterSentencePunctuationIsJustASpace()
            => Assert.Equal("First. Second", TextFilter.Clean("First.\nSecond"));

        [Fact]
        public void PeriodCommaCollisionKeepsTheDeliberateMark() {
            Assert.Equal("end, next", TextFilter.Clean("end. , next"));
            Assert.Equal("end. next", TextFilter.Clean("end, . next"));
        }

        [Fact]
        public void CollapsesWhitespaceAndNbsp()
            => Assert.Equal("a b", TextFilter.Clean("a    b "));

        [Fact]
        public void EmptyAndNullAreEmpty() {
            Assert.Equal("", TextFilter.Clean(null));
            Assert.Equal("", TextFilter.Clean("  "));
        }

        [Fact]
        public void SpokenLineJoinsNonEmptyParts()
            => Assert.Equal("Continue, button", SpokenLine.Join("Continue", null, "button", ""));
    }
}
