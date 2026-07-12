using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;

namespace SnazzyP4.Windows
{
    /// <summary>
    /// One floating window per section, used in detached mode.
    /// Windows are only movable while Edit Layout allows individual dragging or Move All performs a collective drag, and otherwise they are frozen in place.
    /// </summary>
    public class SectionWindow : Window, IDisposable
    {
        /// <summary>
        /// The owning plugin.
        /// </summary>
        private readonly Plugin plugin;

        /// <summary>
        /// The section this window renders.
        /// </summary>
        private readonly SectionDef definition;

        /// <summary>
        /// The window position observed on the previous frame, used for the group move follow and position sync.
        /// </summary>
        private Vector2 lastPosition;

        /// <summary>
        /// Whether a position has been observed yet, so the first frame can force the saved position.
        /// </summary>
        private bool positionKnown;

        /// <summary>
        /// Whether this window is currently being dragged individually in Edit Layout.
        /// </summary>
        private bool editDragging;

        /// <summary>
        /// Creates a detached window for a section.
        /// </summary>
        public SectionWindow(Plugin plugin, SectionDef definition)
            : base($"{definition.Name}###SnazzyP4_{definition.Id}")
        {
            this.plugin = plugin;
            this.definition = definition;
        }

        /// <summary>
        /// The section id this window renders.
        /// </summary>
        public string Id => definition.Id;

        /// <summary>
        /// Whether this is the combined set window with its divider pinned to a fixed position.
        /// </summary>
        private bool AnchorDividerActive(Configuration configuration)
        {
            return definition.Id == "CombinedSets"
                   && configuration.CombineSets
                   && configuration.CombineSetsAnchorDivider;
        }

        /// <summary>
        /// Whether this section contains interactive buttons rather than a pure text panel.
        /// </summary>
        public bool HasButtons => definition.HasButtons;

        /// <summary>
        /// Disposes the window. There is nothing to release.
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        /// Sets the window flags, background opacity and forced position before the window is drawn.
        /// </summary>
        public override void PreDraw()
        {
            var configuration = plugin.Configuration;
            ImGui.SetNextWindowBgAlpha(configuration.EffectiveBackgroundAlpha(definition.Id));
            Flags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse
                    | MainWindow.OverlayFlags(configuration.EffectiveNoTitleBar(definition.Id), configuration.ClickThrough);

            // The window is never moved by ImGui's native title-bar drag; individual Edit Layout drags and Move All group drags reposition it manually so that a press anywhere on it counts, since its buttons are disabled.
            Flags |= ImGuiWindowFlags.NoMove;

            // When the combined panel pins its divider, the saved position is the divider point and the window origin is derived from it each frame so the divider stays put while the sets grow outward.
            if (AnchorDividerActive(configuration))
            {
                var divider = configuration.GetDetachedPosition(definition.Id, definition.DefaultOffset);
                if (configuration.EditMode && editDragging)
                {
                    divider += ImGui.GetIO().MouseDelta;
                }
                else if (configuration.MoveAllActive && plugin.GroupMove != Vector2.Zero)
                {
                    divider += plugin.GroupMove;
                }

                configuration.SetDetachedPosition(definition.Id, divider);
                plugin.DetachedPositionDirty.Remove(definition.Id);

                // Side by side the divider is vertical so its X is pinned; stacked the divider is horizontal so its Y is pinned.
                Position = configuration.CombineSetsHorizontal
                    ? new Vector2(divider.X - plugin.Solver.CombinedDividerOffsetX, divider.Y)
                    : new Vector2(divider.X, divider.Y - plugin.Solver.CombinedDividerOffsetY);
                PositionCondition = ImGuiCond.Always;
                return;
            }

            // Force the saved position on first appearance or when a position slider changed it, follow the individual drag in Edit Layout, follow the group during Move All, otherwise leave the position untouched.
            if (!positionKnown || plugin.DetachedPositionDirty.Remove(definition.Id))
            {
                Position = configuration.GetDetachedPosition(definition.Id, definition.DefaultOffset);
                PositionCondition = ImGuiCond.Always;
            }
            else if (configuration.EditMode && editDragging)
            {
                Position = lastPosition + ImGui.GetIO().MouseDelta;
                PositionCondition = ImGuiCond.Always;
            }
            else if (configuration.MoveAllActive && plugin.GroupMove != Vector2.Zero)
            {
                Position = lastPosition + plugin.GroupMove;
                PositionCondition = ImGuiCond.Always;
            }
            else
            {
                Position = null;
            }
        }

        /// <summary>
        /// Draws the section content, keeps the saved detached position in sync and starts a group drag when Move All is on.
        /// </summary>
        public override void Draw()
        {
            var configuration = plugin.Configuration;
            lastPosition = ImGui.GetWindowPos();
            positionKnown = true;

            // Keeping the saved detached position in sync lets the settings sliders track dragging, and it is persisted on mouse release.
            // While the divider is pinned the saved position is the divider point, managed in PreDraw, so it is not overwritten with the window origin here.
            if (!AnchorDividerActive(configuration))
            {
                configuration.SetDetachedPosition(definition.Id, lastPosition);
            }

            var sectionScale = configuration.GetSectionScale(definition.Id);
            ImGui.SetWindowFontScale(configuration.UiScale * configuration.MacroUiScale * sectionScale);
            plugin.Solver.CurrentSection = definition.Id;
            plugin.Solver.CurrentFontScale = configuration.UiScale * configuration.MacroUiScale * sectionScale;
            MainWindow.PushSectionStyle(configuration.EffectiveButtonAlpha(definition.Id));
            definition.Draw(configuration.UiScale * configuration.MacroUiScale * ImGuiHelpers.GlobalScale * sectionScale);
            MainWindow.PopSectionStyle();

            if (configuration.ClickThrough)
            {
                MainWindow.DrawDisplayOnlyOverlay(ImGui.GetWindowPos(), ImGui.GetWindowPos() + ImGui.GetWindowSize());
            }

            // The buttons are disabled during Move All, so a press anywhere on the window counts as the start of a group drag.
            if (configuration.MoveAllActive
                && ImGui.IsWindowHovered()
                && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                plugin.GroupDragActive = true;
            }

            // In Edit Layout the buttons are disabled too, so a press anywhere on the window begins dragging this window alone until the mouse is released.
            if (configuration.EditMode)
            {
                if (ImGui.IsWindowHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    editDragging = true;
                }

                if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
                {
                    editDragging = false;
                }
            }
            else
            {
                editDragging = false;
            }
        }
    }
}
