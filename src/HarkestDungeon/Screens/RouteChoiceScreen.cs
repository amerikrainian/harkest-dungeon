using System.Collections.Generic;
using Assets.Code.Game;
using Assets.Code.Map.Generation.Row;
using Assets.Code.UI;
using Assets.Code.Utils;
using DD2A11y.Core.Nav;
using DD2A11y.Elements;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine;

namespace DD2A11y.Screens {
    /// <summary>
    /// The route menu at a road fork. The game's own junction flow slows the coach to a halt
    /// and waits indefinitely while no route is chosen - that wait is this screen: one element
    /// per route banner in left-to-right order, full detail in the buffer, Enter committing
    /// through the game's own selection (the coach then drives itself through the branch and
    /// the screen releases). Escape dismisses back to free driving for manual steering; the
    /// menu returns at the next junction.
    /// </summary>
    public sealed class RouteChoiceScreen : GameScreen {
        private static readonly AccessTools.FieldRef<TriggerIntersectionBhv, IntersectionState> StateField =
            AccessTools.FieldRefAccess<TriggerIntersectionBhv, IntersectionState>("m_CurrentState");
        private static readonly AccessTools.FieldRef<TriggerIntersectionBhv, List<RoadIndicatorUIBhv>> PreviewIconsField =
            AccessTools.FieldRefAccess<TriggerIntersectionBhv, List<RoadIndicatorUIBhv>>("m_PreviewIcons");

        private TriggerIntersectionBhv _intersection;
        private TriggerIntersectionBhv _dismissed;
        private float _nextScanTime;

        public override string Name => S.ScreenFork;

        public override object ResolveTarget() {
            if (GameModeMgr.CurrentMode != GameModeType.DRIVING || Singleton<GameModeMgr>.Instance.IsChangingState()) {
                _intersection = null;
                _dismissed = null;
                return null;
            }
            if (_dismissed != null && !AwaitingChoice(_dismissed)) {
                _dismissed = null;
            }
            // The current match stays valid frame-to-frame without a scene scan; scanning for a
            // new waiting junction is throttled - the game's own wait is indefinite, so a beat
            // of latency opening the menu is invisible.
            if (_intersection != null && AwaitingChoice(_intersection) && _intersection != _dismissed) {
                return _intersection;
            }
            _intersection = null;
            if (Time.unscaledTime < _nextScanTime) {
                return null;
            }
            _nextScanTime = Time.unscaledTime + 0.25f;
            foreach (var candidate in Object.FindObjectsOfType<TriggerIntersectionBhv>()) {
                if (candidate != _dismissed && AwaitingChoice(candidate)) {
                    _intersection = candidate;
                    break;
                }
            }
            return _intersection;
        }

        // The junction's own "stopped and waiting for a route" condition.
        private static bool AwaitingChoice(TriggerIntersectionBhv intersection) {
            if (intersection == null) {
                return false;
            }
            return StateField(intersection) == IntersectionState.SLOW_DOWN
                && intersection.SelectedIntersectionOptionIndex == -1;
        }

        public override Container BuildRoot(object target) {
            var intersection = (TriggerIntersectionBhv)target;
            var root = new RootContainer(ContainerShape.VerticalList, back: () => _dismissed = intersection);
            foreach (var indicator in PreviewIconsField(intersection)) {
                if (indicator != null && indicator.gameObject.activeInHierarchy) {
                    root.Add(new RouteElement(indicator));
                }
            }
            return root;
        }
    }
}
