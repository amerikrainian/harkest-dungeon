using System.Collections.Generic;
using Assets.Code.UI;
using Assets.Code.UI.Items;
using DD2A11y.Core.Nav;
using DD2A11y.Elements;
using DD2A11y.Game;
using HarmonyLib;
using S = DD2A11y.Core.Strings.Strings;

namespace DD2A11y.Screens {
    /// <summary>
    /// The altar's collection gallery (<c>AltarCollectionSubscreenBhv</c> - "The
    /// Recollection", the bar-only panel the hub lists after the regions): a filter tab
    /// selector first (All Items, Combat Items, Trinkets, ... - Left/Right switch through
    /// the game's own filter press, rebuilding the list below), then every collected item as
    /// a browse-only row with its tooltip in the buffer. The game lists the items one per
    /// frame, newest-to-view first, so the tree follows the live set as it fills. Escape
    /// closes through the panel's own flow (a raw stack pop would leave the altar's region
    /// markers disabled).
    /// </summary>
    public sealed class AltarCollectionScreen : GameScreen {
        private static readonly AccessTools.FieldRef<AltarCollectionSubscreenBhv, InventoryFilterBhv[]> FiltersField =
            AccessTools.FieldRefAccess<AltarCollectionSubscreenBhv, InventoryFilterBhv[]>("m_filters");
        private static readonly AccessTools.FieldRef<AltarCollectionSubscreenBhv, int> FilterIndexField =
            AccessTools.FieldRefAccess<AltarCollectionSubscreenBhv, int>("m_filterSelectorIndex");

        private AltarCollectionSubscreenBhv _panel;
        private Container _root;
        private Container _items;
        private int _builtSignature;
        private Dictionary<UninteractableRewardItemBhv, AltarCollectionItemElement> _elements =
            new Dictionary<UninteractableRewardItemBhv, AltarCollectionItemElement>();

        public override string Name {
            get {
                string title = UiText.ChildLabel(_panel != null ? _panel.gameObject : null, "exit_anchor");
                return string.IsNullOrEmpty(title) ? S.ScreenGeneric : title;
            }
        }

        public override object ResolveTarget() {
            var top = StackTop.Object();
            _panel = top == null ? null : top.GetComponent<AltarCollectionSubscreenBhv>();
            return _panel;
        }

        public override Container BuildRoot(object target) {
            var panel = (AltarCollectionSubscreenBhv)target;
            _root = new RootContainer(ContainerShape.VerticalList, back: panel.CloseSubscreen);
            _root.Add(new TabSelectorElement(
                () => FilterIndexField(panel),
                () => FiltersField(panel).Length,
                index => {
                    var filters = FiltersField(panel);
                    return index >= 0 && index < filters.Length
                        ? GameLoc.TryGet(filters[index].GetTitleLocKey()) : null;
                },
                index => {
                    var filters = FiltersField(panel);
                    if (index >= 0 && index < filters.Length) {
                        panel.OnInventoryFilterPressed(filters[index]);
                    }
                }));
            _items = new Container(ContainerShape.VerticalList);
            _root.Add(_items);
            _elements.Clear();
            Populate(panel);
            return _root;
        }

        public override bool OnUpdate(object target) {
            var panel = (AltarCollectionSubscreenBhv)target;
            if (Signature(panel) != _builtSignature) {
                Populate(panel);
            }
            return false;
        }

        // Elements are keyed to their live widget and reused, so focus survives the game's
        // one-item-per-frame listing and only the new rows append below it.
        private void Populate(AltarCollectionSubscreenBhv panel) {
            var previous = _elements;
            _elements = new Dictionary<UninteractableRewardItemBhv, AltarCollectionItemElement>();
            _items.Clear();
            foreach (var item in panel.GetComponentsInChildren<UninteractableRewardItemBhv>(includeInactive: false)) {
                if (!previous.TryGetValue(item, out var element)) {
                    element = new AltarCollectionItemElement(item);
                }
                _elements[item] = element;
                _items.Add(element);
            }
            _builtSignature = Signature(panel);
        }

        // An instance-id signature, not a count: a filter switch recycles every widget into
        // brand-new instances, which a count can read as unchanged while every reference dies.
        private static int Signature(AltarCollectionSubscreenBhv panel) {
            int signature = 17;
            foreach (var item in panel.GetComponentsInChildren<UninteractableRewardItemBhv>(includeInactive: false)) {
                signature = signature * 31 + item.GetInstanceID();
            }
            return signature;
        }
    }
}
