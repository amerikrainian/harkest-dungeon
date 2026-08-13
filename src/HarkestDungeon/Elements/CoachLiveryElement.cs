using Assets.Code.Game;
using Assets.Code.Utils;
using DD2A11y.Game;
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
            // The base and faction liveries have no name string anywhere in the game; the
            // id's own words stand in, the base one by the game's word for default.
            const string prefix = "wagon_skin_";
            string bare = id.StartsWith(prefix, System.StringComparison.Ordinal)
                ? id.Substring(prefix.Length) : id;
            return bare == "base" ? GameLoc.TryGet("default_label") ?? bare : bare.Replace('_', ' ');
        }
    }
}
