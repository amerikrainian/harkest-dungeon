using System;
using System.Collections.Generic;
using Assets.Code.Campaign;
using Assets.Code.Game;
using Assets.Code.UI;
using Assets.Code.UI.HeroSelect;
using Assets.Code.Utils;
using DD2A11y.Core.Nav;
using DD2A11y.Elements;
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
        private static readonly System.Reflection.FieldInfo EmbarkBtnField =
            AccessTools.Field(typeof(EmbarkUiBhv), "m_embarkBtn");
        private static readonly System.Reflection.FieldInfo MouseEmbarkBtnField =
            AccessTools.Field(typeof(EmbarkUiBhv), "m_mouseEmbarkBtn");
        private static readonly System.Reflection.FieldInfo ApplyAllField =
            AccessTools.Field(typeof(EmbarkUiBhv), "m_applyAllRelationshipsButton");
        private static readonly System.Reflection.MethodInfo IsDropValidMethod =
            AccessTools.Method(typeof(HeroSelectActorUIBhv), "IsDropValid");
        private static readonly System.Reflection.MethodInfo IsDropAcceptedMethod =
            AccessTools.Method(typeof(HeroSelectActorUIBhv), "IsDropAccepted");
        private static readonly System.Reflection.MethodInfo OnDropAcceptedMethod =
            AccessTools.Method(typeof(HeroSelectActorUIBhv), "OnDropAccepted");

        private readonly Action<string, bool> _speak;
        private HeroSelectBhv _heroSelect;
        private Container _root;
        private int _builtSlots;
        private HeroSlotElement _grabbed;

        public CrossroadsScreen(Action<string, bool> speak) {
            _speak = speak;
        }

        public override string Name => S.ScreenCrossroads;

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
            Populate();
            return _root;
        }

        public override bool OnUpdate(object target) {
            var slots = UnityEngine.Object.FindObjectsOfType<HeroSelectActorUIBhv>();
            if (slots.Length != _builtSlots) {
                _root.Clear();
                Populate();
            }
            return false;
        }

        /// <summary>The Space key: pick up the focused hero, or place a grabbed hero on the
        /// focused slot through the game's own drop rules (into a rank, swapping ranks, or
        /// back to the pool).</summary>
        public void ToggleGrab(UIElement current) {
            var hero = current as HeroSlotElement;
            if (hero == null) {
                return;
            }
            if (_grabbed == null) {
                if (!hero.CanGrab) {
                    return;
                }
                _grabbed = hero;
                _speak(S.CrossroadsGrabbed(hero.Label), true);
                return;
            }
            if (_grabbed.Slot == hero.Slot) {
                _grabbed = null;
                _speak(S.CrossroadsGrabCancelled, true);
                return;
            }

            var source = _grabbed.Slot;
            var target = hero.Slot;
            bool valid = (bool)IsDropValidMethod.Invoke(target, new object[] { source })
                && (bool)IsDropAcceptedMethod.Invoke(target, new object[] { source });
            if (!valid) {
                _speak(S.CrossroadsCannotPlace, true);
                return;
            }
            OnDropAcceptedMethod.Invoke(target, new object[] { source });
            _grabbed = null;
            _speak(hero.GetFocusText(), true); // the landing slot, read live with its new occupant
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
            party.Sort((a, b) => a.RosterIndex.CompareTo(b.RosterIndex));
            pool.Sort(BySiblingIndex);

            AddStrip(S.CrossroadsParty, party);
            AddStrip(S.CrossroadsRoster, pool);

            var actions = new Container(ContainerShape.VerticalList);
            var embarkUi = UnityEngine.Object.FindObjectOfType<EmbarkUiBhv>();
            if (embarkUi != null) {
                AddButtonFrom(actions, MouseEmbarkBtnField.GetValue(embarkUi) as GameObject);
                AddButtonFrom(actions, EmbarkBtnField.GetValue(embarkUi) as GameObject);
                AddButtonFrom(actions, ApplyAllField.GetValue(embarkUi) as GameObject);
            }
            if (!actions.IsEmptyContainer) {
                _root.Add(actions);
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
                    strip.Add(new HeroSlotElement(slot, button));
                }
            }
            if (!strip.IsEmptyContainer) {
                _root.Add(strip);
            }
        }

        private static void AddButtonFrom(Container container, GameObject holder) {
            if (holder == null || !holder.activeInHierarchy) {
                return;
            }
            var button = holder.GetComponentInChildren<Button>(includeInactive: false);
            if (button != null) {
                container.Add(new SelectableElement(button, null, holder));
            }
        }

        private static int BySiblingIndex(HeroSelectActorUIBhv a, HeroSelectActorUIBhv b)
            => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex());
    }
}
