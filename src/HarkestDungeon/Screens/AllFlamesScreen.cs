using Assets.Code.UI.Managers;
using Assets.Code.UI.Screens;
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
    /// The all-flames celebration the game shows on the game-over screen once every confession
    /// boss has been beaten with every Infernal Flame (a Modal-layer prefab that is one
    /// click-anywhere button under a title and a description, with no widget class of its
    /// own - known by the prefab the game pushes). Reads as a dialog: the title and the body
    /// as the one element, Enter and Escape the button's own dismiss.
    /// </summary>
    public sealed class AllFlamesScreen : GameScreen {
        private static readonly System.Reflection.FieldInfo PrefabField =
            AccessTools.Field(typeof(CommonUiBhv), "m_allTorchCompletionPresentationPrefab");

        private GameObject _screen;

        public override string Name => S.ScreenDialog;

        public override object ResolveTarget() {
            var top = StackTop.Object();
            if (top == null || PrefabField == null || !SingletonMonoBehaviour<CommonUiBhv>.HasInstance()) {
                return _screen = null;
            }
            var prefab = PrefabField.GetValue(SingletonMonoBehaviour<CommonUiBhv>.Instance) as GameObject;
            _screen = prefab != null && top.name == prefab.name + "(Clone)" ? top : null;
            return _screen;
        }

        public override Container BuildRoot(object target) {
            var screen = (GameObject)target;
            var button = screen.GetComponent<Button>();
            System.Action dismiss;
            if (button != null) {
                dismiss = () => button.onClick.Invoke();
            } else {
                dismiss = screen.GetComponent<UiScreenBhv>().TryCloseScreen;
            }
            var root = new RootContainer(ContainerShape.VerticalList, back: dismiss);
            root.Add(new AltarRevealElement(dismiss,
                () => UiText.ChildLabel(screen, "Title"),
                () => UiText.ChildLabel(screen, "Label")));
            return root;
        }
    }
}
