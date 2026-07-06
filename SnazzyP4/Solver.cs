using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace SnazzyP4;

/// <summary>
/// Holds the fight-solving state and draws each independent section.
/// The section draw methods are shared by the unified hub window, which positions them with cursor offsets, and by the detached per-section windows.
/// </summary>
public class Solver
{
    /// <summary>
    /// The logical (unscaled) edge length of an icon button.
    /// </summary>
    private const float IconButtonSize = 52f;

    /// <summary>
    /// The stages of the Exdeath and short/long input sequence.
    /// </summary>
    private enum Phase
    {
        /// <summary>
        /// Waiting for the first Exdeath button.
        /// </summary>
        WaitFirstExdeath,

        /// <summary>
        /// Waiting for the first short/long mechanic pick.
        /// </summary>
        WaitFirstShortLong,

        /// <summary>
        /// Waiting for the second Exdeath button.
        /// </summary>
        WaitSecondExdeath,

        /// <summary>
        /// Waiting for the second short/long mechanic pick.
        /// </summary>
        WaitSecondShortLong,

        /// <summary>
        /// Every input has been made.
        /// </summary>
        Done,
    }

    /// <summary>
    /// The short/long mechanics that can be picked in a window.
    /// </summary>
    private enum MechanicKind
    {
        /// <summary>
        /// Lightning, which spreads when real and stacks when fake.
        /// </summary>
        Lightning,

        /// <summary>
        /// Drop, which stacks when real and spreads when fake.
        /// </summary>
        Drop,

        /// <summary>
        /// Acceleration, which stands still when real and moves when fake.
        /// </summary>
        Acceleration,
    }

    /// <summary>
    /// A single short/long mechanic selection captured during an Exdeath window.
    /// </summary>
    /// <param name="Kind">The mechanic that was picked.</param>
    /// <param name="IsShort">Whether it was picked in the short column.</param>
    /// <param name="IsReal">Whether the owning Exdeath window was real.</param>
    private readonly record struct Selection(MechanicKind Kind, bool IsShort, bool IsReal);

    /// <summary>
    /// A full copy of the solver's input state, captured before each button press so the last press can be undone.
    /// The two collections are copied so later mutation does not change a stored snapshot.
    /// </summary>
    private sealed record Snapshot(
        bool FirstExdeathReal,
        bool SecondExdeathReal,
        bool FirstExdeathPressed,
        bool SecondExdeathPressed,
        Phase Phase,
        List<Selection> Selections,
        (string Text, Vector4 Color)? FirstSetChaos,
        (string Text, Vector4 Color)? SecondSetChaos,
        bool InfernoReal,
        bool TsunamiReal,
        bool ThunderPressed,
        bool ThunderReal,
        bool ThunderLastFake,
        bool BlizzardPressed,
        bool BlizzardReal,
        bool BlizzardLastFake,
        bool ShortMarkerSent,
        bool LongMarkerSent);

    /// <summary>
    /// The plugin configuration this solver reads colours and options from.
    /// </summary>
    private readonly Configuration configuration;

    /// <summary>
    /// The short/long mechanic selections made so far.
    /// </summary>
    private readonly List<Selection> selections = new();

    /// <summary>
    /// The First Set chaos resolution and its colour. Inferno always resolves in the first set, so this holds the Inferno press.
    /// </summary>
    private (string Text, Vector4 Color)? firstSetChaos;

    /// <summary>
    /// The Second Set chaos resolution and its colour. Tsunami always resolves in the second set, so this holds the Tsunami press.
    /// </summary>
    private (string Text, Vector4 Color)? secondSetChaos;

    /// <summary>Whether the pressed Inferno was the real variant (only meaningful once <see cref="firstSetChaos"/> is set).</summary>
    private bool infernoReal;

    /// <summary>Whether the pressed Tsunami was the real variant (only meaningful once <see cref="secondSetChaos"/> is set).</summary>
    private bool tsunamiReal;

    /// <summary>Whether the chronological party-chat summary has already been sent for the current pull.</summary>
    private bool chronoSent;

    /// <summary>
    /// The stack of pre-press state snapshots, newest on top, used by <see cref="Undo"/> to step back one button press at a time.
    /// </summary>
    private readonly Stack<Snapshot> undoStack = new();

    /// <summary>
    /// The current stage of the input sequence.
    /// </summary>
    private Phase phase = Phase.WaitFirstExdeath;

    /// <summary>
    /// Whether the first Exdeath was real.
    /// </summary>
    private bool firstExdeathReal;

    /// <summary>
    /// Whether the second Exdeath was real.
    /// </summary>
    private bool secondExdeathReal;

    /// <summary>
    /// Whether the first Exdeath button has been pressed.
    /// </summary>
    private bool firstExdeathPressed;

    /// <summary>
    /// Whether the second Exdeath button has been pressed.
    /// </summary>
    private bool secondExdeathPressed;

    /// <summary>
    /// Whether a Thunder or Fake Thunder button has been pressed.
    /// </summary>
    private bool thunderPressed;

    /// <summary>
    /// Whether the pressed Thunder button was the real variant.
    /// </summary>
    private bool thunderReal;

    /// <summary>
    /// The Last Fake override toggle for the Thunder line.
    /// </summary>
    private bool thunderLastFake;

    /// <summary>
    /// Whether a Blizzard or Fake Blizzard button has been pressed.
    /// </summary>
    private bool blizzardPressed;

    /// <summary>
    /// Whether the pressed Blizzard button was the real variant.
    /// </summary>
    private bool blizzardReal;

    /// <summary>
    /// The Last Fake override toggle for the Blizzard line.
    /// </summary>
    private bool blizzardLastFake;

    /// <summary>
    /// Whether the short-set marker command has already been sent this pull.
    /// </summary>
    private bool shortMarkerSent;

    /// <summary>
    /// Whether the long-set marker command has already been sent this pull.
    /// </summary>
    private bool longMarkerSent;

    /// <summary>
    /// The section id the hosting window is about to draw.
    /// This lets per-section appearance such as labels and opacity be resolved from the configuration.
    /// </summary>
    public string CurrentSection = string.Empty;

    /// <summary>
    /// The font scale the hosting window applied for the current section.
    /// It is used to scale the Last Fake toggle text independently of the button size.
    /// </summary>
    public float CurrentFontScale = 1f;

    /// <summary>
    /// The horizontal distance from the combined panel's window origin to its divider, measured on the last frame it was drawn side by side.
    /// The detached window uses this to keep the divider pinned at a fixed position while the two sets grow outward from it.
    /// </summary>
    public float CombinedDividerOffsetX;

    /// <summary>
    /// The vertical distance from the combined panel's window origin to its divider, measured on the last frame it was drawn stacked.
    /// The detached window uses this to keep the divider pinned while the two sets grow up and down from it.
    /// </summary>
    public float CombinedDividerOffsetY;

    /// <summary>
    /// Creates a solver bound to the plugin configuration.
    /// </summary>
    public Solver(Configuration configuration)
    {
        this.configuration = configuration;
    }

    /// <summary>
    /// Whether the label and gaze have advanced to the second Exdeath window.
    /// </summary>
    private bool OnSecondExdeath => phase is Phase.WaitSecondExdeath or Phase.WaitSecondShortLong or Phase.Done;

