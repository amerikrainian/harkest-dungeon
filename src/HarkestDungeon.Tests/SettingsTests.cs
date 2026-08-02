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
        public void IntSetting_UsesDefaultWhenNothingStored_AndParsesStored() {
            var store = new MemoryStore();
            Assert.Equal(80, new IntSetting("k", () => "label", 80, 20, 200, store).Value);

            store.Values["k"] = "120";
            Assert.Equal(120, new IntSetting("k", () => "label", 80, 20, 200, store).Value);

            store.Values["k"] = "999";
            Assert.Equal(200, new IntSetting("k", () => "label", 80, 20, 200, store).Value);

            store.Values["k"] = "far";
            Assert.Equal(80, new IntSetting("k", () => "label", 80, 20, 200, store).Value);
        }

        [Fact]
        public void IntSetting_SetClampsAndPersists_AndResetRestoresTheDefault() {
            var store = new MemoryStore();
            var setting = new IntSetting("k", () => "label", 80, 20, 200, store);

            setting.Set(83);
            Assert.Equal(83, setting.Value);
            Assert.Equal("83", store.Values["k"]);

            setting.Set(999);
            Assert.Equal(200, setting.Value);

            setting.Set(1);
            Assert.Equal(20, setting.Value);

            setting.Reset();
            Assert.Equal(80, setting.Value);
            Assert.Equal("80", store.Values["k"]);
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
