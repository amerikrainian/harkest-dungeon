using System;
using DD2A11y.Core.Text;
using Xunit;

namespace DD2A11y.Tests {
    // SpriteText.Resolver is process-global, so each test installs its resolver inside a
    // try/finally that restores null; the collection keeps these tests off other threads.
    [Collection("SpriteTextResolver")]
    public class SpriteTextTests {
        private static string WithResolver(Func<string, string?> resolver, string raw) {
            SpriteText.Resolver = resolver;
            try {
                return TextFilter.Clean(raw);
            } finally {
                SpriteText.Resolver = null;
            }
        }

        [Fact]
        public void ResolvedSpriteBecomesAWord()
            => Assert.Equal("heal 10%", WithResolver(
                name => name == "icon_healthup" ? "heal" : null,
                "<sprite name=\"icon_healthup\">10%"));

        [Fact]
        public void UnresolvedSpriteIsStrippedLikeMarkup()
            => Assert.Equal("10%", WithResolver(name => null, "<sprite name=\"icon_mystery\"> 10%"));

        [Fact]
        public void WithoutAResolverSpritesAreStripped()
            => Assert.Equal("10%", TextFilter.Clean("<sprite name=\"icon_healthup\">10%"));

        [Fact]
        public void SpriteTagWithExtraAttributesResolves()
            => Assert.Equal("Combo", WithResolver(
                name => name == "token_combo" ? "Combo" : null,
                "<sprite name=\"token_combo\" color=#FFFFFFFF>"));

        [Fact]
        public void StripRemovesTheGlyphAWordAlreadyStandsBesideOf()
            => Assert.Equal("+2 Mastery", WithResolver(
                name => name == "icon_heropoints" ? "heropoints" : null,
                SpriteText.Strip("+2<font=\"NDDunkelD-Bold SDF\"><sprite name=\"icon_heropoints\"></font> Mastery")));

        [Fact]
        public void ResolvedWordInsideColorMarkupReadsInline()
            => Assert.Equal("when target Combo", WithResolver(
                name => name == "token_combo" ? "Combo" : null,
                "when target <color=#B0935EFF><sprite name=\"token_combo\"></color>"));
    }
}
