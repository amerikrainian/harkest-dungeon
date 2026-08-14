using Assets.Code.Data;
using Assets.Code.UI.Screens;
using Assets.Code.UI.Widgets;
using DD2A11y.Core.Nav;
using DD2A11y.Elements;
using DD2A11y.Game;
using HarmonyLib;
using UnityEngine.UI;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Screens {
    /// <summary>
    /// The Loathing reset screen, pushed by the game itself when the meter maxes mid-drive:
    /// what the confession boss just gained (the stacking max-HP buff plus the boss's own
    /// visible reset effects) reads as a dialog - the game's title and its composed
    /// description as the one element, reviewable line by line. Enter and Escape are the
    /// screen's own click-anywhere dismiss; the driving surface re-announces underneath.
    /// </summary>
    public sealed class DoomResetScreen : GameScreen {
        private static readonly AccessTools.FieldRef<DoomResetScreenWidgetBhv, DataContextBhv> ContextField =
            AccessTools.FieldRefAccess<DoomResetScreenWidgetBhv, DataContextBhv>("m_dataContextBhv");

        private System.Func<string> _body;
        private bool _awaitingBody;

        public override string Name => S.ScreenDialog;

        public override object ResolveTarget() {
            var top = StackTop.Object();
            return top == null ? null : top.GetComponentInChildren<DoomResetScreenWidgetBhv>(includeInactive: false);
        }

        public override Container BuildRoot(object target) {
            var widget = (DoomResetScreenWidgetBhv)target;
            // The whole screen is one click-anywhere button whose onClick is the game's own
            // close; the fallback covers a prefab reshape.
            var button = widget.GetComponent<Button>();
            System.Action resume;
            if (button != null) {
                resume = () => button.onClick.Invoke();
            } else {
                resume = widget.GetComponent<UiScreenBhv>().TryCloseScreen;
            }
            var root = new RootContainer(ContainerShape.VerticalList, back: resume);
            _body = () => {
                var context = ContextField(widget);
                return context == null ? null : context.GetStringValue("boss_desc");
            };
            root.Add(new AltarRevealElement(resume,
                () => GameLoc.TryGet("doom_reset_screen_title"),
                _body));
            // The widget writes the description in its open step, a beat after the object
            // tops the stack; its arrival requests the one re-announce.
            _awaitingBody = string.IsNullOrEmpty(_body());
            return root;
        }

        public override bool OnUpdate(object target) {
            if (_awaitingBody && !string.IsNullOrEmpty(_body())) {
                _awaitingBody = false;
                return true;
            }
            return false;
        }
    }
}
