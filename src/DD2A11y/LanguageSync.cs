using System;
using System.IO;
using Assets.Code.Locale;
using Assets.Code.Utils;
using DD2A11y.Core.Strings;
using HarmonyLib;
using UnityEngine;

namespace DD2A11y {
    /// <summary>
    /// Follows the game language: when it changes, loads the matching mod translation from
    /// lang/&lt;code&gt;.txt (falling back to the bare prefix, e.g. de for de_DE), or resets to
    /// English when no file exists. Checked once a second - a language switch is rare.
    /// </summary>
    public sealed class LanguageSync {
        private static readonly System.Reflection.MethodInfo GetActualMethod =
            AccessTools.Method(typeof(LanguageDefinition), "GetActual");
        private static readonly System.Reflection.FieldInfo LanguageField =
            AccessTools.Field(typeof(LanguageDefinition), "m_language");

        private readonly string _langDir;
        private string _applied;
        private float _nextCheck;

        public LanguageSync(string langDir) {
            _langDir = langDir;
        }

        public void Tick() {
            if (Time.unscaledTime < _nextCheck) {
                return;
            }
            _nextCheck = Time.unscaledTime + 1f;

            string code = CurrentCode();
            if (code == _applied) {
                return;
            }
            _applied = code;
            Apply(code);
        }

        private static string CurrentCode() {
            try {
                var loc = Singleton<Localization>.Instance;
                var language = loc?.GetLanguage();
                if (language == null) {
                    return "en";
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
