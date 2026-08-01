using System.Collections.Generic;
using DD2A11y.Core.Settings;
using DD2A11y.Core.Text;
using Xunit;

namespace DD2A11y.Tests {
    public class SettingsTests {
        private sealed class MemoryStore : ISettingsStore {
            public readonly Dictionary<string, string> Values = new();

            public string GetString(string key, string defaultValue)
                => Values.TryGetValue(key, out var value) ? value : defaultValue;

            public void SetString(string key, string value) => Values[key] = value;
        }

        [Fact]
        public void TextSetting_UsesDefaultWhenNothingStored() {
            var setting = new TextSetting("k", () => "label", "fallback", new MemoryStore());
            Assert.Equal("fallback", setting.Value);
        }

        [Fact]
        public void TextSetting_LoadsStoredValue() {
            var store = new MemoryStore();
            store.Values["k"] = "stored";
            var setting = new TextSetting("k", () => "label", "fallback", store);
            Assert.Equal("stored", setting.Value);
        }

        [Fact]
        public void TextSetting_SetPersistsAndApplies() {
            var store = new MemoryStore();
            string? applied = null;
            var setting = new TextSetting("k", () => "label", "d", store, v => applied = v);
            Assert.Equal("d", applied); // the load applies too

            setting.Set(" - ");
            Assert.Equal(" - ", store.Values["k"]);
            Assert.Equal(" - ", setting.Value);
            Assert.Equal(" - ", applied);
        }

        [Fact]
        public void TextSetting_ResetRestoresDefault() {
            var store = new MemoryStore();
            store.Values["k"] = "custom";
            var setting = new TextSetting("k", () => "label", "d", store);
            setting.Reset();
            Assert.Equal("d", setting.Value);
            Assert.Equal("d", store.Values["k"]);
        }

        [Fact]
        public void SeparatorSetting_DrivesSpokenLineJoin() {
            string before = SpokenLine.Separator;
            try {
                var store = new MemoryStore();
                var settings = new ModSettings(store);
                settings.Separator.Set(" - ");
                Assert.Equal("Exit game - button", SpokenLine.Join("Exit game", "button"));
            } finally {
                SpokenLine.Separator = before;
            }
        }

        [Fact]
        public void SpokenChars_SpellsCharactersWithNamedSpace() {
            Assert.Equal(", space", SpokenChars.Spell(", "));
            Assert.Equal("space - space", SpokenChars.Spell(" - "));
            Assert.Equal("", SpokenChars.Spell(""));
        }
    }
}
