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
    /// The crossroads (HERO_SELECT game mode): the party strip, the hero roster, and the embark
    /// controls, as three vertical blocks the arrows flow through (party and roster are
    /// horizontal strips; Up/Down spill between blocks). Hero detail lives in each slot's
    /// tooltip, reviewed through the buffer.
    /// </summary>
    public sealed class CrossroadsScreen : GameScreen {
        private static readonly System.Reflection.FieldInfo EmbarkBtnField =
            AccessTools.Field(typeof(EmbarkUiBhv), "m_embarkBtn");
        private static readonly System.Reflection.FieldInfo MouseEmbarkBtnField =
            AccessTools.Field(typeof(EmbarkUiBhv), "m_mouseEmbarkBtn");
        private static readonly System.Reflection.FieldInfo ApplyAllField =
            AccessTools.Field(typeof(EmbarkUiBhv), "m_applyAllRelationshipsButton");

        private HeroSelectBhv _heroSelect;
        private Container _root;
        private int _builtSlots;

        public override string Name => S.ScreenCrossroads;

        public override object ResolveTarget() {
            if (GameModeMgr.CurrentMode != GameModeType.HERO_SELECT || Singleton<GameModeMgr>.Instance.IsChangingState()) {
                return null;
            }
            if (_heroSelect == null) {
                _heroSelect = Object.FindObjectOfType<HeroSelectBhv>();
            }
            return _heroSelect;
        }

        public override Container BuildRoot(object target) {
            _root = new RootContainer(ContainerShape.VerticalList);
            Populate();
            return _root;
        }

        public override bool OnUpdate(object target) {
            var slots = Object.FindObjectsOfType<HeroSelectActorUIBhv>();
            if (slots.Length != _builtSlots) {
                _root.Clear();
                Populate();
            }
            return false;
        }

        private void Populate() {
            var slots = Object.FindObjectsOfType<HeroSelectActorUIBhv>();
            _builtSlots = slots.Length;

            var party = new List<HeroSelectActorUIBhv>();
            var roster = new List<HeroSelectActorUIBhv>();
            foreach (var slot in slots) {
                (slot.IsRosterSlot ? roster : party).Add(slot);
            }
            party.Sort(BySiblingIndex);
            roster.Sort((a, b) => a.RosterIndex.CompareTo(b.RosterIndex));

            AddStrip(S.CrossroadsParty, party);
            AddStrip(S.CrossroadsRoster, roster);

            var actions = new Container(ContainerShape.VerticalList);
            var embarkUi = Object.FindObjectOfType<EmbarkUiBhv>();
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
