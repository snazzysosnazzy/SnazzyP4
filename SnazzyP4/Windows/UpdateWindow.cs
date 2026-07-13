using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

namespace SnazzyP4.Windows
{
    /// <summary>
    /// The one-time notice shown the first time the plugin is opened after an update, listing every change since the previous version.
    /// </summary>
    public class UpdateWindow : Window, IDisposable
    {
        /// <summary>
        /// The owning plugin.
        /// </summary>
        private readonly Plugin plugin;

        /// <summary>
        /// Creates the update notice window.
        /// </summary>
        /// <param name="plugin">The owning plugin.</param>
        public UpdateWindow(Plugin plugin) : base("Snazzy P4 - Updated###SnazzyP4Update")
        {
            this.plugin = plugin;
            Size = new Vector2(560f, 560f);
            SizeCondition = ImGuiCond.FirstUseEver;
        }

        /// <summary>
        /// Disposes the window. There is nothing to release.
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        /// Draws the update summary, the scrollable changelog since the previous version and the dismiss controls.
        /// </summary>
        public override void Draw()
        {
            var from = plugin.UpdateFromVersion;
            if (string.IsNullOrEmpty(from))
            {
                ImGui.TextWrapped($"Welcome to Snazzy P4 v{Plugin.Version}! Here is everything the plugin can do, by version:");
            }
            else
            {
                ImGui.TextWrapped($"Snazzy P4 was updated from v{from} to v{Plugin.Version}. Here is everything that changed since your last version:");
            }

            ImGui.Separator();

            using (var child = ImRaii.Child("##updatechanges", new Vector2(0f, -ImGui.GetFrameHeightWithSpacing() - 4f)))
            {
                if (child)
                {
                    ChangelogWindow.DrawEntries(plugin.ChangesSince(from));
                }
            }

            ImGui.Separator();
            if (ImGui.Button("Got it"))
            {
                IsOpen = false;
            }

            ImGui.SameLine();
            if (ImGui.Button("Open full changelog"))
            {
                plugin.ToggleChangelog();
            }

            ImGui.SameLine();
            if (ImGui.Button("Never show version update messages"))
            {
                plugin.Configuration.SuppressUpdateNotices = true;
                plugin.Configuration.Save();
                IsOpen = false;
            }
        }
    }
}
