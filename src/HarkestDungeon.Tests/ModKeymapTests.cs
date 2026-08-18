using System.Collections.Generic;
using System.Linq;
using DD2A11y.Core.Input;
using DD2A11y.Core.Settings;
using Xunit;

namespace DD2A11y.Tests {
    public class ModKeymapTests {
        private sealed class MemoryStore : ISettingsStore {
            public readonly Dictionary<string, string> Values = new();

            public string GetString(string key, string defaultValue)
                => Values.TryGetValue(key, out var value) ? value : defaultValue;

            public void SetString(string key, string value) => Values[key] = value;
        }

        private sealed class FakeBinding : InputBinding {
            private readonly string _data;

            public FakeBinding(string data) => _data = data;

            public override string DisplayName => _data;
            public override bool JustPressed() => false;
            public override bool Held() => false;
            public override bool Released() => false;
            public override string Type => "fake";
            public override string Serialize() => _data;
        }

        private static InputBinding? Parse(string text)
            => text.StartsWith("bad") ? null : new FakeBinding(text);

        private static (InputManager Input, MemoryStore Store, List<string> Warnings, ModKeymap Keymap) Make() {
            var input = new InputManager();
            var store = new MemoryStore();
            var warnings = new List<string>();
            var keymap = new ModKeymap(input, store, Parse, warnings.Add);
            return (input, store, warnings, keymap);
        }

        [Fact]
        public void Load_AppliesAStoredOverride_AndKeepsTheDefaultSnapshot() {
            var (input, store, _, keymap) = Make();
            var action = input.Register("a", "A", InputCategory.UI).AddBinding(new FakeBinding("X"));
            store.Values["a"] = "Y;Z";

            keymap.Load();

            Assert.Equal(new[] { "Y", "Z" }, action.Bindings.Select(b => b.Serialize()));
            Assert.Equal(new[] { "X" }, keymap.DefaultsOf(action).Select(b => b.Serialize()));
        }

        [Fact]
        public void Load_UnboundSentinel_StripsTheKeys() {
            var (input, store, _, keymap) = Make();
            var action = input.Register("a", "A", InputCategory.UI).AddBinding(new FakeBinding("X"));
            store.Values["a"] = "none";

            keymap.Load();

            Assert.Empty(action.Bindings);
        }

        [Fact]
        public void Load_UnparseableOverride_KeepsDefaultsAndWarns() {
            var (input, store, warnings, keymap) = Make();
            var action = input.Register("a", "A", InputCategory.UI).AddBinding(new FakeBinding("X"));
            store.Values["a"] = "Y;bad1";

            keymap.Load();

            Assert.Equal(new[] { "X" }, action.Bindings.Select(b => b.Serialize()));
            Assert.Single(warnings);
        }

        [Fact]
        public void Add_AppendsToTheSet_AndPersists() {
            var (input, store, _, keymap) = Make();
            var action = input.Register("a", "A", InputCategory.UI).AddBinding(new FakeBinding("X"));
            keymap.Load();

            keymap.Add(action, new FakeBinding("Y"));

            Assert.Equal(new[] { "X", "Y" }, action.Bindings.Select(b => b.Serialize()));
            Assert.Equal("X;Y", store.Values["a"]);
        }

        [Fact]
        public void Remove_DeletesByChord_AndTheLastDeletePersistsAsUnbound() {
            var (input, store, _, keymap) = Make();
            var action = input.Register("a", "A", InputCategory.UI)
                .AddBinding(new FakeBinding("X")).AddBinding(new FakeBinding("Y"));
            keymap.Load();

            keymap.Remove(action, new FakeBinding("X"));
            Assert.Equal(new[] { "Y" }, action.Bindings.Select(b => b.Serialize()));
            Assert.Equal("Y", store.Values["a"]);

            keymap.Remove(action, new FakeBinding("Y"));
            Assert.Empty(action.Bindings);
            Assert.Equal("none", store.Values["a"]);
        }

        [Fact]
        public void Carries_MatchesByChord() {
            var (input, _, _, keymap) = Make();
            var action = input.Register("a", "A", InputCategory.UI).AddBinding(new FakeBinding("X"));
            keymap.Load();

            Assert.True(keymap.Carries(action, new FakeBinding("X")));
            Assert.False(keymap.Carries(action, new FakeBinding("Y")));
        }

        [Fact]
        public void Reset_RestoresTheDefaults_AndClearsTheStoredOverride() {
            var (input, store, _, keymap) = Make();
            var action = input.Register("a", "A", InputCategory.UI)
                .AddBinding(new FakeBinding("X")).AddBinding(new FakeBinding("X2"));
            keymap.Load();
            keymap.Add(action, new FakeBinding("Y"));

            keymap.Reset(action);

            Assert.Equal(new[] { "X", "X2" }, action.Bindings.Select(b => b.Serialize()));
            Assert.Equal("", store.Values["a"]);
        }

        [Fact]
        public void RoundTrip_ADeletedSetStaysDeletedAcrossALoad() {
            var (input, store, _, keymap) = Make();
            var action = input.Register("a", "A", InputCategory.UI).AddBinding(new FakeBinding("X"));
            keymap.Load();
            keymap.Remove(action, new FakeBinding("X"));

            // A fresh session over the same store: the emptied action must not resurrect its
            // default behind the player's back.
            var input2 = new InputManager();
            var action2 = input2.Register("a", "A", InputCategory.UI).AddBinding(new FakeBinding("X"));
            new ModKeymap(input2, store, Parse, _ => { }).Load();

            Assert.Empty(action2.Bindings);
        }
    }
}
