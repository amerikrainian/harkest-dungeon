using Assets.Code.UI.Screens;
using Assets.Code.Utils;
using UnityEngine;

namespace DD2A11y.Screens {
    /// <summary>The game object of the topmost screen on the game's screen stack, or null.</summary>
    internal static class StackTop {
        public static GameObject Object() {
            if (!SingletonMonoBehaviour<ScreenStackBhv>.HasInstance()) {
                return null;
            }
            var item = SingletonMonoBehaviour<ScreenStackBhv>.Instance.GetTopMostScreenInstance();
            return item?.m_screenObj;
        }
    }
}
