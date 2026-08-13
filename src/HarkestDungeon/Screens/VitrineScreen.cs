using System.Collections.Generic;
using Assets.Code.Boss;
using Assets.Code.Item;
using Assets.Code.Library;
using Assets.Code.Map.Generation.Biome;
using Assets.Code.Profile;
using Assets.Code.UI.Managers;
using Assets.Code.UI.Widgets;
using Assets.Code.Unlock;
using Assets.Code.Utils;
using DD2A11y.Core.Nav;
using DD2A11y.Elements;
using DD2A11y.Game;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Screens {
    /// <summary>
    /// The Infernal Flame Vitrine (the crossroads Z screen): a completion tracker, nothing on
    /// it activates. The flame tiers read as one row of informational entries - each flame's
    /// full item card (what it does, its unlock condition) in the buffer - then one row per
    /// confession boss: the boss's name, how many flames it has been felled under, and the
    /// completed flames' names as buffer lines. Everything composes from the same model the
    /// widget draws its diamonds from. Escape closes through the game's own close.
    /// </summary>
    public sealed class VitrineScreen : GameScreen {
        // Declares the vitrine key's category, so the same key that opened it toggles it
        // closed, like the game's own.
        private static readonly Core.Input.InputCategory[] Categories =
            { Core.Input.InputCategory.Roster, Core.Input.InputCategory.UI };

        private Container _root;

        public override Core.Input.InputCategory[] InputCategories => Categories;

        public override string Name
            => GameLoc.TryGet("infernal_torch_boss_completion_title") ?? S.ScreenGeneric;

        public override object ResolveTarget() {
            var top = StackTop.Object();
            return top == null ? null : top.GetComponentInChildren<TorchCompletionWidgetBhv>(includeInactive: false);
        }

        public override Container BuildRoot(object target) {
            _root = new RootContainer(ContainerShape.VerticalList,
                back: () => SingletonMonoBehaviour<CommonUiBhv>.Instance.AttemptToCloseTorchCompletionScreen());

            var flames = new List<ItemDefinition>(FlameItems());
            var row = new Container(ContainerShape.HorizontalList);
            foreach (var flame in flames) {
                var captured = flame;
                row.Add(new ReadoutElement(
                    () => ItemDescription.GetTitle(captured),
                    detail: () => FlameCard(captured)));
            }
            if (!row.IsEmptyContainer) {
                _root.Add(row);
            }

            foreach (var boss in Bosses()) {
                var captured = boss;
                _root.Add(new ReadoutElement(
                    () => GameLoc.TryGet("boss_choice_" + captured.m_Id + "_label"),
                    () => S.VitrineFlames(CompletedFlames(captured, flames).Count, flames.Count),
                    () => CompletedFlameNames(captured, flames)));
            }
            return _root;
        }

        // The same lists the widget spawns its categories and rows from.
        private static IEnumerable<ItemDefinition> FlameItems() {
            var track = SingletonMonoBehaviour<Library<string, UnlockTrackDefinition>>.Instance
                .GetLibraryElement("infernal_flame");
            if (track == null) {
                yield break;
            }
            foreach (var unlock in track.Unlocks) {
                foreach (var item in SingletonMonoBehaviour<Library<string, ItemDefinition>>.Instance
                             .GetLibraryElements(item => item.m_UnlockId == unlock.m_Id && !item.m_hideInCollection)) {
                    yield return item;
                }
            }
        }

        private static IEnumerable<BossDefinition> Bosses() {
            var library = SingletonMonoBehaviour<Library<string, BossDefinition>>.Instance;
            for (int i = 0; i < library.GetNumberOfLibraryElements(); i++) {
                var boss = library.GetLibraryElementAtIndex(i);
                if (boss.m_SelectBiomeType == BiomeType.VALLEY) {
                    yield return boss;
                }
            }
        }

        private static IEnumerable<string> FlameCard(ItemDefinition flame)
            => Core.Text.SpokenLine.NonEmptyLines(ItemDescription.GetDescription(
                flame, 0, includeRunStatModification: false, durationAmount: 0,
                canSell: false, showDiscard: false));

        private static List<ItemDefinition> CompletedFlames(BossDefinition boss, List<ItemDefinition> flames) {
            var profile = SingletonMonoBehaviour<ProfileBhv>.Instance.GetCurrentProfile();
            var completed = new List<ItemDefinition>();
            foreach (var flame in flames) {
                if (profile.GetDoesBossHaveVictoryStageCoachFlameItemId(boss.m_Id, flame.m_id)) {
                    completed.Add(flame);
                }
            }
            return completed;
        }

        private static IEnumerable<string> CompletedFlameNames(BossDefinition boss, List<ItemDefinition> flames) {
            foreach (var flame in CompletedFlames(boss, flames)) {
                yield return ItemDescription.GetTitle(flame);
            }
        }
    }
}