    /// <summary>
    /// Whether the layout is being edited.
    /// While editing, the buttons are disabled so a click drags the section, and the text panels show a placeholder resolution.
    /// </summary>
    private bool LayoutEditActive => configuration.EditMode || configuration.MoveAllActive;

    /// <summary>
    /// The configured colour for the fire (Inferno) callout.
    /// </summary>
    private Vector4 FireColor => configuration.ColorFire;

    /// <summary>
    /// The configured colour for the water (Tsunami) callout.
    /// </summary>
    private Vector4 WaterColor => configuration.ColorWater;

    /// <summary>
    /// The configured colour for the Thunder callout.
    /// </summary>
    private Vector4 ThunderColor => configuration.ColorThunder;

    /// <summary>
    /// The configured colour for the Blizzard callout.
    /// </summary>
    private Vector4 BlizzardColor => configuration.ColorBlizzard;

    /// <summary>
    /// The configured colour for the real gaze callout.
    /// </summary>
    private Vector4 GazeRealColor => configuration.ColorGazeReal;

    /// <summary>
    /// The configured colour for the fake gaze callout.
    /// </summary>
    private Vector4 GazeFakeColor => configuration.ColorGazeFake;

    /// <summary>
    /// The configured colour for the Acceleration movement callout.
    /// </summary>
    private Vector4 AccelerationColor => configuration.ColorAcceleration;

    /// <summary>
    /// Determines whether a section currently has anything to display, which is used to hide empty text panels.
    /// Non-text sections and the layout-edit preview always report content.
    /// </summary>
    public bool SectionHasContent(string sectionId)
    {
        if (LayoutEditActive)
        {
            return true;
        }

        return sectionId switch
        {
            "FirstSet" => SetHasContent(true, firstExdeathPressed, 0),
            "SecondSet" => SetHasContent(false, secondExdeathPressed, 1),
            "CombinedSets" => SetHasContent(true, firstExdeathPressed, 0)
                              || SetHasContent(false, secondExdeathPressed, 1),
            "ThunderText" => thunderPressed || blizzardPressed,
            _ => true,
        };
    }

