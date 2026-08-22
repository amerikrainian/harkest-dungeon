using Assets.Code.Game;
using Assets.Code.Utils;
using DD2A11y.Game;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine;
using UnityEngine.UI;

namespace DD2A11y.Elements {
    /// <summary>
    /// The stagecoach sheet's livery cycler: named by the game's own "Stagecoach Livery"
    /// title, its value the applied skin's name. Enter cycles to the next unlocked livery
    /// through the button's own click and re-reads the landing, so the new skin is spoken.
    /// The game greys the button while fewer than two liveries are unlocked (and at kingdom
    /// inns whose skin-change feature is locked); the base status reads that as unavailable.
    /// </summary>
    public sealed class CoachLiveryElement : SelectableElement {
        public CoachLiveryElement(Button button, GameObject rowScope)
            : base(button, rowScope: rowScope) { }

        public override string Label
            => GameLoc.TryGet("stage_coach_skin_tooltip_title") ?? base.Label;

        public override string Value => SkinName();

        public override bool ReannounceOnActivate => true;

        private static string SkinName() {
            if (Singleton<GameTypeMgr>.Instance == null) {
                return null;
            }
            var coach = Singleton<GameTypeMgr>.Instance.StageCoach;
            string id = coach == null ? null : coach.GetStageCoachSkinId();
            if (id == null) {
                return null;
            }
            string name = GameLoc.TryGet("stagecoach_skin_" + id);
            if (name != null) {
                return name;
            }
            // The base and gang liveries have no skin name string of their own; the base one
            // reads as the game's word for default, a kingdom gang's by the game's title for
            // that gang's campaign ("Secrets of the Coven" - the skin id pluralizes some gang
            // ids, so the singular is retried), the slime pet's by an authored word, and only
            // a livery named nowhere speaks its id's own words.
            const string prefix = "wagon_skin_";
            string bare = id.StartsWith(prefix, System.StringComparison.Ordinal)
                ? id.Substring(prefix.Length) : id;
            if (bare == "base") {
                return GameLoc.TryGet("default_label") ?? bare;
            }
            if (bare == "slime") {
                return S.CoachSkinSlime;
            }
            return GameLoc.TryGet("kingdom_select_gang_disclaimer_title_" + bare)
                ?? (bare.EndsWith("s", System.StringComparison.Ordinal)
                    ? GameLoc.TryGet("kingdom_select_gang_disclaimer_title_"
                        + bare.Substring(0, bare.Length - 1)) : null)
                ?? bare.Replace('_', ' ');
        }
    }
}
