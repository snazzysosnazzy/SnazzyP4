using System.Collections.Generic;

namespace SnazzyP4;

/// <summary>
/// The registry of customisable display text.
/// Each entry pairs an id with its default value, the label shown in settings and the group it belongs to, so the solver, the configuration and the settings window all share one source of truth.
/// </summary>
public static class TextLabels
{
    /// <summary>The First Set panel label.</summary>
    public const string FirstSetLabel = "FirstSetLabel";

    /// <summary>The Second Set panel label.</summary>
    public const string SecondSetLabel = "SecondSetLabel";

    /// <summary>The Kefka text panel label.</summary>
    public const string KefkaLabel = "KefkaLabel";

    /// <summary>The Last Fake header shown beside the Kefka lines.</summary>
    public const string LastFakeHeader = "LastFakeHeader";

    /// <summary>The prefix used before the spread target letter.</summary>
    public const string SpreadPrefix = "SpreadPrefix";

    /// <summary>The prefix used before the stack target letter.</summary>
    public const string StackPrefix = "StackPrefix";

    /// <summary>The joiner placed between the body and the Acceleration word when they share a line.</summary>
    public const string AndJoiner = "AndJoiner";

    /// <summary>The Acceleration callout when standing still.</summary>
    public const string StandStill = "StandStill";

    /// <summary>The Acceleration callout when moving.</summary>
    public const string Move = "Move";

    /// <summary>The real gaze callout.</summary>
    public const string GazeReal = "GazeReal";

    /// <summary>The fake gaze callout.</summary>
    public const string GazeFake = "GazeFake";

    /// <summary>The real Inferno callout.</summary>
    public const string InfernoReal = "InfernoReal";

    /// <summary>The fake Inferno callout.</summary>
    public const string InfernoFake = "InfernoFake";

    /// <summary>The real Tsunami callout.</summary>
    public const string TsunamiReal = "TsunamiReal";

    /// <summary>The fake Tsunami callout.</summary>
    public const string TsunamiFake = "TsunamiFake";

    /// <summary>The Thunder name used in the Kefka text and toggles.</summary>
    public const string ThunderName = "ThunderName";

    /// <summary>The Blizzard name used in the Kefka text and toggles.</summary>
    public const string BlizzardName = "BlizzardName";

    /// <summary>The word used for a real Kefka line.</summary>
    public const string RealWord = "RealWord";

    /// <summary>The word used for a fake Kefka line.</summary>
    public const string FakeWord = "FakeWord";

    /// <summary>The first Exdeath section header.</summary>
    public const string ExdeathFirstHeader = "ExdeathFirstHeader";

    /// <summary>The second Exdeath section header.</summary>
    public const string ExdeathSecondHeader = "ExdeathSecondHeader";

    /// <summary>The Exdeath real column header.</summary>
    public const string RealColumnHeader = "RealColumnHeader";

    /// <summary>The Exdeath fake column header.</summary>
    public const string FakeColumnHeader = "FakeColumnHeader";

    /// <summary>The short column header.</summary>
    public const string ShortColumnHeader = "ShortColumnHeader";

    /// <summary>The long column header.</summary>
    public const string LongColumnHeader = "LongColumnHeader";

    /// <summary>The Chaos section header.</summary>
    public const string ChaosHeader = "ChaosHeader";

    /// <summary>The Kefka buttons section header.</summary>
    public const string KefkaButtonsHeader = "KefkaButtonsHeader";

    /// <summary>The Reset button text.</summary>
    public const string ResetButton = "ResetButton";

    /// <summary>The Undo button text.</summary>
    public const string UndoButton = "UndoButton";

    /// <summary>The Hide button text.</summary>
    public const string HideButton = "HideButton";

    /// <summary>The Show button text.</summary>
    public const string ShowButton = "ShowButton";

    /// <summary>The Support spread target letter.</summary>
    public const string SpreadLetterSupport = "SpreadLetterSupport";

    /// <summary>The Support stack target letter.</summary>
    public const string StackLetterSupport = "StackLetterSupport";

    /// <summary>The DPS spread target letter.</summary>
    public const string SpreadLetterDps = "SpreadLetterDps";

