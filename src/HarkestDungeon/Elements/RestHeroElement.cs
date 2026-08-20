using System.Collections.Generic;
using Assets.Code.Actor;
using Assets.Code.UI.Widgets;
using DD2A11y.Core.Nav;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine;
using UnityEngine.UI;

namespace DD2A11y.Elements {
    /// <summary>
    /// One hero on the inn's rest strip: name with HP and stress read from the live actor, the
    /// slot's own status tooltip as buffer lines, and Enter through the game's own submit (which
    /// applies a selected rest item or opens the hero's own interactions). A heroless slot showing
    /// the game's roster-addition hover (a dead hero's chair with recruits waiting) reads as the
    /// game's own "Add hero to party" button; its Enter opens the Select Replacement Hero screen
    /// through the same submit. Hides while the slot has neither.
    /// </summary>
    public sealed class RestHeroElement : SelectableElement {
        private static readonly AccessTools.FieldRef<RestItemSlotBhv, GameObject> AdditionHoverField =
            AccessTools.FieldRefAccess<RestItemSlotBhv, GameObject>("m_rosterAdditionHover");

        private readonly RestItemSlotBhv _slot;

        public RestHeroElement(RestItemSlotBhv slot, Selectable selectable)
            : base(selectable) {
            _slot = slot;
        }

        private ActorInstance Actor => _slot != null && _slot.IsActive ? Actors.Get(_slot.ActorGuid) : null;

        private bool IsRecruitChair {
            get {
                if (_slot == null || _slot.IsActive) {
                    return false;
                }
                var hover = AdditionHoverField(_slot);
                return hover != null && hover.activeSelf;
            }
        }

        public override bool CanFocus => base.CanFocus && (Actor != null || IsRecruitChair);

        public override string Label => IsRecruitChair
            ? GameLoc.TryGet("inn_replacement_add_tooltip")
            : Actors.Name(Actor);

        public override string Role => IsRecruitChair ? S.RoleButton : null;

        public override string Value => Actors.StatusLine(Actor);

        // The chair's Selectable sits non-interactable (the game's RefreshRosterAddition assigns
        // its interactable property while the hover is still down, and the setter forces false
        // then), yet the slot's submit works regardless - the base's interactable gating must
        // not withhold Activate or speak "unavailable" here.
        public override string Status => IsRecruitChair ? null : base.Status;

        // Re-assert the value the game computed and lost to that setter ordering; it accepts
        // now that the hover is up. Without this the flag survives the hire (the occupied
        // branch never assigns it) and the recruit's slot would read unavailable and refuse
        // Enter until an inventory action resets it.
        public override void OnFocused() {
            if (IsRecruitChair && !Selectable.interactable) {
                _slot.interactable = true;
            }
        }

        public override IEnumerable<ElementAction> GetActions() {
            if (IsRecruitChair) {
                yield return new ElementAction(ActionIds.Activate, Submit);
                yield break;
            }
            foreach (var action in base.GetActions()) {
                yield return action;
            }
        }

        public override IEnumerable<string> GetSideBufferLines(string bufferKey)
            => bufferKey == Core.Buffers.BufferKeys.Hero
                ? HeroStatus.Lines(Actor) : base.GetSideBufferLines(bufferKey);
    }
}
