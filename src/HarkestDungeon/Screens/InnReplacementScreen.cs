using Assets.Code.UI;
using Assets.Code.UI.Screens;
using Assets.Code.UI.Tooltips;
using Assets.Code.UI.Widgets;
using DD2A11y.Core.Nav;
using DD2A11y.Elements;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine.UI;

namespace DD2A11y.Screens {
    /// <summary>
    /// The kingdoms Select Replacement Hero screen, opened from an inn rest slot: the
    /// stationed-effects readout first (what a stationed hero gains, from its tooltip), then
    /// one row per candidate reading name and class, with the game's own marker for a hero
    /// already at this inn as the value and the add/station tooltip in the buffer. Enter is
    /// the row's own submit; Escape closes through the screen's teardown.
    /// </summary>
    public sealed class InnReplacementScreen : GameScreen {
        private static readonly AccessTools.FieldRef<InnReplacementScreenWidgetBhv, TextTooltipBhv> EffectsTipField =
            AccessTools.FieldRefAccess<InnReplacementScreenWidgetBhv, TextTooltipBhv>("m_stationedEffectsTooltip");

        private InnReplacementScreenWidgetBhv _widget;

        public override string Name => GameLoc.TryGet("inn_replacement_screen_title") ?? S.ScreenGeneric;

        public override object ResolveTarget() {
            var top = StackTop.Object();
            _widget = top == null ? null : top.GetComponentInChildren<InnReplacementScreenWidgetBhv>(includeInactive: false);
            return _widget;
        }

        public override Container BuildRoot(object target) {
            var widget = (InnReplacementScreenWidgetBhv)target;
            var screen = widget.GetComponentInParent<UiScreenBhv>();
            var root = new RootContainer(ContainerShape.VerticalList, back: () => screen.TryCloseScreen());

            var effectsTip = EffectsTipField(widget);
            if (effectsTip != null) {
                root.Add(new ReadoutElement(
                    () => UiText.FirstLabel(effectsTip.gameObject),
                    detail: () => TooltipReader.LinesOf(effectsTip)));
            }

            foreach (var row in widget.GetComponentsInChildren<InnReplacementActorBhv>(includeInactive: false)) {
                var selectable = row.GetComponent<Selectable>();
                if (selectable != null) {
                    root.Add(new InnReplacementRowElement(row, selectable));
                }
            }
            return root;
        }
    }
}
