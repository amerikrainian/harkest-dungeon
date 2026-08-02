using System;
using System.Collections.Generic;
using Assets.Code.UI;
using Assets.Code.UI.Banter;
using Assets.Code.UI.Items;
using DD2A11y.Core.Nav;
using DD2A11y.Elements;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using TMPro;
using UnityEngine;

namespace DD2A11y.Screens {
    /// <summary>
    /// The advance-or-escape dialog (<c>DungeonConfirmationDialogBhv</c>) between the battles of
    /// a multi-battle roadside node - lairs (the Library and its kin) and guardian nodes share
    /// it, retitling through its own loc keys. Named by the dialog's own title; reads the
    /// description, the party ribbons, the loot the cleared battles secured and the next
    /// battle's offer, then the two choices. The sighted commit is a one-second pointer hold on
    /// either button - Enter drives the widget's own confirm/decline methods, which invoke the
    /// game's stored commands and close the screen. The game refuses to close the dialog
    /// without a choice, so Escape answers "unavailable" rather than committing the escape.
    /// </summary>
    public sealed class LairAdvanceScreen : GameScreen {
        private static readonly AccessTools.FieldRef<DungeonConfirmationDialogBhv, TextMeshProUGUI> TitleField =
            AccessTools.FieldRefAccess<DungeonConfirmationDialogBhv, TextMeshProUGUI>("m_titleText");
        private static readonly AccessTools.FieldRef<DungeonConfirmationDialogBhv, TextMeshProUGUI> DescField =
            AccessTools.FieldRefAccess<DungeonConfirmationDialogBhv, TextMeshProUGUI>("m_descText");
        private static readonly AccessTools.FieldRef<DungeonConfirmationDialogBhv, GameObject> AdvanceField =
            AccessTools.FieldRefAccess<DungeonConfirmationDialogBhv, GameObject>("m_continueButton");
        private static readonly AccessTools.FieldRef<DungeonConfirmationDialogBhv, GameObject> EscapeField =
            AccessTools.FieldRefAccess<DungeonConfirmationDialogBhv, GameObject>("m_declineBtn");
        private static readonly AccessTools.FieldRef<DungeonConfirmationDialogBhv, Transform> LootedField =
            AccessTools.FieldRefAccess<DungeonConfirmationDialogBhv, Transform>("m_lootedItemsContainer");
        private static readonly AccessTools.FieldRef<DungeonConfirmationDialogBhv, Transform> UpcomingField =
            AccessTools.FieldRefAccess<DungeonConfirmationDialogBhv, Transform>("m_upcomingItemsContainer");
        private static readonly AccessTools.FieldRef<DungeonConfirmationDialogBhv, List<HeroRibbonBhv>> RibbonsField =
            AccessTools.FieldRefAccess<DungeonConfirmationDialogBhv, List<HeroRibbonBhv>>("m_heroRibbons");

        private readonly Action<string, bool> _speak;
        private DungeonConfirmationDialogBhv _dialog;

        public LairAdvanceScreen(Action<string, bool> speak) {
            _speak = speak;
        }

        // The title is set directly (not databound) before the screen tops the stack, so it is
        // readable on the entry frame.
        public override string Name {
            get {
                var title = _dialog == null ? null : TitleField(_dialog);
                return title == null || string.IsNullOrEmpty(title.text) ? S.ScreenGeneric : title.text;
            }
        }

        public override object ResolveTarget() {
            var top = StackTop.Object();
            _dialog = top == null ? null : top.GetComponentInChildren<DungeonConfirmationDialogBhv>(includeInactive: false);
            return _dialog;
        }

        public override Container BuildRoot(object target) {
            var dialog = (DungeonConfirmationDialogBhv)target;
            var root = new RootContainer(ContainerShape.VerticalList,
                back: () => _speak(S.StatusUnavailable, true));

            var desc = DescField(dialog);
            root.Add(new StaticTextElement(() => desc == null ? null : desc.text));

            foreach (var ribbon in RibbonsField(dialog)) {
                root.Add(new HeroRibbonElement(ribbon));
            }

            AddLoot(root, LootedField(dialog), S.LairLooted);
            AddLoot(root, UpcomingField(dialog), S.LairNextLoot);

            var advance = AdvanceField(dialog);
            root.Add(new ActionElement(() => UiText.FirstLabel(advance), S.RoleButton, dialog.OnConfirm));
            var escape = EscapeField(dialog);
            if (escape != null && escape.activeSelf) {
                root.Add(new ActionElement(() => UiText.FirstLabel(escape), S.RoleButton, dialog.OnDecline));
            }
            return root;
        }

        // One row per reward icon, grouped under what the group means; the icon's only visible
        // text is its quantity badge, so the rows read like the kingdom panels' rewards.
        private static void AddLoot(Container root, Transform container, string label) {
            if (container == null) {
                return;
            }
            var group = new Container(ContainerShape.VerticalList, label);
            foreach (var reward in container.GetComponentsInChildren<UninteractableRewardItemBhv>(includeInactive: false)) {
                var captured = reward;
                group.Add(new ReadoutElement(
                    () => RewardItems.Title(captured),
                    value: () => RewardItems.Quantity(captured),
                    detail: () => TooltipReader.Lines(captured.gameObject)));
            }
            if (!group.IsEmptyContainer) {
                root.Add(group);
            }
        }
    }
}
