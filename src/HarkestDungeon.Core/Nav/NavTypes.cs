namespace DD2A11y.Core.Nav {
    /// <summary>A focus-move direction (arrow keys).</summary>
    public enum NavDirection { Up, Down, Left, Right }

    /// <summary>
    /// Container shape - how a navigator traverses it.
    /// VerticalList/HorizontalList: arrows move among items; the whole container is one Tab-stop.
    /// Panel: Tab/Shift-Tab traverse its focusable descendants; arrows do nothing.
    /// Table/Grid/Tree exist in the reference design and will be added when a screen needs them.
    /// </summary>
    public enum ContainerShape { VerticalList, HorizontalList, Panel }
}
