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
        /// A press locks in the latest Exdeath's real/fake and the resolution shows in both set panels.
        /// </summary>
        Simple,

        /// <summary>
        /// No Exdeath debuff buttons at all.
        /// Each set panel lists every debuff's resolution with both roles' target letters, driven by the real/fake Exdeath presses alone.
        /// </summary>
        GigaSimple,
    }
}
