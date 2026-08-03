using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using DD2A11y.Core.Strings;
using Xunit;

namespace DD2A11y.Tests {
    /// <summary>
    /// Validates every shipped lang/*.txt translation file against the strings table: complete,
    /// well-formed, right plural rule for its language, format slots intact. A broken entry would
    /// otherwise fail silently at runtime as a dropped or garbled spoken line.
    /// </summary>
    public class LangFilesTests {
        /// <summary>The shipped files and the plural rule each language requires. The codes are
        /// the game's own (LanguageDefinition.m_language), so LanguageSync matches them exactly.</summary>
        private static readonly Dictionary<string, string> ExpectedRules = new Dictionary<string, string> {
            ["en"] = "english",
            ["cs"] = "czech",
            ["de_DE"] = "english",
            ["es"] = "english",
            ["es_lat"] = "english",
            ["fr"] = "french",
            ["it"] = "english",
            ["ja"] = "one",
            ["ko"] = "one",
            ["pl"] = "polish",
            ["pt_BR"] = "french",
            ["ru"] = "slavic",
            ["tw_CN"] = "one",
            ["uk"] = "slavic",
            ["zh_CN"] = "one",
        };

        public static IEnumerable<object[]> LanguageCodes()
            => ExpectedRules.Keys.OrderBy(c => c, StringComparer.Ordinal).Select(c => new object[] { c });

        [Fact]
        public void ShippedFileSet_MatchesExpectedLanguages() {
            var files = Directory.GetFiles(Path.Combine(RepoRoot(), "lang"), "*.txt")
                .Select(Path.GetFileNameWithoutExtension)
                .OrderBy(c => c, StringComparer.Ordinal);
            Assert.Equal(ExpectedRules.Keys.OrderBy(c => c, StringComparer.Ordinal), files);
        }

        [Theory]
        [MemberData(nameof(LanguageCodes))]
        public void LangFile_IsCompleteAndWellFormed(string code) {
            string content = File.ReadAllText(Path.Combine(RepoRoot(), "lang", code + ".txt"));
            Assert.DoesNotContain('�', content); // a replacement char means broken UTF-8

            var entries = Translation.ParseFile(content, out var errors);
            Assert.Empty(errors);

            // Every non-comment line landed as its own entry (a duplicate key would be
            // silently last-wins in the parser).
            int dataLines = content.Split('\n')
                .Select(l => l.TrimEnd('\r').Trim())
                .Count(l => l.Length > 0 && l[0] != '#');
            Assert.Equal(entries.Count, dataLines);

            Assert.True(entries.TryGetValue(Translation.PluralKey, out string? ruleName));
            Assert.Equal(ExpectedRules[code], ruleName);
            var rule = PluralRules.Resolve(ruleName);
            Assert.NotNull(rule);
            int formCount = 1 + Enumerable.Range(0, 301).Max(n => rule!(n));

            var english = EnglishReference();
            var keys = entries.Keys.Where(k => k != Translation.PluralKey)
                .OrderBy(k => k, StringComparer.Ordinal);
            Assert.Equal(english.Keys.OrderBy(k => k, StringComparer.Ordinal), keys);

            foreach (var pair in english) {
                string value = entries[pair.Key];
                Assert.False(string.IsNullOrWhiteSpace(value));
                if (pair.Value.Contains('|')) {
                    string[] forms = value.Split('|');
                    Assert.Equal(formCount, forms.Length);
                    foreach (string form in forms) {
                        Assert.Contains("{0}", form);
                        AssertFormats(pair.Key, form);
                    }
                } else {
                    Assert.DoesNotContain('|', value);
                    for (int slot = 0; slot < 5; slot++) {
                        string marker = "{" + slot + "}";
                        Assert.Equal(pair.Value.Contains(marker), value.Contains(marker));
                    }
                    AssertFormats(pair.Key, value);
                }
            }
        }

        /// <summary>A malformed template (stray brace, out-of-range slot) throws at speak time and
        /// the line is lost; formatting with dummy args here surfaces that at build time.</summary>
        private static void AssertFormats(string key, string template) {
            try {
                string.Format(CultureInfo.InvariantCulture, template, 1, 2, 3);
            } catch (FormatException ex) {
                Assert.Fail($"{key}: template '{template}' does not format: {ex.Message}");
            }
        }

        /// <summary>Key-to-English-value reference, parsed from lang/en.txt (which another test
        /// pins to Strings.DumpTemplate, so it is the full table).</summary>
        private static Dictionary<string, string> EnglishReference() {
            var entries = Translation.ParseFile(
                File.ReadAllText(Path.Combine(RepoRoot(), "lang", "en.txt")), out var errors);
            Assert.Empty(errors);
            entries.Remove(Translation.PluralKey);
            return entries;
        }

        private static string RepoRoot() {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "HarkestDungeon.slnx"))) {
                dir = dir.Parent;
            }
            Assert.NotNull(dir);
            return dir!.FullName;
        }
    }
}