    /// <summary>
    /// Determines whether a set panel has any body, gaze, chaos or completion content to show.
    /// </summary>
    private bool SetHasContent(bool isShort, bool gazeKnown, int chaosIndex)
    {
        if (phase == Phase.Done || gazeKnown || ChaosForSet(chaosIndex).HasValue)
        {
            return true;
        }

        foreach (var selection in selections)
        {
            if (selection.IsShort == isShort)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Draws the Exdeath real/fake buttons and the short/long mechanic grid.
    /// </summary>
    public void DrawExdeath(float scale)
    {
        var style = ImGui.GetStyle();
        var columnStride = IconButtonSize * scale + style.FramePadding.X * 2 + style.ItemSpacing.X;
        var exdeathEnabled = phase is Phase.WaitFirstExdeath or Phase.WaitSecondExdeath;
        var labelsHidden = configuration.EffectiveHideLabels(CurrentSection);

        if (!labelsHidden)
        {
            ImGui.TextUnformatted(configuration.GetText(OnSecondExdeath ? TextLabels.ExdeathSecondHeader : TextLabels.ExdeathFirstHeader));
        }

        if (!labelsHidden)
        {
            var headerStartX = ImGui.GetCursorPosX();
            ImGui.TextUnformatted(configuration.GetText(TextLabels.RealColumnHeader));
            ImGui.SameLine(headerStartX + columnStride);
            ImGui.TextUnformatted(configuration.GetText(TextLabels.FakeColumnHeader));
        }

        if (IconButton("##RealExdeath", "RealExdeath.png", exdeathEnabled, scale))
        {
            OnExdeath(true);
        }

        ImGui.SameLine();

        if (IconButton("##FakeExdeath", "FakeExdeath.png", exdeathEnabled, scale))
        {
            OnExdeath(false);
        }

        ImGuiHelpers.ScaledDummy(4f);

        if (!labelsHidden)
        {
            var headerStartX = ImGui.GetCursorPosX();
            ImGui.TextUnformatted(configuration.GetText(TextLabels.ShortColumnHeader));
            ImGui.SameLine(headerStartX + columnStride);
            ImGui.TextUnformatted(configuration.GetText(TextLabels.LongColumnHeader));
        }

        DrawShortLongRow("Lightning.png", MechanicKind.Lightning, scale);
        DrawShortLongRow("Drop.png", MechanicKind.Drop, scale);
        DrawShortLongRow("Acceleration.png", MechanicKind.Acceleration, scale);
    }

    /// <summary>
    /// Draws one row of the short/long grid for a single mechanic.
    /// </summary>
    private void DrawShortLongRow(string iconFile, MechanicKind kind, float scale)
    {
        if (IconButton($"##Short{kind}", iconFile, ShortLongEnabled(kind, true), scale))
        {
            OnShortLong(kind, true);
        }

        ImGui.SameLine();

        if (IconButton($"##Long{kind}", iconFile, ShortLongEnabled(kind, false), scale))
        {
            OnShortLong(kind, false);
        }
    }

    /// <summary>
    /// Determines whether a short/long button is currently pressable.
    /// A button is enabled only during a pick window and only if it would not create a contradiction, since each set holds at most one body result (Lightning or Drop) and at most one Acceleration.
    /// </summary>
    private bool ShortLongEnabled(MechanicKind kind, bool isShort)
    {
        if (phase is not (Phase.WaitFirstShortLong or Phase.WaitSecondShortLong))
        {
            return false;
        }

        return !SlotFilled(isShort, kind == MechanicKind.Acceleration);
    }

    /// <summary>
    /// Determines whether a set already has a body slot or acceleration slot filled.
    /// </summary>
    private bool SlotFilled(bool isShort, bool acceleration)
    {
        foreach (var selection in selections)
        {
            if (selection.IsShort == isShort && (selection.Kind == MechanicKind.Acceleration) == acceleration)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The horizontal alignment used when rendering a set's lines within its column.
    /// </summary>
    private enum SetAlignment
    {
        /// <summary>
        /// Lines start at the left edge of the column.
        /// </summary>
        Left,

        /// <summary>
        /// Lines end at the right edge of the column.
        /// </summary>
        Right,

        /// <summary>
        /// Lines are centred within the column.
        /// </summary>
        Center,
    }

    /// <summary>
    /// A single run of text within a resolution line.
    /// A plain run uses the default text colour, a coloured run uses its own colour, and a disabled run uses the muted placeholder colour.
    /// </summary>
    private readonly record struct SetRun(string Text, Vector4 Color, bool HasColor, bool Disabled);

    /// <summary>
    /// Draws the First Set resolution, which is the short picks with the first gaze and first chaos.
    /// </summary>
    public void DrawFirstSet(float scale)
    {
        RenderLines(BuildSetPanel(configuration.GetText(TextLabels.FirstSetLabel), true, firstExdeathPressed, firstExdeathReal, 0), SetAlignment.Left, 0f);
    }

    /// <summary>
    /// Draws the Second Set resolution, which is the long picks with the second gaze and second chaos.
    /// </summary>
    public void DrawSecondSet(float scale)
    {
        RenderLines(BuildSetPanel(configuration.GetText(TextLabels.SecondSetLabel), false, secondExdeathPressed, secondExdeathReal, 1), SetAlignment.Left, 0f);
    }

    /// <summary>
    /// Draws the First Set and Second Set together in one panel, divided by a line.
    /// The sets stack vertically or sit side by side, and can optionally expand outward from the divider instead of the left edge.
    /// </summary>
    public void DrawCombinedSets(float scale)
    {
        var firstLines = BuildSetPanel(configuration.GetText(TextLabels.FirstSetLabel), true, firstExdeathPressed, firstExdeathReal, 0);
        var secondLines = BuildSetPanel(configuration.GetText(TextLabels.SecondSetLabel), false, secondExdeathPressed, secondExdeathReal, 1);
        var fromCenter = configuration.CombineSetsExpandFromCenter;

        var dividerColor = ImGui.GetColorU32(configuration.CombineDividerColor);
        var dividerThickness = Math.Max(1f, configuration.CombineDividerThickness * scale);

        if (!configuration.CombineSetsHorizontal)
        {
            // Stacked: expanding from the centre centres every line on the panel's midline.
            var stackedWidth = Math.Max(MaxLineWidth(firstLines), MaxLineWidth(secondLines));
            var stackedAlignment = fromCenter ? SetAlignment.Center : SetAlignment.Left;
            RenderLines(firstLines, stackedAlignment, fromCenter ? stackedWidth : 0f);

            ImGui.Spacing();
            var dividerScreen = ImGui.GetCursorScreenPos();
            CombinedDividerOffsetY = dividerScreen.Y - ImGui.GetWindowPos().Y;
            ImGui.GetWindowDrawList().AddLine(
                new Vector2(dividerScreen.X, dividerScreen.Y),
                new Vector2(dividerScreen.X + stackedWidth, dividerScreen.Y),
                dividerColor, dividerThickness);
            ImGui.Dummy(new Vector2(stackedWidth, dividerThickness));
            ImGui.Spacing();

            RenderLines(secondLines, stackedAlignment, fromCenter ? stackedWidth : 0f);
            return;
        }

        // Side by side: expanding from the centre right-aligns the first set against the divider so both sets grow outward from it.
        var firstWidth = MaxLineWidth(firstLines);
        var top = ImGui.GetCursorScreenPos().Y;
        using (ImRaii.Group())
        {
            RenderLines(firstLines, fromCenter ? SetAlignment.Right : SetAlignment.Left, firstWidth);
        }

        var firstMax = ImGui.GetItemRectMax();
        var dividerX = firstMax.X + 8f * scale;

        // Record where the divider sits relative to the window so the detached window can pin it in place.
        CombinedDividerOffsetX = dividerX - ImGui.GetWindowPos().X;

        ImGui.SameLine(0f, 24f * scale);
        using (ImRaii.Group())
        {
            RenderLines(secondLines, SetAlignment.Left, 0f);
        }

        var bottom = Math.Max(firstMax.Y, ImGui.GetItemRectMax().Y);
        ImGui.GetWindowDrawList().AddLine(
            new Vector2(dividerX, top),
            new Vector2(dividerX, bottom),
            dividerColor,
            dividerThickness);
    }

    /// <summary>
    /// Builds a set panel's lines, prefixing the set label unless labels are hidden.
    /// </summary>
    private List<List<SetRun>> BuildSetPanel(string label, bool isShort, bool gazeKnown, bool gazeReal, int chaosIndex)
    {
        var lines = new List<List<SetRun>>();
        if (!configuration.EffectiveHideLabels(CurrentSection))
        {
            lines.Add(new List<SetRun> { PlainRun(label) });
        }

        lines.AddRange(BuildSetLines(isShort, gazeKnown, gazeReal, chaosIndex));
        return lines;
    }

    /// <summary>
    /// Builds the resolution lines for one set as coloured runs.
    /// The body comes from the short/long picks in this set, while the gaze comes from the matching Exdeath press order, so the first press drives the First Set gaze and the second press drives the Second Set gaze.
    /// </summary>
    private List<List<SetRun>> BuildSetLines(bool isShort, bool gazeKnown, bool gazeReal, int chaosIndex)
    {
        var lines = new List<List<SetRun>>();

        if (LayoutEditActive)
        {
            var standStill = configuration.GetText(TextLabels.StandStill);
            lines.Add(BuildResolutionLine(true, standStill));
            if (!configuration.AccelerationSameLine)
            {
                lines.Add(new List<SetRun> { ColorRun(standStill, AccelerationColor) });
            }

            lines.Add(new List<SetRun> { ColorRun(configuration.GetText(TextLabels.GazeReal), GazeRealColor) });

            // Chaos is static: the first set resolves Inferno, the second resolves Tsunami.
            lines.Add(isShort
                ? new List<SetRun> { ColorRun(configuration.GetText(TextLabels.InfernoReal), FireColor) }
                : new List<SetRun> { ColorRun(configuration.GetText(TextLabels.TsunamiReal), WaterColor) });
            return lines;
        }

        var complete = phase == Phase.Done;
        var anyPick = false;
        bool? bodySpread = null;
        string? accelerationText = null;

        // A set holds at most one body pick (Lightning or Drop) and at most one Acceleration.
        foreach (var selection in selections)
        {
            if (selection.IsShort != isShort)
            {
                continue;
            }

            anyPick = true;
            if (selection.Kind == MechanicKind.Acceleration)
            {
                accelerationText = configuration.GetText(selection.IsReal ? TextLabels.StandStill : TextLabels.Move);
            }
            else
            {
                bodySpread = MechanicText(selection.Kind, selection.IsReal) == "Spread";
            }
        }

        // Once the whole sequence resolves, a set with no Spread and no Stack always defaults to Stack.
        if (complete && bodySpread is null)
        {
            bodySpread = false;
        }

        if (bodySpread.HasValue)
        {
            lines.Add(BuildResolutionLine(bodySpread.Value, accelerationText));

            // When the acceleration is not appended to the body line, it drops onto its own line.
            if (accelerationText != null && !configuration.AccelerationSameLine)
            {
                lines.Add(new List<SetRun> { ColorRun(accelerationText, AccelerationColor) });
            }
        }
        else if (accelerationText != null)
        {
            lines.Add(new List<SetRun> { ColorRun(accelerationText, AccelerationColor) });
        }

        if (gazeKnown)
        {
            lines.Add(new List<SetRun>
            {
                ColorRun(gazeReal ? configuration.GetText(TextLabels.GazeReal) : configuration.GetText(TextLabels.GazeFake),
                    gazeReal ? GazeRealColor : GazeFakeColor),
            });
        }

        var chaos = ChaosForSet(chaosIndex);
        if (chaos.HasValue)
        {
            lines.Add(new List<SetRun> { ColorRun(chaos.Value.Text, chaos.Value.Color) });
        }

        if (!anyPick && !gazeKnown && !complete && !chaos.HasValue)
        {
            lines.Add(new List<SetRun> { DisabledRun("--") });
        }

        return lines;
    }

    /// <summary>
    /// Returns the chaos resolution for a set: index 0 is the first set (Inferno), index 1 is the second set (Tsunami).
    /// </summary>
    private (string Text, Vector4 Color)? ChaosForSet(int chaosIndex) => chaosIndex == 0 ? firstSetChaos : secondSetChaos;

    /// <summary>
    /// Builds one spread or stack body line with the role-based target letter, appending the Acceleration word when it shares the line.
    /// Support uses A for stack and D for spread, while DPS uses C for stack and B for spread.
    /// </summary>
    private List<SetRun> BuildResolutionLine(bool spread, string? accelerationText)
    {
        var support = configuration.IsSupport;
        string letter;
        Vector4 color;
        if (spread)
        {
            letter = configuration.GetText(support ? TextLabels.SpreadLetterSupport : TextLabels.SpreadLetterDps);
            color = support ? configuration.ColorSpreadSupport : configuration.ColorSpreadDps;
        }
        else
        {
            letter = configuration.GetText(support ? TextLabels.StackLetterSupport : TextLabels.StackLetterDps);
            color = support ? configuration.ColorStackSupport : configuration.ColorStackDps;
        }

        var line = new List<SetRun>
        {
            PlainRun(configuration.GetText(spread ? TextLabels.SpreadPrefix : TextLabels.StackPrefix)),
            ColorRun(letter, color),
        };

        if (accelerationText != null && configuration.AccelerationSameLine)
        {
            line.Add(PlainRun(configuration.GetText(TextLabels.AndJoiner)));
            line.Add(ColorRun(accelerationText, AccelerationColor));
        }

        return line;
    }

    /// <summary>
    /// Renders a panel's lines, aligning each line within the given column width.
    /// </summary>
    private static void RenderLines(List<List<SetRun>> lines, SetAlignment alignment, float columnWidth)
    {
        var baseX = ImGui.GetCursorPosX();
        foreach (var line in lines)
        {
            if (alignment != SetAlignment.Left)
            {
                var offset = alignment == SetAlignment.Right
                    ? columnWidth - LineWidth(line)
                    : (columnWidth - LineWidth(line)) * 0.5f;
                ImGui.SetCursorPosX(baseX + Math.Max(0f, offset));
            }

            for (var index = 0; index < line.Count; index++)
            {
                if (index > 0)
                {
                    ImGui.SameLine(0, 0);
                }

                var run = line[index];
                if (run.Disabled)
                {
                    ImGui.TextDisabled(run.Text);
                }
                else if (run.HasColor)
                {
                    ImGui.TextColored(run.Color, run.Text);
                }
                else
                {
                    ImGui.TextUnformatted(run.Text);
                }
            }
        }
    }

    /// <summary>
    /// Measures the pixel width of a line by summing its runs.
    /// </summary>
    private static float LineWidth(List<SetRun> line)
    {
        var width = 0f;
        foreach (var run in line)
        {
            width += ImGui.CalcTextSize(run.Text).X;
        }

        return width;
    }

    /// <summary>
    /// Measures the widest line in a panel.
    /// </summary>
    private static float MaxLineWidth(List<List<SetRun>> lines)
    {
        var max = 0f;
        foreach (var line in lines)
        {
            max = Math.Max(max, LineWidth(line));
        }

        return max;
    }

    /// <summary>
    /// Creates a run drawn in the default text colour.
    /// </summary>
    private static SetRun PlainRun(string text) => new(text, default, false, false);

    /// <summary>
    /// Creates a run drawn in a specific colour.
    /// </summary>
    private static SetRun ColorRun(string text, Vector4 color) => new(text, color, true, false);

    /// <summary>
    /// Creates a run drawn in the muted placeholder colour.
    /// </summary>
    private static SetRun DisabledRun(string text) => new(text, default, false, true);

    /// <summary>
    /// Determines whether a selection resolves to a spread.
    /// </summary>
    private static bool IsSpread(Selection selection) => MechanicText(selection.Kind, selection.IsReal) == "Spread";

    /// <summary>
    /// Determines whether a set contains a spread selection.
    /// </summary>
    private bool SetHasSpread(bool isShort)
    {
        foreach (var selection in selections)
        {
            if (selection.IsShort == isShort && IsSpread(selection))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Sends each set's marker command once, as soon as that set's spread is known.
    /// </summary>
    public void UpdateAutoMarkers()
    {
        if (!configuration.AutoMarker)
        {
            return;
        }

        var support = configuration.IsSupport;
        if (!shortMarkerSent && SetHasSpread(true))
        {
            PlaceMarker(support ? configuration.MarkerFirstSetSupport : configuration.MarkerFirstSetDps);
            shortMarkerSent = true;
        }

        if (!longMarkerSent && SetHasSpread(false))
        {
            PlaceMarker(support ? configuration.MarkerSecondSetSupport : configuration.MarkerSecondSetDps);
            longMarkerSent = true;
        }
    }

    /// <summary>
    /// Places a configured head marker on the player, skipping it when no marker is configured.
    /// </summary>
    private void PlaceMarker(string marker)
    {
        if (string.IsNullOrWhiteSpace(marker))
        {
            return;
        }

        Plugin.ExecuteGameCommand($"/mk {marker} <me>");
    }

    /// <summary>
    /// Draws the Reset button, which is disabled while the layout is being edited.
    /// </summary>
    public void DrawReset(float scale)
    {
        using (ImRaii.Disabled(LayoutEditActive))
        {
            if (ImGui.Button(configuration.GetText(TextLabels.ResetButton), new Vector2(90, 34) * scale))
            {
                ResetAll();
            }
        }
    }

    /// <summary>
    /// Draws the Undo button, which steps back the last button press and is disabled while editing the layout or when there is nothing to undo.
    /// </summary>
    public void DrawUndo(float scale)
    {
        using (ImRaii.Disabled(LayoutEditActive || !CanUndo))
        {
            if (ImGui.Button(configuration.GetText(TextLabels.UndoButton), new Vector2(90, 34) * scale))
            {
                Undo();
            }
        }
    }

    /// <summary>
    /// Draws the Hide/Show button, which is disabled while the layout is being edited.
    /// </summary>
    public void DrawHideToggle(float scale)
    {
        using (ImRaii.Disabled(LayoutEditActive))
        {
            if (ImGui.Button(configuration.GetText(configuration.Hidden ? TextLabels.ShowButton : TextLabels.HideButton), new Vector2(90, 34) * scale))
            {
                configuration.Hidden = !configuration.Hidden;
                configuration.Save();
            }
        }
    }

    /// <summary>
    /// Draws the Chaos section with the Inferno and Tsunami buttons.
    /// </summary>
    public void DrawFireWaterButtons(float scale)
    {
        if (!configuration.EffectiveHideLabels(CurrentSection))
        {
            ImGui.TextUnformatted(configuration.GetText(TextLabels.ChaosHeader));
        }

        // Inferno and Tsunami each resolve once per pull, so each pair disables after it is pressed until the next reset.
        var infernoEnabled = !firstSetChaos.HasValue;
        var tsunamiEnabled = !secondSetChaos.HasValue;

        if (IconButton("##Inferno", "Inferno.png", infernoEnabled, scale))
        {
            OnChaos(configuration.GetText(TextLabels.InfernoReal), FireColor, "inferno", true);
        }

        ImGui.SameLine();

        if (IconButton("##FakeInferno", "FakeInferno.png", infernoEnabled, scale))
        {
            OnChaos(configuration.GetText(TextLabels.InfernoFake), FireColor, "inferno", false);
        }

        if (IconButton("##Tsunami", "Tsunami.png", tsunamiEnabled, scale))
        {
            OnChaos(configuration.GetText(TextLabels.TsunamiReal), WaterColor, "tsunami", true);
        }

        ImGui.SameLine();

        if (IconButton("##FakeTsunami", "FakeTsunami.png", tsunamiEnabled, scale))
        {
            OnChaos(configuration.GetText(TextLabels.TsunamiFake), WaterColor, "tsunami", false);
        }
    }

    /// <summary>
    /// Draws the Kefka section with the Thunder and Blizzard buttons.
    /// </summary>
    public void DrawThunderButtons(float scale)
    {
        if (!configuration.EffectiveHideLabels(CurrentSection))
        {
            ImGui.TextUnformatted(configuration.GetText(TextLabels.KefkaButtonsHeader));
        }

        if (IconButton("##Thunder", "Thunder.png", true, scale))
        {
            OnThunder(true);
        }

        ImGui.SameLine();

        if (IconButton("##FakeThunder", "FakeThunder.png", true, scale))
        {
            OnThunder(false);
        }

        if (IconButton("##Blizzard", "Blizzard.png", true, scale))
        {
            OnBlizzard(true);
        }

        ImGui.SameLine();

        if (IconButton("##FakeBlizzard", "FakeBlizzard.png", true, scale))
        {
            OnBlizzard(false);
        }
    }

    /// <summary>
    /// Records a Thunder press (real or fake), capturing an undo point first.
    /// </summary>
    private void OnThunder(bool real)
    {
        PushUndo();
        thunderPressed = true;
        thunderReal = real;
    }

    /// <summary>
    /// Records a Blizzard press (real or fake), capturing an undo point first.
    /// </summary>
    private void OnBlizzard(bool real)
    {
        PushUndo();
        blizzardPressed = true;
        blizzardReal = real;
    }

    /// <summary>
    /// Draws the Kefka text panel with the Thunder and Blizzard resolutions.
    /// When the toggles are not detached, the inline Last Fake toggles are drawn beside each line.
    /// </summary>
    public void DrawThunderText(float scale)
    {
        var showDocked = configuration.ShowLastFake && configuration.LastFakeAnnounceEnabled
                         && configuration.LastFakeAnnounceDocked;
        var side = configuration.LastFakeAnnounceDockSide;

        if (showDocked && side == "top")
        {
            DrawAnnounceLastFakeButton(scale);
        }

        if (showDocked && side == "left")
        {
            DrawAnnounceLastFakeButton(scale);
            ImGui.SameLine();
        }

        using (ImRaii.Group())
        {
            DrawThunderContent();
        }

        if (showDocked && side == "right")
        {
            ImGui.SameLine();
            DrawAnnounceLastFakeButton(scale);
        }

        if (showDocked && side == "bottom")
        {
            DrawAnnounceLastFakeButton(scale);
        }
    }

    /// <summary>
    /// Draws the Kefka panel content (header and resolution lines), excluding the optional docked ANNOUNCE button.
    /// </summary>
    private void DrawThunderContent()
    {
        var labelsHidden = configuration.EffectiveHideLabels(CurrentSection);
        var inlineToggles = configuration.ShowLastFake && !configuration.DetachToggleButtons
                            && !configuration.HideMacroButtons;
        var startX = ImGui.GetCursorPosX();
        var toggleColumn = Math.Max(ImGui.CalcTextSize("THUNDER FAKE").X, ImGui.CalcTextSize("BLIZZARD FAKE").X)
                           + ImGui.CalcTextSize("  ").X;

        // The "< Kefka >" label and the "Last Fake?" label share the same header row.
        // During layout editing the header shows whenever the inline toggles are enabled so their column can be positioned.
        if (!labelsHidden)
        {
            ImGui.TextUnformatted(configuration.GetText(TextLabels.KefkaLabel));
            if (inlineToggles && (LayoutEditActive || thunderPressed || blizzardPressed))
            {
                ImGui.SameLine(startX + toggleColumn);
                ImGui.TextUnformatted(configuration.GetText(TextLabels.LastFakeHeader));
            }
        }

        // While editing the layout, the panel shows sample lines and, when the inline toggles are enabled, the toggle buttons so they populate the panel and can be positioned.
        if (LayoutEditActive)
        {
            var real = configuration.GetText(TextLabels.RealWord);
            DrawKefkaSampleLine("thunder", ref thunderLastFake, $"{configuration.GetText(TextLabels.ThunderName)} {real}", ThunderColor, startX, toggleColumn, inlineToggles);
            DrawKefkaSampleLine("blizzard", ref blizzardLastFake, $"{configuration.GetText(TextLabels.BlizzardName)} {real}", BlizzardColor, startX, toggleColumn, inlineToggles);
            return;
        }

        if (!thunderPressed && !blizzardPressed)
        {
            ImGui.TextDisabled("--");
            return;
        }

        if (thunderPressed)
        {
            DrawKefkaLine("thunder", ref thunderLastFake, thunderReal, configuration.GetText(TextLabels.ThunderName), ThunderColor, startX, toggleColumn, inlineToggles);
        }

        if (blizzardPressed)
        {
            DrawKefkaLine("blizzard", ref blizzardLastFake, blizzardReal, configuration.GetText(TextLabels.BlizzardName), BlizzardColor, startX, toggleColumn, inlineToggles);
        }
    }

    /// <summary>
    /// Draws one Kefka resolution line and, optionally, its inline Last Fake toggle.
    /// The displayed real/fake value is the button value combined with the Last Fake toggle when the toggle feature is enabled.
    /// </summary>
    private void DrawKefkaLine(string idSuffix, ref bool lastFake, bool buttonReal, string name,
        Vector4 color, float startX, float toggleColumn, bool drawToggleInline)
    {
        var effectiveReal = buttonReal ^ (configuration.ShowLastFake && lastFake);
        ImGui.TextColored(color, $"{name} {configuration.GetText(effectiveReal ? TextLabels.RealWord : TextLabels.FakeWord)}");
        if (drawToggleInline)
        {
            ImGui.SameLine(startX + toggleColumn);
            DrawToggle(ref lastFake, idSuffix);
        }
    }

    /// <summary>
    /// Draws a sample Kefka line while the layout is being edited, with the inline Last Fake toggle when it is enabled.
    /// </summary>
    private void DrawKefkaSampleLine(string idSuffix, ref bool lastFake, string sample, Vector4 color,
        float startX, float toggleColumn, bool drawToggleInline)
    {
        ImGui.TextColored(color, sample);
        if (drawToggleInline)
        {
            ImGui.SameLine(startX + toggleColumn);
            DrawToggle(ref lastFake, idSuffix);
        }
    }

    /// <summary>
    /// Draws the Last Fake ANNOUNCE button, which sends the current Kefka values to the configured channel.
    /// </summary>
    public void DrawAnnounceLastFakeButton(float scale)
    {
        using (ImRaii.Disabled(LayoutEditActive))
        {
            if (ImGui.Button("ANNOUNCE", new Vector2(100, 34) * scale))
            {
                AnnounceLastFake();
            }
        }
    }

    /// <summary>
    /// Sends the Last Fake announcement, replacing the {KefkaThunder} and {KefkaBlizzard} macros with their current values.
    /// </summary>
    private void AnnounceLastFake()
    {
        var thunder = KefkaMacroValue(thunderPressed, thunderReal ^ (configuration.ShowLastFake && thunderLastFake));
        var blizzard = KefkaMacroValue(blizzardPressed, blizzardReal ^ (configuration.ShowLastFake && blizzardLastFake));
        var message = configuration.LastFakeAnnounceMessage
            .Replace("{KefkaThunder}", thunder)
            .Replace("{KefkaBlizzard}", blizzard);

        if (!string.IsNullOrWhiteSpace(message))
        {
            Plugin.ExecuteGameCommand($"{configuration.LastFakeAnnounceChannel} {message.Trim()}");
        }
    }

    /// <summary>
    /// Resolves a Kefka macro to its current text, using a question mark when that mechanic has not been pressed.
    /// </summary>
    private string KefkaMacroValue(bool pressed, bool effectiveReal)
        => !pressed ? "?" : (effectiveReal ? configuration.LastFakeAnnounceRealText : configuration.LastFakeAnnounceFakeText);

    /// <summary>
    /// Draws a Last Fake toggle.
    /// It renders as a plain checkbox, or as a green REAL / red FAKE button whose label, size and opacity can be customised.
    /// The toggle is disabled while the layout is being edited so a click drags the panel instead of flipping it.
    /// </summary>
    private void DrawToggle(ref bool lastFake, string idSuffix)
    {
        using var editDisabled = ImRaii.Disabled(LayoutEditActive);
        if (configuration.UseBasicToggles)
        {
            ImGui.Checkbox($"##lf_{idSuffix}", ref lastFake);
            return;
        }

        var custom = !configuration.UseSharedToggleSettings;
        var label = ToggleLabel(lastFake);
        var buttonColor = lastFake ? configuration.ColorToggleFake : configuration.ColorToggleReal;

        if (custom)
        {
            ImGui.SetWindowFontScale(CurrentFontScale * configuration.ToggleTextScale);
            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, configuration.ToggleButtonAlpha);
        }

        ImGui.PushStyleColor(ImGuiCol.Button, buttonColor);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ShiftColor(buttonColor, 0.12f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, ShiftColor(buttonColor, -0.12f));

        var frameHeight = ImGui.GetFrameHeight();
        var scaleX = custom ? configuration.ToggleButtonScaleX : 1f;
        var scaleY = custom ? configuration.ToggleButtonScaleY : 1f;

        bool clicked;
        if (string.IsNullOrEmpty(label))
        {
            clicked = ImGui.Button($"##lf_{idSuffix}", new Vector2(frameHeight * scaleX, frameHeight * scaleY));
        }
        else
        {
            var width = ImGui.CalcTextSize(label).X + ImGui.GetStyle().FramePadding.X * 2f;
            clicked = ImGui.Button($"{label}##lf_{idSuffix}", new Vector2(width * scaleX, frameHeight * scaleY));
        }

        if (clicked)
        {
            lastFake = !lastFake;
        }

        ImGui.PopStyleColor(3);
        if (custom)
        {
            ImGui.PopStyleVar();
            ImGui.SetWindowFontScale(CurrentFontScale);
        }
    }

    /// <summary>
    /// Resolves the label text for a Last Fake toggle in its current state.
    /// </summary>
    private string ToggleLabel(bool lastFake)
    {
        if (configuration.UseCustomToggleText)
        {
            return lastFake ? configuration.CustomFakeText : configuration.CustomRealText;
        }

        return lastFake ? "FAKE" : "REAL";
    }

    /// <summary>
    /// Returns a copy of a colour with its red, green and blue channels shifted for hover and active states.
    /// </summary>
    private static Vector4 ShiftColor(Vector4 color, float delta) => new(
        Math.Clamp(color.X + delta, 0f, 1f),
        Math.Clamp(color.Y + delta, 0f, 1f),
        Math.Clamp(color.Z + delta, 0f, 1f),
        color.W);

    /// <summary>
    /// Draws both Last Fake toggles for the combined detached panel.
    /// </summary>
    public void DrawLastFakeToggles(float scale)
    {
        DrawNamedToggle("thunder", ref thunderLastFake, configuration.GetText(TextLabels.ThunderName));
        if (configuration.ToggleButtonsHorizontal)
        {
            ImGui.SameLine();
        }

        DrawNamedToggle("blizzard", ref blizzardLastFake, configuration.GetText(TextLabels.BlizzardName));
    }

    /// <summary>
    /// Draws the Thunder Last Fake toggle for its own detached panel.
    /// </summary>
    public void DrawLastFakeThunderToggle(float scale)
    {
        DrawNamedToggle("thunder", ref thunderLastFake, configuration.GetText(TextLabels.ThunderName));
    }

    /// <summary>
    /// Draws the Blizzard Last Fake toggle for its own detached panel.
    /// </summary>
    public void DrawLastFakeBlizzardToggle(float scale)
    {
        DrawNamedToggle("blizzard", ref blizzardLastFake, configuration.GetText(TextLabels.BlizzardName));
    }

    /// <summary>
    /// Draws a labelled Last Fake toggle for the detached panels.
    /// </summary>
    private void DrawNamedToggle(string idSuffix, ref bool lastFake, string name)
    {
        if (!configuration.EffectiveHideLabels(CurrentSection))
        {
            ImGui.TextUnformatted(name);
            ImGui.SameLine();
        }

        DrawToggle(ref lastFake, idSuffix);
    }

    /// <summary>
    /// Draws an icon button, disabling it while the layout is being edited so a click drags the section.
    /// A unique ImGui id is pushed because the same texture is reused across columns, and opacity is applied to the whole section by the hosting window.
    /// </summary>
    private bool IconButton(string id, string iconFile, bool enabled, float scale)
    {
        var size = new Vector2(IconButtonSize, IconButtonSize) * scale;
        using (ImRaii.Disabled(!enabled || LayoutEditActive))
        using (ImRaii.PushId(id))
        {
            var texture = Plugin.TextureProvider.GetFromFile(Plugin.Icon(iconFile)).GetWrapOrDefault();
            if (texture != null)
            {
                return ImGui.ImageButton(texture.Handle, size);
            }

            return ImGui.Button(iconFile, size);
        }
    }

    /// <summary>
    /// Advances the sequence when an Exdeath button is pressed and fires the Exdeath announcements for that set and real/fake.
    /// </summary>
    private void OnExdeath(bool real)
    {
        if (phase == Phase.WaitFirstExdeath)
        {
            PushUndo();
            firstExdeathReal = real;
            firstExdeathPressed = true;
            phase = Phase.WaitFirstShortLong;
            FireAnnouncements(ActiveExdeath, "exdeath", true, real, null);
        }
        else if (phase == Phase.WaitSecondExdeath)
        {
            PushUndo();
            secondExdeathReal = real;
            secondExdeathPressed = true;
            phase = Phase.WaitSecondShortLong;
            FireAnnouncements(ActiveExdeath, "exdeath", false, real, null);
        }
    }

    /// <summary>
    /// Whether there is at least one button press that can be undone.
    /// </summary>
    public bool CanUndo => undoStack.Count > 0;

    /// <summary>
    /// Captures the current input state onto the undo stack. Called before each state-changing button press.
    /// </summary>
    private void PushUndo()
    {
        undoStack.Push(new Snapshot(
            firstExdeathReal,
            secondExdeathReal,
            firstExdeathPressed,
            secondExdeathPressed,
            phase,
            new List<Selection>(selections),
            firstSetChaos,
            secondSetChaos,
            infernoReal,
            tsunamiReal,
            thunderPressed,
            thunderReal,
            thunderLastFake,
            blizzardPressed,
            blizzardReal,
            blizzardLastFake,
            shortMarkerSent,
            longMarkerSent));
    }

    /// <summary>
    /// Reverts the most recent button press, restoring the input state and on-screen resolution to just before it.
    /// Chat announcements and markers already sent for that press are not recalled.
    /// </summary>
    public void Undo()
    {
        if (undoStack.Count == 0)
        {
            return;
        }

        var snapshot = undoStack.Pop();
        firstExdeathReal = snapshot.FirstExdeathReal;
        secondExdeathReal = snapshot.SecondExdeathReal;
        firstExdeathPressed = snapshot.FirstExdeathPressed;
        secondExdeathPressed = snapshot.SecondExdeathPressed;
        phase = snapshot.Phase;

        selections.Clear();
        selections.AddRange(snapshot.Selections);

        firstSetChaos = snapshot.FirstSetChaos;
        secondSetChaos = snapshot.SecondSetChaos;
        infernoReal = snapshot.InfernoReal;
        tsunamiReal = snapshot.TsunamiReal;

        thunderPressed = snapshot.ThunderPressed;
        thunderReal = snapshot.ThunderReal;
        thunderLastFake = snapshot.ThunderLastFake;
        blizzardPressed = snapshot.BlizzardPressed;
        blizzardReal = snapshot.BlizzardReal;
        blizzardLastFake = snapshot.BlizzardLastFake;

        shortMarkerSent = snapshot.ShortMarkerSent;
        longMarkerSent = snapshot.LongMarkerSent;
    }

    /// <summary>
    /// Records a short/long mechanic pick and advances the sequence.
    /// </summary>
    private void OnShortLong(MechanicKind kind, bool isShort)
    {
        bool real;
        if (phase == Phase.WaitFirstShortLong)
        {
            real = firstExdeathReal;
        }
        else if (phase == Phase.WaitSecondShortLong)
        {
            real = secondExdeathReal;
        }
        else
        {
            return;
        }

        PushUndo();
        selections.Add(new Selection(kind, isShort, real));
        phase = phase == Phase.WaitFirstShortLong ? Phase.WaitSecondExdeath : Phase.Done;
    }

    /// <summary>
    /// Records a chaos twister press and fires the Chaos announcements for that mechanic and real/fake.
    /// Inferno always resolves in the first set and Tsunami in the second; each ignores further presses until reset.
    /// </summary>
    private void OnChaos(string text, Vector4 color, string slotId, bool isReal)
    {
        var isInferno = slotId == "inferno";
        if (isInferno ? firstSetChaos.HasValue : secondSetChaos.HasValue)
        {
            return;
        }

        PushUndo();
        if (isInferno)
        {
            firstSetChaos = (text, color);
            infernoReal = isReal;
        }
        else
        {
            secondSetChaos = (text, color);
            tsunamiReal = isReal;
        }

        FireAnnouncements(ActiveChaos, "chaos", isInferno, isReal, slotId);
    }

    /// <summary>
    /// The Exdeath announcement configuration for the currently selected channel.
    /// </summary>
    private AnnouncementCategory ActiveExdeath => configuration.GetAnnouncements(configuration.AnnouncementChannel).Exdeath;

    /// <summary>
    /// The Chaos announcement configuration for the currently selected channel.
    /// </summary>
    private AnnouncementCategory ActiveChaos => configuration.GetAnnouncements(configuration.AnnouncementChannel).Chaos;

    /// <summary>
    /// Fires the announcements for one category, set and real/fake, sending each to the selected channel.
    /// Ordered mode sends every enabled slot in order (Exdeath) or only the pressed slot (Chaos, via <paramref name="onlySlot"/>); simple mode sends each non-empty line.
    /// </summary>
    private void FireAnnouncements(AnnouncementCategory category, string categoryId, bool isFirst, bool isReal, string? onlySlot)
    {
        if (!configuration.AnnouncementsEnabled || configuration.AnnouncementChronological)
        {
            return;
        }

        var globalChannel = configuration.AnnouncementChannel;
        var leaf = category.GetLeaf(isFirst, isReal);

        if (category.Ordered)
        {
            foreach (var slot in leaf.Slots)
            {
                if (!slot.Enabled)
                {
                    continue;
                }

                // The pressed-mechanic filter (Chaos) applies only to built-in mechanic slots;
                // the title and any custom slots always fire when enabled.
                var isMechanic = slot.Id != "title" && !slot.IsCustom;
                if (onlySlot != null && isMechanic && slot.Id != onlySlot)
                {
                    continue;
                }

                var channel = SlotChannel(slot, globalChannel);
                if (!SlotAllowed(slot.Id, channel))
                {
                    continue;
                }

                if (slot.UseCustomMessage)
                {
                    foreach (var message in slot.Messages)
                    {
                        SendAnnouncement(channel, message);
                    }
                }
                else
                {
                    SendAnnouncement(channel, AnnouncementData.DefaultMessage(categoryId, slot.Id, isFirst, isReal, configuration.AnnouncementShowSetNumber));
                }
            }

            return;
        }

        // Simple mode has no per-mechanic split; treat the whole leaf by category (Chaos is party-safe, Exdeath is not).
        if (!SlotAllowed(categoryId == "chaos" ? "inferno" : "spread", globalChannel))
        {
            return;
        }

        foreach (var line in leaf.SimpleText.Replace("\r", string.Empty).Split('\n'))
        {
            SendAnnouncement(globalChannel, line);
        }
    }

    /// <summary>
    /// Resolves the channel a slot is sent to: its own channel when per-channel announcements are on (Personal Mode) and set, otherwise the selected channel.
    /// </summary>
    private string SlotChannel(AnnouncementSlot slot, string globalChannel)
        => configuration.IsPersonalMode && configuration.PerChannelAnnouncements && !string.IsNullOrEmpty(slot.Channel)
            ? slot.Channel
            : globalChannel;

    /// <summary>
    /// Whether a slot may be sent to a channel under the current mode.
    /// Party Mode only allows party-safe slots (gaze, Inferno, Tsunami). Personal Mode allows everything, but blocks
    /// non-party-safe slots from party (/p) chat unless the party override is enabled.
    /// </summary>
    private bool SlotAllowed(string slotId, string channel)
    {
        var partySafe = AnnouncementData.IsPartySafe(slotId);
        if (!configuration.IsPersonalMode)
        {
            return partySafe;
        }

        if (partySafe)
        {
            return true;
        }

        return channel != "/p" || configuration.PersonalModePartyOverride;
    }

    /// <summary>
    /// Sends one announcement message to a chat channel, ignoring empty messages.
    /// </summary>
    private static void SendAnnouncement(string channel, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        Plugin.ExecuteGameCommand($"{channel} {message.Trim()}");
    }

    /// <summary>
    /// When the chronological summary mode is enabled, sends the whole announcement list to party chat once the
    /// full sequence and both chaos presses are complete. It sends exactly once per pull and re-arms after a reset or undo.
    /// Called every frame from the plugin update.
    /// </summary>
    public void MaybeSendChronological()
    {
        if (!configuration.AnnouncementsEnabled || !configuration.AnnouncementChronological)
        {
            return;
        }

        var complete = phase == Phase.Done && firstSetChaos.HasValue && secondSetChaos.HasValue;
        if (!complete)
        {
            chronoSent = false;
            return;
        }

        if (chronoSent)
        {
            return;
        }

        chronoSent = true;
        SendChronological();
    }

    /// <summary>
    /// Builds and sends the chronological announcement list, in resolution order, to the currently selected channel:
    /// first-set debuffs, 1st gaze, Inferno, second-set debuffs, 2nd gaze, Tsunami.
    /// It reads the selected channel's configured announcement messages.
    /// </summary>
    private void SendChronological()
    {
        var channel = configuration.AnnouncementChannel;
        var announcements = configuration.GetAnnouncements(channel);
        var exdeath = announcements.Exdeath;
        var chaos = announcements.Chaos;

        var messages = new List<string>();
        var showSet = configuration.AnnouncementShowSetNumber;

        // 1. First set Exdeath debuffs (everything enabled except the gaze).
        CollectLeafMessages(messages, exdeath, "exdeath", true, firstExdeathReal, includeGaze: false, includeNonGaze: true, showSet, channel);
        // 2. First gaze.
        CollectLeafMessages(messages, exdeath, "exdeath", true, firstExdeathReal, includeGaze: true, includeNonGaze: false, showSet, channel);
        // 3. Inferno (first set chaos).
        CollectLeafMessages(messages, chaos, "chaos", true, infernoReal, includeGaze: false, includeNonGaze: true, showSet, channel);
        // 4. Second set Exdeath debuffs (everything enabled except the gaze).
        CollectLeafMessages(messages, exdeath, "exdeath", false, secondExdeathReal, includeGaze: false, includeNonGaze: true, showSet, channel);
        // 5. Second gaze.
        CollectLeafMessages(messages, exdeath, "exdeath", false, secondExdeathReal, includeGaze: true, includeNonGaze: false, showSet, channel);
        // 6. Tsunami (second set chaos).
        CollectLeafMessages(messages, chaos, "chaos", false, tsunamiReal, includeGaze: false, includeNonGaze: true, showSet, channel);

        foreach (var message in messages)
        {
            SendAnnouncement(channel, message);
        }
    }

    /// <summary>
    /// Appends one leaf's enabled announcement messages to the chronological list.
    /// The gaze slot is separated out via <paramref name="includeGaze"/>/<paramref name="includeNonGaze"/> so it can be placed after the other debuffs.
    /// Simple-mode leaves have no per-mechanic split, so their text is added only on the non-gaze pass.
    /// </summary>
    private void CollectLeafMessages(List<string> output, AnnouncementCategory category, string categoryId, bool isFirst, bool isReal, bool includeGaze, bool includeNonGaze, bool includeSetNumber, string channel)
    {
        var leaf = category.GetLeaf(isFirst, isReal);
        if (!category.Ordered)
        {
            // Simple mode has no per-mechanic split; allow it by category (Chaos party-safe, Exdeath not) on the non-gaze pass.
            if (!includeNonGaze || !SlotAllowed(categoryId == "chaos" ? "inferno" : "spread", channel))
            {
                return;
            }

            foreach (var line in leaf.SimpleText.Replace("\r", string.Empty).Split('\n'))
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    output.Add(line.Trim());
                }
            }

            return;
        }

        foreach (var slot in leaf.Slots)
        {
            if (!slot.Enabled)
            {
                continue;
            }

            var isGaze = slot.Id == "gaze";
            if (isGaze ? !includeGaze : !includeNonGaze)
            {
                continue;
            }

            if (!SlotAllowed(slot.Id, channel))
            {
                continue;
            }

            if (slot.UseCustomMessage)
            {
                foreach (var message in slot.Messages)
                {
                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        output.Add(message.Trim());
                    }
                }
            }
            else
            {
                var message = AnnouncementData.DefaultMessage(categoryId, slot.Id, isFirst, isReal, includeSetNumber);
                if (!string.IsNullOrWhiteSpace(message))
                {
                    output.Add(message);
                }
            }
        }
    }

    /// <summary>
    /// Resolves a mechanic pick to its callout word.
    /// </summary>
    private static string MechanicText(MechanicKind kind, bool real) => kind switch
    {
        MechanicKind.Lightning => real ? "Spread" : "Stack",
        MechanicKind.Drop => real ? "Stack" : "Spread",
        MechanicKind.Acceleration => real ? "STAND STILL" : "MOVE",
        _ => string.Empty,
    };

    /// <summary>
    /// Clears all solver state and removes any markers this plugin placed on the player.
    /// </summary>
    public void ResetAll()
    {
        firstExdeathReal = false;
        secondExdeathReal = false;
        firstExdeathPressed = false;
        secondExdeathPressed = false;
        phase = Phase.WaitFirstExdeath;
        selections.Clear();

        firstSetChaos = null;
        secondSetChaos = null;
        infernoReal = false;
        tsunamiReal = false;
        chronoSent = false;

        thunderPressed = false;
        thunderReal = false;
        thunderLastFake = false;
        blizzardPressed = false;
        blizzardReal = false;
        blizzardLastFake = false;

        shortMarkerSent = false;
        longMarkerSent = false;

        undoStack.Clear();

        if (configuration.AutoMarker)
        {
            Plugin.ExecuteGameCommand("/mk off <me>");
        }
    }

    /// <summary>
    /// Presses an Exdeath button from a slash command, mirroring the icon button.
    /// </summary>
    public void CommandExdeath(bool real) => OnExdeath(real);

    /// <summary>
    /// Presses a Lightning short or long button from a slash command, ignoring the press when the slot is not currently pickable.
    /// </summary>
    public void CommandLightning(bool isShort) => CommandShortLong(MechanicKind.Lightning, isShort);

    /// <summary>
    /// Presses a Drop short or long button from a slash command, ignoring the press when the slot is not currently pickable.
    /// </summary>
    public void CommandDrop(bool isShort) => CommandShortLong(MechanicKind.Drop, isShort);

    /// <summary>
    /// Presses an Acceleration short or long button from a slash command, ignoring the press when the slot is not currently pickable.
    /// </summary>
    public void CommandAcceleration(bool isShort) => CommandShortLong(MechanicKind.Acceleration, isShort);

    /// <summary>
    /// Applies a short/long pick from a slash command only when the matching button would be enabled.
    /// </summary>
    private void CommandShortLong(MechanicKind kind, bool isShort)
    {
        if (ShortLongEnabled(kind, isShort))
        {
            OnShortLong(kind, isShort);
        }
    }

    /// <summary>
    /// Presses an Inferno button from a slash command, mirroring the real and fake icon buttons.
    /// </summary>
    public void CommandInferno(bool real) => OnChaos(
        configuration.GetText(real ? TextLabels.InfernoReal : TextLabels.InfernoFake), FireColor, "inferno", real);

    /// <summary>
    /// Presses a Tsunami button from a slash command, mirroring the real and fake icon buttons.
    /// </summary>
    public void CommandTsunami(bool real) => OnChaos(
        configuration.GetText(real ? TextLabels.TsunamiReal : TextLabels.TsunamiFake), WaterColor, "tsunami", real);

    /// <summary>
    /// Presses a Thunder button from a slash command, mirroring the real and fake icon buttons.
    /// </summary>
    public void CommandThunder(bool real) => OnThunder(real);

    /// <summary>
    /// Presses a Blizzard button from a slash command, mirroring the real and fake icon buttons.
    /// </summary>
    public void CommandBlizzard(bool real) => OnBlizzard(real);

    /// <summary>
    /// Sets the Thunder Last Fake toggle from a slash command, where fake marks the last as fake.
    /// </summary>
    public void CommandLastThunder(bool fake) => thunderLastFake = fake;

    /// <summary>
    /// Sets the Blizzard Last Fake toggle from a slash command, where fake marks the last as fake.
    /// </summary>
    public void CommandLastBlizzard(bool fake) => blizzardLastFake = fake;
}
