using Assets.Code.Game;
using Assets.Code.UI;
using Assets.Code.UI.Screens;
using Assets.Code.Utils;
using UnityEngine;

namespace DD2A11y.Screens {
    /// <summary>The game object of the topmost screen on the game's screen stack, or null.
    /// Null while <see cref="Veiled"/>: screens pushed under a transition exist before any
    /// player has seen them (the inn's inventory panel arrives mid-assembly), and a screen
    /// nobody saw must not read. Modals resolve through <see cref="Raw"/> so a dialog
    /// interrupting a transition still speaks.</summary>
    internal static class StackTop {
        /// <summary>The transition veil: a game-mode change or a screen-fader wipe in
        /// progress - the frames where the player sees the curtain, not the screens under
        /// it. The first boot frames, before the mode manager exists, are veiled too.</summary>
        public static bool Veiled
            => !Singleton<GameModeMgr>.HasInstance()
               || Singleton<GameModeMgr>.Instance.IsChangingState()
               || (SingletonMonoBehaviour<ScreenFaderBhv>.HasInstance()
                   && !SingletonMonoBehaviour<ScreenFaderBhv>.Instance.IsClear);

        public static GameObject Object() => Veiled ? null : Raw();

        public static GameObject Raw() {
            if (!SingletonMonoBehaviour<ScreenStackBhv>.HasInstance()) {
                return null;
            }
            var item = SingletonMonoBehaviour<ScreenStackBhv>.Instance.GetTopMostScreenInstance();
            return item?.m_screenObj;
        }
    }
}
