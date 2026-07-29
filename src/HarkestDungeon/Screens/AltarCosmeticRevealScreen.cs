using Assets.Code.Data;
using Assets.Code.UI.Screens;
using DD2A11y.Core.Nav;
using DD2A11y.Elements;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Screens {
    /// <summary>
    /// The cosmetic altar's reward reveal, read as a modal like the recollection's: while a
    /// purchase presents, the one element speaks the reward's kind ("Weapon Kit", "Hero
    /// Palette") and its name, buffer-reviewable, and Enter or Escape continues (the game's
    /// own Submit step). Unlike the item panel there is no icon-load lag to gate on: the
    /// panel writes both bindings synchronously when the purchase lands, so presenting plus
    /// a non-empty description is current by construction.
    /// </summary>
    public sealed class AltarCosmeticRevealScreen : GameScreen {
        private static readonly AccessTools.FieldRef<AltarCosmeticSubScreenBhv, DataContextBhv> PanelContextField =
            AccessTools.FieldRefAccess<AltarCosmeticSubScreenBhv, DataContextBhv>("m_dataContextBhv");

        private AltarCosmeticSubScreenBhv _panel;

        public override string Name => S.AltarUnlocked;

        public override object ResolveTarget() {
            var top = StackTop.Object();
            var panel = top == null ? null : top.GetComponent<AltarCosmeticSubScreenBhv>();
            if (panel == null || !panel.IsPresenting) {
                _panel = null;
                return null;
            }
            var context = PanelContextField(panel);
            if (context == null || string.IsNullOrEmpty(context.GetStringValue("item_desc"))) {
                _panel = null;
                return null;
            }
            _panel = panel;
            return panel;
        }

        public override Container BuildRoot(object target) {
            var panel = (AltarCosmeticSubScreenBhv)target;
            var root = new RootContainer(ContainerShape.VerticalList, back: panel.OnTimelineResume);
            root.Add(new AltarRevealElement(panel.OnTimelineResume,
                () => {
                    var context = PanelContextField(panel);
                    return context == null ? null : Core.Text.TextFilter.Clean(context.GetStringValue("item_name"));
                },
                () => {
                    var context = PanelContextField(panel);
                    return context == null ? null : context.GetStringValue("item_desc");
                }));
            return root;
        }
    }
}
