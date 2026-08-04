using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Game {
    /// <summary>
    /// Names the game's inline sprite glyphs for speech (the SpriteText resolver). A token glyph
    /// ("token_combo") resolves through the game's own unglyphed token name loc key
    /// ("token_name_combo"), a dot glyph ("icon_blight") through its dot name ("dot_name_blight");
    /// the effect and stat icons that have no name string anywhere in the game carry authored
    /// words. An icon outside all of that speaks its own humanized name (game-derived, like a raw
    /// id fallback) - a glyph in running text carries meaning, so dropping one silently loses
    /// information ("-2" with no stat). Only known-decorative glyphs are dropped.
    /// </summary>
    public static class SpriteWords {
        private const string TokenPrefix = "token_";
        private const string IconPrefix = "icon_";
        private const string CostPrefix = "cost_";

        public static string Resolve(string spriteName) {
            if (spriteName.StartsWith(TokenPrefix, System.StringComparison.Ordinal)) {
                // An icon id can diverge from its token id ("token_blind-line" is blind's icon),
                // so the glossary's icon-to-token mapping is tried before falling back. Not
                // every token glyph has a token behind it at all (the stress-over-time glyph is
                // "token_stress") - the humanized name keeps the meaning there.
                string bareToken = spriteName.Substring(TokenPrefix.Length);
                string name = GameLoc.TryGet("token_name_" + bareToken);
                if (name != null) {
                    return name;
                }
                string tokenId = TokenGlossary.TokenIdOf(spriteName);
                if (tokenId != null) {
                    name = GameLoc.TryGet("token_name_" + tokenId);
                }
                return name ?? Humanize(bareToken);
            }
            if (spriteName.StartsWith(CostPrefix, System.StringComparison.Ordinal)) {
                // Currency glyphs in cost text ("<cost_faction> 8"); the faction currency's
                // display name is Baubles, which no game string spells out.
                return spriteName == "cost_faction" ? S.SpriteBaubles
                    : Humanize(spriteName.Substring(CostPrefix.Length));
            }
            if (!spriteName.StartsWith(IconPrefix, System.StringComparison.Ordinal)) {
                return null;
            }
            string bare = spriteName.Substring(IconPrefix.Length);
            string dotName = GameLoc.TryGet("dot_name_" + bare);
            if (dotName != null) {
                return dotName;
            }
            switch (spriteName) {
                // Effect icons the game appends to skill text (its ActorDataEffectDescription)
                // and the stat icons its trinket/buff lines use as their only stat "word".
                case "icon_healthup": return S.SpriteHeal;
                case "icon_buff": return S.SpriteBuff;
                case "icon_debuff": return S.SpriteDebuff;
                case "icon_stress":
                case "icon_stress_white": return S.SpriteStress;
                case "icon_disease_outline": return S.SpriteDisease;
                case "icon_speed": return S.SpriteSpeed;
                case "icon_health":
                case "icon_health_v2": return S.SpriteHealth;
                // Decorative: the seal glyph always precedes the path's own name text.
                case "icon_heroseal": return null;
                default: return Humanize(bare);
            }
        }

        // "candle_glyph" -> "candle": the identifier's own words, minus rendering suffixes.
        private static string Humanize(string bare) {
            foreach (var suffix in new[] { "_glyph", "_outline", "_white", "_v2" }) {
                if (bare.EndsWith(suffix, System.StringComparison.Ordinal)) {
                    bare = bare.Substring(0, bare.Length - suffix.Length);
                    break;
                }
            }
            return bare.Replace('_', ' ');
        }
    }
}
