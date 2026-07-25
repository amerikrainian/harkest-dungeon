using System;
using Assets.Code.Game;
using Assets.Code.Map.Minimap;
using Assets.Code.UI.Managers;
using Assets.Code.Utils;
using DD2A11y.Core.Nav;
using DD2A11y.Elements;
using DD2A11y.Game;
using DD2A11y.Input;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Screens {
    /// <summary>
    /// The road map (M while driving). The game keeps driving underneath, so this screen shares
    /// the keyboard instead of taking it: our arrows walk the map cursor (the game's arrow
    /// bindings are suppressed while it stands), WASD keeps steering the coach, and M, Z, and
    /// Escape stay the game's own. Up/Down walk the route with path retrace, Left/Right swap
    /// fork alternatives, Home returns to the wagon, End jumps to the biome's destination; the
    /// cursor's full dossier (tooltip, markers, roads out, row position) is the buffer.
    /// </summary>
    public sealed class MapScreen : GameScreen {
        private readonly Action<string, bool> _speak;
        private readonly MapViewer _viewer;
        private readonly DrivingKeySuppressor _suppressor = new DrivingKeySuppressor();

        public MapScreen(Action<string, bool> speak, Core.Audio.IAudioEngine audio) {
            _speak = speak;
            _viewer = new MapViewer(speak, audio);
        }

        public override string Name => S.ScreenMap;

        public override bool CapturesKeyboard => false;

        public override object ResolveTarget() {
            if (GameModeMgr.CurrentMode != GameModeType.DRIVING
                || !SingletonMonoBehaviour<CommonUiBhv>.HasInstance()
                || !SingletonMonoBehaviour<CommonUiBhv>.Instance.IsMinimapActive
                || !SingletonMonoBehaviour<Assets.Code.Map.MapMgrBhv>.HasInstance()) {
                return null;
            }
            return SingletonMonoBehaviour<Assets.Code.Map.MapMgrBhv>.Instance.GetMinimapMgr();
        }

        public override Container BuildRoot(object target) {
            _viewer.Reset();
            _suppressor.Reassert();
            var root = new RootContainer(ContainerShape.VerticalList);
            root.Add(new ReadoutElement(_viewer.CursorLine, detail: _viewer.DetailLines));
            return root;
        }

        public override bool OnUpdate(object target) {
            _suppressor.Reassert();
            return false;
        }

        public override bool HandleAction(string actionKey) {
            switch (actionKey) {
                case UiActions.Up: _viewer.Forward(); return true;
                case UiActions.Down: _viewer.Backward(); return true;
                case UiActions.Left: _viewer.CycleFork(-1); return true;
                case UiActions.Right: _viewer.CycleFork(+1); return true;
                case UiActions.Home: _viewer.JumpToWagon(); return true;
                case UiActions.End: _viewer.JumpToEnd(); return true;
                // Escape stays the game's: its own Back listener closes the map (our gate is
                // not holding the keyboard), and OnLeave speaks the dismissal.
                default: return false;
            }
        }

        public override void OnLeave() {
            _suppressor.Restore();
            // Free driving has no screen to re-announce behind the map, so the close is spoken
            // here.
            _speak(S.MapClosed, true);
        }
    }
}
