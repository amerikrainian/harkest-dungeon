using System.Collections.Generic;
using Assets.Code.UI;
using Assets.Code.UI.Screens;
using DD2A11y.Core.Buffers;
using DD2A11y.Core.Nav;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine;

namespace DD2A11y.Elements {
    /// <summary>
    /// One path seal on the mastery trainer's Change Path panel: the game's own seal label
    /// (path name plus its bonus candle glyphs), "selected" on the seal the panel highlights.
    /// Enter previews the path through the trainer's own SelectPath (drives the comparison
    /// panel and arms the purchase button; nothing commits), and the re-announce speaks the
    /// landed selection. The buffer carries this path's own card - the hero-seal tooltip
    /// text - so any option reads without selecting it first.
    /// </summary>
    public sealed class PathOptionElement : UIElement {
        private static readonly AccessTools.FieldRef<InnUpgradeSkillsBhv, GameObject> SelectedField =
            AccessTools.FieldRefAccess<InnUpgradeSkillsBhv, GameObject>("m_selectedPathObj");

        private readonly InnUpgradeSkillsBhv _panel;
        private readonly GameObject _pathObject;

        public PathOptionElement(InnUpgradeSkillsBhv panel, GameObject pathObject) {
            _panel = panel;
            _pathObject = pathObject;
        }

        public override bool CanFocus => _pathObject != null && _pathObject.activeInHierarchy;

        public override string Label => UiText.FirstLabel(_pathObject);

        public override string Role => S.RoleButton;

        public override string Status => SelectedField(_panel) == _pathObject ? S.StatusSelected : null;

        public override bool ReannounceOnActivate => true;

        public override IEnumerable<ElementAction> GetActions() {
            yield return new ElementAction(ActionIds.Activate, () => _panel.SelectPath(_pathObject));
        }

        protected override IEnumerable<string> GetDetailLines() {
            var select = _pathObject.GetComponent<ActorPathSelectBhv>();
            return select == null
                ? System.Linq.Enumerable.Empty<string>()
                : PathComparison.Card(select.PathId, _panel.ActiveActorGuid);
        }

        public override IEnumerable<string> GetSideBufferLines(string bufferKey)
            => bufferKey == BufferKeys.Hero
                ? HeroStatus.Lines(_panel.ActiveActorGuid) : base.GetSideBufferLines(bufferKey);
    }
}
