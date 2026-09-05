using System.Collections.Generic;
using Assets.Code.CommonLogic.Pooling;
using Assets.Code.UI.Mods;
using Assets.Code.UI.Screens;
using Assets.Code.UI.Widgets;
using DD2A11y.Core.Nav;
using DD2A11y.Elements;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DD2A11y.Screens {
    /// <summary>
    /// Import Save Data (the mods side of the title menu): copies save profiles into mod
    /// profiles. Reads the game's own headers with their lists beneath - each save profile as a
    /// toggle named for the profile ("Darkest, toggle, off"; Enter marks it for the copy), each
    /// mod profile as its name with its icon-only rename and delete buttons as their own
    /// authored-label controls (rename runs the row's own inline edit with keystroke echo,
    /// delete the widget's own removal, which refuses the last profile) - then Copy and Close.
    /// Escape closes through the screen's own teardown. The floor read only the Copy button.
    /// </summary>
    public sealed class ImportSaveScreen : GameScreen {
        private static readonly AccessTools.FieldRef<ModImportSaveWidgetBhv, GameObjectPoolBhv> SavePoolField =
            AccessTools.FieldRefAccess<ModImportSaveWidgetBhv, GameObjectPoolBhv>("m_saveProfilePool");
        private static readonly AccessTools.FieldRef<ModImportSaveWidgetBhv, GameObjectPoolBhv> ModPoolField =
            AccessTools.FieldRefAccess<ModImportSaveWidgetBhv, GameObjectPoolBhv>("m_modProfilePool");
        private static readonly AccessTools.FieldRef<ModImportSaveWidgetBhv, List<GameObject>> ActiveModsField =
            AccessTools.FieldRefAccess<ModImportSaveWidgetBhv, List<GameObject>>("m_activeModObjects");

        private readonly System.Action<string, bool> _speak;
        private ModImportSaveWidgetBhv _widget;
        private Container _root;
        private Core.Text.TypingEcho _echo;
        private ImportModProfileBhv _editing;
        private int _builtSignature;

        public ImportSaveScreen(System.Action<string, bool> speak) {
            _speak = speak;
        }

        public override string Name {
            get {
                string title = _widget == null ? null : UiText.ChildLabel(_widget.gameObject, "Title");
                return string.IsNullOrEmpty(title) ? S.ScreenGeneric : title;
            }
        }

        public override object ResolveTarget() {
            var top = StackTop.Object();
            _widget = top == null ? null : top.GetComponentInChildren<ModImportSaveWidgetBhv>(includeInactive: false);
            return _widget;
        }

        public override Container BuildRoot(object target) {
            var widget = (ModImportSaveWidgetBhv)target;
            var screen = widget.GetComponentInParent<UiScreenBhv>();
            _root = new RootContainer(ContainerShape.VerticalList, back: screen.TryCloseScreen);
            _echo = new Core.Text.TypingEcho(() => _widget != null && _widget.IsInputtingText(), () => {
                var field = _editing == null ? null : _editing.GetComponentInChildren<TMP_InputField>(includeInactive: false);
                return field == null ? "" : field.text;
            }, _speak);
            Populate(widget);
            return _root;
        }

        // Both lists are pooled: a copy or a delete recycles their rows into new instances.
        public override bool OnUpdate(object target) {
            var widget = (ModImportSaveWidgetBhv)target;
            if (_echo.Tick() && _editing != null) {
                _speak(UiText.ChildLabel(_editing.gameObject, "ModName"), true);
                _editing = null;
            }
            if (Signature(widget) != _builtSignature) {
                _root.Clear();
                Populate(widget);
            }
            return false;
        }

        private void Populate(ModImportSaveWidgetBhv widget) {
            _root.Add(new StaticTextElement(() => UiText.ChildLabel(widget.gameObject, "Header")));
            foreach (var save in Rows<ImportSaveProfileBhv>(SavePoolField(widget))) {
                var captured = save;
                var toggle = save.GetComponentInChildren<Toggle>(includeInactive: false);
                if (toggle != null) {
                    _root.Add(new SelectableElement(toggle, () => captured == null ? null : UiText.ChildLabel(captured.gameObject, "ModName")));
                }
            }
            _root.Add(new StaticTextElement(() => UiText.ChildLabel(widget.gameObject, "Header (1)")));
            foreach (var mod in Rows<ImportModProfileBhv>(ModPoolField(widget))) {
                var captured = mod;
                _root.Add(new ReadoutElement(() => captured == null ? null : UiText.ChildLabel(captured.gameObject, "ModName")));
                _root.Add(new ActionElement(() => S.LoadoutRename, S.RoleButton, () => {
                    _editing = captured;
                    captured.OnEditNameButtonPressed();
                }));
                _root.Add(new ActionElement(() => S.LoadoutDelete, S.RoleButton, () => {
                    var active = ActiveModsField(widget);
                    if (active != null && active.Count > 1) {
                        widget.OnDeleteProfilePressed(captured);
                    } else {
                        Core.Speech.SpeechPipeline.Instance?.Speak(S.StatusUnavailable);
                    }
                }));
            }
            foreach (var button in widget.GetComponentsInChildren<Button>(includeInactive: false)) {
                if (button.GetComponentInParent<ImportModProfileBhv>() != null
                    || button.GetComponentInParent<ImportSaveProfileBhv>() != null) {
                    continue;
                }
                if (UiText.HasAnyTextSource(button.gameObject)) {
                    _root.Add(new SelectableElement(button));
                } else {
                    // The icon-only close button, named for what it does.
                    var captured = button;
                    _root.Add(new SelectableElement(captured, () => GameLoc.TryGet("loot_screen_close")));
                }
            }
            _builtSignature = Signature(widget);
        }

        private static IEnumerable<T> Rows<T>(GameObjectPoolBhv pool) where T : Component {
            if (pool == null) {
                yield break;
            }
            foreach (var row in pool.GetComponentsInChildren<T>(includeInactive: false)) {
                yield return row;
            }
        }

        private static int Signature(ModImportSaveWidgetBhv widget) {
            int signature = 17;
            foreach (var row in Rows<ImportSaveProfileBhv>(SavePoolField(widget))) {
                signature = signature * 31 + row.GetInstanceID();
            }
            foreach (var row in Rows<ImportModProfileBhv>(ModPoolField(widget))) {
                signature = signature * 31 + row.GetInstanceID();
            }
            return signature;
        }
    }
}
