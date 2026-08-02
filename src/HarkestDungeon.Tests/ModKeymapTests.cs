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
        public void Rebind_Persists_AndDisplacesTheChordFromOtherActions() {
            var (input, store, _, keymap) = Make();
            var up = input.Register("up", "Up", InputCategory.UI).AddBinding(new FakeBinding("X"));
            var down = input.Register("down", "Down", InputCategory.UI).AddBinding(new FakeBinding("Y"));
            keymap.Load();

            var displaced = keymap.Rebind(up, new FakeBinding("Y"));

            Assert.Equal(new[] { down }, displaced);
            Assert.Equal(new[] { "Y" }, up.Bindings.Select(b => b.Serialize()));
            Assert.Empty(down.Bindings);
            Assert.Equal("Y", store.Values["up"]);
            Assert.Equal("none", store.Values["down"]);
        }

        [Fact]
        public void Rebind_ToItsOwnChord_DisplacesNothing() {
            var (input, _, _, keymap) = Make();
            var action = input.Register("a", "A", InputCategory.UI).AddBinding(new FakeBinding("X"));
            keymap.Load();

            Assert.Empty(keymap.Rebind(action, new FakeBinding("X")));
            Assert.Equal(new[] { "X" }, action.Bindings.Select(b => b.Serialize()));
        }

        [Fact]
        public void Reset_RestoresTheDefaults_AndClearsTheStoredOverride() {
            var (input, store, _, keymap) = Make();
            var action = input.Register("a", "A", InputCategory.UI)
                .AddBinding(new FakeBinding("X")).AddBinding(new FakeBinding("X2"));
            keymap.Load();
            keymap.Rebind(action, new FakeBinding("Y"));

            keymap.Reset(action);

            Assert.Equal(new[] { "X", "X2" }, action.Bindings.Select(b => b.Serialize()));
            Assert.Equal("", store.Values["a"]);
        }

        [Fact]
        public void RoundTrip_AStrippedActionStaysStrippedAcrossALoad() {
            var (input, store, _, keymap) = Make();
            var up = input.Register("up", "Up", InputCategory.UI).AddBinding(new FakeBinding("X"));
            var down = input.Register("down", "Down", InputCategory.UI).AddBinding(new FakeBinding("Y"));
            keymap.Load();
            keymap.Rebind(up, new FakeBinding("Y"));

            // A fresh session over the same store: the displaced action must not resurrect its
            // default (which would silently re-conflict with the rebind).
            var input2 = new InputManager();
            var up2 = input2.Register("up", "Up", InputCategory.UI).AddBinding(new FakeBinding("X"));
            var down2 = input2.Register("down", "Down", InputCategory.UI).AddBinding(new FakeBinding("Y"));
            new ModKeymap(input2, store, Parse, _ => { }).Load();

            Assert.Equal(new[] { "Y" }, up2.Bindings.Select(b => b.Serialize()));
            Assert.Empty(down2.Bindings);
        }
    }
}
