using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace SnazzyP4.Windows
{
    /// <summary>
    /// A standalone window that lists the detailed changelog for every version, opened from the settings title bar.
    /// </summary>
    public class ChangelogWindow : Window, IDisposable
    {
        /// <summary>
        /// Creates the changelog window.
        /// </summary>
        public ChangelogWindow() : base("Snazzy P4 - Changelog###SnazzyP4Changelog")
        {
            Size = new Vector2(560f, 640f);
            SizeCondition = ImGuiCond.FirstUseEver;
        }

        /// <summary>
        /// Disposes the window. There is nothing to release.
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        /// Draws the full changelog.
        /// </summary>
        public override void Draw()
        {
            DrawEntries(Changelog.Entries);
        }

        /// <summary>
        /// Draws a set of changelog entries, each with its version heading and detailed change lines.
        /// This is shared by the changelog window and the update notice.
        /// </summary>
        public static void DrawEntries(Changelog.Entry[] entries)
        {
            foreach (var entry in entries)
            {
                ImGui.TextColored(new Vector4(1f, 0.84f, 0f, 1f), $"v{entry.Version}");
                if (!string.IsNullOrEmpty(entry.Date))
                {
                    ImGui.SameLine();
                    ImGui.TextDisabled($"- {entry.Date}");
                }

                foreach (var change in entry.Changes)
                {
                    ImGui.TextWrapped($"•  {change}");
                    ImGui.Spacing();
                }

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
            }
        }
    }
}
