using Assets.Code.Kingdom.UI;
using Assets.Code.UI.Items;
using Assets.Code.UI.Screens;
using DD2A11y.Core.Nav;
using DD2A11y.Elements;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using TMPro;

namespace DD2A11y.Screens {
    /// <summary>
    /// The kingdom map's biome cell panel (a <c>ScreenKingdomMapBiomePanel</c> widget on a Map
    /// layer stack entry) - purely informational: the enemy roster under the game's own
    /// Enemies header (the biome's name is the screen name), the expedition rewards, the
    /// active upgrades and modifier, and the kill contract with its rewards when one is
    /// posted, then the close button. Escape closes it too.
    /// </summary>
    public sealed class KingdomBiomePanelScreen : GameScreen {
        private static readonly AccessTools.FieldRef<ScreenKingdomMapBiomePanel, TextMeshProUGUI> UpgradesLabelField =
            AccessTools.FieldRefAccess<ScreenKingdomMapBiomePanel, TextMeshProUGUI>("m_biomeUpgradesDescriptionLabel");
        private static readonly AccessTools.FieldRef<ScreenKingdomMapBiomePanel, TextMeshProUGUI> ContractTitleField =
            AccessTools.FieldRefAccess<ScreenKingdomMapBiomePanel, TextMeshProUGUI>("m_killContractTitleLabel");
        private static readonly AccessTools.FieldRef<ScreenKingdomMapBiomePanel, TextMeshProUGUI> ContractDescField =
            AccessTools.FieldRefAccess<ScreenKingdomMapBiomePanel, TextMeshProUGUI>("m_killContractDescLabel");

        private ScreenKingdomMapBiomePanel _panel;
        private Container _root;
        private int _builtSignature;

        public override string Name => BiomeName(_panel) ?? S.ScreenGeneric;

        // The same name the cell label shows; a boss-subtype biome has no type and stays
        // nameless on screen. On the entry frame the panel has not bound its cell yet, so the
        // game's viewed-cell query (set before the push) answers instead.
        private static string BiomeName(ScreenKingdomMapBiomePanel panel) {
            var cell = panel == null ? null : panel.SelectedCell;
            if (cell == null) {
                cell = ViewedCell<Assets.Code.Kingdom.KingdomMapCellBiome>();
            }
            if (cell == null || !cell.HasBiomeType) {
                return null;
            }
            return GameLoc.TryGet($"biome_name_{cell.BiomeType}");
        }

        // The game's own "Enemies:" header labels the roster - the biome's name is already the
        // screen name. Composed from the model with the same loc key the panel binds, so the
        // line is complete on the entry frame (the context binding lands a beat later).
        private static string EnemiesLine(ScreenKingdomMapBiomePanel panel) {
            var cell = panel.SelectedCell;
            if (cell == null) {
                cell = ViewedCell<Assets.Code.Kingdom.KingdomMapCellBiome>();
            }
            if (cell == null || !cell.HasBiomeType) {
                return null;
            }
            string enemies = GameLoc.TryGet(
                "kingdoms_map_panel_" + cell.BiomeType.ToString().ToLowerInvariant() + "_enemies");
            string header = GameLoc.TryGet("kingdom_biome_panel_enemies_header");
            if (string.IsNullOrEmpty(enemies)) {
                return header;
            }
            return string.IsNullOrEmpty(header) ? enemies : header.TrimEnd() + " " + enemies;
        }

        internal static T ViewedCell<T>() where T : Assets.Code.Kingdom.KingdomMapCellBase {
            using (var query = Assets.Code.Kingdom.Queries.QueryKingdomCellIsCurrentlyViewed.Trigger()) {
                foreach (var viewed in query.DisplayedMapCells) {
                    if (viewed is T match) {
                        return match;
                    }
                }
            }
            return null;
        }

        public override object ResolveTarget() {
            var top = StackTop.Object();
            _panel = top == null ? null : top.GetComponentInChildren<ScreenKingdomMapBiomePanel>(false);
            return _panel;
        }

        public override Container BuildRoot(object target) {
            var panel = (ScreenKingdomMapBiomePanel)target;
            var screen = panel.GetComponentInParent<UiScreenBhv>();
            _root = new RootContainer(ContainerShape.VerticalList, back: () => screen.TryCloseScreen());
            Populate(panel);
            return _root;
        }

        public override bool OnUpdate(object target) {
            var panel = (ScreenKingdomMapBiomePanel)target;
            if (Signature(panel) != _builtSignature) {
                _root.Clear();
                Populate(panel);
                return true;
            }
            return false;
        }

        private void Populate(ScreenKingdomMapBiomePanel panel) {
            _root.Add(new ReadoutElement(() => EnemiesLine(panel)));
            // The label keeps template text when unbound; the game populates it only for
            // active upgrades or a modifier, so that is the gate.
            var cell = panel.SelectedCell;
            var upgrades = UpgradesLabelField(panel);
            if (upgrades != null && cell != null
                && (cell.BiomeUpgradeInstances.Count > 0 || cell.BiomeModifier != null)) {
                _root.Add(new ReadoutElement(() => upgrades == null ? null : upgrades.text));
            }
            foreach (var reward in panel.GetComponentsInChildren<UninteractableRewardItemBhv>(includeInactive: false)) {
                var captured = reward;
                _root.Add(new ReadoutElement(
                    () => RewardItems.Title(captured),
                    value: () => RewardItems.Quantity(captured),
                    detail: () => TooltipReader.Lines(captured.gameObject)));
            }
            // Model-gated: the labels keep template text ("Name") until a contract populates.
            var title = ContractTitleField(panel);
            if (title != null && cell != null && cell.GetHasKillContract()) {
                var desc = ContractDescField(panel);
                _root.Add(new ReadoutElement(
                    () => title == null ? null : title.text,
                    detail: () => desc == null || !desc.gameObject.activeInHierarchy
                        ? (System.Collections.Generic.IEnumerable<string>)new string[0]
                        : new[] { desc.text }));
            }
            // The close button is prefab-wired (no serialized field), so a sweep finds it.
            foreach (var button in panel.GetComponentsInChildren<UnityEngine.UI.Button>(includeInactive: false)) {
                if (UiText.HasAnyTextSource(button.gameObject)) {
                    _root.Add(new SelectableElement(button));
                }
            }
            _builtSignature = Signature(panel);
        }

        private static int Signature(ScreenKingdomMapBiomePanel panel) {
            int signature = 17;
            foreach (var reward in panel.GetComponentsInChildren<UninteractableRewardItemBhv>(includeInactive: false)) {
                signature = signature * 31 + reward.GetInstanceID();
            }
            var title = ContractTitleField(panel);
            signature = signature * 31 + (title != null && title.gameObject.activeInHierarchy ? 1 : 0);
            return signature;
        }
    }
}
