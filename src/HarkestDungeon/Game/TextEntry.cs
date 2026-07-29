using Assets.Code.UI.Managers;
using Assets.Code.Utils;

namespace DD2A11y.Game {
    /// <summary>The game's own text-entry state. While a field is being typed into, the mod's UI
    /// keys pause so every keystroke reaches the field (the game's shortcut layer pauses the same
    /// way); Enter and Escape end the edit through the field itself.</summary>
    public static class TextEntry {
        public static bool IsTyping =>
            SingletonMonoBehaviour<CommonUiBhv>.HasInstance()
            && SingletonMonoBehaviour<CommonUiBhv>.Instance.IsInputtingText;
    }
}
