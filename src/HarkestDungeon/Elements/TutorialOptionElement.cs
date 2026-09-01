using System.Collections.Generic;
using Assets.Code.Data;
using Assets.Code.Tutorial;
using Assets.Code.UI.Widgets;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Text;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine;
using UnityEngine.UI;

namespace DD2A11y.Elements {
    /// <summary>
    /// One tutorial on the archive: the entry's title, with the game's unviewed notification
    /// icon spoken as a "New" prefix. Enter is the game's own option click - it shows the
    /// entry's text panel, clears the icon, and saves - and the landed title and full text are
    /// spoken back, staying in the buffer line by line while the entry is the one on display.
    /// Browsing must never mirror into the uGUI selection: these options activate on
    /// selection (the game's controller path), so a mirrored focus would view entries as the
    /// user scrolls past them.
    /// </summary>
    public sealed class TutorialOptionElement : SelectableElement {
        private static readonly AccessTools.FieldRef<TutorialArchiveOptionBhv, TutorialType> TypeField =
            AccessTools.FieldRefAccess<TutorialArchiveOptionBhv, TutorialType>("m_type");

        /// <summary>The option's tutorial type; null on a row Init never touched (the
        /// screen's spawn template).</summary>
        public static TutorialType TypeOf(TutorialArchiveOptionBhv option) => TypeField(option);
        private static readonly AccessTools.FieldRef<TutorialArchiveWidgetBhv, GameObject> SelectedField =
            AccessTools.FieldRefAccess<TutorialArchiveWidgetBhv, GameObject>("m_selectedTutorial");

        private readonly TutorialArchiveWidgetBhv _widget;
        private readonly TutorialArchiveOptionBhv _option;
        private readonly DataContextBhv _context;

        public TutorialOptionElement(TutorialArchiveWidgetBhv widget, TutorialArchiveOptionBhv option, Selectable selectable)
            : base(selectable) {
            _widget = widget;
            _option = option;
            _context = option.GetComponent<DataContextBhv>();
        }

        /// <summary>Whether the game still shows the entry's unviewed notification icon.</summary>
        public bool IsNew => _context.GetBoolValue("notification_icon");

        public override string Label {
            get {
                string title = Title() ?? base.Label;
                return IsNew ? SpokenLine.Join(S.TutorialNew, title) : title;
            }
        }

        public override IEnumerable<ElementAction> GetActions() {
            yield return new ElementAction(ActionIds.Activate,
                () => _widget.OnOptionClicked(_option.gameObject));
        }

        public override bool ReannounceOnActivate => true;

        public override string GetValueText() => SpokenLine.Join(Title(), Description());

        protected override IEnumerable<string> GetDetailLines() {
            if (SelectedField(_widget) != _option.gameObject) {
                yield break;
            }
            foreach (var line in SpokenLine.NonEmptyLines(Description())) {
                yield return line;
            }
        }

        private string Title() => GameLoc.TryGet("tutorial_t" + TypeField(_option).m_eventId + "_title");

        private string Description() => GameLoc.TryGet("tutorial_t" + TypeField(_option).m_eventId + "_description");
    }
}
