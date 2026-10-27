using DD2A11y.Core.Nav;
using Xunit;

namespace DD2A11y.Tests {
    public class FocusSpotTests {
        // A hero sheet's content: a stat readout, a skills row, a button, a trinkets row.
        private static Container Sheet(int skills, int trinkets, string stat = "HP 30/30", bool loadouts = true) {
            var items = new Container(ContainerShape.VerticalList);
            items.Add(new TestElement(stat));
            var skillRow = new Container(ContainerShape.HorizontalList, "Skills");
            for (int i = 0; i < skills; i++) {
                skillRow.Add(new TestElement("skill " + i));
            }
            items.Add(skillRow);
            if (loadouts) {
                items.Add(new TestElement("Skill Loadouts"));
            }
            var trinketRow = new Container(ContainerShape.HorizontalList, "Trinkets");
            for (int i = 0; i < trinkets; i++) {
                trinketRow.Add(new TestElement("trinket " + i));
            }
            items.Add(trinketRow);
            return items;
        }

        private static UIElement[] PathTo(Container scope, int index, int inner = -1) {
            var top = scope.Children[index];
            return inner < 0 ? new[] { top } : new[] { top, ((Container)top).Children[inner] };
        }

        [Fact]
        public void LandsOnTheSameLabeledButton() {
            var before = Sheet(5, 2);
            var spot = FocusSpot.Capture(before, PathTo(before, 2))!;
            var after = Sheet(5, 2);
            Assert.Same(after.Children[2], spot.Resolve(after));
        }

        [Fact]
        public void KeepsTheColumnWithinALabeledRow() {
            var before = Sheet(5, 2);
            var spot = FocusSpot.Capture(before, PathTo(before, 1, 3))!;
            var after = Sheet(5, 2);
            Assert.Same(((Container)after.Children[1]).Children[3], spot.Resolve(after));
        }

        [Fact]
        public void ClampsTheColumnToAShorterRow() {
            var before = Sheet(5, 2);
            var spot = FocusSpot.Capture(before, PathTo(before, 1, 4))!;
            var after = Sheet(3, 2);
            Assert.Same(((Container)after.Children[1]).Children[2], spot.Resolve(after));
        }

        [Fact]
        public void FindsTheLabelAfterTheLayoutShifts() {
            var before = Sheet(5, 2);
            var spot = FocusSpot.Capture(before, PathTo(before, 3, 1))!;
            // No loadouts button: the trinkets row moved up one position.
            var after = Sheet(5, 2, loadouts: false);
            Assert.Same(((Container)after.Children[2]).Children[1], spot.Resolve(after));
        }

        [Fact]
        public void FallsBackToThePositionWhenTheLabelChanges() {
            var before = Sheet(5, 2, stat: "HP 30/30");
            var spot = FocusSpot.Capture(before, PathTo(before, 0))!;
            var after = Sheet(5, 2, stat: "HP 12/41");
            Assert.Same(after.Children[0], spot.Resolve(after));
        }

        [Fact]
        public void SkipsAnEmptyRowAtThePosition() {
            var before = Sheet(5, 2);
            var spot = FocusSpot.Capture(before, PathTo(before, 3, 0))!;
            var after = new Container(ContainerShape.VerticalList);
            var stat = new TestElement("HP 1/1");
            after.Add(stat);
            after.Add(new Container(ContainerShape.HorizontalList, "Skills"));
            Assert.Same(stat, spot.Resolve(after));
        }

        [Fact]
        public void CapturesNothingOutsideTheScope() {
            var root = new Container(ContainerShape.VerticalList);
            var header = new TestElement("Hellion");
            root.Add(header);
            var items = Sheet(5, 2);
            root.Add(items);
            Assert.Null(FocusSpot.Capture(items, new UIElement[] { header }));
        }
    }
}
