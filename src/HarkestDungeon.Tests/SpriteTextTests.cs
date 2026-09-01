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

        private static string? Mastery(string name) => name == "icon_heropoints" ? "Mastery" : null;

        [Fact]
        public void GlyphCaptionedByTheWordAfterItIsDroppedThroughMarkup()
            => Assert.Equal("+2 Mastery", WithResolver(Mastery,
                "+2<font=\"NDDunkelD-Bold SDF\"><sprite name=\"icon_heropoints\"></font> Mastery"));

        [Fact]
        public void GlyphCaptionedByTheWordBeforeItIsDroppedCaseInsensitively()
            => Assert.Equal("costs one mastery. Baubles are used", WithResolver(
                name => name == "icon_heropoints" ? "Mastery" : name == "icon_factioncurrency" ? "Baubles" : null,
                "costs one mastery <sprite name=\"icon_heropoints\">.\nBaubles <sprite name=\"icon_factioncurrency\"> are used"));

        [Fact]
        public void GlyphCaptionedByAColoredWordBeforeAPunctuationMarkIsDropped()
            => Assert.Equal("for relics, baubles.", WithResolver(
                name => name == "icon_relic" ? "Relics" : null,
                "for <color=#FFF>relics</color> <sprite name=\"icon_relic\">, baubles."));

        [Fact]
        public void GlyphCaptionedByATwoWordCompoundBeforeItIsDropped()
            => Assert.Equal("Mastery points are earned", WithResolver(Mastery,
                "Mastery points <sprite name=\"icon_heropoints\"> are earned"));

        [Fact]
        public void GlyphCaptionedInParenthesesIsDropped()
            => Assert.Equal("repair (armor) and (wheels)", WithResolver(
                name => name == "icon_currency_armor" ? "Armor" : name == "icon_currency_wheel" ? "Wheels" : null,
                "repair <sprite name=\"icon_currency_armor\"> (armor) and <sprite name=\"icon_currency_wheel\"> (wheels)"));

        [Fact]
        public void GlyphWhoseWordOnlyRecursTwoWordsAheadIsSpoken()
            => Assert.Equal("Quirks with rare are rare.", WithResolver(
                name => name == "icon_rare_quirk" ? "rare" : null,
                "Quirks with <sprite name=\"icon_rare_quirk\"> are rare."));

        [Fact]
        public void GlyphBesideALongerWordIsSpoken()
            => Assert.Equal("Masterful Mastery", WithResolver(Mastery,
                "Masterful <sprite name=\"icon_heropoints\">"));

        [Fact]
        public void RepeatedGlyphsAreEachSpoken()
            => Assert.Equal("stress stress", WithResolver(
                name => name == "token_stress" ? "stress" : null,
                "<sprite name=\"token_stress\"><sprite name=\"token_stress\">"));

        [Fact]
        public void ResolvedWordInsideColorMarkupReadsInline()
            => Assert.Equal("when target Combo", WithResolver(
                name => name == "token_combo" ? "Combo" : null,
                "when target <color=#B0935EFF><sprite name=\"token_combo\"></color>"));
    }
}
