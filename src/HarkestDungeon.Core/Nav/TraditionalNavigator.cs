using System;
using System.Collections.Generic;
using static DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Core.Nav {
    /// <summary>
    /// Windows-screen-reader-style navigation: Tab / Shift-Tab traverse Panel tab-stops (a list
    /// counts as one stop), arrows move within a list (or adjust a focused slider/stepper/tab
    /// selector), Enter activates, Escape asks the screen to go back, Home/End jump to the ends
    /// of the enclosing tab stop (the whole screen when no Panel splits it). Entering a
    /// container auto-focuses its representative child.
    /// </summary>
    public sealed class TraditionalNavigator : Navigator {
        public TraditionalNavigator(Action<string, bool> speak) : base(speak) { }

        protected override void BuildInitialFocus() {
            if (Root == null) {
                return;
            }
            var first = RepresentativeChild(Root);
            if (first == null) {
                return;
            }
            Root.SetFocusedChild(first);
            AppendWithDescend(first);
        }

        public override bool Handle(string actionKey) {
            switch (actionKey) {
                case UiActions.Up: return Arrow(NavDirection.Up);
                case UiActions.Down: return Arrow(NavDirection.Down);
                case UiActions.Left: return Arrow(NavDirection.Left);
                case UiActions.Right: return Arrow(NavDirection.Right);
                case UiActions.Next: return Tab(1);
                case UiActions.Prev: return Tab(-1);
                case UiActions.Home: return JumpEdge(first: true);
                case UiActions.End: return JumpEdge(first: false);
                case UiActions.Activate: {
                    if (Current == null) {
                        return false;
                    }
                    // Inside a popup, activating an option commits it: the option's action runs
                    // and the popup closes, the restored focus reading back the new value.
                    if (PopupOpen) {
                        if (!Current.InvokeAction(ActionIds.Activate)) {
                            return false;
                        }
                        ClosePopup();
                        return true;
                    }
                    var popup = Current.BuildPopup();
                    if (popup != null) {
                        OpenPopup(popup);
                        return true;
                    }
                    // Consume only when the element answered the press; a focused element with
                    // no Activate action leaves the key unconsumed rather than silently eating
                    // it. A refusal speaks once, here, with no re-announce: the element's state
                    // did not change.
                    switch (Current.Perform(ActionIds.Activate)) {
                        case ActionOutcome.Missing:
                            return false;
                        case ActionOutcome.Refused:
                            Speak(StatusUnavailable, interrupt: true);
                            return true;
                    }
                    if (Current.ReannounceOnActivate) {
                        Speak(Current.GetValueText(), interrupt: true);
                        // An activation that rebuilds the screen under the cursor (a pick that
                        // shows the Reset button) re-lands on the element's replacement;
                        // recording the line keeps that re-land from repeating the new state.
                        MarkCurrentLineAnnounced();
                    }
                    return true;
                }
                case UiActions.Back:
                    if (PopupOpen) {
                        ClosePopup();
                        return true;
                    }
                    // Screen-level back/close: consume only if the root advertises a back action.
                    return Root != null && Root.InvokeAction(ActionIds.Back);
                default:
                    return false; // not a nav key
            }
        }

        public override bool WouldHandle(string actionKey) {
            switch (actionKey) {
                case UiActions.Up: return CanArrow(NavDirection.Up);
                case UiActions.Down: return CanArrow(NavDirection.Down);
                case UiActions.Left: return CanArrow(NavDirection.Left);
                case UiActions.Right: return CanArrow(NavDirection.Right);
                case UiActions.Next: return CanTab(1);
                case UiActions.Prev: return CanTab(-1);
                case UiActions.Home: return CanJump(first: true);
                case UiActions.End: return CanJump(first: false);
                case UiActions.Activate:
                    return Current != null
                        && (Current.HasAction(ActionIds.Activate) || !PopupOpen && Current.OpensPopup);
                case UiActions.Back:
                    return PopupOpen || (Root != null && Root.HasAction(ActionIds.Back));
                default:
                    return false;
            }
        }

        // An arrow answers where the focused element adjusts that way, or where the lists (or
        // the vertical spill) have somewhere to go.
        private bool CanArrow(NavDirection dir) {
            if (Current == null) {
                return false;
            }
            if (dir == NavDirection.Left || dir == NavDirection.Right) {
                return Current.HasAction(dir == NavDirection.Left ? ActionIds.Decrease : ActionIds.Increase)
                    || FindMove(dir) != null;
            }
            return FindMove(dir) != null || FindSpill(dir) != null;
        }

        private bool CanTab(int step) {
            var stops = ComputeTabStops();
            var target = stops.Count == 0 ? null : TabTarget(stops, step);
            return target != null && !Path.Contains(target);
        }

        private bool CanJump(bool first) {
            var target = EdgeTarget(first);
            return target != null && target != Current;
        }

        private bool Arrow(NavDirection dir) {
            if (Current == null) {
                return false;
            }

            // A focused slider/stepper/tab selector advertises increase/decrease; Left/Right adjust
            // it and re-announce the outcome - the new value, or an element-chosen message when the
            // adjust changed nothing. The value-text diff tells the element whether it moved.
            if (dir == NavDirection.Left || dir == NavDirection.Right) {
                string adjust = dir == NavDirection.Left ? ActionIds.Decrease : ActionIds.Increase;
                string before = Current.GetValueText();
                if (Current.InvokeAction(adjust)) {
                    bool changed = Current.GetValueText() != before;
                    Speak(Current.GetAdjustText(adjust, changed), interrupt: true);
                    // An adjust that rebuilds the screen under the cursor (a pager) re-lands
                    // on the element's replacement; recording the line keeps that re-land
                    // from repeating what was just spoken.
                    MarkCurrentLineAnnounced();
                    return true;
                }
            }

            var snapshot = new List<UIElement>(Path);
            if (Move(dir)) {
                AnnounceDelta(snapshot, interrupt: true);
                return true;
            }
            // Could not move within the lists. A vertical move spills into the adjacent block of an
            // enclosing VerticalList - up from a bottom bar back into the list above it, say - so a
            // multi-element screen reads as one top-to-bottom flow.
            if (dir == NavDirection.Up || dir == NavDirection.Down) {
                return TrySpillVertical(dir);
            }
            return false;
        }

        // Vertical movement that cannot proceed inside the current block spills into the adjacent
        // block of an enclosing VerticalList. Climbs to the block that is a direct child of such a
        // list, steps to the neighbor in the move direction, and enters it direction-aware: a
        // vertical list entered from below lands on its last focusable, a row keeps its remembered
        // column. A list at its own edge keeps climbing - a horizontal row at the top of an inner
        // list still spills to whatever sits above that list. Returns false at the outer edge so
        // the caller consumes without wrapping.
        private bool TrySpillVertical(NavDirection dir) {
            var spill = FindSpill(dir);
            if (spill == null) {
                return false;
            }
            var (list, block, neighbor) = spill.Value;
            var snapshot = new List<UIElement>(Path);
            int idx = Path.IndexOf(block);
            if (idx >= 0) {
                Path.RemoveRange(idx, Path.Count - idx);
            }
            AppendWithDescend(neighbor, dir);
            list.SetFocusedChild(neighbor);
            AnnounceDelta(snapshot, interrupt: true);
            return true;
        }

        // Where a vertical spill lands: the enclosing VerticalList, the block of it the move
        // leaves, and the neighbor block it enters. Null at the outer edge.
        private (Container list, UIElement block, UIElement neighbor)? FindSpill(NavDirection dir) {
            UIElement? block = Current;
            while (block != null) {
                while (block != null && (block.Parent == null || block.Parent.Shape != ContainerShape.VerticalList)) {
                    block = block.Parent;
                }
                if (block == null) {
                    return null;
                }
                var list = block.Parent!;
                var neighbor = list.GetNeighbor(block, dir);
                if (neighbor == null) {
                    block = list;
                    continue;
                }
                return (list, block, neighbor);
            }
            return null;
        }

        // Arrow movement within list-shaped containers, spilling into a same-shape parent at the edge.
        private bool Move(NavDirection dir) {
            var move = FindMove(dir);
            if (move == null) {
                return false;
            }
            var (container, movingFrom, next) = move.Value;
            int idx = Path.IndexOf(movingFrom);
            if (idx >= 0) {
                Path.RemoveRange(idx, Path.Count - idx);
            }
            AppendWithDescend(next, dir);
            container.SetFocusedChild(next);
            return true;
        }

        // Where an arrow move lands: the container it moves in, the path element it leaves, and
        // the neighbor it enters. Null when the lists have nowhere to go that way.
        private (Container container, UIElement from, UIElement next)? FindMove(NavDirection dir) {
            var movingFrom = Current;
            var container = movingFrom?.Parent;
            while (container != null && movingFrom != null) {
                var next = container.GetNeighbor(movingFrom, dir);
                if (next != null) {
                    return (container, movingFrom, next);
                }
                var parent = container.Parent;
                if (parent != null && parent.Shape == container.Shape) {
                    movingFrom = container;
                    container = parent;
                    continue;
                }
                return null;
            }
            return null;
        }

        private bool Tab(int step) {
            var stops = ComputeTabStops();
            if (stops.Count == 0) {
                return false;
            }
            var target = TabTarget(stops, step);
            if (target == null) {
                return true; // at an end; consume, no wrap
            }

            var snapshot = new List<UIElement>(Path);
            BuildPathTo(target);
            // Re-descend so re-entering a list restores its remembered/representative item.
            var landed = Current;
            if (landed != null) {
                Path.RemoveAt(Path.Count - 1);
                AppendWithDescend(landed);
            }
            AnnounceDelta(snapshot, interrupt: true);
            return true;
        }

        // The stop Tab lands on, or null at an end of a root that does not wrap.
        private UIElement? TabTarget(List<UIElement> stops, int step) {
            // Current may be deeper than its tab-stop (an item inside a list whose stop is the list's
            // representative), so walk up to the nearest element that IS a stop.
            int idx = -1;
            for (var e = Current; e != null && idx < 0; e = e.Parent) {
                idx = stops.IndexOf(e);
            }

            int ni = idx < 0 ? (step >= 0 ? 0 : stops.Count - 1) : idx + step;
            if (ni < 0 || ni >= stops.Count) {
                if (Root == null || !Root.WrapTabStops) {
                    return null;
                }
                ni = (ni + stops.Count) % stops.Count;
            }
            return stops[ni];
        }

        // Home/End: jump to the first/last element of the enclosing tab stop - Panels are hard
        // boundaries (Home/End never cross what Tab separates), and with no Panel above the
        // focus the jump spans the whole screen. Nested lists flatten: the landing is the true
        // edge leaf, not an inner list's representative.
        private bool JumpEdge(bool first) {
            var target = EdgeTarget(first);
            if (target == null || target == Current) {
                return true;
            }

            var snapshot = new List<UIElement>(Path);
            BuildPathTo(target);
            // A jump resets the remembered focus of the lists it left: after Home lands above
            // a nested list, Down walks in at its first item, not at whatever a prior End left
            // remembered there. Arrow moves keep their memory (rows keep their column).
            foreach (var element in snapshot) {
                if (element is Container departed && !Path.Contains(departed)) {
                    departed.SetFocusedChild(null);
                }
            }
            AnnounceDelta(snapshot, interrupt: true);
            return true;
        }

        // The edge leaf of the enclosing tab stop, or null with nothing focused.
        private UIElement? EdgeTarget(bool first) {
            if (Current == null) {
                return null;
            }
            UIElement scope = Current;
            while (scope.Parent != null && scope.Parent.Shape != ContainerShape.Panel) {
                scope = scope.Parent;
            }
            UIElement? target = scope;
            while (target is Container c) {
                target = first ? c.FirstFocusable() : c.LastFocusable();
            }
            return target;
        }
    }
}