    /// <summary>The DPS stack target letter.</summary>
    public const string StackLetterDps = "StackLetterDps";

    /// <summary>
    /// Every customisable text entry with its id, default value, settings label and group.
    /// </summary>
    public static readonly (string Id, string Default, string Label, string Group)[] Entries =
    {
        (FirstSetLabel, "< First Set >", "First Set label", "Panel labels"),
        (SecondSetLabel, "< Second Set >", "Second Set label", "Panel labels"),
        (KefkaLabel, "< Kefka >", "Kefka panel label", "Panel labels"),
        (LastFakeHeader, "Last Fake?", "Last Fake? header", "Panel labels"),

        (SpreadPrefix, "Spread on ", "Spread prefix (letter is appended)", "Resolutions"),
        (StackPrefix, "Stack on ", "Stack prefix (letter is appended)", "Resolutions"),
        (AndJoiner, " and ", "Acceleration joiner", "Resolutions"),
        (StandStill, "STAND STILL", "Acceleration - real (stand still)", "Resolutions"),
        (Move, "MOVE", "Acceleration - fake (move)", "Resolutions"),
        (GazeReal, "Gaze REAL / LOOK AWAY", "Gaze - real", "Resolutions"),
        (GazeFake, "Gaze FAKE / LOOK", "Gaze - fake", "Resolutions"),

        (InfernoReal, "FIRE TWISTER", "Inferno - real", "Chaos"),
        (InfernoFake, "FIRE DONUT", "Inferno - fake", "Chaos"),
        (TsunamiReal, "WATER DONUT", "Tsunami - real", "Chaos"),
        (TsunamiFake, "WATER TWISTER", "Tsunami - fake", "Chaos"),

        (ThunderName, "THUNDER", "Thunder name", "Kefka text"),
        (BlizzardName, "BLIZZARD", "Blizzard name", "Kefka text"),
        (RealWord, "REAL", "\"REAL\" word", "Kefka text"),
        (FakeWord, "FAKE", "\"FAKE\" word", "Kefka text"),

        (ExdeathFirstHeader, "1st Exdeath", "1st Exdeath header", "Headers"),
        (ExdeathSecondHeader, "2nd Exdeath", "2nd Exdeath header", "Headers"),
        (RealColumnHeader, "Real", "Exdeath \"Real\" column", "Headers"),
        (FakeColumnHeader, "Fake", "Exdeath \"Fake\" column", "Headers"),
        (ShortColumnHeader, "SHORT", "\"SHORT\" column", "Headers"),
        (LongColumnHeader, "LONG", "\"LONG\" column", "Headers"),
        (ChaosHeader, "Chaos", "Chaos section header", "Headers"),
        (KefkaButtonsHeader, "Kefka", "Kefka buttons header", "Headers"),

        (ResetButton, "RESET", "Reset button", "Buttons"),
        (UndoButton, "UNDO", "Undo button", "Buttons"),
        (HideButton, "HIDE", "Hide button", "Buttons"),
        (ShowButton, "SHOW", "Show button", "Buttons"),

        (SpreadLetterSupport, "D", "Spread letter - Support", "Target letters"),
        (StackLetterSupport, "A", "Stack letter - Support", "Target letters"),
        (SpreadLetterDps, "B", "Spread letter - DPS", "Target letters"),
        (StackLetterDps, "C", "Stack letter - DPS", "Target letters"),
    };

    /// <summary>
    /// The default values keyed by id, built once from the entries.
    /// </summary>
    private static readonly Dictionary<string, string> DefaultsById = BuildDefaults();

    /// <summary>
    /// Returns the default text for an id, or an empty string when the id is unknown.
    /// </summary>
    public static string Default(string id) => DefaultsById.TryGetValue(id, out var value) ? value : string.Empty;

    /// <summary>
    /// Builds the id-to-default lookup from the entry table.
    /// </summary>
    private static Dictionary<string, string> BuildDefaults()
    {
        var defaults = new Dictionary<string, string>();
        foreach (var (id, defaultValue, _, _) in Entries)
        {
            defaults[id] = defaultValue;
        }

        return defaults;
    }
}
