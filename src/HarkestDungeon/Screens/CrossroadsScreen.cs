using System;
using System.Collections.Generic;
using Assets.Code.Campaign;
using Assets.Code.Game;
using Assets.Code.UI;
using Assets.Code.UI.HeroSelect;
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
    /// The crossroads (HERO_SELECT game mode): the party ranks (the game's "roster slots",
    /// Rank1-4), the selectable hero pool, and the embark controls, as vertical blocks the
    /// arrows flow through. Enter runs the game's quick-transfer (pool hero into the party /
    /// out again); Space grabs a hero and places them on another slot through the game's own
    /// drop logic (move into a specific rank, swap ranks, send back to the pool); the inspect
    /// key opens the hero sheet. Hero detail lives in each slot's tooltip, reviewed through
    /// the buffer.
    /// </summary>
    public sealed class CrossroadsScreen : GameScreen {
        // The embark control is the hero-select canvas's confirm button (a bare Selectable the
        // game drives with a hold-to-confirm); it only activates once every party slot is filled.
        private static readonly AccessTools.FieldRef<HeroSelectBhv, UnityEngine.UI.Selectable> ConfirmButtonField =
            AccessTools.FieldRefAccess<HeroSelectBhv, UnityEngine.UI.Selectable>("m_ConfirmButton");
        // The canvas overlays' opener buttons, each surfaced now that its panel reads.
        private static readonly AccessTools.FieldRef<HeroSelectBhv, GameObject> PathButtonField =
            AccessTools.FieldRefAccess<HeroSelectBhv, GameObject>("m_pathSelectionButton");
        private static readonly AccessTools.FieldRef<HeroSelectBhv, Button> LoadoutButtonField =
            AccessTools.FieldRefAccess<HeroSelectBhv, Button>("m_partyLoadoutButton");
        // The shown hero's name field and the icon-only buttons beside it.
        private static readonly AccessTools.FieldRef<HeroSelectBhv, TMPro.TMP_InputField> NameFieldRef =
            AccessTools.FieldRefAccess<HeroSelectBhv, TMPro.TMP_InputField>("m_textInput");
        // Serialized as a GameObject (the game reaches its Selectable through GetComponent).
        private static readonly AccessTools.FieldRef<HeroSelectBhv, GameObject> ResetButtonField =
            AccessTools.FieldRefAccess<HeroSelectBhv, GameObject>("m_resetButton");
        private static readonly System.Reflection.MethodInfo IsDropValidMethod =
            AccessTools.Method(typeof(HeroSelectActorUIBhv), "IsDropValid");
        private static readonly System.Reflection.MethodInfo IsDropAcceptedMethod =
            AccessTools.Method(typeof(HeroSelectActorUIBhv), "IsDropAccepted");
        private static readonly System.Reflection.MethodInfo OnDropAcceptedMethod =
            AccessTools.Method(typeof(HeroSelectActorUIBhv), "OnDropAccepted");

        private readonly Action<string, bool> _speak;
        private readonly Core.Text.TypingEcho _echo;
        private readonly Audio.RankToneLadder _ladder;
        private HeroSelectBhv _heroSelect;
        private Container _root;
        private int _builtSlots;
        private bool _builtEmbarkVisible;
        private HeroSlotElement _grabbed;

        public CrossroadsScreen(Action<string, bool> speak, Core.Audio.IAudioEngine audio,
                                Core.Settings.BoolSetting tones) {
            _speak = speak;
            _ladder = new Audio.RankToneLadder(audio, tones);
            // The hero rename runs the game's own edit flow, which sets its IsInputtingText -
            // so the mod's keys already pause; this only echoes what the field takes.
            _echo = new Core.Text.TypingEcho(() => Game.TextEntry.IsTyping, HeroName, speak);
        }

        private string HeroName() {
            var field = _heroSelect == null ? null : NameFieldRef(_heroSelect);
            return field == null ? "" : field.text;
        }

        public override string Name => S.ScreenCrossroads;

        // The hero slots advertise rename/reroll, so the roster keys are live here.
        private static readonly Core.Input.InputCategory[] RosterCategories =
            { Core.Input.InputCategory.Roster, Core.Input.InputCategory.UI };
        public override Core.Input.InputCategory[] InputCategories => RosterCategories;

        public override object ResolveTarget() {
            if (GameModeMgr.CurrentMode != GameModeType.HERO_SELECT || Singleton<GameModeMgr>.Instance.IsChangingState()) {
                return null;
            }
            if (_heroSelect == null) {
                _heroSelect = UnityEngine.Object.FindObjectOfType<HeroSelectBhv>();
            }
            return _heroSelect;
        }

        public override Container BuildRoot(object target) {
            _root = new RootContainer(ContainerShape.VerticalList);
            _grabbed = null;
            _ladder.Clear();
            Populate();
            return _root;
        }

        /// <summary>Focus-landing audio: the panel's aggregate rank-pip rows, computed from the
        /// live model so a committed path change or skill swap sounds through immediately. A
        /// PARTY hero answers the standing question - their own rank's lone rank tone, then
        /// their reach as the target phrase; a POOL hero answers the recruiting question - the
        /// full ladder, rank and target voices chorded per rank (the slot's buffer carries both
        /// rows as exact counts). Any other landing cancels what is left of a ladder - it
        /// described a hero no longer focused.</summary>
        public void OnFocusSettled(UIElement element) {
            if (!(element is HeroSlotElement hero) || !hero.HasHero) {
                _ladder.Clear();
                return;
            }
            var actor = hero.Slot.ActorInstance;
            int limit = RankCoverage.EquipLimit(actor);
            if (hero.Slot.IsRosterSlot) {
                _ladder.ScheduleParty(RankCoverage.LaunchCounts(actor)[hero.Slot.RosterIndex],
                    RankCoverage.TargetCounts(actor), limit);
            } else {
                _ladder.ScheduleLadder(RankCoverage.LaunchCounts(actor),
                    RankCoverage.TargetCounts(actor), limit);
            }
        }

        public override bool OnUpdate(object target) {
            _ladder.Tick();
            if (_echo.Tick()) {
                // The rename ended: read the accepted name (the game restores the previous one
                // when the field was left empty).
                _speak(Core.Text.SpokenLine.Join(S.HeroNameField, HeroName()), true);
            }
            var slots = UnityEngine.Object.FindObjectsOfType<HeroSelectActorUIBhv>();
            if (slots.Length != _builtSlots || EmbarkVisible() != _builtEmbarkVisible) {
                _root.Clear();
                Populate();
            }
            return false;
        }

        // The confirm button appears (SetActive) only once the party has no empty slot.
        private bool EmbarkVisible() {
            var confirm = ConfirmButtonField(_heroSelect);
            return confirm != null && confirm.gameObject.activeInHierarchy;
        }

        /// <summary>Enter and the grab key alike: pick up the focused hero, or place a grabbed
        /// hero on the focused slot through the game's own drop rules (into a rank, swapping
        /// ranks, or back to the pool). One move, one state - the game's own Enter two-step is
        /// deliberately not used, because it armed hidden selection state that desynced from
        /// our focus.</summary>
        public void ToggleGrab(UIElement current) {
            var hero = current as HeroSlotElement;
            if (hero == null) {
                return;
            }
            if (_grabbed == null) {
                if (!hero.CanGrab) {
                    // An empty rank or a locked hero: nothing to pick up, and silence here
                    // would read as a dropped keypress.
                    _speak(S.StatusUnavailable, true);
                    return;
                }
                _grabbed = hero;
                _speak(S.Grabbed(hero.HeroName), true); // the hero, not their rank
                return;
            }
            if (_grabbed.Slot == hero.Slot) {
                _grabbed = null;
                _speak(S.GrabCancelled, true);
                return;
            }

            var source = _grabbed.Slot;
            var target = hero.Slot;
            bool valid = (bool)IsDropValidMethod.Invoke(target, new object[] { source })
                && (bool)IsDropAcceptedMethod.Invoke(target, new object[] { source });
            if (!valid) {
                _speak(S.CannotPlace, true);
                return;
            }
            OnDropAcceptedMethod.Invoke(target, new object[] { source });
            _grabbed = null;
            _speak(hero.GetFocusText(), true); // the landing slot, read live with its new occupant
            // The drop put a new hero under the focus without a focus move, so the landing
            // ladder replays for the arrival - the moment the player judges the new rank.
            OnFocusSettled(hero);
        }

        private void Populate() {
            var slots = UnityEngine.Object.FindObjectsOfType<HeroSelectActorUIBhv>();
            _builtSlots = slots.Length;

            // The game's "roster slots" are the four party ranks; the rest is the hero pool.
            var party = new List<HeroSelectActorUIBhv>();
            var pool = new List<HeroSelectActorUIBhv>();
            foreach (var slot in slots) {
                (slot.IsRosterSlot ? party : pool).Add(slot);
            }
            // Rank 4 leftmost, rank 1 last - the same left-to-right order the combat
            // battlefield row walks the party in.
            party.Sort((a, b) => b.RosterIndex.CompareTo(a.RosterIndex));
            pool.Sort(BySiblingIndex);

            AddStrip(S.CrossroadsParty, party);
            AddStrip(S.CrossroadsRoster, pool);

            var actions = new Container(ContainerShape.VerticalList);
            var partyName = UnityEngine.Object.FindObjectOfType<PartyNameBhv>();
            if (partyName != null) {
                // Live-guarded: the closure can be read the frame the canvas is torn down, when
                // the captured component is Unity-dead but not null.
                actions.Add(new ReadoutElement(
                    () => partyName == null ? null : UiText.FirstLabel(partyName.gameObject)));
            }
            // Restores the shown hero's cosmetics and memories; the game asks to confirm.
            // Only present on a run survivor, hence the live active check.
            var reset = ResetButtonField(_heroSelect);
            var resetSelectable = reset == null ? null : reset.GetComponent<Selectable>();
            if (resetSelectable != null && reset.activeInHierarchy) {
                actions.Add(new SelectableElement(resetSelectable, () => S.HeroReset));
            }

            // The focused hero's path seal: icon-only on the canvas, so it takes the panel's
            // own name. Opens the path overlay, which reads as its own screen.
            var pathButton = PathButtonField(_heroSelect);
            if (pathButton != null && pathButton.activeInHierarchy) {
                actions.Add(new ActionElement(() => S.ScreenPathSelect, S.RoleButton,
                    _heroSelect.TogglePathSelectionPanel));
            }

            var loadoutButton = LoadoutButtonField(_heroSelect);
            if (loadoutButton != null && loadoutButton.gameObject.activeInHierarchy) {
                actions.Add(new SelectableElement(loadoutButton));
            }

            // The Infernal Flame Vitrine (boss blessings and torch trophies on the coach),
            // which the game itself opens only on the StageCoach hotkey in expeditions - a
            // key the captured gate swallows and nothing on screen advertises. Named by the
            // vitrine's own title; it reads as its own screen once open.
            if (Singleton<GameTypeMgr>.Instance != null
                && Singleton<GameTypeMgr>.Instance.CurrentGameType == GameType.EXPEDITION) {
                actions.Add(new ActionElement(
                    () => GameLoc.TryGet("infernal_torch_boss_completion_title"), S.RoleButton,
                    () => SingletonMonoBehaviour<CommonUiBhv>.Instance.ToggleTorchCompletionScreen()));
            }

            var confirm = ConfirmButtonField(_heroSelect);
            _builtEmbarkVisible = confirm != null && confirm.gameObject.activeInHierarchy;
            if (_builtEmbarkVisible) {
                // The game's own confirm flow: validates the party, asks its equip-skills
                // confirmation dialog when a hero has unequipped skills, then starts the run.
                var confirmObject = confirm.gameObject;
                actions.Add(new ActionElement(() => UiText.FirstLabel(confirmObject), S.RoleButton,
                    _heroSelect.ConfirmRosterSelection));
            }
            if (confirm != null) {
                AddSiblingButton(actions, confirm.transform.parent, "RandomCompBtn");
            }
            if (!actions.IsEmptyContainer) {
                _root.Add(actions);
            }
        }

        // A canvas control with no serialized field on HeroSelectBhv, located by its stable
        // prefab name next to the confirm button; logged loudly if the game renames it.
        private static void AddSiblingButton(Container container, Transform canvas, string name) {
            var holder = canvas == null ? null : canvas.Find(name);
            if (holder == null) {
                Plugin.Log.LogWarning("CrossroadsScreen: no '" + name + "' under the hero select canvas");
                return;
            }
            var button = holder.GetComponent<Button>();
            if (button != null && holder.gameObject.activeInHierarchy) {
                container.Add(new SelectableElement(button));
            }
        }

        private void AddStrip(string label, List<HeroSelectActorUIBhv> slots) {
            if (slots.Count == 0) {
                return;
            }
            var strip = new Container(ContainerShape.HorizontalList, label);
            foreach (var slot in slots) {
                var button = slot.GetComponent<Button>();
                if (button != null && slot.gameObject.activeInHierarchy) {
                    strip.Add(new HeroSlotElement(slot, button, Display, ToggleGrab,
                        rename: _heroSelect.OnEditNameButtonPressed,
                        reroll: RerollName));
                }
            }
            if (!strip.IsEmptyContainer) {
                _root.Add(strip);
            }
        }

        // Make a hero the one the scene shows, through the game's own selection call - the
        // display, stats, path panel, and the name/reroll targets all follow it. Silent
        // (playAudio false): the mod already announces the landing, and the game's hero sting
        // on every arrow press would bury it. A no-op when the game is mid-selection, which is
        // its own guard.
        private void Display(HeroSlotElement hero) {
            _heroSelect.OnActorSelected(hero.Slot, playAudio: false);
        }

        // The game rolls a new name onto the shown hero without a word; speak the result, and
        // the hero it landed on, since nothing else would.
        private void RerollName() {
            _heroSelect.RollNewActorName();
            _speak(Core.Text.SpokenLine.Join(S.HeroNameField, HeroName()), true);
        }

        private static int BySiblingIndex(HeroSelectActorUIBhv a, HeroSelectActorUIBhv b)
            => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex());
    }
}
