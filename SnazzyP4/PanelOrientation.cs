namespace SnazzyP4
{
    /// <summary>
    /// The button layouts a macro button panel can render in.
    /// </summary>
    public enum PanelOrientation
    {
        /// <summary>
        /// The panel's standard layout, with each button pair on its own row and the column headers shown.
        /// </summary>
        Standard,

        /// <summary>
        /// Every button in the panel stacked in a single column.
        /// </summary>
        Vertical,

        /// <summary>
        /// Every button in the panel laid out in a single row.
        /// </summary>
        Horizontal,
    }
}
