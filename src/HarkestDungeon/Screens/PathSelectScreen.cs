using System;
using System.Collections.Generic;
using Assets.Code.Campaign;
using Assets.Code.Game;
using Assets.Code.UI;
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
    /// The crossroads path-select overlay (the "Change Path" seal): a canvas panel on the
    /// hero-select scene, not a stack SCREEN, so it is matched off the game's own panel flag
    /// and registered above the crossroads. One row per path the hero can take; Enter previews
    /// a path (the game's own <c>SelectPath</c>, which only drives the comparison panel and
    /// enables the confirm button), the comparison readout carries the previewed path's full
    /// card in its buffer, and the confirm button commits. Escape closes through the game's own
    /// toggle.
    /// </summary>
    public sealed class PathSelectScreen : GameScreen {
        private static readonly AccessTools.FieldRef<HeroSelectBhv, bool> PanelOpenField =
            AccessTools.FieldRefAccess<HeroSelectBhv, bool>("m_pathPanelOpen");
        private static readonly AccessTools.FieldRef<HeroSelectBhv, Dictionary<GameObject, string>> PathsField =
            AccessTools.FieldRefAccess<HeroSelectBhv, Dictionary<GameObject, string>>("m_pathsAdded");
        private static readonly AccessTools.FieldRef<HeroSelectBhv, Button> ConfirmField =
            AccessTools.FieldRefAccess<HeroSelectBhv, Button>("m_pathConfirmButton");
        private static readonly AccessTools.FieldRef<HeroSelectBhv, ActorPathComparisonBhv> ComparisonField =
            AccessTools.FieldRefAccess<HeroSelectBhv, ActorPathComparisonBhv>("m_pathComparisonBhv");
        private static readonly AccessTools.FieldRef<HeroSelectBhv, UnityEngine.Playables.PlayableDirector> PanelField =
            AccessTools.FieldRefAccess<HeroSelectBhv, UnityEngine.Playables.PlayableDirector>("m_pathSelectionPanelDirector");
        private static readonly AccessTools.FieldRef<HeroSelectBhv, uint> SpawnedGuidField =
            AccessTools.FieldRefAccess<HeroSelectBhv, uint>("m_SpawnedActorGuid");

        private readonly Audio.RankToneLadder _ladder;
        private HeroSelectBhv _heroSelect;
        private Container _root;
        private int _builtSignature;
        // The seal rows by element, for the focus-landing ladder; the seal's path is read
        // live off its widget (the pool re-purposes seals across heroes).
        private readonly Dictionary<UIElement, GameObject> _seals =
            new Dictionary<UIElement, GameObject>();

        public PathSelectScreen(Core.Audio.IAudioEngine audio) {
            _ladder = new Audio.RankToneLadder(audio);
        }

        public override string Name {
            get {
                var panel = _heroSelect == null ? null : PanelField(_heroSelect);
                string title = UiText.FirstLabel(panel == null ? null : panel.gameObject);
                return string.IsNullOrEmpty(title) ? S.ScreenPathSelect : title;
            }
        }

        public override object ResolveTarget() {
            if (GameModeMgr.CurrentMode != GameModeType.HERO_SELECT
                || Singleton<GameModeMgr>.Instance.IsChangingState()) {
                _heroSelect = null;
                return null;
            }
            if (_heroSelect == null) {
                _heroSelect = UnityEngine.Object.FindObjectOfType<HeroSelectBhv>();
            }
            return _heroSelect != null && PanelOpenField(_heroSelect) ? _heroSelect : null;
        }

        public override Container BuildRoot(object target) {
            var heroSelect = (HeroSelectBhv)target;
            _root = new RootContainer(ContainerShape.VerticalList,
                back: () => heroSelect.TogglePathSelectionPanel());
            _ladder.Clear();
            Populate(heroSelect);
            return _root;
        }

        /// <summary>Focus-landing audio: a seal landing plays the full rank-and-target ladder
        /// for the hero AS THAT PATH would re-skill them (the comparison panel's what-if pips,
        /// without needing the seal previewed first - like the seal's card). Any other
        /// landing cancels a pending ladder.</summary>
        public void OnFocusSettled(UIElement element) {
            var sealObject = element != null && _seals.TryGetValue(element, out var held) ? held : null;
            var select = sealObject == null ? null : sealObject.GetComponent<ActorPathSelectBhv>();
            var actor = select == null ? null : Actors.Get(SpawnedGuidField(_heroSelect));
            if (actor == null) {
                _ladder.Clear();
                return;
            }
            _ladder.ScheduleLadder(RankCoverage.LaunchCounts(actor, select.PathId),
                RankCoverage.TargetCounts(actor, select.PathId), RankCoverage.EquipLimit(actor));
        }

        public override bool OnUpdate(object target) {
            _ladder.Tick();
            var heroSelect = (HeroSelectBhv)target;
            // The panel spawns its path rows a beat after opening, and repopulates them from
            // a pool on every hero switch - the same seal objects come back carrying another
            // hero's paths, so the signature keys on the ids too, never just the count.
            if (PathsSignature(heroSelect) != _builtSignature) {
                _root.Clear();
                Populate(heroSelect);
            }
            return false;
        }

        private static int PathsSignature(HeroSelectBhv heroSelect) {
            var paths = PathsField(heroSelect);
            int signature = 17;
            if (paths != null) {
                foreach (var entry in paths) {
                    signature = signature * 31 + (entry.Key == null ? 0 : entry.Key.GetInstanceID());
                    signature = signature * 31 + (entry.Value == null ? 0 : entry.Value.GetHashCode());
                }
            }
            return signature;
        }

        private void Populate(HeroSelectBhv heroSelect) {
            _builtSignature = PathsSignature(heroSelect);
            _seals.Clear();
            var paths = PathsField(heroSelect);
            if (paths != null) {
                var row = new Container(ContainerShape.HorizontalList);
                foreach (var entry in paths) {
                    var pathObject = entry.Key;
                    if (pathObject == null || !pathObject.activeInHierarchy) {
                        continue;
                    }
                    // Preview only: the game's SelectPath drives the comparison panel and arms
                    // the confirm button; the path itself changes on confirm. The buffer is
                    // this seal's own card, read through the seal widget live (the pool
                    // re-purposes seals across heroes) - so any path reads without previewing
                    // it first; the comparison panel shows only the previewed path, and
                    // reading it under an unpreviewed seal spoke the wrong one.
                    var seal = new ActionElement(
                        () => UiText.FirstLabel(pathObject), S.RoleButton,
                        () => heroSelect.SelectPath(pathObject),
                        extraBufferLines: () => SealCard(heroSelect, pathObject));
                    _seals[seal] = pathObject;
                    row.Add(seal);
                }
                if (!row.IsEmptyContainer) {
                    _root.Add(row);
                }
            }

            // The comparison card for the previewed path (name, flavour, rank/target, effects),
            // read live: its detail is the buffer, so the row itself stays terse.
            var comparison = ComparisonField(heroSelect);
            if (comparison != null) {
                _root.Add(new ReadoutElement(
                    () => S.PathDetails,
                    detail: () => ComparisonLines(heroSelect)));
            }

            var confirm = ConfirmField(heroSelect);
            if (confirm != null && confirm.gameObject.activeInHierarchy) {
                _root.Add(new SelectableElement(confirm));
            }
        }

        private static IEnumerable<string> ComparisonLines(HeroSelectBhv heroSelect)
            => PathComparison.Lines(ComparisonField(heroSelect));

        private static IEnumerable<string> SealCard(HeroSelectBhv heroSelect, GameObject sealObject) {
            var select = sealObject == null ? null : sealObject.GetComponent<ActorPathSelectBhv>();
            return select == null
                ? System.Linq.Enumerable.Empty<string>()
                : PathComparison.Card(select.PathId, SpawnedGuidField(heroSelect));
        }
    }
}
