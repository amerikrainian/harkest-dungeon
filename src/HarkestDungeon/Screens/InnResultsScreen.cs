using Assets.Code.UI.RunLog;
using Assets.Code.UI.Widgets;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Text;
using DD2A11y.Elements;
using DD2A11y.Game;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine.UI;

namespace DD2A11y.Screens {
    /// <summary>
    /// The travelogue (the inn-arrival run recap; the inn hub's Travelogue button reopens it):
    /// reads like a modal - each run-log line is its own focusable text row, then the Loathing
    /// meter readout, then Continue (present on arrival only; a reopened travelogue closes with
    /// Escape instead). Continue is the screen's own button; Escape runs the game's own
    /// continue-or-close.
    /// </summary>
    public sealed class InnResultsScreen : GameScreen {
        private SubScreenBiomeResultsBhv _results;
        private Container _root;
        private Container _entries;
        private int _builtEntries;

        public override string Name {
            get {
                string title = _results != null ? UiText.FirstLabel(_results.gameObject) : null;
                return string.IsNullOrEmpty(title) ? S.ScreenGeneric : title;
            }
        }

        public override object ResolveTarget() {
            var top = StackTop.Object();
            _results = top == null ? null : top.GetComponentInChildren<SubScreenBiomeResultsBhv>(includeInactive: false);
            return _results;
        }

        public override Container BuildRoot(object target) {
            var results = (SubScreenBiomeResultsBhv)target;
            _root = new RootContainer(ContainerShape.VerticalList, back: () => Back(results));

            _entries = new Container(ContainerShape.VerticalList);
            _root.Add(_entries);
            PopulateEntries(results);

            var doom = results.GetComponentInChildren<DoomMeterWidgetBhv>(includeInactive: false);
            if (doom != null) {
                // The Loathing meter's own tooltip (level label + chapter), read live.
                _root.Add(new ReadoutElement(() => doom == null ? null
                    : SpokenLine.Join(", ", TooltipReader.Lines(doom.gameObject))));
            }

            var buttons = new Container(ContainerShape.VerticalList);
            foreach (var button in results.GetComponentsInChildren<Button>(includeInactive: false)) {
                if (UiText.HasAnyTextSource(button.gameObject)) {
                    buttons.Add(new SelectableElement(button));
                }
            }
            _root.Add(buttons);
            return _root;
        }

        public override bool OnUpdate(object target) {
            var results = (SubScreenBiomeResultsBhv)target;
            if (results.GetComponentsInChildren<LogEntryBhv>(includeInactive: false).Length != _builtEntries) {
                PopulateEntries(results);
            }
            return false;
        }

        // One focusable text row per run-log entry, reading the game's own rendered line.
        private void PopulateEntries(SubScreenBiomeResultsBhv results) {
            _entries.Clear();
            var rows = results.GetComponentsInChildren<LogEntryBhv>(includeInactive: false);
            foreach (var row in rows) {
                var captured = row;
                _entries.Add(new ReadoutElement(() => captured == null ? null : UiText.AllText(captured.gameObject)));
            }
            _builtEntries = rows.Length;
        }

        // On arrival the screen's only exit is its continue flow; reopened from the hub it has
        // no continue button and closes like any stack screen.
        private static void Back(SubScreenBiomeResultsBhv results) {
            foreach (var button in results.GetComponentsInChildren<Button>(includeInactive: false)) {
                if (UiText.HasAnyTextSource(button.gameObject)) {
                    results.HandleContinueButton();
                    return;
                }
            }
            results.TryCloseScreen();
        }
    }
}
