using System.Collections.Generic;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Game {
    /// <summary>
    /// Names the game's inline sprite glyphs for speech (the SpriteText resolver). A token glyph
    /// ("token_combo") resolves through the game's own unglyphed token name loc key
    /// ("token_name_combo"), a dot glyph ("icon_blight") through its dot name ("dot_name_blight"),
    /// most other icons through the game's story-preview icon words
    /// ("story_icon_description_icon_move_Preview" is the word for "icon_move") or the loc keys
    /// <see cref="ResidualName"/> pairs them with; the few effect icons with no name string
    /// anywhere in the game carry authored words. An icon outside all of that speaks its own
    /// humanized name (game-derived, like a raw id fallback) - a glyph in running text carries
    /// meaning, so dropping one silently loses information ("-2" with no stat). The fallback is
    /// raw-id English whatever the game language, so each first use is logged: a logged glyph is
    /// a candidate for a real mapping here. Only known-decorative glyphs are dropped.
    /// </summary>
    public static class SpriteWords {
        private const string TokenPrefix = "token_";
        private const string IconPrefix = "icon_";
        private const string CostPrefix = "cost_";

        public static string Resolve(string spriteName) {
            if (spriteName.StartsWith(TokenPrefix, System.StringComparison.Ordinal)) {
                // An icon id can diverge from its token id ("token_blind-line" is blind's icon),
                // so the glossary's icon-to-token mapping is tried before falling back.
                string bareToken = spriteName.Substring(TokenPrefix.Length);
                string name = GameLoc.TryGet("token_name_" + bareToken);
                if (name != null) {
                    return name;
                }
                string tokenId = TokenGlossary.TokenIdOf(spriteName);
                if (tokenId != null) {
                    name = GameLoc.TryGet("token_name_" + tokenId);
                    if (name != null) {
                        return name;
                    }
                }
                // Not every token glyph has a token behind it: "token_horror" is the horror
                // dot's glyph, and the stress-over-time glyph is "token_stress", named by the
                // authored stress word.
                string trimmedToken = TrimRenderSuffix(bareToken);
                return GameLoc.TryGet("dot_name_" + trimmedToken)
                    ?? (trimmedToken == "stress" ? S.SpriteStress : Fallback(spriteName, trimmedToken));
            }
            if (spriteName.StartsWith(CostPrefix, System.StringComparison.Ordinal)) {
                // Currency glyphs in cost text ("<cost_faction> 8"); the faction currency's
                // Baubles name lives on its inventory tooltip.
                if (spriteName == "cost_faction") {
                    return GameLoc.TryGet("inventory_tooltip_biome_currency") ?? S.SpriteBaubles;
                }
                string bareCost = TrimRenderSuffix(spriteName.Substring(CostPrefix.Length));
                return ResidualName(bareCost) ?? Fallback(spriteName, bareCost);
            }
            if (!spriteName.StartsWith(IconPrefix, System.StringComparison.Ordinal)) {
                return null;
            }
            string bare = spriteName.Substring(IconPrefix.Length);
            string dotName = GameLoc.TryGet("dot_name_" + bare);
            if (dotName != null) {
                return dotName;
            }
            string word = TrimRenderSuffix(bare);
            if (word != bare) {
                // A suffixed dot glyph still names its dot.
                dotName = GameLoc.TryGet("dot_name_" + word);
                if (dotName != null) {
                    return dotName;
                }
            }
            // The game's road-story icon legend names many plain icons too ("icon_move" is
            // "Move" there) - its own word for the glyph, in the game language.
            string storyName = GameLoc.TryGet("story_icon_description_" + spriteName + "_Preview");
            if (storyName != null) {
                return storyName;
            }
            switch (word) {
                // Effect icons the game appends to skill text (its ActorDataEffectDescription)
                // and the stat icons its trinket/buff lines use as their only stat "word".
                case "healthup": return S.SpriteHeal;
                case "buff": return S.SpriteBuff;
                case "debuff": return S.SpriteDebuff;
                case "stress": return S.SpriteStress;
                case "disease": return S.SpriteDisease;
                case "speed": return S.SpriteSpeed;
                case "health": return S.SpriteHealth;
                // The deathblow-resist glyph; its humanized name ("death") loses the resist
                // meaning the icon carries ("+4% death" for a deathblow-RES buff).
                case "death": return S.SpriteDeathblow;
                // Decorative: the seal glyph always precedes the path's own name text, the
                // laurel glyph the "Upgrade" caption's own word.
                case "heroseal": return null;
                case "upgraded_skill": return null;
            }
            // A stat icon depicting a token ("icon_stun" in "+10% <icon_stun> RES" buff lines)
            // carries the token's own name.
            return GameLoc.TryGet("token_name_" + word)
                ?? ResidualName(word)
                ?? Fallback(spriteName, word);
        }

        // Icons whose word lives on an unrelated game string: the currency tooltips, the coach
        // stat sources, the Loathing meter title, the regen dot's "hot" tag.
        private static string ResidualName(string word) {
            switch (word) {
                case "heropoints": return GameLoc.TryGet("inventory_tooltip_heropoints");
                case "relic": return GameLoc.TryGet("inventory_tooltip_relics");
                case "factioncurrency": return GameLoc.TryGet("inventory_tooltip_biome_currency");
                case "candle": return GameLoc.TryGet("item_name_candles");
                case "materials": return GameLoc.TryGet("item_name_materials");
                case "currency_armor": return GameLoc.TryGet("source_stage_coach_armor");
                case "currency_wheel": return GameLoc.TryGet("source_stage_coach_wheels");
                case "doom": return GameLoc.TryGet("doom_meter_tooltip_title");
                case "regen": return GameLoc.TryGet("dot_name_hot");
                case "torch_wagon":
                    return GameLoc.TryGet("story_icon_description_icon_story_torch_Preview");
                default: return null;
            }
        }

        // "candle_glyph" -> "candle": rendering suffixes off, the identifier itself kept.
        private static string TrimRenderSuffix(string bare) {
            foreach (var suffix in new[] { "_glyph", "_outline", "_white", "_v2" }) {
                if (bare.EndsWith(suffix, System.StringComparison.Ordinal)) {
                    return bare.Substring(0, bare.Length - suffix.Length);
                }
            }
            return bare;
        }

        private static readonly HashSet<string> LoggedFallbacks = new HashSet<string>();

        // The identifier's own words - raw-id English in every language, so the first use of
        // each glyph is logged as a mapping candidate.
        private static string Fallback(string spriteName, string bare) {
            string word = bare.Replace('_', ' ');
            if (LoggedFallbacks.Add(spriteName)) {
                Plugin.Log.LogInfo($"sprite word fallback: {spriteName} -> \"{word}\"");
            }
            return word;
        }
    }
}
