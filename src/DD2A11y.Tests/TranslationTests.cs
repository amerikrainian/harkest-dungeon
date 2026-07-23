using System;
using System.Collections.Generic;
using System.IO;
using DD2A11y.Core.Strings;
using Xunit;

namespace DD2A11y.Tests {
    public class TranslationTests : IDisposable {
        public void Dispose() => Translation.Reset();

        [Fact]
        public void ParseFile_ReadsKeyValues_AndReportsMalformedLines() {
            var entries = Translation.ParseFile("# comment\n\nA = hello\nB = x = y\nbroken line\n", out var errors);
            Assert.Equal("hello", entries["A"]);
            Assert.Equal("x = y", entries["B"]);
            Assert.Single(errors);
            Assert.Contains("line 5", errors[0]);
        }

        [Fact]
        public void Load_OverridesValue_AndFallsBackToEnglishForMissingKeys() {
            var report = Translation.Load(new Dictionary<string, string> { ["ScreenMainMenu"] = "hauptmenü" });
            Assert.Equal(1, report.Applied);
            Assert.Equal("hauptmenü", Strings.ScreenMainMenu);
            Assert.Equal("settings", Strings.ScreenSettings);
        }

        [Fact]
        public void Load_SetsAsideUnknownAndEmptyKeys() {
            var report = Translation.Load(new Dictionary<string, string> {
                ["NoSuchKey"] = "x",
                ["ScreenSettings"] = "  ",
            });
            Assert.Equal(0, report.Applied);
            Assert.Equal(new[] { "NoSuchKey" }, report.UnknownKeys);
            Assert.Equal(new[] { "ScreenSettings" }, report.EmptyKeys);
            Assert.Equal("settings", Strings.ScreenSettings);
        }

        [Fact]
        public void Load_UnknownPluralRule_IsReportedAndEnglishKept() {
            var report = Translation.Load(new Dictionary<string, string> { ["_plural"] = "klingon" });
            Assert.Equal("klingon", report.UnknownPluralRule);
        }

        [Fact]
        public void Templates_CarryWordOrder() {
            Translation.Load(new Dictionary<string, string> { ["BufferLine"] = "{1} (in {0})" });
            Assert.Equal("line one (in control)", Strings.BufferLine("control", "line one"));
        }

        [Fact]
        public void CommittedEnglishTemplate_MatchesDumpTemplate() {
            string path = Path.Combine(RepoRoot(), "lang", "en.txt");
            string committed = File.ReadAllText(path).Replace("\r\n", "\n");
            Assert.Equal(Strings.DumpTemplate(), committed);
        }

        private static string RepoRoot() {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "DD2A11y.slnx"))) {
                dir = dir.Parent;
            }
            Assert.NotNull(dir);
            return dir!.FullName;
        }
    }

    public class PluralRulesTests {
        [Theory]
        [InlineData(1, 0)]
        [InlineData(0, 1)]
        [InlineData(5, 1)]
        public void English(int n, int form) => Assert.Equal(form, PluralRules.English(n));

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 0)]
        [InlineData(2, 1)]
        public void French(int n, int form) => Assert.Equal(form, PluralRules.French(n));

        [Theory]
        [InlineData(1, 0)]
        [InlineData(21, 0)]
        [InlineData(3, 1)]
        [InlineData(12, 2)]
        [InlineData(25, 2)]
        public void Slavic(int n, int form) => Assert.Equal(form, PluralRules.Slavic(n));

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 1)]
        [InlineData(2, 2)]
        [InlineData(7, 3)]
        [InlineData(15, 4)]
        [InlineData(101, 5)]
        public void Arabic(int n, int form) => Assert.Equal(form, PluralRules.Arabic(n));

        [Fact]
        public void Resolve_UnknownName_IsNull() => Assert.Null(PluralRules.Resolve("nope"));
    }
}
