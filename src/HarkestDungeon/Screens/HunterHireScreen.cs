using System.Collections.Generic;
using Assets.Code.Data;
using Assets.Code.Roster;
using Assets.Code.UI;
using Assets.Code.UI.Screens;
using Assets.Code.UI.Widgets;
using Assets.Code.Utils;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Speech;
using DD2A11y.Core.Text;
using DD2A11y.Elements;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine;
using UnityEngine.UI;

namespace DD2A11y.Screens {
    /// <summary>
    /// The Bounty Hunter hire dialog (Confessions; the inn's wanted poster opens it). The
    /// offer first - the game's description with the candle fee, from the widget's own
    /// binding - then, with a full party, the game's "select a hero to replace" line and one
    /// row per party hero (name and class, "selected" once picked, vitals in the hero
    /// buffer; Enter picks through the icon's own button), then the Hire and Decline
    /// buttons. Hire with nothing picked is the game's silent no-op, spoken as unavailable.
    /// Escape declines through the screen's own close, which the game answers with its
    /// decline sting.
    /// </summary>
    public sealed class HunterHireScreen : GameScreen {
        private static readonly AccessTools.FieldRef<HunterHireScreenWidgetBhv, GameObject> FullPartyContainerField =
            AccessTools.FieldRefAccess<HunterHireScreenWidgetBhv, GameObject>("m_fullPartySelectionContainer");
        private static readonly AccessTools.FieldRef<HunterHireScreenWidgetBhv, List<GameObject>> HeroObjectsField =
            AccessTools.FieldRefAccess<HunterHireScreenWidgetBhv, List<GameObject>>("m_fullPartyHeroObjects");
        private static readonly AccessTools.FieldRef<HunterHireScreenWidgetBhv, DataContextBhv> ContextField =
            AccessTools.FieldRefAccess<HunterHireScreenWidgetBhv, DataContextBhv>("m_dataContextBhv");

        private HunterHireScreenWidgetBhv _widget;

        public override string Name => GameLoc.TryGet("hunter_hire_dialog_title_label") ?? S.ScreenGeneric;

        public override object ResolveTarget() {
            var top = StackTop.Object();
            _widget = top == null ? null : top.GetComponentInChildren<HunterHireScreenWidgetBhv>(includeInactive: false);
            return _widget;
        }

        public override Container BuildRoot(object target) {
            var widget = (HunterHireScreenWidgetBhv)target;
            var screen = widget.GetComponentInParent<UiScreenBhv>();
            var root = new RootContainer(ContainerShape.VerticalList, back: () => screen.TryCloseScreen());
            var context = ContextField(widget);
            root.Add(new ReadoutElement(() => context.GetStringValue("hire_desc")));
            var container = FullPartyContainerField(widget);
            bool fullParty = container != null && container.activeSelf;
            if (fullParty) {
                root.Add(new ReadoutElement(() => GameLoc.TryGet("hunter_hire_select_hero_label")));
                var heroObjects = HeroObjectsField(widget);
                for (int i = 0; i < heroObjects.Count; i++) {
                    var icon = heroObjects[i].GetComponent<HighlightableIconButtonBhv>();
                    var selectable = heroObjects[i].GetComponent<Selectable>();
                    if (icon != null && selectable != null) {
                        root.Add(new ChoiceElement(i, icon, selectable));
                    }
                }
            }
            foreach (var button in widget.GetComponentsInChildren<Button>(includeInactive: false)) {
                if (button.gameObject.name == "ConfirmBtn") {
                    root.Add(new HireElement(widget, button, fullParty));
                } else if (button.gameObject.name == "DeclineBtn") {
                    root.Add(new SelectableElement(button));
                }
            }
            return root;
        }

        // The party hero behind the i-th icon, the same index the widget's own SelectHero
        // resolves.
        private static uint PartyGuid(int index) {
            var guids = Singleton<Assets.Code.Game.GameTypeMgr>.Instance.RosterManager.GetActorGuids(RosterStatusType.PARTY);
            return index < guids.Count ? guids[index] : 0;
        }

        private static bool AnyPicked(HunterHireScreenWidgetBhv widget) {
            foreach (var heroObject in HeroObjectsField(widget)) {
                var icon = heroObject.GetComponent<HighlightableIconButtonBhv>();
                if (icon != null && icon.Selected) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>A party hero to replace: name and class, "selected" once picked; Enter is
        /// the icon's own click (the widget's SelectHero), and the row re-reads with its new
        /// state. On a controller the game's own SelectHero hires at once.</summary>
        private sealed class ChoiceElement : SelectableElement {
            private readonly int _index;
            private readonly HighlightableIconButtonBhv _icon;

            public ChoiceElement(int index, HighlightableIconButtonBhv icon, Selectable selectable) : base(selectable) {
                _index = index;
                _icon = icon;
            }

            public override string Label {
                get {
                    var actor = Actors.Get(PartyGuid(_index));
                    return actor == null ? null
                        : SpokenLine.Join(Actors.Name(actor), GameLoc.TryGet(actor.ActorDataClass.Id));
                }
            }

            public override string Status => _icon.Selected ? S.StatusSelected : null;

            public override bool ReannounceOnActivate => true;

            public override IEnumerable<string> GetSideBufferLines(string bufferKey)
                => bufferKey == Core.Buffers.BufferKeys.Hero
                    ? HeroStatus.Lines(PartyGuid(_index)) : base.GetSideBufferLines(bufferKey);
        }

        /// <summary>The Hire button: the widget's OnHirePressed, which with a full party and
        /// no hero picked does nothing - spoken as unavailable instead.</summary>
        private sealed class HireElement : SelectableElement {
            private readonly HunterHireScreenWidgetBhv _widget;
            private readonly bool _fullParty;

            public HireElement(HunterHireScreenWidgetBhv widget, Button button, bool fullParty) : base(button) {
                _widget = widget;
                _fullParty = fullParty;
            }

            public override IEnumerable<ElementAction> GetActions() {
                yield return new ElementAction(ActionIds.Activate, () => {
                    if (_fullParty && !AnyPicked(_widget)) {
                        SpeechPipeline.Instance?.Speak(S.StatusUnavailable, interrupt: true);
                        return;
                    }
                    Submit();
                });
            }
        }
    }
}
