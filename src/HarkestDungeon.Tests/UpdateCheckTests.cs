using DD2A11y.Core;
using Xunit;

namespace DD2A11y.Tests {
    /// <summary>
    /// The version rules behind the startup update announcement: what the release payload
    /// names, and that only a strictly newer release ever counts.
    /// </summary>
    public class UpdateCheckTests {
        [Fact]
        public void LatestVersion_ReadsTheTag_AndStripsTheV() {
            string json = "{\"url\":\"x\",\"tag_name\":\"v0.2.2\",\"name\":\"Harkest Dungeon v0.2.2\"}";
            Assert.Equal("0.2.2", UpdateCheck.LatestVersion(json));
        }

        [Fact]
        public void LatestVersion_KeepsAnUnprefixedTag() {
            Assert.Equal("1.4", UpdateCheck.LatestVersion("{\"tag_name\": \"1.4\"}"));
        }

        [Fact]
        public void LatestVersion_NullOnMissingTag() {
            Assert.Null(UpdateCheck.LatestVersion("{\"message\":\"Not Found\"}"));
        }

        [Fact]
        public void LatestVersion_NullOnBareVTag() {
            Assert.Null(UpdateCheck.LatestVersion("{\"tag_name\":\"v\"}"));
        }

        [Theory]
        [InlineData("0.2.2", "0.2.1", true)]
        [InlineData("0.3", "0.2.9", true)]
        [InlineData("1.0.0", "0.9.9", true)]
        [InlineData("0.2.1.1", "0.2.1", true)]
        [InlineData("0.2.1", "0.2.1", false)]
        [InlineData("1.0", "1", false)]
        [InlineData("0.2.0", "0.2.1", false)]
        [InlineData("0.2.1", "0.3.0", false)]
        public void IsNewer_ComparesNumericComponents(string remote, string local, bool newer) {
            Assert.Equal(newer, UpdateCheck.IsNewer(remote, local));
        }

        // A suffixed component parses as zero, so a pre-release tag compares conservatively
        // (never announced over a clean local build of the same line) instead of throwing.
        [Fact]
        public void IsNewer_NonNumericComponentsCountAsZero() {
            Assert.False(UpdateCheck.IsNewer("0.2.1-beta", "0.2.1"));
            Assert.False(UpdateCheck.IsNewer("0.2.2-beta", "0.2.1"));
            Assert.True(UpdateCheck.IsNewer("0.3.0-beta", "0.2.1"));
        }
    }
}
