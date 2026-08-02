using System;
using System.Collections.Generic;
using DD2A11y.Core.Settings;

namespace DD2A11y.Core.Input {
    /// <summary>
    /// Player rebinding of the mod's own keys. Holds every action's authored default bindings
    /// (snapshotted at <see cref="Load"/>, before overrides apply) and persists per-action
    /// overrides through the settings store: empty = the defaults stand, the
    /// <see cref="Unbound"/> sentinel = the action was stripped of its keys by a displacement,
    /// anything else = serialized bindings. A rebind installs exactly one binding and removes
    /// the same chord from every other action (the game's own duplicate-removal behavior), so
    /// one chord never fires two commands. Parsing and serialization are delegated so this
    /// stays engine-free.
    /// </summary>
    public sealed class ModKeymap {
        private const string Unbound = "none";
        private const char Separator = ';';

        private readonly InputManager _input;
        private readonly ISettingsStore _store;
        private readonly Func<string, InputBinding?> _parse;
        private readonly Action<string> _warn;
        private readonly Dictionary<string, InputBinding[]> _defaults =
            new Dictionary<string, InputBinding[]>();

        public ModKeymap(InputManager input, ISettingsStore store,
                         Func<string, InputBinding?> parse, Action<string> warn) {
            _input = input;
            _store = store;
            _parse = parse;
            _warn = warn;
        }

        /// <summary>Snapshot every registered action's defaults, then apply stored overrides.
        /// Called once, after registration. An override that does not parse is dropped with a
        /// warning and the defaults stand, so a stale or hand-mangled entry never bricks a
        /// key.</summary>
        public void Load() {
            foreach (var action in _input.Actions) {
                var defaults = new InputBinding[action.Bindings.Count];
                for (int i = 0; i < defaults.Length; i++) {
                    defaults[i] = action.Bindings[i];
                }
                _defaults[action.Key] = defaults;

                string stored = _store.GetString(action.Key, "");
                if (stored.Length == 0) {
                    continue;
                }
                if (stored == Unbound) {
                    action.ReplaceBindings(Array.Empty<InputBinding>());
                    continue;
                }
                var parsed = new List<InputBinding>();
                bool ok = true;
                foreach (var part in stored.Split(Separator)) {
                    var binding = _parse(part);
                    if (binding == null) {
                        _warn("keymap: stored binding '" + part + "' for " + action.Key
                              + " did not parse; keeping the default keys");
                        ok = false;
                        break;
                    }
                    parsed.Add(binding);
                }
                if (ok) {
                    action.ReplaceBindings(parsed);
                }
            }
        }

        /// <summary>Install <paramref name="binding"/> as the action's one binding and remove
        /// the same chord from every other action. Returns the actions that lost a key to the
        /// change, for the caller to speak.</summary>
        public IReadOnlyList<InputAction> Rebind(InputAction action, InputBinding binding) {
            var displaced = new List<InputAction>();
            foreach (var other in _input.Actions) {
                if (other == action) {
                    continue;
                }
                var kept = new List<InputBinding>();
                foreach (var existing in other.Bindings) {
                    if (existing.Chord != binding.Chord) {
                        kept.Add(existing);
                    }
                }
                if (kept.Count != other.Bindings.Count) {
                    other.ReplaceBindings(kept);
                    Persist(other);
                    displaced.Add(other);
                }
            }
            action.ReplaceBindings(new[] { binding });
            Persist(action);
            return displaced;
        }

        /// <summary>Restore the action's authored default bindings.</summary>
        public void Reset(InputAction action) {
            action.ReplaceBindings(_defaults[action.Key]);
            _store.SetString(action.Key, "");
        }

        /// <summary>The action's authored default bindings (for the row's buffer line).</summary>
        public IReadOnlyList<InputBinding> DefaultsOf(InputAction action) => _defaults[action.Key];

        private void Persist(InputAction action) {
            if (action.Bindings.Count == 0) {
                _store.SetString(action.Key, Unbound);
                return;
            }
            var parts = new string[action.Bindings.Count];
            for (int i = 0; i < parts.Length; i++) {
                parts[i] = action.Bindings[i].Serialize();
            }
            _store.SetString(action.Key, string.Join(Separator.ToString(), parts));
        }
    }
}
