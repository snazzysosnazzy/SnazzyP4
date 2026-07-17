namespace SnazzyP4
{
    /// <summary>
    /// The gameplay modes the solver can run in.
    /// The mode decides which macro buttons exist, how the set panels resolve and which slash commands fire.
    /// </summary>
    public enum SolverMode
    {
        /// <summary>
        /// The full solver with a short and a long button for each Exdeath debuff.
        /// </summary>
        Classic,

        /// <summary>
        /// One button per Exdeath debuff with no short/long split.
        /// A press locks in the latest Exdeath's real/fake and the resolutions show in their own debuff panel.
        /// </summary>
        Simple,
    }
}
