using System.Collections.Generic;
using Assets.Code.Data;
using Assets.Code.UI.Inn;
using Assets.Code.UI.Screens;
using Assets.Code.UI.Tooltips;
using Assets.Code.Unlock;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Speech;
using DD2A11y.Core.Text;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine.UI;

namespace DD2A11y.Elements {
    /// <summary>
    /// A Kingdoms mastery trainer hero-upgrade row (an <c>InnActorUpgradeUnlockBhv</c> on the
    /// trainer's Hero Upgrades tab): one of the track's permanent hero unlocks, an icon whose
    /// content lives only in its tooltip. The upgrade's effect lines are the label ("+10%
    /// death RES, +20% stun RES, -10% Fatigue Limit"), "owned" once unlocked, "unavailable"
    /// for a step the inn has not opened or that is not the track's next, the full tooltip in
    /// the buffer. Enter stands in for the sighted hold: the trainer's own purchase for the
    /// next available step when the mastery points cover it.
    /// </summary>
    public sealed class MasteryUnlockElement : SelectableElement {
        private static readonly AccessTools.FieldRef<InnActorUpgradeUnlockBhv, UnlockDefinition> DefinitionField =
            AccessTools.FieldRefAccess<InnActorUpgradeUnlockBhv, UnlockDefinition>("m_unlockDefinition");
        private static readonly AccessTools.FieldRef<InnActorUpgradeUnlockBhv, DataContextBhv> ContextField =
            AccessTools.FieldRefAccess<InnActorUpgradeUnlockBhv, DataContextBhv>("m_dataContextBhv");

        private readonly InnActorUpgradeUnlockBhv _row;
        private readonly InnUpgradeSkillsBhv _panel;

        public MasteryUnlockElement(InnActorUpgradeUnlockBhv row, InnUpgradeSkillsBhv panel, Selectable selectable)
            : base(selectable, null, row.gameObject) {
            _row = row;
            _panel = panel;
        }

        public override string Label {
            get {
                var effects = Effects();
                return effects.Count == 0 ? base.Label : SpokenLine.Join(", ", effects);
            }
        }

        public override string Status {
            get {
                var context = ContextField(_row);
                if (context != null && context.GetBoolValue("unlocked")) {
                    return S.StatusOwned;
                }
                return Purchasable ? null : S.StatusUnavailable;
            }
        }

        private bool Purchasable {
            get {
                var definition = DefinitionField(_row);
                return definition != null && _panel.IsNextActorUnlock(definition) && _panel.IsAvailableToUpgrade(definition);
            }
        }

        public override IEnumerable<ElementAction> GetActions() {
            yield return new ElementAction(ActionIds.Activate, () => {
                // The row's own press gate (next in the track, opened by the inn, affordable);
                // the purchase respawns the rows, and the rebuild announces the new state.
                if (Purchasable && _panel.CanAffordSkill) {
                    _panel.PurchaseActorUnlock();
                } else {
                    SpeechPipeline.Instance?.Speak(S.StatusUnavailable);
                }
            });
        }

        protected override IEnumerable<string> GetDetailLines() => TooltipReader.Lines(_row.gameObject);

        // The tooltip's effect lines without the availability sentence the widget puts first
        // while the inn has not opened the step (its "upgrade_locked" binding).
        private List<string> Effects() {
            var lines = new List<string>(TooltipReader.Lines(_row.gameObject));
            var context = ContextField(_row);
            if (lines.Count > 0 && context != null && context.GetBoolValue("upgrade_locked")) {
                lines.RemoveAt(0);
            }
            return lines;
        }
    }
}
