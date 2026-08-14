using System.Collections.Generic;
using Assets.Code.Game;
using Assets.Code.Inn.Presentation;
using Assets.Code.Item;
using Assets.Code.Library;
using Assets.Code.Run;
using Assets.Code.Utils;
using DD2A11y.Core.Text;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Game {
    /// <summary>
    /// Run-wide status glances, composed from the run model so they answer from any screen:
    /// the flame level, the coach's armor and wheels, and the wallet. Null outside a run -
    /// the glance keys answer absence with silence.
    /// </summary>
    public static class RunStatus {
        public static string FlameLine() {
            var mgr = Singleton<GameTypeMgr>.Instance;
            if (mgr == null || !mgr.IsGameTypeStarted || mgr.RunValues == null) {
                return null;
            }
            return S.DrivingFlame(((int)mgr.RunValues.GetValue(RunValueType.TORCH)).ToString());
        }

        public static string CoachLine() {
            var armor = CoachStat("stage_coach_sheet_armor_stat_label",
                RunValueType.STAGE_COACH_ARMOR, RunStatType.STAGE_COACH_ARMOR_MAX_VALUE);
            var wheels = CoachStat("stage_coach_sheet_wheel_stat_label",
                RunValueType.STAGE_COACH_WHEELS, RunStatType.STAGE_COACH_WHEELS_MAX_VALUE);
            string line = SpokenLine.Join(armor, wheels);
            return string.IsNullOrEmpty(line) ? null : line;
        }

        /// <summary>A coach stat in the game's own sheet composition ("Armor: {0}/{1}");
        /// shared with the driving HUD's status readouts.</summary>
        public static string CoachStat(string locKey, RunValueType value, RunStatType max) {
            string format = GameLoc.TryGet(locKey);
            var mgr = Singleton<GameTypeMgr>.Instance;
            if (format == null || mgr == null || !mgr.IsGameTypeStarted || mgr.RunValues == null) {
                return null;
            }
            return string.Format(format,
                mgr.RunValues.GetValue(value), mgr.RunDataManager.GetBaseStatValue(max));
        }

        /// <summary>The pet riding the stagecoach's pet slot, or null.</summary>
        public static IReadOnlyItemInstance Pet() {
            var mgr = Singleton<GameTypeMgr>.Instance;
            var coach = mgr == null ? null : mgr.StageCoach;
            if (coach == null) {
                return null;
            }
            var items = coach.GetSlotInventory(ItemSlotType.PET).GetValidItems();
            return items.Count > 0 ? items[0] : null;
        }

        /// <summary>The wallet the game's currency bar shows: Relics (the gold item), Mastery
        /// (minus points pending at an inn trainer, like the bar), the Baubles total over the
        /// faction-tagged currency items with each held type by its own name, and a kingdom's
        /// Materials.</summary>
        public static string WalletLine() {
            var mgr = Singleton<GameTypeMgr>.Instance;
            if (mgr == null || !mgr.IsGameTypeStarted || mgr.PlayerInventory == null) {
                return null;
            }
            var parts = new List<string> {
                SpokenLine.Join(GameLoc.TryGet("inventory_tooltip_relics"),
                    mgr.PlayerInventory.GetGoldQty().ToString()),
            };
            int mastery = (int)mgr.RunValues.GetValue(RunValueType.HERO_UPGRADE_POINTS);
            if (GameModeMgr.CurrentMode == GameModeType.INN
                && SingletonMonoBehaviour<InnPresentationBhv>.HasInstance()) {
                mastery -= SingletonMonoBehaviour<InnPresentationBhv>.Instance.UpgradeSkillsPointsSpent;
            }
            parts.Add(SpokenLine.Join(GameLoc.TryGet("inventory_tooltip_heropoints"), mastery.ToString()));

            int baubles = 0;
            var held = new List<string>();
            foreach (var item in SingletonMonoBehaviour<Library<string, ItemDefinition>>.Instance
                         .GetLibraryElements(HasFactionTag)) {
                int qty = mgr.PlayerInventory.GetItemQty(item);
                baubles += qty;
                if (qty > 0) {
                    held.Add(SpokenLine.Join(ItemDescription.GetTitle(item), qty.ToString()));
                }
            }
            parts.Add(SpokenLine.Join(GameLoc.TryGet("inventory_tooltip_biome_currency"), baubles.ToString()));
            parts.AddRange(held);

            if (mgr.CurrentGameType == GameType.KINGDOM) {
                var materials = SingletonMonoBehaviour<Library<string, ItemDefinition>>.Instance
                    .GetLibraryElement("materials");
                if (materials != null) {
                    parts.Add(SpokenLine.Join(ItemDescription.GetTitle(materials),
                        mgr.PlayerInventory.GetItemQty(materials).ToString()));
                }
            }
            return SpokenLine.Join(parts.ToArray());
        }

        private static bool HasFactionTag(ItemDefinition item) {
            if (item.m_tags == null) {
                return false;
            }
            foreach (var tag in item.m_tags) {
                if (tag == "faction") {
                    return true;
                }
            }
            return false;
        }
    }
}
