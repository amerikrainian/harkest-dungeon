using System;
using System.IO;
using Assets.Code.Locale;
using Assets.Code.Utils;
using DD2A11y.Core.Strings;
using HarmonyLib;

namespace DD2A11y {
    /// <summary>
    /// Follows the game language: when it changes, loads the matching mod translation from
    /// lang/&lt;code&gt;.txt (falling back to the bare prefix, e.g. de for de_DE), or resets to
    /// English when no file exists. A postfix on the game's language setter applies a switch
    /// synchronously, so even the read-back of the control that committed it speaks the new
    /// language; the per-frame check covers boot (before the game restores its persisted
    /// language nothing is applied - <see cref="Resolved"/> flips once the first real
    /// language lands) and any setter path the patch missed.
    /// </summary>
    public sealed class LanguageSync {
        private static readonly System.Reflection.MethodInfo GetActualMethod =
            AccessTools.Method(typeof(LanguageDefinition), "GetActual");
        private static readonly System.Reflection.FieldInfo LanguageField =
            AccessTools.Field(typeof(LanguageDefinition), "m_language");

        private static LanguageSync _instance;

        private readonly string _langDir;
        private string _applied;

        public LanguageSync(string langDir) {
            _langDir = langDir;
        }

        /// <summary>Whether the game's language has been read at least once (the boot-time
        /// announcement waits on this so it speaks in the restored language).</summary>
        public bool Resolved => _applied != null;

        public void Tick() => SyncNow();

        private void SyncNow() {
            string code = CurrentCode();
            if (code == null || code == _applied) {
                return;
            }
            _applied = code;
            Apply(code);
        }

        /// <summary>Postfix the game's language setter so a switch lands in the strings table
        /// before anything later in the same call stack composes speech.</summary>
        public void AttachLanguagePatch() {
            _instance = this;
            var target = AccessTools.Method(typeof(Localization), nameof(Localization.SetLanguage));
            if (target == null) {
                Plugin.Log.LogError(
                    "language sync: Localization.SetLanguage not found; translations apply a frame late");
                return;
            }
            new Harmony("dd2a11y.language").Patch(target,
                postfix: new HarmonyMethod(AccessTools.Method(typeof(LanguageSync), nameof(LanguageSet))));
        }

        private static void LanguageSet() {
            try {
                _instance.SyncNow();
            } catch (Exception ex) {
                // Never let a mod failure escape into the game's language setter.
                Plugin.Log.LogError("language sync: applying on language change failed: " + ex);
            }
        }

        private static string CurrentCode() {
            try {
                var loc = Singleton<Localization>.Instance;
                var language = loc?.GetLanguage();
                if (language == null) {
                    return null;
                }
                var actual = GetActualMethod.Invoke(language, null) as LanguageDefinition ?? language;
                return LanguageField.GetValue(actual) as string ?? "en";
            } catch (Exception ex) {
                Plugin.Log.LogWarning("language sync: could not read game language: " + ex.Message);
                return "en";
            }
        }

        private void Apply(string code) {
            foreach (var candidate in new[] { code, code.ToLowerInvariant(), Prefix(code) }) {
                string file = Path.Combine(_langDir, candidate + ".txt");
                if (!File.Exists(file)) {
                    continue;
                }
                var entries = Translation.ParseFile(File.ReadAllText(file), out var errors);
                foreach (var error in errors) {
                    Plugin.Log.LogWarning($"lang {candidate}.txt: {error}");
                }
                var report = Translation.Load(entries);
                foreach (var key in report.UnknownKeys) {
                    Plugin.Log.LogWarning($"lang {candidate}.txt: unknown key {key}");
                }
                foreach (var key in report.EmptyKeys) {
                    Plugin.Log.LogWarning($"lang {candidate}.txt: empty key {key}");
                }
                if (report.UnknownPluralRule != null) {
                    Plugin.Log.LogWarning($"lang {candidate}.txt: unknown plural rule {report.UnknownPluralRule}");
                }
                Plugin.Log.LogInfo($"lang: applied {report.Applied} strings from {candidate}.txt");
                return;
            }
            Translation.Reset();
            Plugin.Log.LogInfo($"lang: no file for {code}, using English");
        }

        private static string Prefix(string code) {
            int underscore = code.IndexOf('_');
            return (underscore > 0 ? code.Substring(0, underscore) : code).ToLowerInvariant();
        }
    }
}
