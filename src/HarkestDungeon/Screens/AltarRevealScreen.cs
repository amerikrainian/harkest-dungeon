using Assets.Code.Data;
using Assets.Code.UI.Screens;
using DD2A11y.Core.Nav;
using DD2A11y.Elements;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Screens {
    /// <summary>
    /// The altar's item reveal, read as a modal: while a recollection purchase presents its
    /// unlocked item, this screen takes over with the item's name and description as the one
    /// element (spoken in full, buffer-reviewable), so browsing cannot wander mid-reveal.
    /// Enter or Escape continues - the game's own Submit step - and the recollection panel
    /// re-announces underneath with the updated counts.
    /// </summary>
    public sealed class AltarRevealScreen : GameScreen {
        private static readonly AccessTools.FieldRef<AltarItemSubScreenBhv, DataContextBhv> PanelContextField =
            AccessTools.FieldRefAccess<AltarItemSubScreenBhv, DataContextBhv>("m_dataContextBhv");
        private static readonly AccessTools.FieldRef<AltarItemSubScreenBhv, string> ActiveRewardField =
            AccessTools.FieldRefAccess<AltarItemSubScreenBhv, string>("m_activeRewardId");

        private AltarItemSubScreenBhv _panel;

        public override string Name => S.AltarUnlocked;

        public override object ResolveTarget() {
            var top = StackTop.Object();
            var panel = top == null ? null : top.GetComponent<AltarItemSubScreenBhv>();
            if (panel == null || !panel.IsPresenting) {
                _panel = null;
                return null;
            }
            string rewardId = ActiveRewardField(panel);
            if (string.IsNullOrEmpty(rewardId)) {
                _panel = null;
                return null;
            }
            // The name binding lags the purchase by the icon load; match only once it holds
            // THIS reward's name, so the previous reveal's text is never read for the new one.
            var context = PanelContextField(panel);
            string name = context == null ? null : context.GetStringValue("item_name");
            if (string.IsNullOrEmpty(name) || name != GameLoc.TryGet("item_name_" + rewardId)) {
                _panel = null;
                return null;
            }
            _panel = panel;
            return panel;
        }

        public override Container BuildRoot(object target) {
            var panel = (AltarItemSubScreenBhv)target;
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
