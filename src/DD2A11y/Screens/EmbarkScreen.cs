using System.Collections.Generic;
using Assets.Code.Data;
using Assets.Code.Game;
using Assets.Code.UI;
using Assets.Code.UI.Managers;
using Assets.Code.Utils;
using DD2A11y.Core.Nav;
using DD2A11y.Elements;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine;
using UnityEngine.UI;

namespace DD2A11y.Screens {
    /// <summary>
    /// The embark staging scene (the EMBARK game mode, between the crossroads or an inn and
    /// the drive): the party stands at the coach, pending hero relationships reveal, and the
    /// depart button rolls the coach out. Layout: one element per pending relationship (the
    /// heroes' names; Enter commits it through the game's own press and the game plays its
    /// reveal), the apply-all button when the game shows one, then the depart button (the
    /// game's own continue label, with the destination region when one is set). Depart drives
    /// the game's keyboard path, which self-validates: with relationships still pending it
    /// answers with the game's own reminder dialog instead of leaving. Escape opens the
    /// pause menu.
    /// </summary>
    public sealed class EmbarkScreen : GameScreen {
        private static readonly AccessTools.FieldRef<EmbarkUiBhv, List<EmbarkRelationshipBtnBhv>> RelationshipButtonsField =
            AccessTools.FieldRefAccess<EmbarkUiBhv, List<EmbarkRelationshipBtnBhv>>("m_relationshipButtons");
        private static readonly AccessTools.FieldRef<EmbarkUiBhv, GameObject> ApplyAllField =
            AccessTools.FieldRefAccess<EmbarkUiBhv, GameObject>("m_applyAllRelationshipsButton");
        private static readonly AccessTools.FieldRef<EmbarkUiBhv, GameObject> MouseEmbarkField =
            AccessTools.FieldRefAccess<EmbarkUiBhv, GameObject>("m_mouseEmbarkBtn");

        private EmbarkUiBhv _embark;
        private Container _root;

        public override string Name => S.ScreenEmbark;

        public override object ResolveTarget() {
            if (GameModeMgr.CurrentMode != GameModeType.EMBARK || Singleton<GameModeMgr>.Instance.IsChangingState()) {
                _embark = null;
                return null;
            }
            if (_embark == null) {
                _embark = UnityEngine.Object.FindObjectOfType<EmbarkUiBhv>();
            }
            return _embark;
        }

        // The scene's widgets are a fixed serialized set the game only toggles and re-inits
        // (the relationship rows are a pool, the apply-all button follows an option), so the
        // tree is built once over ALL of them: an inactive widget's element hides through its
        // live CanFocus, and the entry churn never replaces elements under the cursor.
        public override Container BuildRoot(object target) {
            var embark = (EmbarkUiBhv)target;
            _root = new RootContainer(ContainerShape.VerticalList,
                back: () => SingletonMonoBehaviour<CommonUiBhv>.Instance.TogglePauseMenu());
            foreach (var row in RelationshipButtonsField(embark)) {
                var selectable = row == null ? null : row.GetComponent<Selectable>();
                if (selectable != null) {
                    _root.Add(new EmbarkRelationshipElement(row, selectable));
                }
            }
            var applyAll = ApplyAllField(embark);
            var applyAllButton = applyAll == null ? null : applyAll.GetComponent<Button>();
            if (applyAllButton != null) {
                _root.Add(new SelectableElement(applyAllButton));
            }
            _root.Add(new ActionElement(
                () => ContinueLabel(embark),
                S.RoleButton,
                () => {
                    embark.OnEmbarkSubmit();
                    embark.OnEmbark();
                },
                value: () => embark.HasRelationshipsApplied ? null : S.StatusUnavailable));
            return _root;
        }

        // The depart caption lives in the embark button's binding ("Continue", or
        // "Continue: <region>" when a destination is set); the loc key is the fallback for
        // the frame before the binding fills.
        private string ContinueLabel(EmbarkUiBhv embark) {
            var button = MouseEmbarkField(embark);
            var context = button == null ? null : button.GetComponent<DataContextBhv>();
            string label = context == null ? null : context.GetStringValue("embark_label");
            return string.IsNullOrEmpty(label) ? GameLoc.TryGet("embark_continue_label") : label;
        }

    }
}
