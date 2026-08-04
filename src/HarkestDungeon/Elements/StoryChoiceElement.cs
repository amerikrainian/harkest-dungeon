using System.Collections.Generic;
using Assets.Code.Actor.Events;
using Assets.Code.Story;
using Assets.Code.Story.Events;
using Assets.Code.UI.Story;
using DD2A11y.Core.Nav;
using DD2A11y.Core.Text;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using TMPro;

namespace DD2A11y.Elements {
    /// <summary>
    /// One choice in a road story - always a hero (every story type keys its choices by actor):
    /// the hero's name with their HP and stress, and the full consequences in the buffer - the
    /// hero's bark line, then the game's own choice previews (the sighted Alt panel: effects on
    /// the party, then on the enemy side). Enter commits through the game's own selection event
    /// (the hold-to-choose equivalent), honoring its hoverable gate; C inspects the hero.
    /// </summary>
    public sealed class StoryChoiceElement : UIElement {
        private static readonly AccessTools.FieldRef<StoryChoiceButtonBhv, StoryType> StoryTypeField =
            AccessTools.FieldRefAccess<StoryChoiceButtonBhv, StoryType>("m_StoryType");
        // StoryBhv is internal to the game assembly; its choice lookup resolves by reflection,
        // loudly when the shape changed.
        private static readonly System.Type StoryBhvType = AccessTools.TypeByName("Assets.Code.Story.StoryBhv");
        private static readonly System.Reflection.MethodInfo GetChoiceMethod =
            AccessTools.Method(StoryBhvType, "GetStoryChoiceFromActorGuid");

        private readonly StoryChoiceButtonBhv _button;

        public StoryChoiceElement(StoryChoiceButtonBhv button) {
            _button = button;
        }

        public override bool CanFocus => _button != null && _button.gameObject.activeInHierarchy;

        public override string Label => Actors.Name(Actors.Get(_button.ActorGuid));

        public override string Value {
            get {
                var actor = Actors.Get(_button.ActorGuid);
                if (actor == null) {
                    return null;
                }
                string hp = Format("status_bar_health", (int)actor.DisplayedHp, (int)actor.DisplayedHpMax);
                string stress = Format("status_bar_stress", (int)actor.Stress, (int)actor.StressMax);
                return SpokenLine.Join(hp, stress);
            }
        }

        private static string Format(string locKey, int current, int max) {
            string format = GameLoc.TryGet(locKey);
            return format == null ? current + "/" + max : string.Format(format, current, max);
        }

        public override IEnumerable<ElementAction> GetActions() {
            yield return new ElementAction(ActionIds.Activate, Choose);
            yield return new ElementAction("inspect", () => EventInspectActor.Trigger(_button.ActorGuid));
        }

        // The same event the completed mouse hold fires; the game's own hoverable gate decides
        // whether a choice is currently accepted (presentation, darkened choices).
        private void Choose() {
            if (_button.Hoverable) {
                EventSelectStoryChoice.Trigger(StoryTypeField(_button), _button.ActorGuid);
            }
        }

        protected override IEnumerable<string> GetDetailLines() {
            string label = TextFilter.Clean(Label);
            foreach (var line in BarkLines(label)) {
                yield return line;
            }
            var choice = ChoiceFor(_button.ActorGuid);
            if (choice == null) {
                yield break;
            }
            for (int i = 0; i < choice.m_PlayerStoryChoicePreviewIds.Count; i++) {
                yield return SpokenLine.Join(S.CrossroadsParty, PreviewLine(choice, i, player: true));
            }
            for (int i = 0; i < choice.m_EnemyStoryChoicePreviewIds.Count; i++) {
                yield return SpokenLine.Join(S.CombatEnemies, PreviewLine(choice, i, player: false));
            }
        }

        private static StoryChoiceDefinition ChoiceFor(uint actorGuid) {
            if (GetChoiceMethod == null) {
                Plugin.Log.LogWarning("StoryChoiceElement: StoryBhv.GetStoryChoiceFromActorGuid not found; previews unavailable");
                return null;
            }
            var story = UnityEngine.Object.FindObjectOfType(StoryBhvType);
            return story == null ? null
                : (StoryChoiceDefinition)GetChoiceMethod.Invoke(story, new object[] { actorGuid });
        }

        // The sighted Alt panel's own composition: one loc-keyed description per preview icon
        // (a distinct key when the value is negative), with the number when the bar shows one.
        private static string PreviewLine(StoryChoiceDefinition choice, int index, bool player) {
            var ids = player ? choice.m_PlayerStoryChoicePreviewIds : choice.m_EnemyStoryChoicePreviewIds;
            var values = player ? choice.m_PlayerStoryChoicePreviewValues : choice.m_EnemyStoryChoicePreviewValues;
            var showNumbers = player ? choice.m_PlayerStoryChoicePreviewShowNumbers : choice.m_EnemyStoryChoicePreviewShowNumbers;
            string id = ids[index];
            int value = index < values.Count ? values[index] : 0;
            bool showNumber = index < showNumbers.Count && showNumbers[index];
            string description = null;
            if (value < 0) {
                description = GameLoc.TryGet("story_icon_description_" + id + "_negative");
            }
            description = description ?? GameLoc.TryGet("story_icon_description_" + id) ?? id;
            return showNumber ? description + " " + value : description;
        }

        // The hero's visible flavor line(s) under the button, minus placeholders (unbound
        // purple loc keys), bars, and the name that already leads the focus text.
        private IEnumerable<string> BarkLines(string label) {
            foreach (var tmp in _button.GetComponentsInChildren<TMP_Text>(includeInactive: false)) {
                string raw = tmp.text;
                if (string.IsNullOrWhiteSpace(raw) || raw.Contains("<color=purple>")) {
                    continue;
                }
                string clean = TextFilter.Clean(raw);
                if (clean.Length == 0 || clean == label) {
                    continue;
                }
                // Bar numerals (the HP label) already speak through the value.
                if (clean.Length <= 2 || clean.Contains("/")) {
                    continue;
                }
                yield return clean;
            }
        }
    }
}
