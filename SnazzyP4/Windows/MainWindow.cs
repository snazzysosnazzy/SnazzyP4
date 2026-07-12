using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

namespace SnazzyP4.Windows
{
    /// <summary>
    /// The unified hub window.
    /// It draws the toolbar and, in windowed mode, every positioned section, and it also hosts the shared section-style helpers used by the detached windows.
    /// </summary>
    public class MainWindow : Window, IDisposable
    {
        /// <summary>
        /// The logical (unscaled) default width of the windowed hub.
        /// </summary>
        private const float BaseWidth = 820f;

        /// <summary>
        /// The logical (unscaled) default height of the windowed hub.
        /// </summary>
        private const float BaseHeight = 640f;

        /// <summary>
        /// The owning plugin.
        /// </summary>
        private readonly Plugin plugin;

        /// <summary>
        /// The id of the section currently being dragged in Edit Layout, or empty when none is.
        /// </summary>
        private string draggingSectionId = string.Empty;

        /// <summary>
        /// Creates the hub window.
        /// </summary>
        public MainWindow(Plugin plugin) : base("Snazzy P4###SnazzyP4Main")
        {
            this.plugin = plugin;
        }

        /// <summary>
        /// The plugin configuration.
        /// </summary>
        private Configuration Configuration => plugin.Configuration;

        /// <summary>
        /// The combined global, macro and Dalamud scale multiplier applied to the positioned sections.
        /// </summary>
        private float Scaled => Configuration.UiScale * Configuration.MacroUiScale * ImGuiHelpers.GlobalScale;

        /// <summary>
        /// Disposes the window. There is nothing to release.
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        /// Sets the window flags, background opacity and size before the window is drawn.
        /// </summary>
        public override void PreDraw()
        {
            // The hub always uses the universal appearance values.
            ImGui.SetNextWindowBgAlpha(Configuration.BackgroundAlpha);
            var overlay = OverlayFlags(Configuration.NoTitleBar, Configuration.ClickThrough);
            if (Configuration.Detached || Configuration.Hidden)
            {
                Flags = ImGuiWindowFlags.AlwaysAutoResize | overlay;
            }
            else
            {
                // A fixed default size that the user can resize keeps added resolution text from growing the window on its own.
                Flags = overlay;

                // While editing the layout the hub itself is frozen, otherwise dragging a section (whose text is not an ImGui item) would also drag the whole window.
                if (Configuration.EditMode)
                {
                    Flags |= ImGuiWindowFlags.NoMove;
                }

                Size = new Vector2(BaseWidth, BaseHeight) * Scaled;
                SizeCondition = ImGuiCond.FirstUseEver;
            }
        }

        /// <summary>
        /// Builds the ImGui window flags for the overlay options shared by the hub and detached windows.
        /// </summary>
        public static ImGuiWindowFlags OverlayFlags(bool noTitleBar, bool clickThrough)
        {
            var flags = ImGuiWindowFlags.None;
            if (noTitleBar)
            {
                flags |= ImGuiWindowFlags.NoTitleBar;
            }

            if (clickThrough)
            {
                flags |= ImGuiWindowFlags.NoInputs;
            }

            return flags;
        }

        /// <summary>
        /// Pushes the style used to draw a section at a given opacity.
        /// ImGui's default button and frame backgrounds are translucent, which makes elements look faded even at full opacity, so they are forced to solid and the opacity is applied uniformly through the alpha style variable.
        /// </summary>
        public static void PushSectionStyle(float alpha)
        {
            PushOpaqueColor(ImGuiCol.Button);
            PushOpaqueColor(ImGuiCol.ButtonHovered);
            PushOpaqueColor(ImGuiCol.ButtonActive);
            PushOpaqueColor(ImGuiCol.FrameBg);
            PushOpaqueColor(ImGuiCol.FrameBgHovered);
            PushOpaqueColor(ImGuiCol.FrameBgActive);
            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, alpha);
        }

        /// <summary>
        /// Pops the style pushed by <see cref="PushSectionStyle"/>.
        /// </summary>
        public static void PopSectionStyle()
        {
            ImGui.PopStyleVar();
            ImGui.PopStyleColor(6);
        }

