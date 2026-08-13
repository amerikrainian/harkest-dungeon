using System.Collections.Generic;
using Assets.Code.AltarOfHope;
using Assets.Code.Game;
using Assets.Code.Profile;
using Assets.Code.UI;
using Assets.Code.UI.Managers;
using Assets.Code.UI.Widgets;
using Assets.Code.Utils;
using DD2A11y.Core.Nav;
using DD2A11y.Elements;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using UnityEngine.UI;

namespace DD2A11y.Screens {
    /// <summary>
    /// The Altar of Hope hub (the ALTAR_OF_HOPE game mode), one flat list: the candle
    /// balance, the six region markers of the altar map - each named by the game's own region
    /// string, locked ones reading "unavailable" (the intro restricts the map to one region) -
    /// then The Recollection (the collection gallery, which has no region marker: the sighted
    /// path is the panel tab bar), then Embark. Enter on a region runs the game's own submit,
    /// which opens the region's sub-screen (read by its dedicated screen or the generic
    /// floor); Embark drives the game's own exit flow, including its spend-your-candles-first
    /// reminder dialog. Escape opens the pause menu.
    /// </summary>
    public sealed class AltarScreen : GameScreen {
        private static readonly AccessTools.FieldRef<AltarOfHopeUiBhv, SubScreenCollectionBhv> CollectionField =
            AccessTools.FieldRefAccess<AltarOfHopeUiBhv, SubScreenCollectionBhv>("m_altarSubScreenCollectionBhv");
        private static readonly AccessTools.FieldRef<SubScreenCollectionBhv, List<ISubscreenContent>> SpawnedElementsField =
            AccessTools.FieldRefAccess<SubScreenCollectionBhv, List<ISubscreenContent>>("m_SpawnedElements");

        private AltarOfHopeUiBhv _hub;
        private Container _root;

        public override string Name => GameLoc.TryGet("altar_main_title") ?? S.ScreenAltar;

        public override object ResolveTarget() {
            if (GameModeMgr.CurrentMode != GameModeType.ALTAR_OF_HOPE
                || Singleton<GameModeMgr>.Instance.IsChangingState()) {
                _hub = null;
                return null;
            }
            if (_hub == null) {
                _hub = UnityEngine.Object.FindObjectOfType<AltarOfHopeUiBhv>();
            }
            return _hub != null && _hub.gameObject.activeInHierarchy ? _hub : null;
        }

        public override Container BuildRoot(object target) {
            var hub = (AltarOfHopeUiBhv)target;
            _root = new RootContainer(ContainerShape.VerticalList,
                back: () => SingletonMonoBehaviour<CommonUiBhv>.Instance.TogglePauseMenu());

            _root.Add(CandleBalance());

            var regions = new List<AltarRegionTag>(
                UnityEngine.Object.FindObjectsOfType<AltarRegionTag>());
            regions.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
            foreach (var region in regions) {
                var selectable = region.GetComponent<Selectable>();
                if (selectable != null) {
                    _root.Add(new AltarRegionElement(region, selectable));
                }
            }

            // The Recollection: the collection gallery is a bar-only panel, opened through
            // the game's own toggle - the same call the bar button's click lands in. Hidden
            // during the intro altar, which hides its bar button too.
            var collection = CollectionField(hub);
            AltarCollectionSubscreenBhv gallery = null;
            foreach (var element in SpawnedElementsField(collection)) {
                gallery = element as AltarCollectionSubscreenBhv;
                if (gallery != null) {
                    break;
                }
            }
            if (gallery != null && !(SingletonMonoBehaviour<AltarOfHopeBhv>.HasInstance()
                    && SingletonMonoBehaviour<AltarOfHopeBhv>.Instance.IsIntro)) {
                var captured = gallery;
                _root.Add(new ActionElement(() => captured.GetScreenName(), S.RoleButton, () => {
                    if (!collection.HasAnySubScreensOpen()) {
                        collection.ToggleSubScreenElement(captured);
                    }
                },
                    // The bar button's notification dot: unviewed items wait inside.
                    value: () => captured.GetShouldShowNotificationIcon() ? S.TutorialNew : null));
            }

            _root.Add(new ActionElement(() => GameLoc.TryGet("embark_continue_label"),
                S.RoleButton, hub.OnEmbark));
            return _root;
        }

        /// <summary>The profile's candle balance, captioned by the game's own name for the
        /// currency; shared with the altar sub-screens where the balance drains.</summary>
        internal static ReadoutElement CandleBalance() {
            return new ReadoutElement(
                () => GameLoc.TryGet("item_name_candles"),
                () => ((int)SingletonMonoBehaviour<ProfileBhv>.Instance.GetCurrentProfile()
                    .ProfileValues.GetValue(ProfileValueType.CANDLES)).ToString());
        }
    }
}
