using System.Collections.Generic;
using Assets.Code.Data;
using Assets.Code.UI.Screens;
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
    /// The floor under every stack screen the mod has no dedicated reader for (glossary, road
    /// node panels): a generic sweep of the screen's selectables so no
    /// surface is ever dead air. Registered last, so dedicated screens always win. Only real
    /// SCREEN stack entries are taken - driving HUD widgets (minimap, goals) register on the
    /// stack too and must not capture the keyboard mid-drive.
    ///
    /// Results surfaces (end expedition, game over, Kingdoms results) share one score-row
    /// prefab; those rows read as readouts with their value ("Candles Found: 3" - a 0 is the
    /// sighted cross mark), followed by the run total the visual panel shows as a bare number.
    /// </summary>
    public sealed class GenericScreen : GameScreen {
        private static readonly AccessTools.FieldRef<ResultsScoreUIWidgetBhv, DataContextBhv> ResultsContextField =
            AccessTools.FieldRefAccess<ResultsScoreUIWidgetBhv, DataContextBhv>("m_dataContextBhv");
        private UiScreenBhv _screen;
        private Container _root;
        private int _builtSignature;
        private bool _awaitingLabel;
        private Dictionary<object, UIElement> _byWidget = new Dictionary<object, UIElement>();

        public override string Name {
            get {
                string title = UiText.FirstLabel(_screen != null ? _screen.gameObject : null);
                return string.IsNullOrEmpty(title) ? S.ScreenGeneric : title;
            }
        }

        // The game-over screen's flame-unlock toast opens the vitrine on click; the vitrine
        // key answers there too, so the results floor declares the Roster category.
        private static readonly Core.Input.InputCategory[] ResultsCategories =
            { Core.Input.InputCategory.Roster, Core.Input.InputCategory.UI };

        public override Core.Input.InputCategory[] InputCategories =>
            Assets.Code.Game.GameModeMgr.CurrentMode == Assets.Code.Game.GameModeType.RESULTS
                ? ResultsCategories : base.InputCategories;

        public override object ResolveTarget() {
            if (!SingletonMonoBehaviour<ScreenStackBhv>.HasInstance() || StackTop.Veiled) {
                return null;
            }
            var top = SingletonMonoBehaviour<ScreenStackBhv>.Instance.GetTopMostScreenInstance();
            if (top == null || top.m_screenType != ScreenStackBhv.ScreenOrderType.SCREEN || top.m_screenObj == null) {
                return null;
            }
            _screen = top.m_screenObj.GetComponent<UiScreenBhv>();
            return _screen;
        }

        public override Container BuildRoot(object target) {
            var screen = (UiScreenBhv)target;
            // A hub's sub-screen panel must close through its own flow - the hub re-enables
            // its own controls there (the altar re-enables its region markers); a raw stack
            // pop leaves the hub behind it dead.
            _root = new RootContainer(ContainerShape.VerticalList, back: () => {
                if (screen is SubScreenElementBhv panel) {
                    panel.CloseSubscreen();
                } else {
                    screen.TryCloseScreen();
                }
            });
            _byWidget.Clear();
            Populate(screen);
            return _root;
        }

        public override bool OnUpdate(object target) {
            var screen = (UiScreenBhv)target;
            if (Signature(screen) != _builtSignature) {
                _root.Clear();
                Populate(screen);
            }
            return PauseScreen.LabelArrived(_root, ref _awaitingLabel);
        }

        // The results screens animate their score rows in one at a time; every arrival
        // re-populates. Elements are keyed to their live widget and reused across rebuilds, so
        // the focused element survives and the row stream stays silent (the landing already
        // announced; the grown list reads on demand). A recycled widget gets a fresh element,
        // orphaning focus - the router then re-lands and announces, the correct read for a
        // genuinely replaced surface.
        private void Populate(UiScreenBhv screen) {
            var previous = _byWidget;
            _byWidget = new Dictionary<object, UIElement>();
            var elements = new List<UIElement>();
            int lastScoreRow = -1;
            foreach (var selectable in screen.GetComponentsInChildren<Selectable>(includeInactive: false)) {
                if (!Include(selectable)) {
                    continue;
                }
                var score = selectable.GetComponent<GameOverScoreLabelBhv>();
                if (!previous.TryGetValue(selectable, out var element)) {
                    element = score != null ? ScoreRow(score) : new SelectableElement(selectable);
                }
                _byWidget[selectable] = element;
                elements.Add(element);
                if (score != null) {
                    lastScoreRow = elements.Count - 1;
                }
            }
            var results = screen.GetComponentInChildren<ResultsScoreUIWidgetBhv>(includeInactive: false);
            if (results != null) {
                if (!previous.TryGetValue(results, out var total)) {
                    total = TotalRow(results);
                }
                _byWidget[results] = total;
                elements.Insert(lastScoreRow + 1, total);
            }
            foreach (var element in elements) {
                _root.Add(element);
            }
            _builtSignature = Signature(screen);
            var first = _root.FirstFocusable();
            _awaitingLabel = first != null && string.IsNullOrEmpty(first.Label);
        }

        // A score row composes like the sighted row: the game's reason label, then its number
        // ("Candles Found: 3"); the rows that did not score hold 0, the cross mark's meaning.
        // The row's explanation tooltip is the buffer.
        internal static UIElement ScoreRow(GameOverScoreLabelBhv row) {
            return new ReadoutElement(
                () => {
                    var context = row == null ? null : row.GetComponent<DataContextBhv>();
                    if (context == null) {
                        return null;
                    }
                    string reason = context.GetStringValue("score_reason");
                    if (string.IsNullOrEmpty(reason)) {
                        return null;
                    }
                    string value = context.GetStringValue("score_value");
                    return string.IsNullOrEmpty(value) ? reason : reason.TrimEnd() + " " + value;
                },
                detail: () => row == null ? (IEnumerable<string>)new string[0] : TooltipReader.Lines(row.gameObject));
        }

        // The run total, which the visual panel shows as a bare number beside a candle icon
        // (no game caption string exists for it). The game-over flow stores an already
        // composed line in the same binding; only a bare number gets the authored caption.
        private static UIElement TotalRow(ResultsScoreUIWidgetBhv results) {
            return new ReadoutElement(() => {
                var context = results == null ? null : ResultsContextField(results);
                string total = context == null ? null : context.GetStringValue("total_score");
                if (string.IsNullOrEmpty(total)) {
                    return null;
                }
                return IsDigits(total) ? S.ResultsTotal(total) : total;
            });
        }

        private static bool IsDigits(string text) {
            foreach (char c in text) {
                if (c < '0' || c > '9') {
                    return false;
                }
            }
            return true;
        }

        // An instance-id signature, not a count: pooled widgets recycle into the same number of
        // brand-new instances, which a count reads as unchanged while every held reference dies.
        private static int Signature(UiScreenBhv screen) {
            int signature = 17;
            foreach (var selectable in screen.GetComponentsInChildren<Selectable>(includeInactive: false)) {
                if (Include(selectable)) {
                    signature = signature * 31 + selectable.GetInstanceID();
                }
            }
            return signature;
        }

        private static bool Include(Selectable selectable) {
            if (selectable is Scrollbar || selectable.GetComponent<SelectOnEmptyFallbackBhv>() != null) {
                return false;
            }
            return UiText.HasAnyTextSource(selectable.gameObject);
        }

    }
}