        /// <summary>
        /// Pushes a style colour with its alpha forced to fully opaque.
        /// </summary>
        private static unsafe void PushOpaqueColor(ImGuiCol styleColor)
        {
            var color = *ImGui.GetStyleColorVec4(styleColor);
            color.W = 1f;
            ImGui.PushStyleColor(styleColor, color);
        }

        /// <summary>
        /// Draws the toolbar and, in windowed mode, the positioned sections.
        /// </summary>
        public override void Draw()
        {
            plugin.MaybeShowUpdateNotice();
            ImGui.SetWindowFontScale(Configuration.UiScale * Configuration.ToolbarScale);
            DrawToolbar();

            if (Configuration.Detached)
            {
                return;
            }

            if (!Configuration.Hidden && Configuration.ShowToolbar)
            {
                ImGui.Separator();
            }

            DrawPositionedSections();
        }

        /// <summary>
        /// Draws the toolbar controls, honouring the hidden, collapsed and floating-hide states.
        /// </summary>
        private void DrawToolbar()
        {
            if (Configuration.Hidden)
            {
                // The toolbar provides the unhide button only when the Hide control is not floating.
                if (!Configuration.FloatingHideButton && ImGui.Button("Show"))
                {
                    plugin.Solver.SetHidden(false);
                }

                return;
            }

            if (!Configuration.ShowToolbar)
            {
                return;
            }

            if (Configuration.ToolbarCollapsed)
            {
                if (ImGui.Button(">>##expandbar"))
                {
                    Configuration.ToolbarCollapsed = false;
                    Configuration.Save();
                }

                return;
            }

            if (ImGui.Button("<<##collapsebar"))
            {
                Configuration.ToolbarCollapsed = true;
                Configuration.Save();
            }

            ImGui.SameLine();
            ImGui.TextUnformatted("Snazzy P4");
            ImGui.SameLine();

            if (!Configuration.FloatingHideButton)
            {
                if (ImGui.Button("Hide"))
                {
                    plugin.Solver.SetHidden(true);
                }

                ImGui.SameLine();
            }

            if (ImGui.Button("Settings"))
            {
                plugin.ToggleConfigUi();
            }

            ImGui.SameLine();

            var editMode = Configuration.EditMode;
            if (ImGui.Checkbox("Edit layout", ref editMode))
            {
                plugin.SetEditMode(editMode);
            }

            ImGui.SameLine();

            var detached = Configuration.Detached;
            if (ImGui.Checkbox("Detached", ref detached))
            {
                Configuration.Detached = detached;
                Configuration.Save();
            }

            ImGui.SameLine();

            if (Configuration.Detached)
            {
                var moveAll = Configuration.MoveAllActive;
                if (ImGui.Checkbox("Move All", ref moveAll))
                {
                    plugin.SetMoveAll(moveAll);
                }

                ImGui.SameLine();
            }

            // The Undo control only docks to the toolbar when it is not floating as its own section.
            if (!Configuration.FloatingUndoButton)
            {
                using (ImRaii.Disabled(plugin.LayoutEditActive || !plugin.Solver.CanUndo))
                {
                    if (ImGui.Button("Undo"))
                    {
                        plugin.Solver.Undo();
                    }
                }

                ImGui.SameLine();
            }

            // The Reset control only docks to the toolbar when it is not floating as its own section.
            if (!Configuration.FloatingResetButton)
            {
                using (ImRaii.Disabled(plugin.LayoutEditActive))
                {
                    if (ImGui.Button("Reset"))
                    {
                        plugin.Solver.ResetAll();
                    }
                }
            }
        }

