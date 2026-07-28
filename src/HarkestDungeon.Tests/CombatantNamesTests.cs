using DD2A11y.Core.Text;
using Xunit;

namespace DD2A11y.Tests {
    public class CombatantNamesTests {
        [Fact]
        public void SharedName_SpeaksWithRank() {
            var team = new[] { "Lost Soul", "Lost Soul", "Lost Soul", "Widow" };
            Assert.Equal("Lost Soul 2", CombatantNames.Spoken("Lost Soul", 2, team));
        }

        [Fact]
        public void UniqueName_SpeaksBare() {
            var team = new[] { "Lost Soul", "Lost Soul", "Lost Soul", "Widow" };
            Assert.Equal("Widow", CombatantNames.Spoken("Widow", 4, team));
        }

        [Fact]
        public void LastSurvivorOfAPack_DropsTheNumber() {
            var team = new[] { "Lost Soul", "Widow" };
            Assert.Equal("Lost Soul", CombatantNames.Spoken("Lost Soul", 1, team));
        }

        [Fact]
        public void NamelessTeammates_DoNotCountAsHolders() {
            var team = new[] { "Widow", null, null };
            Assert.Equal("Widow", CombatantNames.Spoken("Widow", 1, team));
        }
    }
}
