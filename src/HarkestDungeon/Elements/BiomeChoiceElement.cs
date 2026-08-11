using System.Collections.Generic;
using Assets.Code.Data;
using Assets.Code.Inn;
using Assets.Code.Inn.Presentation;
using Assets.Code.Loot;
using Assets.Code.Map.Generation.Biome;
using Assets.Code.Run;
using Assets.Code.UI.Items;
using Assets.Code.UI.Tooltips;
using Assets.Code.Utils;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine;
using UnityEngine.UI;

namespace DD2A11y.Elements {
    /// <summary>
    /// One route option on the inn's Select Route screen: the destination region's own name,
    /// "selected" when it is the chosen route, and the offer's detail as buffer lines in the
    /// slot's visible order - the modifier name with its effect tooltip, the goal name with the
    /// goal tooltip, then the game's own reward header and the rewards. Enter is the game's own
    /// submit, which marks this route as the chosen one (departure happens later, through the
    /// inn's own depart flow).
    /// </summary>
    public sealed class BiomeChoiceElement : SelectableElement {
        private static readonly AccessTools.FieldRef<BiomeChoiceBhv, TextTooltipBhv> ModifierTooltipField =
            AccessTools.FieldRefAccess<BiomeChoiceBhv, TextTooltipBhv>("m_tooltipBhv");
        private static readonly AccessTools.FieldRef<BiomeChoiceBhv, TextTooltipBhv> GoalTooltipField =
            AccessTools.FieldRefAccess<BiomeChoiceBhv, TextTooltipBhv>("m_biomeGoalTooltip");
        private static readonly AccessTools.FieldRef<BiomeChoiceBhv, GameObject> TextRewardField =
            AccessTools.FieldRefAccess<BiomeChoiceBhv, GameObject>("m_textRewardObj");
        private static readonly AccessTools.FieldRef<BiomeChoiceBhv, UninteractableRewardItemBhv> RewardItemField =
            AccessTools.FieldRefAccess<BiomeChoiceBhv, UninteractableRewardItemBhv>("m_rewardItem");

        private readonly BiomeChoiceBhv _choice;
        private readonly int _index;

        public BiomeChoiceElement(BiomeChoiceBhv choice, Selectable selectable, int index)
            : base(selectable, () => ChoiceName(choice)) {
            _choice = choice;
            _index = index;
        }

        private static string ChoiceName(BiomeChoiceBhv choice) {
            var context = choice == null ? null : choice.GetComponent<DataContextBhv>();
            string stored = context == null ? null : context.GetStringValue("biome_label");
            // The binding usually stores the loc key; one branch stores the resolved name.
            return GameLoc.TryGet(stored) ?? stored;
        }

        public override string Status
            => Singleton<InnBhv>.Instance.GetSelectedBiomeChoiceIndex() == _index ? S.StatusSelected : base.Status;

        public override bool ReannounceOnActivate => true;

        // The modifier and goal names are plain data-bound labels (only their tooltips are
        // tooltip components), and the mastery-point reward's count rides only in bound text,
        // so those lines come from the offer's model - which also carries the full modifier
        // name where the label may be ellipsized to fit. Tooltips not placed here (the
        // mountain's equip-trophy prompt) still read, after them.
        protected override IEnumerable<string> GetDetailLines() {
            var model = Model();
            if (model == null || model.m_BiomeGoal == null) {
                foreach (var line in base.GetDetailLines()) {
                    yield return line;
                }
                yield break;
            }
            var placed = new HashSet<TooltipUiBhv>();
            yield return GameLoc.TryGet("biome_mutator_" + model.m_BiomeModifier.m_Id);
            foreach (var line in Place(ModifierTooltipField(_choice), placed)) {
                yield return line;
            }
            yield return GameLoc.TryGet("biome_goal_" + model.m_BiomeGoal.m_Id);
            foreach (var line in Place(GoalTooltipField(_choice), placed)) {
                yield return line;
            }
            foreach (var line in RewardLines(model, placed)) {
                yield return line;
            }
            foreach (var tooltip in RowScope.GetComponentsInChildren<TooltipUiBhv>(includeInactive: false)) {
                if (tooltip.enabled && !placed.Contains(tooltip)) {
                    foreach (var line in TooltipReader.LinesOf(tooltip)) {
                        yield return line;
                    }
                }
            }
        }

        // The widgets pair with the offer list by spawn order, the same index the game's own
        // Init receives.
        private BiomeChoice Model() {
            var choices = Singleton<InnBhv>.Instance.GetBiomeChoices();
            return choices != null && _index < choices.Count ? choices[_index] : null;
        }

        // Mirrors the game's own reward display: mastery points as the header-and-count text
        // ("+2 Mastery", the run log's wording for this reward), an item through the reward
        // icon's tooltip (title, type with stack count, description).
        private IEnumerable<string> RewardLines(BiomeChoice model, HashSet<TooltipUiBhv> placed) {
            var reward = model.m_BiomeGoalReward;
            if (reward == null) {
                yield break;
            }
            bool headed = false;
            foreach (var loot in reward.LootPreRoll.LootRewards) {
                if (loot.m_type == LootType.PROVISION
                    && CustomEnum<RunValueType>.Cast(loot.m_id) == RunValueType.HERO_UPGRADE_POINTS) {
                    if (!headed) {
                        headed = true;
                        yield return GameLoc.TryGet("inn_biome_goal_reward_label");
                    }
                    string format = GameLoc.TryGet("loot_rewards_hero_points_label");
                    // The label's heropoints glyph sits beside the word "Mastery" it stands
                    // for; spoken it would double up.
                    yield return format == null
                        ? loot.m_qty.ToString()
                        : Core.Text.SpriteText.Strip(string.Format(format, loot.m_qty));
                    foreach (var line in Place(TextRewardField(_choice).GetComponent<TextTooltipBhv>(), placed)) {
                        yield return line;
                    }
                } else if (loot.m_type == LootType.ITEM) {
                    if (!headed) {
                        headed = true;
                        yield return GameLoc.TryGet("inn_biome_goal_reward_label");
                    }
                    foreach (var line in Place(RewardItemField(_choice).GetComponent<TooltipUiBhv>(), placed)) {
                        yield return line;
                    }
                }
            }
        }

        // The reward icon is shared, so a second reward of the same kind reads once - the
        // widget shows only the last one it was given.
        private static IEnumerable<string> Place(TooltipUiBhv tooltip, HashSet<TooltipUiBhv> placed) {
            if (tooltip == null || !placed.Add(tooltip) || !tooltip.isActiveAndEnabled) {
                yield break;
            }
            foreach (var line in TooltipReader.LinesOf(tooltip)) {
                yield return line;
            }
        }
    }
}
