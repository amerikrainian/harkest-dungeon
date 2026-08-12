using System.Collections.Generic;
using DD2A11y.Core.Text;
using Xunit;

namespace DD2A11y.Tests {
    public class CombatantNamesTests {
        private static KeyValuePair<uint, string?> M(uint guid, string? name)
            => new KeyValuePair<uint, string?>(guid, name);

        [Fact]
        public void SharedName_NumbersInFirstSightOrder() {
            var names = new CombatantNames();
            names.Observe(new uint[] { 10, 11, 12, 13 });
            var team = new[] { M(10, "Lost Soul"), M(11, "Lost Soul"), M(12, "Lost Soul"), M(13, "Widow") };
            Assert.Equal("Lost Soul 1", names.Spoken(10, "Lost Soul", team));
            Assert.Equal("Lost Soul 2", names.Spoken(11, "Lost Soul", team));
            Assert.Equal("Lost Soul 3", names.Spoken(12, "Lost Soul", team));
        }

        [Fact]
        public void UniqueName_SpeaksBare() {
            var names = new CombatantNames();
            names.Observe(new uint[] { 10, 11, 12, 13 });
            var team = new[] { M(10, "Lost Soul"), M(11, "Lost Soul"), M(12, "Lost Soul"), M(13, "Widow") };
            Assert.Equal("Widow", names.Spoken(13, "Widow", team));
        }

        [Fact]
        public void PositionShuffle_KeepsTheNumbers() {
            var names = new CombatantNames();
            names.Observe(new uint[] { 10, 11 });
            var shuffled = new[] { M(11, "Widow"), M(10, "Widow") };
            Assert.Equal("Widow 1", names.Spoken(10, "Widow", shuffled));
            Assert.Equal("Widow 2", names.Spoken(11, "Widow", shuffled));
        }

        [Fact]
        public void Death_CompactsTheSurvivors() {
            var names = new CombatantNames();
            names.Observe(new uint[] { 10, 11, 12 });
            var survivors = new[] { M(11, "Lost Soul"), M(12, "Lost Soul") };
            Assert.Equal("Lost Soul 1", names.Spoken(11, "Lost Soul", survivors));
            Assert.Equal("Lost Soul 2", names.Spoken(12, "Lost Soul", survivors));
        }

        [Fact]
        public void SoleSurvivorOfAPack_DropsTheNumber() {
            var names = new CombatantNames();
            names.Observe(new uint[] { 10, 11 });
            var survivor = new[] { M(11, "Widow") };
            Assert.Equal("Widow", names.Spoken(11, "Widow", survivor));
        }

        [Fact]
        public void LateSpawn_AppendsAfterTheSurvivors() {
            var names = new CombatantNames();
            names.Observe(new uint[] { 10, 11 });
            names.Observe(new uint[] { 11, 12 });
            var team = new[] { M(11, "Lost Soul"), M(12, "Lost Soul") };
            Assert.Equal("Lost Soul 1", names.Spoken(11, "Lost Soul", team));
            Assert.Equal("Lost Soul 2", names.Spoken(12, "Lost Soul", team));
        }

        [Fact]
        public void NamelessTeammates_DoNotCountAsHolders() {
            var names = new CombatantNames();
            names.Observe(new uint[] { 10, 11, 12 });
            var team = new[] { M(10, "Widow"), M(11, null), M(12, null) };
            Assert.Equal("Widow", names.Spoken(10, "Widow", team));
        }

        [Fact]
        public void Reset_ForgetsTheOrder() {
            var names = new CombatantNames();
            names.Observe(new uint[] { 10, 11 });
            names.Reset();
            names.Observe(new uint[] { 11, 10 });
            var team = new[] { M(10, "Widow"), M(11, "Widow") };
            Assert.Equal("Widow 1", names.Spoken(11, "Widow", team));
            Assert.Equal("Widow 2", names.Spoken(10, "Widow", team));
        }
    }
}