        /// <summary>
        /// Draws each visible section at its stored offset, with an edit drag surface and the display-only overlay as needed.
        /// </summary>
        private void DrawPositionedSections()
        {
            var inputOutput = ImGui.GetIO();
            var scale = Scaled;
            var maxLocal = Vector2.Zero;

            // A section drag ends when the mouse is released; persist the new offset then.
            if (draggingSectionId != string.Empty && !ImGui.IsMouseDown(ImGuiMouseButton.Left))
            {
                Configuration.Save();
                draggingSectionId = string.Empty;
            }

            foreach (var section in plugin.Sections)
            {
                // The Hide, Reset and Undo controls are only drawn here while floating; all other sections hide when the display is hidden.
                var isHide = section.Id == "Hide";
                var isReset = section.Id == "Reset";
                var isUndo = section.Id == "Undo";
                bool draw;
                if (isHide)
                {
                    draw = Configuration.FloatingHideButton;
                }
                else if (isReset)
                {
                    draw = !Configuration.Hidden && Configuration.FloatingResetButton;
                }
                else if (isUndo)
                {
                    draw = !Configuration.Hidden && Configuration.FloatingUndoButton;
                }
                else
                {
                    draw = !Configuration.Hidden;
                }

                if (!draw)
                {
                    continue;
                }

                // Sections disabled by the current configuration are skipped entirely.
                if (!plugin.SectionEnabled(section.Id))
                {
                    continue;
                }

                // A fully resolved button section stays hidden until reset when the hide-resolved option is on.
                if (plugin.Solver.SectionResolvedHidden(section.Id))
                {
                    continue;
                }

                // Empty text sections stay hidden until they have something to show.
                if (!section.HasButtons && !plugin.Solver.SectionHasContent(section.Id))
                {
                    continue;
                }

                // While hidden, the floating Hide button is pinned to the window origin so the unhide control is small and easy to find.
                var offset = (isHide && Configuration.Hidden) ? Vector2.Zero : Configuration.GetOffset(section.Id, section.DefaultOffset);
                var sectionScale = Configuration.GetSectionScale(section.Id);
                ImGui.SetCursorPos(offset * scale);

                using (ImRaii.Group())
                {
                    ImGui.SetWindowFontScale(Configuration.UiScale * Configuration.MacroUiScale * sectionScale);
                    plugin.Solver.CurrentSection = section.Id;
                    plugin.Solver.CurrentFontScale = Configuration.UiScale * Configuration.MacroUiScale * sectionScale;
                    PushSectionStyle(Configuration.EffectiveButtonAlpha(section.Id));
                    section.Draw(scale * sectionScale);
                    PopSectionStyle();
                }

                var sectionMin = ImGui.GetItemRectMin();
                var sectionMax = ImGui.GetItemRectMax();

                // In Edit Layout, a click anywhere on the section starts dragging it,
                // since its buttons are disabled. Later sections win overlapping clicks.
                if (Configuration.EditMode)
                {
                    if (ImGui.IsMouseClicked(ImGuiMouseButton.Left) && ImGui.IsMouseHoveringRect(sectionMin, sectionMax))
                    {
                        draggingSectionId = section.Id;
                    }

                    if (draggingSectionId == section.Id && ImGui.IsMouseDown(ImGuiMouseButton.Left))
                    {
                        offset += inputOutput.MouseDelta / scale;
                        Configuration.SetOffset(section.Id, offset);
                    }

                    ImGui.GetWindowDrawList().AddRect(sectionMin, sectionMax,
                        ImGui.GetColorU32(new Vector4(0.40f, 0.70f, 1.00f, 0.90f)));
                }

                if (Configuration.ClickThrough)
                {
                    DrawDisplayOnlyOverlay(sectionMin, sectionMax);
                }

                // Tracking the furthest extent keeps the window's scroll region reaching every section.
                var localExtent = sectionMax - ImGui.GetWindowPos()
                                  + new Vector2(ImGui.GetScrollX(), ImGui.GetScrollY());
                maxLocal = Vector2.Max(maxLocal, localExtent);
            }

            ImGui.SetCursorPos(maxLocal + new Vector2(0, 8 * scale));
            ImGui.Dummy(Vector2.One);
        }

        /// <summary>
        /// Draws the red display-only warning over a section rectangle while click-through is enabled.
        /// </summary>
        public static void DrawDisplayOnlyOverlay(Vector2 min, Vector2 max)
        {
            const string warning = "DISPLAY ONLY MODE (CHECK SETTINGS)";
            var drawList = ImGui.GetWindowDrawList();
            drawList.AddRectFilled(min, max, ImGui.GetColorU32(new Vector4(0f, 0f, 0f, 0.35f)));

            var textSize = ImGui.CalcTextSize(warning);
            var textPos = min + (max - min - textSize) * 0.5f;
            drawList.AddText(textPos, ImGui.GetColorU32(new Vector4(1f, 0.15f, 0.15f, 1f)), warning);
        }
    }
}
