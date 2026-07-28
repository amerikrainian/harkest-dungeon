using System;
using System.Collections.Generic;
using Assets.Code.Data;
using Assets.Code.Game;
using Assets.Code.Story;
using Assets.Code.Utils;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Text;
using DD2A11y.Elements;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine.UI;

namespace DD2A11y.Screens {
    /// <summary>
    /// A hero story chapter intro (the HERO_STORY_INTRO mode, entered at a shrine): the chapter
    /// title, the hero it belongs to, and the chapter text as buffer lines, then the Continue
    /// button - which the game shows only after its presentation and narration finish, so its
    /// appearance is announced. Enter drives the game's own continue path. The game blocks the
    /// pause menu here, so Escape is inert, matching the sighted experience.
    /// </summary>
    public sealed class HeroStoryIntroScreen : GameScreen {
        private static readonly AccessTools.FieldRef<HeroStoryIntroPresentationBhv, DataContextBhv> DataContextField =
            AccessTools.FieldRefAccess<HeroStoryIntroPresentationBhv, DataContextBhv>("m_dataContextBhv");
        private static readonly AccessTools.FieldRef<HeroStoryIntroPresentationBhv, Button> ContinueButtonField =
            AccessTools.FieldRefAccess<HeroStoryIntroPresentationBhv, Button>("m_continueButton");
        private static readonly AccessTools.FieldRef<HeroStoryIntroPresentationBhv, uint> ActorGuidField =
            AccessTools.FieldRefAccess<HeroStoryIntroPresentationBhv, uint>("m_actorGuid");

        private readonly Action<string, bool> _speak;
        private HeroStoryIntroPresentationBhv _presentation;
        private bool _continueShown;
        private bool _titleAnnounced;

        public HeroStoryIntroScreen(Action<string, bool> speak) {
            _speak = speak;
        }

        public override string Name => Title() ?? S.ScreenGeneric;

        public override object ResolveTarget() {
            if (GameModeMgr.CurrentMode != GameModeType.HERO_STORY_INTRO
                || Singleton<GameModeMgr>.Instance.IsChangingState()) {
                _presentation = null;
                return null;
            }
            if (_presentation == null) {
                _presentation = UnityEngine.Object.FindObjectOfType<HeroStoryIntroPresentationBhv>();
            }
            return _presentation;
        }

        public override Container BuildRoot(object target) {
            var root = new RootContainer(ContainerShape.VerticalList);
            root.Add(new ReadoutElement(HeaderText, detail: BodyLines));
            root.Add(new SelectableElement(ContinueButtonField(_presentation)));
            _continueShown = ContinueVisible();
            _titleAnnounced = Title() != null;
            return root;
        }

        public override bool OnUpdate(object target) {
            // The chapter strings bind when the game's async portrait load lands, which can be
            // after entry; one re-announce then reads the real title instead of the fallback.
            bool announce = false;
            if (!_titleAnnounced && Title() != null) {
                _titleAnnounced = true;
                announce = true;
            }
            // The Continue button fades in when the presentation and narration end - the
            // sighted cue that the screen is ready to advance.
            bool shown = ContinueVisible();
            if (shown != _continueShown) {
                _continueShown = shown;
                if (shown) {
                    string label = UiText.FirstLabel(ContinueButtonField(_presentation).gameObject);
                    if (label != null) {
                        _speak(label, false);
                    }
                }
            }
            return announce;
        }

        private bool ContinueVisible() {
            var button = ContinueButtonField(_presentation);
            return button != null && button.gameObject.activeInHierarchy && button.interactable;
        }

        private string Title() {
            var context = _presentation == null ? null : DataContextField(_presentation);
            string title = context == null ? null : context.GetStringValue("hero_story_title");
            return string.IsNullOrWhiteSpace(title) ? null : TextFilter.Clean(title);
        }

        // Title leads (the distinguishing part), then the hero the chapter belongs to.
        private string HeaderText() {
            string hero = _presentation == null ? null : Actors.Name(Actors.Get(ActorGuidField(_presentation)));
            return SpokenLine.Join(Title(), hero);
        }

        private IEnumerable<string> BodyLines() {
            var context = _presentation == null ? null : DataContextField(_presentation);
            string body = context == null ? null : context.GetStringValue("hero_story_desc_body");
            return SpokenLine.NonEmptyLines(body);
        }
    }
}
