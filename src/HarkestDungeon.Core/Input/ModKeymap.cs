using System;
using System.Collections.Generic;
using DD2A11y.Core.Settings;

namespace DD2A11y.Core.Input {
    /// <summary>
    /// Player rebinding of the mod's own keys. Holds every action's authored default bindings
    /// (snapshotted at <see cref="Load"/>, before overrides apply) and persists per-action
    /// overrides through the settings store: empty = the defaults stand, the
    /// <see cref="Unbound"/> sentinel = every binding was deleted, anything else = serialized
    /// bindings. An action carries a LIST of bindings, grown and shrunk one at a time
    /// (say-the-spire2's model); several actions may share a chord, since screens reuse
    /// chords across categories and the live-category priority picks the command. Parsing
    /// and serialization are delegated so this stays engine-free.
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

        /// <summary>Whether the action already carries this chord (an add of it is a no-op).</summary>
        public bool Carries(InputAction action, InputBinding binding) {
            foreach (var existing in action.Bindings) {
                if (existing.Chord == binding.Chord) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Append a binding to the action's set and persist.</summary>
        public void Add(InputAction action, InputBinding binding) {
            var grown = new List<InputBinding>(action.Bindings) { binding };
            action.ReplaceBindings(grown);
            Persist(action);
        }

        /// <summary>Delete one binding (by chord) from the action's set and persist.</summary>
        public void Remove(InputAction action, InputBinding binding) {
            var kept = new List<InputBinding>();
            foreach (var existing in action.Bindings) {
                if (existing.Chord != binding.Chord) {
                    kept.Add(existing);
                }
            }
            action.ReplaceBindings(kept);
            Persist(action);
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
