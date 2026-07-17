using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace SnazzyP4
{
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
        /// <param name="FirstExdeathReal">Whether the first Exdeath was real.</param>
        /// <param name="SecondExdeathReal">Whether the second Exdeath was real.</param>
        /// <param name="FirstExdeathPressed">Whether the first Exdeath button had been pressed.</param>
        /// <param name="SecondExdeathPressed">Whether the second Exdeath button had been pressed.</param>
        /// <param name="Phase">The stage of the input sequence.</param>
        /// <param name="Selections">A copy of the short/long selections made so far.</param>
        /// <param name="FirstSetChaos">The First Set chaos resolution, when Inferno had been pressed.</param>
        /// <param name="SecondSetChaos">The Second Set chaos resolution, when Tsunami had been pressed.</param>
        /// <param name="InfernoReal">Whether the pressed Inferno was the real variant.</param>
        /// <param name="TsunamiReal">Whether the pressed Tsunami was the real variant.</param>
        /// <param name="ThunderPressed">Whether a Thunder button had been pressed.</param>
        /// <param name="ThunderReal">Whether the pressed Thunder was the real variant.</param>
        /// <param name="ThunderLastFake">The Thunder line's Last Fake toggle.</param>
        /// <param name="BlizzardPressed">Whether a Blizzard button had been pressed.</param>
        /// <param name="BlizzardReal">Whether the pressed Blizzard was the real variant.</param>
        /// <param name="BlizzardLastFake">The Blizzard line's Last Fake toggle.</param>
        /// <param name="ShortMarkerSent">Whether the short-set marker had already been placed.</param>
        /// <param name="LongMarkerSent">Whether the long-set marker had already been placed.</param>
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
        /// <param name="configuration">The plugin configuration the solver reads its options from.</param>
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
        /// Whether the hide-resolved-buttons option is currently in effect.
        /// Layout editing always shows every button so the sections can be positioned.
        /// </summary>
        private bool HideResolvedActive => configuration.HideResolvedButtons && !LayoutEditActive;

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
        /// Determines whether a button section is fully resolved and hidden by the hide-resolved-buttons option.
        /// The section reappears once Reset clears the inputs. Text panels and the Last Fake toggles are unaffected.
        /// </summary>
        /// <param name="sectionId">The section id to test.</param>
        /// <returns>True when the option is active and that section's inputs are fully entered.</returns>
        public bool SectionResolvedHidden(string sectionId)
        {
            if (!HideResolvedActive)
            {
                return false;
            }

            return sectionId switch
            {
                "Exdeath" => phase == Phase.Done,
                "Debuffs" or "ShortDebuffs" or "LongDebuffs" => phase == Phase.Done,
                "FireWaterButtons" => firstSetChaos.HasValue && secondSetChaos.HasValue,
                "Inferno" => firstSetChaos.HasValue,
                "Tsunami" => secondSetChaos.HasValue,
                "ThunderButtons" => thunderPressed && blizzardPressed,
                _ => false,
            };
        }

        /// <summary>
        /// Determines whether a section currently has anything to display, which is used to hide empty text panels.
        /// Non-text sections and the layout-edit preview always report content.
        /// </summary>
        /// <param name="sectionId">The section id to test.</param>
        /// <returns>True when the section has something to show.</returns>
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
        /// <param name="isShort">Whether the short (first) set is tested rather than the long one.</param>
        /// <param name="gazeKnown">Whether that set's gaze has been resolved.</param>
        /// <param name="chaosIndex">The set's chaos slot: 0 for Inferno, 1 for Tsunami.</param>
        /// <returns>True when the set has at least one resolved line.</returns>
        private bool SetHasContent(bool isShort, bool gazeKnown, int chaosIndex)
        {
            if (phase == Phase.Done || gazeKnown || ChaosForSet(chaosIndex).HasValue)
            {
                return true;
            }

            // Giga Simple panels only ever show Exdeath and chaos resolutions, which the checks above cover.
            if (configuration.SolverMode == SolverMode.GigaSimple)
            {
                return false;
            }

            foreach (var selection in selections)
            {
                // Simple Mode picks are not tied to a set, so any pick fills both panels.
                if (configuration.SolverMode == SolverMode.Simple || selection.IsShort == isShort)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// A single icon button inside a macro button panel group.
        /// </summary>
        /// <param name="Id">The ImGui id of the button.</param>
        /// <param name="Icon">The icon file drawn on the button.</param>
        /// <param name="Enabled">Whether the button is currently pressable.</param>
        /// <param name="Press">The action run when the button is pressed.</param>
        private readonly record struct PanelButton(string Id, string Icon, bool Enabled, Action Press);

        /// <summary>
        /// One group of a macro button panel, drawn as its own row in the standard layout.
        /// A hidden group keeps its footprint so the remaining buttons do not shift.
        /// </summary>
        /// <param name="Buttons">The group's buttons in their unreversed order.</param>
        /// <param name="Hidden">Whether the group is hidden by the hide-resolved-buttons option.</param>
        private readonly record struct PanelGroup(List<PanelButton> Buttons, bool Hidden);

        /// <summary>
        /// Identifies which debuff buttons a panel hosts.
        /// </summary>
        private enum DebuffColumn
        {
            /// <summary>
            /// The full Classic Mode short/long grid.
            /// </summary>
            Grid,

            /// <summary>
            /// Only the short (first set) column.
            /// </summary>
            ShortOnly,

            /// <summary>
            /// Only the long (second set) column.
            /// </summary>
            LongOnly,

            /// <summary>
            /// The Simple Mode single column.
            /// </summary>
            Single,
        }

        /// <summary>
        /// The debuff mechanics in their display order.
        /// </summary>
        private static readonly MechanicKind[] DebuffOrder = { MechanicKind.Lightning, MechanicKind.Drop, MechanicKind.Acceleration };

        /// <summary>
        /// Draws the Exdeath real/fake buttons and, while they are docked, the mode's debuff buttons below them.
        /// The debuff buttons move to their own panel when the undock setting is on, and Giga Simple Mode has none at all.
        /// </summary>
        /// <param name="scale">The pixel scale applied to the buttons.</param>
        public void DrawExdeath(float scale)
        {
            var exdeathEnabled = phase is Phase.WaitFirstExdeath or Phase.WaitSecondExdeath;
            var labelsHidden = configuration.EffectiveHideLabels(CurrentSection);
            var standard = configuration.GetSectionOrientation(CurrentSection) == PanelOrientation.Standard;

            if (!labelsHidden)
            {
                ImGui.TextUnformatted(configuration.GetText(OnSecondExdeath ? TextLabels.ExdeathSecondHeader : TextLabels.ExdeathFirstHeader));
            }

            if (!labelsHidden && standard)
            {
                DrawColumnHeaders(TextLabels.RealColumnHeader, TextLabels.FakeColumnHeader, scale);
            }

            var groups = new List<PanelGroup>
            {
                new(new List<PanelButton>
                {
                    new("##RealExdeath", "RealExdeath.png", exdeathEnabled, () => OnExdeath(true)),
                    new("##FakeExdeath", "FakeExdeath.png", exdeathEnabled, () => OnExdeath(false)),
                }, false),
            };

            var debuffsDocked = !configuration.SplitExdeathButtons && configuration.SolverMode != SolverMode.GigaSimple;
            var dockedColumn = configuration.SolverMode == SolverMode.Classic ? DebuffColumn.Grid : DebuffColumn.Single;

            // Outside the standard layout the docked debuff buttons join the same single column or row as the pair.
            if (debuffsDocked && !standard)
            {
                groups.AddRange(DebuffGroups(dockedColumn));
                DrawButtonGroups(groups, scale);
                return;
            }

            DrawButtonGroups(groups, scale);
            if (!debuffsDocked)
            {
                return;
            }

            ImGuiHelpers.ScaledDummy(4f);

            if (!labelsHidden && configuration.SolverMode == SolverMode.Classic)
            {
                DrawColumnHeaders(TextLabels.ShortColumnHeader, TextLabels.LongColumnHeader, scale);
            }

            DrawButtonGroups(DebuffGroups(dockedColumn), scale);
        }

        /// <summary>
        /// Draws the undocked debuff buttons panel: the short/long grid in Classic Mode or the single debuff column in Simple Mode.
        /// </summary>
        /// <param name="scale">The pixel scale applied to the buttons.</param>
        public void DrawDebuffButtons(float scale)
        {
            var labelsHidden = configuration.EffectiveHideLabels(CurrentSection);
            if (!labelsHidden)
            {
                ImGui.TextUnformatted(configuration.GetText(TextLabels.DebuffsHeader));
            }

            if (configuration.SolverMode != SolverMode.Classic)
            {
                DrawButtonGroups(DebuffGroups(DebuffColumn.Single), scale);
                return;
            }

            if (!labelsHidden && configuration.GetSectionOrientation(CurrentSection) == PanelOrientation.Standard)
            {
                DrawColumnHeaders(TextLabels.ShortColumnHeader, TextLabels.LongColumnHeader, scale);
            }

            DrawButtonGroups(DebuffGroups(DebuffColumn.Grid), scale);
        }

        /// <summary>
        /// Draws the SHORT column panel used when the undocked debuff grid is split into separate columns.
        /// </summary>
        /// <param name="scale">The pixel scale applied to the buttons.</param>
        public void DrawShortDebuffButtons(float scale)
        {
            if (!configuration.EffectiveHideLabels(CurrentSection))
            {
                ImGui.TextUnformatted(configuration.GetText(TextLabels.ShortColumnHeader));
            }

            DrawButtonGroups(DebuffGroups(DebuffColumn.ShortOnly), scale);
        }

        /// <summary>
        /// Draws the LONG column panel used when the undocked debuff grid is split into separate columns.
        /// </summary>
        /// <param name="scale">The pixel scale applied to the buttons.</param>
        public void DrawLongDebuffButtons(float scale)
        {
            if (!configuration.EffectiveHideLabels(CurrentSection))
            {
                ImGui.TextUnformatted(configuration.GetText(TextLabels.LongColumnHeader));
            }

            DrawButtonGroups(DebuffGroups(DebuffColumn.LongOnly), scale);
        }

        /// <summary>
        /// Builds the debuff button groups for a panel, one group per mechanic.
        /// </summary>
        /// <param name="column">Which debuff buttons the panel hosts.</param>
        /// <returns>The debuff groups in display order.</returns>
        private List<PanelGroup> DebuffGroups(DebuffColumn column)
        {
            var groups = new List<PanelGroup>();
            foreach (var kind in DebuffOrder)
            {
                groups.Add(new PanelGroup(DebuffButtons(kind, column), false));
            }

            return groups;
        }

        /// <summary>
        /// Builds one mechanic's buttons for a panel, keeping the ImGui ids the docked grid has always used.
        /// </summary>
        /// <param name="kind">The mechanic the buttons pick.</param>
        /// <param name="column">Which debuff buttons the panel hosts.</param>
        /// <returns>The mechanic's buttons in their unreversed order.</returns>
        private List<PanelButton> DebuffButtons(MechanicKind kind, DebuffColumn column)
        {
            var icon = $"{kind}.png";
            var enabled = ShortLongEnabled(kind);
            return column switch
            {
                DebuffColumn.Grid => new List<PanelButton>
                {
                    new($"##Short{kind}", icon, enabled, () => OnShortLong(kind, true)),
                    new($"##Long{kind}", icon, enabled, () => OnShortLong(kind, false)),
                },
                DebuffColumn.ShortOnly => new List<PanelButton> { new($"##Short{kind}", icon, enabled, () => OnShortLong(kind, true)) },
                DebuffColumn.LongOnly => new List<PanelButton> { new($"##Long{kind}", icon, enabled, () => OnShortLong(kind, false)) },
                _ => new List<PanelButton> { new($"##Simple{kind}", icon, enabled, () => OnShortLong(kind, true)) },
            };
        }

        /// <summary>
        /// Draws a two-column header row above a pair grid, swapping the sides when the panel's button order is reversed.
        /// </summary>
        /// <param name="firstLabelId">The text label id of the left column header.</param>
        /// <param name="secondLabelId">The text label id of the right column header.</param>
        /// <param name="scale">The pixel scale applied to the buttons the headers sit above.</param>
        private void DrawColumnHeaders(string firstLabelId, string secondLabelId, float scale)
        {
            var style = ImGui.GetStyle();
            var columnStride = IconButtonSize * scale + style.FramePadding.X * 2 + style.ItemSpacing.X;
            var first = configuration.GetText(firstLabelId);
            var second = configuration.GetText(secondLabelId);
            if (configuration.GetSectionReversed(CurrentSection))
            {
                (first, second) = (second, first);
            }

            // SameLine's offset parameter re-adds the group offset, so SetCursorPosX places the second column,
            // keeping it aligned when the section draws inside a positioned group in windowed mode.
            var headerStartX = ImGui.GetCursorPosX();
            ImGui.TextUnformatted(first);
            ImGui.SameLine();
            ImGui.SetCursorPosX(headerStartX + columnStride);
            ImGui.TextUnformatted(second);
        }

        /// <summary>
        /// Draws a macro button panel's groups using the section's configured layout.
        /// The standard layout gives each group its own row, while the vertical and horizontal layouts flatten every button into one column or one row.
        /// Hidden groups keep their footprint in every layout so the remaining buttons do not shift.
        /// </summary>
        /// <param name="groups">The panel's button groups.</param>
        /// <param name="scale">The pixel scale applied to the buttons.</param>
        private void DrawButtonGroups(List<PanelGroup> groups, float scale)
        {
            var orientation = configuration.GetSectionOrientation(CurrentSection);
            var first = true;
            foreach (var group in groups)
            {
                if (orientation == PanelOrientation.Standard)
                {
                    first = true;
                }

                if (group.Hidden)
                {
                    if (!first && orientation == PanelOrientation.Horizontal)
                    {
                        ImGui.SameLine();
                    }

                    DrawHiddenGroupPlaceholder(group.Buttons.Count, orientation, scale);
                    first = false;
                    continue;
                }

                foreach (var button in OrderedButtons(group))
                {
                    if (!first && orientation != PanelOrientation.Vertical)
                    {
                        ImGui.SameLine();
                    }

                    if (IconButton(button.Id, button.Icon, button.Enabled, scale))
                    {
                        button.Press();
                    }

                    first = false;
                }
            }
        }

        /// <summary>
        /// Returns a group's buttons in draw order, swapping them when the section's button order is reversed.
        /// </summary>
        /// <param name="group">The group whose buttons are ordered.</param>
        /// <returns>The buttons in the order they are drawn.</returns>
        private List<PanelButton> OrderedButtons(PanelGroup group)
        {
            if (!configuration.GetSectionReversed(CurrentSection))
            {
                return group.Buttons;
            }

            var reversedButtons = new List<PanelButton>(group.Buttons);
            reversedButtons.Reverse();
            return reversedButtons;
        }

        /// <summary>
        /// Determines whether a short/long button is currently pressable.
        /// A pull assigns one body debuff (Lightning or Drop, never both) and one Acceleration, so a body pick
        /// locks every body button and an Acceleration pick locks both Acceleration buttons until reset.
        /// </summary>
        /// <param name="kind">The mechanic whose buttons are tested.</param>
        /// <returns>True when that mechanic can currently be picked.</returns>
        private bool ShortLongEnabled(MechanicKind kind)
        {
            if (phase is not (Phase.WaitFirstShortLong or Phase.WaitSecondShortLong))
            {
                return false;
            }

            if (kind == MechanicKind.Acceleration)
            {
                return !AccelerationPicked();
            }

            return !BodyPicked();
        }

        /// <summary>
        /// Determines whether a body debuff (Lightning or Drop) has been picked in either set.
        /// </summary>
        /// <returns>True when Lightning or Drop is already in the selections.</returns>
        private bool BodyPicked()
        {
            foreach (var selection in selections)
            {
                if (selection.Kind != MechanicKind.Acceleration)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether an Acceleration has been picked in either set.
        /// </summary>
        /// <returns>True when an Acceleration is already in the selections.</returns>
        private bool AccelerationPicked()
        {
            foreach (var selection in selections)
            {
                if (selection.Kind == MechanicKind.Acceleration)
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
        /// <param name="scale">The pixel scale the hosting window applied.</param>
        public void DrawFirstSet(float scale)
        {
            RenderLines(BuildSetPanel(configuration.GetText(TextLabels.FirstSetLabel), true, firstExdeathPressed, firstExdeathReal, 0), SetAlignment.Left, 0f);
        }

        /// <summary>
        /// Draws the Second Set resolution, which is the long picks with the second gaze and second chaos.
        /// </summary>
        /// <param name="scale">The pixel scale the hosting window applied.</param>
        public void DrawSecondSet(float scale)
        {
            RenderLines(BuildSetPanel(configuration.GetText(TextLabels.SecondSetLabel), false, secondExdeathPressed, secondExdeathReal, 1), SetAlignment.Left, 0f);
        }

        /// <summary>
        /// Draws the First Set and Second Set together in one panel, divided by a line.
        /// The sets stack vertically or sit side by side, and can optionally expand outward from the divider instead of the left edge.
        /// </summary>
        /// <param name="scale">The pixel scale the hosting window applied.</param>
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
                ImGui.GetWindowDrawList().AddLine(new Vector2(dividerScreen.X, dividerScreen.Y),
                                                  new Vector2(dividerScreen.X + stackedWidth, dividerScreen.Y),
                                                  dividerColor,
                                                  dividerThickness);
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
            ImGui.GetWindowDrawList().AddLine(new Vector2(dividerX, top),
                                              new Vector2(dividerX, bottom),
                                              dividerColor,
                                              dividerThickness);
        }

        /// <summary>
        /// Builds a set panel's lines, prefixing the set label unless labels are hidden.
        /// </summary>
        /// <param name="label">The set label shown above the lines.</param>
        /// <param name="isShort">Whether the short (first) set is built rather than the long one.</param>
        /// <param name="gazeKnown">Whether that set's gaze has been resolved.</param>
        /// <param name="gazeReal">Whether that set's gaze was real.</param>
        /// <param name="chaosIndex">The set's chaos slot: 0 for Inferno, 1 for Tsunami.</param>
        /// <returns>The panel's lines as coloured runs, label first.</returns>
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
        /// Builds the resolution lines for one set as coloured runs, dispatching to the active mode's builder.
        /// In Classic Mode the body comes from the short/long picks in this set, while the gaze comes from the matching Exdeath press order, so the first press drives the First Set gaze and the second press drives the Second Set gaze.
        /// </summary>
        /// <param name="isShort">Whether the short (first) set is built rather than the long one.</param>
        /// <param name="gazeKnown">Whether that set's gaze has been resolved.</param>
        /// <param name="gazeReal">Whether that set's gaze was real.</param>
        /// <param name="chaosIndex">The set's chaos slot: 0 for Inferno, 1 for Tsunami.</param>
        /// <returns>The set's resolution lines as coloured runs.</returns>
        private List<List<SetRun>> BuildSetLines(bool isShort, bool gazeKnown, bool gazeReal, int chaosIndex)
        {
            if (configuration.SolverMode == SolverMode.Simple)
            {
                return BuildSimpleSetLines(gazeKnown, gazeReal, chaosIndex);
            }

            if (configuration.SolverMode == SolverMode.GigaSimple)
            {
                return BuildGigaSetLines(gazeKnown, gazeReal, chaosIndex);
            }

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
        /// <param name="chaosIndex">The set's chaos slot: 0 for Inferno, 1 for Tsunami.</param>
        /// <returns>That slot's resolution and colour, or null while unpressed.</returns>
        private (string Text, Vector4 Color)? ChaosForSet(int chaosIndex)
        {
            return chaosIndex == 0 ? firstSetChaos : secondSetChaos;
        }

        /// <summary>
        /// Builds one set's resolution lines for Simple Mode.
        /// The picked debuffs carry no short/long timing, so every pick shows in both panels: the body debuff first, then the Acceleration, followed by the set's gaze and chaos lines.
        /// </summary>
        /// <param name="gazeKnown">Whether that set's gaze has been resolved.</param>
        /// <param name="gazeReal">Whether that set's gaze was real.</param>
        /// <param name="chaosIndex">The set's chaos slot: 0 for Inferno, 1 for Tsunami.</param>
        /// <returns>The set's resolution lines as coloured runs.</returns>
        private List<List<SetRun>> BuildSimpleSetLines(bool gazeKnown, bool gazeReal, int chaosIndex)
        {
            var lines = new List<List<SetRun>>();

            if (LayoutEditActive)
            {
                lines.Add(BuildSimpleBodyLine(MechanicKind.Lightning, true));
                lines.Add(BuildSimpleAccelerationLine(true));
                lines.Add(new List<SetRun> { ColorRun(configuration.GetText(TextLabels.GazeReal), GazeRealColor) });
                lines.Add(chaosIndex == 0
                    ? new List<SetRun> { ColorRun(configuration.GetText(TextLabels.InfernoReal), FireColor) }
                    : new List<SetRun> { ColorRun(configuration.GetText(TextLabels.TsunamiReal), WaterColor) });
                return lines;
            }

            Selection? body = null;
            Selection? acceleration = null;
            foreach (var selection in selections)
            {
                if (selection.Kind == MechanicKind.Acceleration)
                {
                    acceleration = selection;
                }
                else
                {
                    body = selection;
                }
            }

            if (body.HasValue)
            {
                lines.Add(BuildSimpleBodyLine(body.Value.Kind, IsSpread(body.Value)));
            }

            if (acceleration.HasValue)
            {
                lines.Add(BuildSimpleAccelerationLine(acceleration.Value.IsReal));
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

            if (lines.Count == 0)
            {
                lines.Add(new List<SetRun> { DisabledRun("--") });
            }

            return lines;
        }

        /// <summary>
        /// Builds one Simple Mode body line, naming the debuff and resolving it with the player's own role letter.
        /// </summary>
        /// <param name="kind">The picked body debuff, either Lightning or Drop.</param>
        /// <param name="spread">Whether the debuff resolves to a spread rather than a stack.</param>
        /// <returns>The composed line as coloured runs.</returns>
        private List<SetRun> BuildSimpleBodyLine(MechanicKind kind, bool spread)
        {
            var name = configuration.GetText(kind == MechanicKind.Lightning ? TextLabels.LightningName : TextLabels.DropName);
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

            return new List<SetRun>
            {
                PlainRun($"{name} - {configuration.GetText(spread ? TextLabels.SpreadPrefix : TextLabels.StackPrefix)}"),
                ColorRun(letter, color),
            };
        }

        /// <summary>
        /// Builds one Simple Mode Acceleration line using the on-screen stand-still and move labels.
        /// </summary>
        /// <param name="real">Whether the Acceleration resolved on the real branch.</param>
        /// <returns>The composed line as coloured runs.</returns>
        private List<SetRun> BuildSimpleAccelerationLine(bool real)
        {
            var word = configuration.GetText(real ? TextLabels.StandStill : TextLabels.Move);
            return new List<SetRun>
            {
                PlainRun($"{configuration.GetText(TextLabels.AccelerationName)} - "),
                ColorRun(word, AccelerationColor),
            };
        }

        /// <summary>
        /// Builds one set's resolution lines for Giga Simple Mode.
        /// With no debuff buttons, the set's Exdeath press alone resolves every debuff, so the panel lists all three with both roles' letters, then the gaze and chaos lines.
        /// </summary>
        /// <param name="exdeathPressed">Whether that set's Exdeath has been pressed.</param>
        /// <param name="exdeathReal">Whether that set's Exdeath was real.</param>
        /// <param name="chaosIndex">The set's chaos slot: 0 for Inferno, 1 for Tsunami.</param>
        /// <returns>The set's resolution lines as coloured runs.</returns>
        private List<List<SetRun>> BuildGigaSetLines(bool exdeathPressed, bool exdeathReal, int chaosIndex)
        {
            var lines = new List<List<SetRun>>();

            if (LayoutEditActive)
            {
                lines.AddRange(BuildGigaDebuffLines(true));
                lines.Add(new List<SetRun> { ColorRun(configuration.GetText(TextLabels.GazeReal), GazeRealColor) });
                lines.Add(chaosIndex == 0
                    ? new List<SetRun> { ColorRun(configuration.GetText(TextLabels.InfernoReal), FireColor) }
                    : new List<SetRun> { ColorRun(configuration.GetText(TextLabels.TsunamiReal), WaterColor) });
                return lines;
            }

            if (exdeathPressed)
            {
                lines.AddRange(BuildGigaDebuffLines(exdeathReal));
                lines.Add(new List<SetRun>
                {
                    ColorRun(exdeathReal ? configuration.GetText(TextLabels.GazeReal) : configuration.GetText(TextLabels.GazeFake),
                        exdeathReal ? GazeRealColor : GazeFakeColor),
                });
            }

            var chaos = ChaosForSet(chaosIndex);
            if (chaos.HasValue)
            {
                lines.Add(new List<SetRun> { ColorRun(chaos.Value.Text, chaos.Value.Color) });
            }

            if (lines.Count == 0)
            {
                lines.Add(new List<SetRun> { DisabledRun("--") });
            }

            return lines;
        }

        /// <summary>
        /// Builds the three Giga Simple debuff lines for one real/fake branch, in resolution order: Lightning, Drop, Acceleration.
        /// </summary>
        /// <param name="real">Whether the set's Exdeath was real.</param>
        /// <returns>The three debuff lines as coloured runs.</returns>
        private List<List<SetRun>> BuildGigaDebuffLines(bool real)
        {
            return new List<List<SetRun>>
            {
                BuildGigaBodyLine(TextLabels.LightningName, real),
                BuildGigaBodyLine(TextLabels.DropName, !real),
                BuildGigaAccelerationLine(real),
            };
        }

        /// <summary>
        /// Builds one Giga Simple body line, resolving the debuff with both roles' letters, each drawn in its role's colour.
        /// </summary>
        /// <param name="nameLabelId">The text label id of the debuff name.</param>
        /// <param name="spread">Whether the debuff resolves to a spread rather than a stack.</param>
        /// <returns>The composed line as coloured runs.</returns>
        private List<SetRun> BuildGigaBodyLine(string nameLabelId, bool spread)
        {
            var name = configuration.GetText(nameLabelId);
            string supportLetter;
            string dpsLetter;
            Vector4 supportColor;
            Vector4 dpsColor;
            if (spread)
            {
                supportLetter = configuration.GetText(TextLabels.SpreadLetterSupport);
                dpsLetter = configuration.GetText(TextLabels.SpreadLetterDps);
                supportColor = configuration.ColorSpreadSupport;
                dpsColor = configuration.ColorSpreadDps;
            }
            else
            {
                supportLetter = configuration.GetText(TextLabels.StackLetterSupport);
                dpsLetter = configuration.GetText(TextLabels.StackLetterDps);
                supportColor = configuration.ColorStackSupport;
                dpsColor = configuration.ColorStackDps;
            }

            return new List<SetRun>
            {
                PlainRun($"{name} - {configuration.GetText(spread ? TextLabels.SpreadPrefix : TextLabels.StackPrefix)}"),
                ColorRun(supportLetter, supportColor),
                PlainRun("/"),
                ColorRun(dpsLetter, dpsColor),
            };
        }

        /// <summary>
        /// Builds one Giga Simple Acceleration line using the party-facing stillness and motion words.
        /// </summary>
        /// <param name="real">Whether the set's Exdeath was real.</param>
        /// <returns>The composed line as coloured runs.</returns>
        private List<SetRun> BuildGigaAccelerationLine(bool real)
        {
            var word = configuration.GetText(real ? TextLabels.AccelerationStillness : TextLabels.AccelerationMotion);
            return new List<SetRun>
            {
                PlainRun($"{configuration.GetText(TextLabels.AccelerationName)} - "),
                ColorRun(word, AccelerationColor),
            };
        }

        /// <summary>
        /// Builds one spread or stack body line with the role-based target letter, appending the Acceleration word when it shares the line.
        /// Support uses A for stack and D for spread, while DPS uses C for stack and B for spread.
        /// </summary>
        /// <param name="spread">Whether the body resolves to a spread rather than a stack.</param>
        /// <param name="accelerationText">The movement word appended to the line, or null when the set has no Acceleration.</param>
        /// <returns>The composed line as coloured runs.</returns>
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
        /// <param name="lines">The lines to draw, each a list of coloured runs.</param>
        /// <param name="alignment">How each line is placed within the column.</param>
        /// <param name="columnWidth">The column width the alignment is measured against.</param>
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
        /// <param name="line">The line whose runs are measured.</param>
        /// <returns>The rendered width of the line in pixels.</returns>
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
        /// <param name="lines">The lines to measure.</param>
        /// <returns>The widest line's width in pixels.</returns>
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
        /// <param name="text">The text the run displays.</param>
        /// <returns>A run drawn in the default text colour.</returns>
        private static SetRun PlainRun(string text)
        {
            return new(text, default, false, false);
        }

        /// <summary>
        /// Creates a run drawn in a specific colour.
        /// </summary>
        /// <param name="text">The text the run displays.</param>
        /// <param name="color">The colour the run is drawn in.</param>
        /// <returns>A run drawn in the given colour.</returns>
        private static SetRun ColorRun(string text, Vector4 color)
        {
            return new(text, color, true, false);
        }

        /// <summary>
        /// Creates a run drawn in the muted placeholder colour.
        /// </summary>
        /// <param name="text">The text the run displays.</param>
        /// <returns>A run drawn in the disabled text colour.</returns>
        private static SetRun DisabledRun(string text)
        {
            return new(text, default, false, true);
        }

        /// <summary>
        /// Determines whether a selection resolves to a spread.
        /// </summary>
        /// <param name="selection">The selection to classify.</param>
        /// <returns>True when the selection resolves to a spread.</returns>
        private static bool IsSpread(Selection selection)
        {
            return MechanicText(selection.Kind, selection.IsReal) == "Spread";
        }

        /// <summary>
        /// Determines whether a set contains a spread selection.
        /// </summary>
        /// <param name="isShort">Whether the short (first) set is tested rather than the long one.</param>
        /// <returns>True when that set resolves to a spread.</returns>
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
        /// Markers need the short/long timing to pick a set, so they only apply in Classic Mode.
        /// </summary>
        public void UpdateAutoMarkers()
        {
            if (!configuration.AutoMarker || configuration.SolverMode != SolverMode.Classic)
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
        /// <param name="marker">The head marker token, or an empty value to place none.</param>
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
        /// <param name="scale">The pixel scale applied to the button.</param>
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
        /// <param name="scale">The pixel scale applied to the button.</param>
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
        /// <param name="scale">The pixel scale applied to the button.</param>
        public void DrawHideToggle(float scale)
        {
            using (ImRaii.Disabled(LayoutEditActive))
            {
                if (ImGui.Button(configuration.GetText(configuration.Hidden ? TextLabels.ShowButton : TextLabels.HideButton), new Vector2(90, 34) * scale))
                {
                    SetHidden(!configuration.Hidden);
                }
            }
        }

        /// <summary>
        /// Sets the hidden state and persists it. When it becomes hidden and "Reset on Hide" is enabled, it also runs Reset.
        /// All Hide/Show buttons and the /snazzyp4 hide command route through here so the behaviour is consistent.
        /// </summary>
        /// <param name="hidden">Whether the display becomes hidden.</param>
        public void SetHidden(bool hidden)
        {
            if (configuration.Hidden == hidden)
            {
                return;
            }

            configuration.Hidden = hidden;

            // The toolbar follows the display (collapse on hide, expand on show) unless the persistent option keeps its state.
            if (!configuration.PersistToolbarCollapsed)
            {
                configuration.ToolbarCollapsed = hidden;
            }

            configuration.Save();

            if (hidden && configuration.ResetOnHide)
            {
                ResetAll();
            }
        }

        /// <summary>
        /// Draws the Chaos section with the Inferno and Tsunami buttons.
        /// </summary>
        /// <param name="scale">The pixel scale applied to the buttons.</param>
        public void DrawFireWaterButtons(float scale)
        {
            if (!configuration.EffectiveHideLabels(CurrentSection))
            {
                ImGui.TextUnformatted(configuration.GetText(TextLabels.ChaosHeader));
            }

            DrawButtonGroups(new List<PanelGroup> { InfernoGroup(), TsunamiGroup() }, scale);
        }

        /// <summary>
        /// Draws the Inferno pair as its own panel when the Chaos panel is split.
        /// </summary>
        /// <param name="scale">The pixel scale applied to the buttons.</param>
        public void DrawInfernoButtons(float scale)
        {
            if (!configuration.EffectiveHideLabels(CurrentSection))
            {
                ImGui.TextUnformatted(configuration.GetText(TextLabels.InfernoHeader));
            }

            DrawButtonGroups(new List<PanelGroup> { InfernoGroup() }, scale);
        }

        /// <summary>
        /// Draws the Tsunami pair as its own panel when the Chaos panel is split.
        /// </summary>
        /// <param name="scale">The pixel scale applied to the buttons.</param>
        public void DrawTsunamiButtons(float scale)
        {
            if (!configuration.EffectiveHideLabels(CurrentSection))
            {
                ImGui.TextUnformatted(configuration.GetText(TextLabels.TsunamiHeader));
            }

            DrawButtonGroups(new List<PanelGroup> { TsunamiGroup() }, scale);
        }

        /// <summary>
        /// Builds the Inferno pair group.
        /// Inferno resolves once per pull, so the pair disables after its press until the next reset.
        /// </summary>
        /// <returns>The Inferno real/fake pair.</returns>
        private PanelGroup InfernoGroup()
        {
            var infernoEnabled = !firstSetChaos.HasValue;
            return new PanelGroup(new List<PanelButton>
            {
                new("##Inferno", "Inferno.png", infernoEnabled, () => OnChaos(configuration.GetText(TextLabels.InfernoReal), FireColor, "inferno", true)),
                new("##FakeInferno", "FakeInferno.png", infernoEnabled, () => OnChaos(configuration.GetText(TextLabels.InfernoFake), FireColor, "inferno", false)),
            }, HideResolvedActive && !infernoEnabled);
        }

        /// <summary>
        /// Builds the Tsunami pair group.
        /// Tsunami resolves once per pull, so the pair disables after its press until the next reset.
        /// </summary>
        /// <returns>The Tsunami real/fake pair.</returns>
        private PanelGroup TsunamiGroup()
        {
            var tsunamiEnabled = !secondSetChaos.HasValue;
            return new PanelGroup(new List<PanelButton>
            {
                new("##Tsunami", "Tsunami.png", tsunamiEnabled, () => OnChaos(configuration.GetText(TextLabels.TsunamiReal), WaterColor, "tsunami", true)),
                new("##FakeTsunami", "FakeTsunami.png", tsunamiEnabled, () => OnChaos(configuration.GetText(TextLabels.TsunamiFake), WaterColor, "tsunami", false)),
            }, HideResolvedActive && !tsunamiEnabled);
        }

        /// <summary>
        /// Draws the Kefka section with the Thunder and Blizzard buttons.
        /// </summary>
        /// <param name="scale">The pixel scale applied to the buttons.</param>
        public void DrawThunderButtons(float scale)
        {
            if (!configuration.EffectiveHideLabels(CurrentSection))
            {
                ImGui.TextUnformatted(configuration.GetText(TextLabels.KefkaButtonsHeader));
            }

            DrawButtonGroups(new List<PanelGroup> { ThunderGroup(), BlizzardGroup() }, scale);
        }

        /// <summary>
        /// Builds the Thunder pair group.
        /// Thunder locks after its press until the next reset; the Last Fake toggles are a separate control and stay usable regardless.
        /// </summary>
        /// <returns>The Thunder real/fake pair.</returns>
        private PanelGroup ThunderGroup()
        {
            return new PanelGroup(new List<PanelButton>
            {
                new("##Thunder", "Thunder.png", !thunderPressed, () => OnThunder(true)),
                new("##FakeThunder", "FakeThunder.png", !thunderPressed, () => OnThunder(false)),
            }, HideResolvedActive && thunderPressed);
        }

        /// <summary>
        /// Builds the Blizzard pair group.
        /// Blizzard locks after its press until the next reset; the Last Fake toggles are a separate control and stay usable regardless.
        /// </summary>
        /// <returns>The Blizzard real/fake pair.</returns>
        private PanelGroup BlizzardGroup()
        {
            return new PanelGroup(new List<PanelButton>
            {
                new("##Blizzard", "Blizzard.png", !blizzardPressed, () => OnBlizzard(true)),
                new("##FakeBlizzard", "FakeBlizzard.png", !blizzardPressed, () => OnBlizzard(false)),
            }, HideResolvedActive && blizzardPressed);
        }

        /// <summary>
        /// Reserves the exact footprint of a hidden button group so the remaining buttons keep their positions.
        /// </summary>
        /// <param name="buttonCount">How many buttons the hidden group holds.</param>
        /// <param name="orientation">The layout the group would have been drawn in.</param>
        /// <param name="scale">The pixel scale the hidden group would have been drawn at.</param>
        private static void DrawHiddenGroupPlaceholder(int buttonCount, PanelOrientation orientation, float scale)
        {
            var style = ImGui.GetStyle();
            var buttonWidth = IconButtonSize * scale + style.FramePadding.X * 2f;
            var buttonHeight = IconButtonSize * scale + style.FramePadding.Y * 2f;
            if (orientation == PanelOrientation.Vertical)
            {
                ImGui.Dummy(new Vector2(buttonWidth, buttonHeight * buttonCount + style.ItemSpacing.Y * (buttonCount - 1)));
                return;
            }

            ImGui.Dummy(new Vector2(buttonWidth * buttonCount + style.ItemSpacing.X * (buttonCount - 1), buttonHeight));
        }

        /// <summary>
        /// Records a Thunder press (real or fake), capturing an undo point first.
        /// Further presses are ignored until reset, matching the disabled buttons.
        /// </summary>
        /// <param name="real">Whether the real variant was pressed.</param>
        private void OnThunder(bool real)
        {
            if (thunderPressed)
            {
                return;
            }

            PushUndo();
            thunderPressed = true;
            thunderReal = real;
            FireAnnouncements(ActiveKefka, "kefka", true, real, "thunder");
        }

        /// <summary>
        /// Records a Blizzard press (real or fake), capturing an undo point first.
        /// Further presses are ignored until reset, matching the disabled buttons.
        /// </summary>
        /// <param name="real">Whether the real variant was pressed.</param>
        private void OnBlizzard(bool real)
        {
            if (blizzardPressed)
            {
                return;
            }

            PushUndo();
            blizzardPressed = true;
            blizzardReal = real;
            FireAnnouncements(ActiveKefka, "kefka", false, real, "blizzard");
        }

        /// <summary>
        /// Draws the Kefka text panel with the Thunder and Blizzard resolutions.
        /// When the toggles are not detached, the inline Last Fake toggles are drawn beside each line.
        /// </summary>
        /// <param name="scale">The pixel scale the hosting window applied.</param>
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
                    ImGui.SameLine();
                    ImGui.SetCursorPosX(startX + toggleColumn);
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
        /// <param name="idSuffix">The ImGui id suffix for the line's toggle.</param>
        /// <param name="lastFake">The line's Last Fake toggle state.</param>
        /// <param name="buttonReal">Whether the pressed button was the real variant.</param>
        /// <param name="name">The mechanic name shown at the start of the line.</param>
        /// <param name="color">The colour the line is drawn in.</param>
        /// <param name="startX">The cursor position the toggle column is measured from.</param>
        /// <param name="toggleColumn">The distance from the start position to the toggle column.</param>
        /// <param name="drawToggleInline">Whether the Last Fake toggle is drawn beside the line.</param>
        private void DrawKefkaLine(string idSuffix,
                                   ref bool lastFake,
                                   bool buttonReal,
                                   string name,
                                   Vector4 color,
                                   float startX,
                                   float toggleColumn,
                                   bool drawToggleInline)
        {
            var effectiveReal = buttonReal ^ (configuration.ShowLastFake && lastFake);
            ImGui.TextColored(color, $"{name} {configuration.GetText(effectiveReal ? TextLabels.RealWord : TextLabels.FakeWord)}");
            if (drawToggleInline)
            {
                ImGui.SameLine();
                ImGui.SetCursorPosX(startX + toggleColumn);
                DrawToggle(ref lastFake, idSuffix);
            }
        }

        /// <summary>
        /// Draws a sample Kefka line while the layout is being edited, with the inline Last Fake toggle when it is enabled.
        /// </summary>
        /// <param name="idSuffix">The ImGui id suffix for the line's toggle.</param>
        /// <param name="lastFake">The line's Last Fake toggle state.</param>
        /// <param name="sample">The placeholder text shown while editing the layout.</param>
        /// <param name="color">The colour the line is drawn in.</param>
        /// <param name="startX">The cursor position the toggle column is measured from.</param>
        /// <param name="toggleColumn">The distance from the start position to the toggle column.</param>
        /// <param name="drawToggleInline">Whether the Last Fake toggle is drawn beside the line.</param>
        private void DrawKefkaSampleLine(string idSuffix,
                                         ref bool lastFake,
                                         string sample,
                                         Vector4 color,
                                         float startX,
                                         float toggleColumn,
                                         bool drawToggleInline)
        {
            ImGui.TextColored(color, sample);
            if (drawToggleInline)
            {
                ImGui.SameLine();
                ImGui.SetCursorPosX(startX + toggleColumn);
                DrawToggle(ref lastFake, idSuffix);
            }
        }

        /// <summary>
        /// Draws the Last Fake ANNOUNCE button, which sends the current Kefka values to the configured channel.
        /// </summary>
        /// <param name="scale">The pixel scale applied to the button.</param>
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
        /// <param name="pressed">Whether that mechanic's button has been pressed.</param>
        /// <param name="effectiveReal">The line's real/fake value after the Last Fake toggle is applied.</param>
        /// <returns>The configured real or fake text, or a question mark while unpressed.</returns>
        private string KefkaMacroValue(bool pressed, bool effectiveReal)
        {
            if (!pressed)
            {
                return "?";
            }

            return effectiveReal ? configuration.LastFakeAnnounceRealText : configuration.LastFakeAnnounceFakeText;
        }

        /// <summary>
        /// Draws a Last Fake toggle.
        /// It renders as a plain checkbox, or as a green REAL / red FAKE button whose label, size and opacity can be customised.
        /// The toggle is disabled while the layout is being edited so a click drags the panel instead of flipping it.
        /// </summary>
        /// <param name="lastFake">The toggle state the control flips.</param>
        /// <param name="idSuffix">The ImGui id suffix keeping the control unique.</param>
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
        /// <param name="lastFake">The toggle state the label describes.</param>
        /// <returns>The custom label for that state, or the default REAL/FAKE word.</returns>
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
        /// <param name="color">The base colour to shift.</param>
        /// <param name="delta">The amount added to the red, green and blue channels.</param>
        /// <returns>The shifted colour with its channels clamped.</returns>
        private static Vector4 ShiftColor(Vector4 color, float delta)
        {
            return new(Math.Clamp(color.X + delta, 0f, 1f),
                       Math.Clamp(color.Y + delta, 0f, 1f),
                       Math.Clamp(color.Z + delta, 0f, 1f),
                       color.W);
        }

        /// <summary>
        /// Draws both Last Fake toggles for the combined detached panel.
        /// </summary>
        /// <param name="scale">The pixel scale the hosting window applied.</param>
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
        /// <param name="scale">The pixel scale the hosting window applied.</param>
        public void DrawLastFakeThunderToggle(float scale)
        {
            DrawNamedToggle("thunder", ref thunderLastFake, configuration.GetText(TextLabels.ThunderName));
        }

        /// <summary>
        /// Draws the Blizzard Last Fake toggle for its own detached panel.
        /// </summary>
        /// <param name="scale">The pixel scale the hosting window applied.</param>
        public void DrawLastFakeBlizzardToggle(float scale)
        {
            DrawNamedToggle("blizzard", ref blizzardLastFake, configuration.GetText(TextLabels.BlizzardName));
        }

        /// <summary>
        /// Draws a labelled Last Fake toggle for the detached panels.
        /// </summary>
        /// <param name="idSuffix">The ImGui id suffix keeping the control unique.</param>
        /// <param name="lastFake">The toggle state the control flips.</param>
        /// <param name="name">The mechanic name shown next to the toggle.</param>
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
        /// <param name="id">The ImGui id keeping the button unique.</param>
        /// <param name="iconFile">The icon file shown on the button.</param>
        /// <param name="enabled">Whether the button is currently pressable.</param>
        /// <param name="scale">The pixel scale applied to the button.</param>
        /// <returns>True when the button was clicked this frame.</returns>
        private bool IconButton(string id, string iconFile, bool enabled, float scale)
        {
            var size = new Vector2(IconButtonSize, IconButtonSize) * scale;
            using (ImRaii.Disabled(!enabled || LayoutEditActive))
            using (ImRaii.PushId(id))
            {
                // The preloaded lifetime wrap avoids any disk/GPU work at draw time; the shared texture is only a fallback while it loads.
                var texture = Plugin.PreloadedIcon(iconFile)
                              ?? Plugin.TextureProvider.GetFromFile(Plugin.Icon(iconFile)).GetWrapOrDefault();
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
        /// <param name="real">Whether the real variant was pressed.</param>
        private void OnExdeath(bool real)
        {
            // Giga Simple Mode has no debuff picks, so the sequence jumps straight from one Exdeath press to the next.
            var gigaSimple = configuration.SolverMode == SolverMode.GigaSimple;
            if (phase == Phase.WaitFirstExdeath)
            {
                PushUndo();
                firstExdeathReal = real;
                firstExdeathPressed = true;
                phase = gigaSimple ? Phase.WaitSecondExdeath : Phase.WaitFirstShortLong;
                FireAnnouncements(ActiveExdeath, "exdeath", true, real, null);
            }
            else if (phase == Phase.WaitSecondExdeath)
            {
                PushUndo();
                secondExdeathReal = real;
                secondExdeathPressed = true;
                phase = gigaSimple ? Phase.Done : Phase.WaitSecondShortLong;
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
            undoStack.Push(new Snapshot(firstExdeathReal,
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
        /// <param name="kind">The mechanic that was picked.</param>
        /// <param name="isShort">Whether it was picked in the short column.</param>
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
        /// <param name="text">The resolution text shown in the set panel.</param>
        /// <param name="color">The colour the resolution is drawn in.</param>
        /// <param name="slotId">The pressed mechanic, either "inferno" or "tsunami".</param>
        /// <param name="isReal">Whether the real variant was pressed.</param>
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
        /// The Kefka (Thunder/Blizzard) announcement configuration for the currently selected channel.
        /// </summary>
        private AnnouncementCategory ActiveKefka => configuration.GetAnnouncements(configuration.AnnouncementChannel).Kefka;

        /// <summary>
        /// Fires the announcements for one category, set and real/fake, sending each to the selected channel.
        /// Ordered mode sends every enabled slot in order (Exdeath) or only the pressed slot (Chaos, via <paramref name="onlySlot"/>); simple mode sends each non-empty line.
        /// </summary>
        /// <param name="category">The announcement configuration that fires.</param>
        /// <param name="categoryId">The category id, either "exdeath" or "chaos".</param>
        /// <param name="isFirst">Whether the first set's leaf fires rather than the second's.</param>
        /// <param name="isReal">Whether the real branch fires rather than the fake one.</param>
        /// <param name="onlySlot">The only mechanic slot allowed to fire, or null for all of them.</param>
        private void FireAnnouncements(AnnouncementCategory category, string categoryId, bool isFirst, bool isReal, string? onlySlot)
        {
            // Kefka happens outside the debuff resolution window, so it always fires per press even in chronological mode.
            if (!configuration.AnnouncementsEnabled || (configuration.AnnouncementChronological && categoryId != "kefka"))
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
                        var bothRoles = !configuration.IsPersonalMode || channel == "/p";
                        SendAnnouncement(channel, AnnouncementData.DefaultMessage(categoryId, slot.Id, isFirst, isReal, configuration.AnnouncementShowSetNumber,
                                                                                  configuration.SpreadLetters(bothRoles), configuration.StackLetters(bothRoles), bothRoles));
                    }
                }

                return;
            }

            // Simple mode has no per-mechanic split, so the whole leaf is classified by a representative mechanic slot.
            if (!SlotAllowed(RepresentativeSlot(categoryId), globalChannel))
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
        /// <param name="slot">The slot whose channel is resolved.</param>
        /// <param name="globalChannel">The channel selected in the Chat tab.</param>
        /// <returns>The slot's own channel when per-channel routing applies, otherwise the selected one.</returns>
        private string SlotChannel(AnnouncementSlot slot, string globalChannel)
        {
            if (configuration.IsPersonalMode && configuration.PerChannelAnnouncements && !string.IsNullOrEmpty(slot.Channel))
            {
                return slot.Channel;
            }

            return globalChannel;
        }

        /// <summary>
        /// Returns the mechanic slot that stands in for a whole simple-mode leaf when the mode filter runs.
        /// </summary>
        /// <param name="categoryId">The category id: "exdeath", "chaos" or "kefka".</param>
        /// <returns>A built-in mechanic slot id from that category.</returns>
        private static string RepresentativeSlot(string categoryId)
        {
            return categoryId switch
            {
                "chaos" => "inferno",
                "kefka" => "thunder",
                _ => "spread",
            };
        }

        /// <summary>
        /// Whether a slot may be sent to a channel under the current mode.
        /// Party Mode only allows party-safe slots (gaze, Inferno, Tsunami). Personal Mode allows everything, but blocks
        /// non-party-safe slots from party (/p) chat unless the party override is enabled.
        /// </summary>
        /// <param name="slotId">The slot that wants to fire.</param>
        /// <param name="channel">The channel it would be sent to.</param>
        /// <returns>True when the current mode permits that slot on that channel.</returns>
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
        /// <param name="channel">The chat command prefix the message is sent with.</param>
        /// <param name="message">The message text; blank messages are skipped.</param>
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
        /// <param name="output">The list the collected messages are appended to.</param>
        /// <param name="category">The announcement configuration the leaf belongs to.</param>
        /// <param name="categoryId">The category id, either "exdeath" or "chaos".</param>
        /// <param name="isFirst">Whether the first set's leaf is read rather than the second's.</param>
        /// <param name="isReal">Whether the real branch is read rather than the fake one.</param>
        /// <param name="includeGaze">Whether the gaze slot is collected on this pass.</param>
        /// <param name="includeNonGaze">Whether the non-gaze slots are collected on this pass.</param>
        /// <param name="includeSetNumber">Whether generated defaults carry the "[1st]"/"[2nd]" prefix.</param>
        /// <param name="channel">The channel the list will be sent to, used for the mode filter.</param>
        private void CollectLeafMessages(List<string> output, AnnouncementCategory category, string categoryId, bool isFirst, bool isReal, bool includeGaze, bool includeNonGaze, bool includeSetNumber, string channel)
        {
            var leaf = category.GetLeaf(isFirst, isReal);
            if (!category.Ordered)
            {
                // Simple mode has no per-mechanic split, so the leaf is classified by a representative slot on the non-gaze pass.
                if (!includeNonGaze || !SlotAllowed(RepresentativeSlot(categoryId), channel))
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
                    var bothRoles = !configuration.IsPersonalMode || channel == "/p";
                    var message = AnnouncementData.DefaultMessage(categoryId, slot.Id, isFirst, isReal, includeSetNumber,
                                                                  configuration.SpreadLetters(bothRoles), configuration.StackLetters(bothRoles), bothRoles);
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
        /// <param name="kind">The picked mechanic.</param>
        /// <param name="real">Whether the owning Exdeath was real.</param>
        /// <returns>The callout word for that combination.</returns>
        private static string MechanicText(MechanicKind kind, bool real)
        {
            return kind switch
            {
                MechanicKind.Lightning => real ? "Spread" : "Stack",
                MechanicKind.Drop => real ? "Stack" : "Spread",
                MechanicKind.Acceleration => real ? "STAND STILL" : "MOVE",
                _ => string.Empty,
            };
        }

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

            if (configuration.AutoMarker && configuration.SolverMode == SolverMode.Classic)
            {
                Plugin.ExecuteGameCommand("/mk off <me>");
            }
        }

        /// <summary>
        /// Presses an Exdeath button from a slash command, mirroring the icon button.
        /// </summary>
        /// <param name="real">Whether the real variant is pressed.</param>
        public void CommandExdeath(bool real)
        {
            OnExdeath(real);
        }

        /// <summary>
        /// Presses a Lightning short or long button from a slash command, ignoring the press when the slot is not currently pickable.
        /// </summary>
        /// <param name="isShort">Whether the short column is pressed.</param>
        public void CommandLightning(bool isShort)
        {
            CommandShortLong(MechanicKind.Lightning, isShort);
        }

        /// <summary>
        /// Presses a Drop short or long button from a slash command, ignoring the press when the slot is not currently pickable.
        /// </summary>
        /// <param name="isShort">Whether the short column is pressed.</param>
        public void CommandDrop(bool isShort)
        {
            CommandShortLong(MechanicKind.Drop, isShort);
        }

        /// <summary>
        /// Presses an Acceleration short or long button from a slash command, ignoring the press when the slot is not currently pickable.
        /// </summary>
        /// <param name="isShort">Whether the short column is pressed.</param>
        public void CommandAcceleration(bool isShort)
        {
            CommandShortLong(MechanicKind.Acceleration, isShort);
        }

        /// <summary>
        /// Applies a short/long pick from a slash command only in Classic Mode and when the matching button would be enabled.
        /// </summary>
        /// <param name="kind">The mechanic the command picks.</param>
        /// <param name="isShort">Whether the short column is pressed.</param>
        private void CommandShortLong(MechanicKind kind, bool isShort)
        {
            if (configuration.SolverMode != SolverMode.Classic)
            {
                return;
            }

            if (ShortLongEnabled(kind))
            {
                OnShortLong(kind, isShort);
            }
        }

        /// <summary>
        /// Presses the Simple Mode Lightning button from a slash command, ignoring the press outside Simple Mode or while the button is locked.
        /// </summary>
        public void CommandLightning()
        {
            CommandSimplePick(MechanicKind.Lightning);
        }

        /// <summary>
        /// Presses the Simple Mode Drop button from a slash command, ignoring the press outside Simple Mode or while the button is locked.
        /// </summary>
        public void CommandDrop()
        {
            CommandSimplePick(MechanicKind.Drop);
        }

        /// <summary>
        /// Presses the Simple Mode Acceleration button from a slash command, ignoring the press outside Simple Mode or while the button is locked.
        /// </summary>
        public void CommandAcceleration()
        {
            CommandSimplePick(MechanicKind.Acceleration);
        }

        /// <summary>
        /// Applies a Simple Mode debuff pick from a slash command only in Simple Mode and when the matching button would be enabled.
        /// </summary>
        /// <param name="kind">The mechanic the command picks.</param>
        private void CommandSimplePick(MechanicKind kind)
        {
            if (configuration.SolverMode != SolverMode.Simple)
            {
                return;
            }

            if (ShortLongEnabled(kind))
            {
                OnShortLong(kind, true);
            }
        }

        /// <summary>
        /// Presses an Inferno button from a slash command, mirroring the real and fake icon buttons.
        /// </summary>
        /// <param name="real">Whether the real variant is pressed.</param>
        public void CommandInferno(bool real)
        {
            OnChaos(configuration.GetText(real ? TextLabels.InfernoReal : TextLabels.InfernoFake), FireColor, "inferno", real);
        }

        /// <summary>
        /// Presses a Tsunami button from a slash command, mirroring the real and fake icon buttons.
        /// </summary>
        /// <param name="real">Whether the real variant is pressed.</param>
        public void CommandTsunami(bool real)
        {
            OnChaos(configuration.GetText(real ? TextLabels.TsunamiReal : TextLabels.TsunamiFake), WaterColor, "tsunami", real);
        }

        /// <summary>
        /// Presses a Thunder button from a slash command, mirroring the real and fake icon buttons.
        /// </summary>
        /// <param name="real">Whether the real variant is pressed.</param>
        public void CommandThunder(bool real)
        {
            OnThunder(real);
        }

        /// <summary>
        /// Presses a Blizzard button from a slash command, mirroring the real and fake icon buttons.
        /// </summary>
        /// <param name="real">Whether the real variant is pressed.</param>
        public void CommandBlizzard(bool real)
        {
            OnBlizzard(real);
        }

        /// <summary>
        /// Sets the Thunder Last Fake toggle from a slash command, where fake marks the last as fake.
        /// </summary>
        /// <param name="fake">Whether the last Thunder is marked as the fake.</param>
        public void CommandLastThunder(bool fake)
        {
            thunderLastFake = fake;
        }

        /// <summary>
        /// Sets the Blizzard Last Fake toggle from a slash command, where fake marks the last as fake.
        /// </summary>
        /// <param name="fake">Whether the last Blizzard is marked as the fake.</param>
        public void CommandLastBlizzard(bool fake)
        {
            blizzardLastFake = fake;
        }
    }
}
