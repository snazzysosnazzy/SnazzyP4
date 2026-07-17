using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

namespace SnazzyP4.Windows
{
    /// <summary>
    /// The settings window.
    /// It covers the role, appearance, colours, per-section layout, the Last Fake toggle options and the hidden unlock flow.
    /// </summary>
    public class ConfigWindow : Window, IDisposable
    {
        /// <summary>
        /// The warning shown when the player tries to unlock the Last Fake toggles.
        /// </summary>
        private const string ProceedText =
            "Listen. This plugin was designed to ultimately reduce macro bloat and macro " +
            "resolvement text to be more readable. I never wanted this plugin to resolve " +
            "anything automatically for you and I have tried my best to stay true to that. " +
            "But I get it, this phase is hard. So if you REALLY require this then by all means " +
            "do what you have to do; I am not going to tell you how to play the game. " +
            "With that said: Do you wish to proceed?";

        /// <summary>
        /// The owning plugin.
        /// </summary>
        private readonly Plugin plugin;

        /// <summary>
        /// The text typed into the hidden unlock field, not persisted.
        /// </summary>
        private string hiddenInput = string.Empty;

        /// <summary>
        /// Whether the unlock warning and its buttons are being shown.
        /// </summary>
        private bool showProceedPrompt;

        /// <summary>
        /// Whether the unlock warning should be scrolled into view on the next frame.
        /// </summary>
        private bool scrollToPrompt;

        /// <summary>
        /// The most recent settings profile import or export status message.
        /// </summary>
        private string profileStatus = string.Empty;

        /// <summary>
        /// The most recent chat announcement copy status message.
        /// </summary>
        private string chatStatus = string.Empty;

        /// <summary>
        /// Creates the settings window bound to the plugin.
        /// </summary>
        /// <param name="plugin">The owning plugin.</param>
        public ConfigWindow(Plugin plugin)
            : base("Snazzy P4 Settings###SnazzyP4Config",
                   ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse)
        {
            this.plugin = plugin;

            // A title-bar button opens the full changelog window; Dalamud title-bar buttons are icons, so it shows a changelog icon with a "Changelog" tooltip.
            TitleBarButtons.Add(new TitleBarButton
            {
                Icon = FontAwesomeIcon.ClipboardList,
                IconOffset = new Vector2(2f, 1f),
                Click = _ => plugin.ToggleChangelog(),
                ShowTooltip = () => ImGui.SetTooltip("Changelog"),
            });
        }

        /// <summary>
        /// The plugin configuration.
        /// </summary>
        private Configuration Configuration => plugin.Configuration;

        /// <summary>
        /// Disposes the window. There is nothing to release.
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        /// Draws the full settings window.
        /// </summary>
        public override void Draw()
        {
            DrawTitleAndCredit();

            // The bar id is versioned so ImGui discards any saved tab order (a previous "Layout" tab held a stale slot) and lays tabs out in submission order.
            using var tabs = ImRaii.TabBar("##snazzyp4_settings_tabs_v2");
            if (!tabs)
            {
                return;
            }

            using (var tab = ImRaii.TabItem("General"))
            {
                if (tab)
                {
                    DrawGeneralTab();
                }
            }

            using (var tab = ImRaii.TabItem("Chat"))
            {
                if (tab)
                {
                    DrawChatMessages();
                }
            }

            using (var tab = ImRaii.TabItem("Layout"))
            {
                if (tab)
                {
                    DrawAppearance();
                    ImGui.Separator();
                    DrawSections();
                }
            }

            using (var tab = ImRaii.TabItem("Colors"))
            {
                if (tab)
                {
                    DrawColorSettings();
                }
            }

            using (var tab = ImRaii.TabItem("Text"))
            {
                if (tab)
                {
                    DrawTextSettings();
                }
            }

            using (var tab = ImRaii.TabItem("Controller"))
            {
                if (tab)
                {
                    DrawControllerSettings();
                }
            }

            using (var tab = ImRaii.TabItem("Hidden"))
            {
                if (tab)
                {
                    DrawHiddenTab();
                }
            }
        }

        /// <summary>
        /// Draws the General tab with scale, role, markers, layout options, settings profiles and the reset buttons.
        /// </summary>
        private void DrawGeneralTab()
        {
            DrawSuppressUpdateNotices();
            ImGui.Separator();
            DrawModeSelector();
            ImGui.Separator();
            DrawRole();
            ImGui.Separator();
            DrawAutomationSettings();
            ImGui.Separator();
            DrawLayout();
            ImGui.Separator();
            DrawProfileImportExport();
            ImGui.Separator();
            DrawResetButtons();
        }

        /// <summary>
        /// Draws the optional game-state automation toggles: auto open/close on a captured duty, and reset/hide behaviours.
        /// </summary>
        private void DrawAutomationSettings()
        {
            ImGui.TextUnformatted("Automation");

            var autoDuty = Configuration.AutoOpenCloseOnDuty;
            if (ImGui.Checkbox("Auto Open/Close SnazzyP4 upon Enter/Exit of Duty", ref autoDuty))
            {
                Configuration.AutoOpenCloseOnDuty = autoDuty;
                Configuration.Save();
            }

            Tooltip("Opens the plugin when you enter the captured instance and closes it when you leave.");

            ImGui.Indent();
            var current = Plugin.ClientState.TerritoryType;
            ImGui.TextDisabled(Configuration.AutoDutyTerritoryId == 0
                ? "No instance captured yet. Enter Dancing Mad (Ultimate), then click the button below."
                : $"Trigger instance id: {Configuration.AutoDutyTerritoryId}  (you are currently in zone {current}).");

            if (ImGui.Button("Use current instance as the trigger"))
            {
                Configuration.AutoDutyTerritoryId = current;
                Configuration.Save();
            }

            Tooltip("Captures the zone you are standing in as the trigger instance.");

            if (Configuration.AutoDutyTerritoryId != 0)
            {
                ImGui.SameLine();
                if (ImGui.Button("Clear##autoduty"))
                {
                    Configuration.AutoDutyTerritoryId = 0;
                    Configuration.Save();
                }

                Tooltip("Forgets the captured trigger instance.");
            }

            ImGui.Unindent();

            var resetOnHide = Configuration.ResetOnHide;
            if (ImGui.Checkbox("Reset on Hide Button Press", ref resetOnHide))
            {
                Configuration.ResetOnHide = resetOnHide;
                Configuration.Save();
            }

            Tooltip("Runs Reset whenever the display is hidden.");

            var resetOnWipe = Configuration.ResetOnWipe;
            if (ImGui.Checkbox("Reset on Wipe", ref resetOnWipe))
            {
                Configuration.ResetOnWipe = resetOnWipe;
                Configuration.Save();
            }

            Tooltip("Runs Reset when the party wipes. Wipe detection uses the game's duty state and fires when the whole party is defeated.");

            var hideOnWipe = Configuration.HideOnWipe;
            if (ImGui.Checkbox("Hide on Wipe", ref hideOnWipe))
            {
                Configuration.HideOnWipe = hideOnWipe;
                Configuration.Save();
            }

            Tooltip("Hides the display when the party wipes. Wipe detection uses the game's duty state and fires when the whole party is defeated.");
        }

        /// <summary>
        /// Draws the gold, top-of-tab toggle that stops the update/changelog notice from appearing after each new release.
        /// </summary>
        private void DrawSuppressUpdateNotices()
        {
            var suppress = Configuration.SuppressUpdateNotices;
            using (ImRaii.PushColor(ImGuiCol.Text, new Vector4(1f, 0.84f, 0f, 1f)))
            {
                if (ImGui.Checkbox("Never show version update messages", ref suppress))
                {
                    Configuration.SuppressUpdateNotices = suppress;
                    Configuration.Save();
                }
            }

            Tooltip("Stops the changelog popup after each update. The changelog stays available from the title-bar button.");
        }

        /// <summary>
        /// Draws the Hidden tab with the unlock flow and, once unlocked, the Last Fake toggle options.
        /// </summary>
        private void DrawHiddenTab()
        {
            DrawHiddenSettings();
            DrawLastFakeSettings();
        }

        /// <summary>
        /// Draws the settings profile import and export controls, which move the whole configuration through the clipboard.
        /// </summary>
        private void DrawProfileImportExport()
        {
            ImGui.TextUnformatted("Settings profile");
            ImGui.TextDisabled("Copy your whole setup to share it, or paste one in.");

            if (ImGui.Button("Copy settings to clipboard"))
            {
                ImGui.SetClipboardText(Newtonsoft.Json.JsonConvert.SerializeObject(Configuration, Newtonsoft.Json.Formatting.Indented));
                profileStatus = "Copied settings to the clipboard.";
            }

            ImGui.SameLine();
            if (ImGui.Button("Paste settings from clipboard"))
            {
                ApplyProfileFromClipboard();
            }

            if (!string.IsNullOrEmpty(profileStatus))
            {
                ImGui.TextDisabled(profileStatus);
            }
        }

        /// <summary>
        /// Reads a settings profile from the clipboard and applies it, reporting whether it succeeded.
        /// </summary>
        private void ApplyProfileFromClipboard()
        {
            try
            {
                var imported = Newtonsoft.Json.JsonConvert.DeserializeObject<Configuration>(ImGui.GetClipboardText());
                if (imported == null)
                {
                    profileStatus = "Clipboard did not contain valid settings.";
                    return;
                }

                // A profile can carry a different gameplay mode, so the pull resets under the old mode before the switch.
                if (imported.SolverMode != Configuration.SolverMode)
                {
                    plugin.Solver.ResetAll();
                }

                Configuration.CopyFrom(imported);

                // Giga Simple Mode is disabled while its resolution logic is corrected, so an imported selection falls back to Classic.
                if (Configuration.SolverMode == SolverMode.GigaSimple)
                {
                    Configuration.SolverMode = SolverMode.Classic;
                }

                Configuration.Save();
                plugin.MarkAllPositionsDirty();
                profileStatus = "Imported settings from the clipboard.";
            }
            catch (Exception)
            {
                profileStatus = "Clipboard did not contain valid settings.";
            }
        }

        /// <summary>
        /// Draws the plugin title and the gold credit anchored to the right.
        /// </summary>
        private void DrawTitleAndCredit()
        {
            ImGui.TextUnformatted("Snazzy P4");
            ImGui.SameLine();
            ImGui.TextDisabled($"v{Plugin.Version}");
            var credit = "made by snazz";
            ImGui.SameLine();
            ImGui.SetCursorPosX(ImGui.GetContentRegionMax().X - ImGui.CalcTextSize(credit).X);
            ImGui.TextColored(new Vector4(1f, 0.84f, 0f, 1f), credit);
            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.Separator();
        }

        /// <summary>
        /// Draws the collapsible group of global scale multipliers with their quick preset buttons.
        /// </summary>
        private void DrawUiScale()
        {
            if (!ImGui.CollapsingHeader("Scaling"))
            {
                return;
            }

            DrawScaleSlider("Global UI Scale", "##globalscale", () => Configuration.UiScale, value => Configuration.UiScale = value);
            ImGui.TextDisabled("Multiplies everything the plugin draws.");
            DrawScaleSlider("Toolbar Scale", "##toolbarscale", () => Configuration.ToolbarScale, value => Configuration.ToolbarScale = value);
            ImGui.TextDisabled("Multiplies the quick-settings toolbar, on top of the global scale.");
            DrawScaleSlider("Button UI Scale", "##buttonscale", () => Configuration.ButtonUiScale, value => Configuration.ButtonUiScale = value);
            ImGui.TextDisabled("Multiplies the macro button panels, on top of the global scale.");
            DrawScaleSlider("Text Panel UI Scale", "##textscale", () => Configuration.TextUiScale, value => Configuration.TextUiScale = value);
            ImGui.TextDisabled("Multiplies the text panels, on top of the global scale.");
        }

        /// <summary>
        /// Draws the collapsible group of global opacity multipliers: background, toolbar, buttons and text.
        /// </summary>
        private void DrawOpacitySettings()
        {
            if (!ImGui.CollapsingHeader("Opacity"))
            {
                return;
            }

            DrawFloatSlider("Background Opacity##globalbg", () => Configuration.BackgroundAlpha, value => Configuration.BackgroundAlpha = value, 0f, 1f);
            DrawFloatSlider("Toolbar Opacity##globaltoolbar", () => Configuration.ToolbarAlpha, value => Configuration.ToolbarAlpha = value, 0f, 1f);
            DrawFloatSlider("Button Opacity##globalbutton", () => Configuration.ButtonAlpha, value => Configuration.ButtonAlpha = value, 0f, 1f);
            DrawFloatSlider("Text Opacity##globaltext", () => Configuration.TextAlpha, value => Configuration.TextAlpha = value, 0f, 1f);
            ImGui.TextDisabled("Global multipliers. Each section's own opacity below multiplies on top.");
        }

        /// <summary>
        /// Draws the collapsible group of position sliders: the Move All UI shift, the toolbar position,
        /// and the group shifts for the button and text panels.
        /// </summary>
        private void DrawPositionSettings()
        {
            if (!ImGui.CollapsingHeader("Position"))
            {
                return;
            }

            DrawShiftSliders("Move All UI", "##posall",
                             () => Configuration.GlobalUiOffset,
                             value => Configuration.GlobalUiOffset = value,
                             delta => plugin.ShiftSections(delta, true, true));
            Tooltip("Shifts every section together, exactly like a Move All drag; Move All drags update these values live.");

            DrawToolbarPositionSliders();

            DrawShiftSliders("Button Panels", "##posbuttons",
                             () => Configuration.ButtonPanelsOffset,
                             value => Configuration.ButtonPanelsOffset = value,
                             delta => plugin.ShiftSections(delta, true, false));
            Tooltip("Shifts all the macro button panels together.");

            DrawShiftSliders("Text Panels", "##postext",
                             () => Configuration.TextPanelsOffset,
                             value => Configuration.TextPanelsOffset = value,
                             delta => plugin.ShiftSections(delta, false, true));
            Tooltip("Shifts all the text panels together.");
        }

        /// <summary>
        /// Draws a labelled X/Y slider pair whose changes shift a section group by the difference.
        /// </summary>
        /// <param name="label">The heading shown above the sliders.</param>
        /// <param name="id">The ImGui id keeping the sliders unique.</param>
        /// <param name="getOffset">Reads the accumulated shift.</param>
        /// <param name="setOffset">Writes the accumulated shift.</param>
        /// <param name="shift">Applies a pixel delta to the group.</param>
        private void DrawShiftSliders(string label, string id, Func<Vector2> getOffset, Action<Vector2> setOffset, Action<Vector2> shift)
        {
            var limit = ImGui.GetMainViewport().WorkSize;
            var offset = getOffset();
            ImGui.TextUnformatted(label);

            var offsetX = offset.X;
            ImGui.SetNextItemWidth(220f);
            if (ImGui.SliderFloat($"X{id}", ref offsetX, -limit.X, limit.X, "%.0f"))
            {
                shift(new Vector2(offsetX - offset.X, 0f));
                setOffset(new Vector2(offsetX, offset.Y));
                Configuration.Save();
            }

            var offsetY = offset.Y;
            ImGui.SetNextItemWidth(220f);
            if (ImGui.SliderFloat($"Y{id}", ref offsetY, -limit.Y, limit.Y, "%.0f"))
            {
                shift(new Vector2(0f, offsetY - offset.Y));
                setOffset(new Vector2(offset.X, offsetY));
                Configuration.Save();
            }
        }

        /// <summary>
        /// Draws the X/Y sliders that move the toolbar window, mirroring its live screen position.
        /// </summary>
        private void DrawToolbarPositionSliders()
        {
            var limit = ImGui.GetMainViewport().WorkSize;
            var position = plugin.ToolbarPosition();
            ImGui.TextUnformatted("Toolbar");

            var positionX = position.X;
            ImGui.SetNextItemWidth(220f);
            if (ImGui.SliderFloat("X##postoolbar", ref positionX, 0f, limit.X, "%.0f"))
            {
                plugin.MoveToolbar(new Vector2(positionX, position.Y));
            }

            var positionY = position.Y;
            ImGui.SetNextItemWidth(220f);
            if (ImGui.SliderFloat("Y##postoolbar", ref positionY, 0f, limit.Y, "%.0f"))
            {
                plugin.MoveToolbar(new Vector2(position.X, positionY));
            }

            Tooltip("Moves the toolbar window; the sliders follow it when you drag it by hand.");
        }

        /// <summary>
        /// Draws one labelled scale slider with its percentage presets, persisting the clamped value.
        /// </summary>
        /// <param name="label">The heading shown above the slider.</param>
        /// <param name="id">The ImGui id keeping the slider and its presets unique.</param>
        /// <param name="getScale">Reads the current scale value.</param>
        /// <param name="setScale">Writes the new scale value.</param>
        private void DrawScaleSlider(string label, string id, Func<float> getScale, Action<float> setScale)
        {
            ImGui.TextUnformatted(label);
            var scale = getScale();
            ImGui.SetNextItemWidth(220f);
            if (ImGui.SliderFloat(id, ref scale, 0.5f, 3.0f, "%.2fx"))
            {
                SetScale(setScale, scale);
            }

            using (ImRaii.PushId(id))
            {
                if (ImGui.Button("50%"))
                {
                    SetScale(setScale, 0.5f);
                }

                ImGui.SameLine();
                if (ImGui.Button("100%"))
                {
                    SetScale(setScale, 1.0f);
                }

                ImGui.SameLine();
                if (ImGui.Button("150%"))
                {
                    SetScale(setScale, 1.5f);
                }

                ImGui.SameLine();
                if (ImGui.Button("200%"))
                {
                    SetScale(setScale, 2.0f);
                }
            }
        }

        /// <summary>
        /// Draws the gameplay mode selection.
        /// Switching modes resets the current pull, since the input sequences are not compatible mid-fight.
        /// </summary>
        private void DrawModeSelector()
        {
            ImGui.TextUnformatted("Mode");
            var mode = (int)Configuration.SolverMode;
            var modeChanged = false;
            modeChanged |= ImGui.RadioButton("Classic Mode", ref mode, (int)SolverMode.Classic);
            Tooltip("The full solver: a short and a long button for each Exdeath debuff, exactly as the plugin has always worked.");
            modeChanged |= ImGui.RadioButton("Simple Mode (BETA)", ref mode, (int)SolverMode.Simple);
            Tooltip("One Lightning, one Drop and one Acceleration button with no short/long split. A press locks in the latest Exdeath's real/fake, and your resolutions show in their own Debuffs panel. Yet to be broadly tested.");

            if (modeChanged && (SolverMode)mode != Configuration.SolverMode)
            {
                // The reset runs before the switch so the old mode cleans up its own state, including any placed marker.
                plugin.Solver.ResetAll();
                Configuration.SolverMode = (SolverMode)mode;
                Configuration.Save();
            }
        }

        /// <summary>
        /// Draws the role selection and, in Classic Mode, the auto-marker option.
        /// </summary>
        private void DrawRole()
        {
            ImGui.TextUnformatted("Role");
            var role = Configuration.IsSupport ? 0 : 1;
            var roleChanged = false;
            roleChanged |= ImGui.RadioButton("Support (Ignore1 / Bind1)", ref role, 0);
            Tooltip("Uses the Ignore1/Bind1 markers and the A (stack) / D (spread) target letters.");
            roleChanged |= ImGui.RadioButton("DPS (Ignore2 / Bind2)", ref role, 1);
            Tooltip("Uses the Ignore2/Bind2 markers and the C (stack) / B (spread) target letters.");
            if (roleChanged)
            {
                Configuration.IsSupport = role == 0;
                Configuration.Save();
            }

            // Markers need the short/long timing to pick a set, so the option only exists in Classic Mode.
            if (Configuration.SolverMode != SolverMode.Classic)
            {
                return;
            }

            var autoMarker = Configuration.AutoMarker;
            if (ImGui.Checkbox("Apply Marker on Macro Press", ref autoMarker))
            {
                Configuration.AutoMarker = autoMarker;
                Configuration.Save();
            }

            Tooltip("Runs the same /mk self-mark an in-game macro would when you press the spread macro for your set. Nothing is sent without your button press.");

            if (Configuration.AutoMarker)
            {
                DrawMarkerSettings();
            }
        }

        /// <summary>
        /// The head markers that can be auto-placed, paired with their /mk command token; an empty token places nothing.
        /// </summary>
        private static readonly (string Label, string Token)[] Markers =
        {
            ("(none)", ""),
            ("Attack 1", "attack1"),
            ("Attack 2", "attack2"),
            ("Attack 3", "attack3"),
            ("Attack 4", "attack4"),
            ("Attack 5", "attack5"),
            ("Attack 6", "attack6"),
            ("Attack 7", "attack7"),
            ("Attack 8", "attack8"),
            ("Bind 1", "bind1"),
            ("Bind 2", "bind2"),
            ("Bind 3", "bind3"),
            ("Ignore 1 (prohibited)", "ignore1"),
            ("Ignore 2 (prohibited)", "ignore2"),
            ("Square", "square"),
            ("Circle", "circle"),
            ("Cross", "cross"),
            ("Triangle", "triangle"),
        };

        /// <summary>
        /// Draws the customisable marker dropdowns for each role and set plus the marker target field.
        /// </summary>
        private void DrawMarkerSettings()
        {
            ImGui.Indent();
            ImGui.TextUnformatted("Markers");

            DrawMarkerDropdown("First set - Support", () => Configuration.MarkerFirstSetSupport, value => Configuration.MarkerFirstSetSupport = value);
            DrawMarkerDropdown("Second set - Support", () => Configuration.MarkerSecondSetSupport, value => Configuration.MarkerSecondSetSupport = value);
            DrawMarkerDropdown("First set - DPS", () => Configuration.MarkerFirstSetDps, value => Configuration.MarkerFirstSetDps = value);
            DrawMarkerDropdown("Second set - DPS", () => Configuration.MarkerSecondSetDps, value => Configuration.MarkerSecondSetDps = value);
            ImGui.Unindent();
        }

        /// <summary>
        /// Draws one marker dropdown bound to a getter and setter.
        /// </summary>
        /// <param name="label">The label shown next to the dropdown.</param>
        /// <param name="getToken">Reads the currently selected marker token.</param>
        /// <param name="setToken">Writes the newly selected marker token.</param>
        private void DrawMarkerDropdown(string label, Func<string> getToken, Action<string> setToken)
        {
            var current = getToken();
            var preview = current;
            foreach (var (markerLabel, token) in Markers)
            {
                if (token == current)
                {
                    preview = markerLabel;
                    break;
                }
            }

            ImGui.SetNextItemWidth(200f);
            using var combo = ImRaii.Combo($"{label}##mk_{label}", preview);
            Tooltip("The head marker placed on yourself (<me>) for this role and set. Only your own role's two markers are used; both roles are shown so a shared profile covers everyone.");
            if (!combo)
            {
                return;
            }

            foreach (var (markerLabel, token) in Markers)
            {
                if (ImGui.Selectable(markerLabel, current == token))
                {
                    setToken(token);
                    Configuration.Save();
                }
            }
        }

        /// <summary>
        /// Draws the Chat Messages tab with the channel selector and the gaze and chaos announcement options.
        /// </summary>
        private void DrawChatMessages()
        {
            ImGui.TextUnformatted("Chat Messages");

            var enabled = Configuration.AnnouncementsEnabled;
            if (ImGui.Checkbox("Enable chat announcements", ref enabled))
            {
                Configuration.AnnouncementsEnabled = enabled;
                Configuration.Save();
            }

            Tooltip("Master switch. When off, nothing is sent no matter which announcements are toggled on. Announcements only fire from your own button presses, exactly like an in-game macro.");

            ImGui.Separator();
            DrawAnnouncementModeSection();
            ImGui.Separator();

            var chronological = Configuration.AnnouncementChronological;
            if (ImGui.Checkbox("Chronological summary (one ordered list, sent when everything is pressed)", ref chronological))
            {
                Configuration.AnnouncementChronological = chronological;
                Configuration.Save();
            }

            Tooltip("Holds the per-press announcements back. Once both Exdeaths, both debuff picks and both chaos are pressed, the whole list is sent to the selected channel in resolution order: 1st-set debuffs, 1st gaze, Inferno, 2nd-set debuffs, 2nd gaze, Tsunami. Kefka announcements happen outside that window, so they always fire on their press.");

            var showSetNumber = Configuration.AnnouncementShowSetNumber;
            if (ImGui.Checkbox("Include [1st] / [2nd] prefix in default messages", ref showSetNumber))
            {
                Configuration.AnnouncementShowSetNumber = showSetNumber;
                Configuration.Save();
            }

            Tooltip("Affects the generated default Exdeath messages, for example \"[1st] Lightning - Spread\" versus \"Lightning - Spread\".");
            ImGui.Separator();

            DrawChannelSelector();
            Tooltip("Announcements are stored per channel; the selected channel is the one used when a macro button is pressed.");
            DrawCopyToChannel();
            DrawAnnouncementBulkToggles();
            if (!string.IsNullOrEmpty(chatStatus))
            {
                ImGui.TextDisabled(chatStatus);
            }

            ImGui.Separator();

            var announcements = Configuration.GetAnnouncements(Configuration.AnnouncementChannel);
            DrawAnnounceCategory("Announce Exdeath", announcements.Exdeath, "exdeath");
            DrawAnnounceCategory("Announce Chaos", announcements.Chaos, "chaos");
            DrawAnnounceCategory("Announce Kefka", announcements.Kefka, "kefka");
        }

        /// <summary>
        /// Draws the Party / Personal mode selector, its warnings and the Personal-only options (party override, per-channel).
        /// </summary>
        private void DrawAnnouncementModeSection()
        {
            var green = new Vector4(0.45f, 0.85f, 0.45f, 1f);

            ImGui.TextUnformatted("Mode");

            if (Configuration.ShowPersonalMode)
            {
                var personal = Configuration.PersonalMode;
                if (ImGui.RadioButton("Party Mode", !personal) && personal)
                {
                    Configuration.PersonalMode = false;
                    Configuration.Save();
                }

                Tooltip("Sends only the party-safe callouts: gaze and Inferno/Tsunami.");

                ImGui.SameLine();
                if (ImGui.RadioButton("Personal Mode", personal) && !personal)
                {
                    Configuration.PersonalMode = true;
                    Configuration.Save();
                }

                Tooltip("Adds your debuff, title and custom callouts. They are kept out of /p party chat unless the override below is on; use Party Mode to send to your party.");
            }
            else
            {
                ImGui.TextColored(green, "Party Mode");
                Tooltip("Sends only the party-safe callouts: gaze and Inferno/Tsunami.");
            }

            if (Configuration.IsPersonalMode)
            {
                var perChannel = Configuration.PerChannelAnnouncements;
                if (ImGui.Checkbox("Per-channel announcements (set a channel per announcement)", ref perChannel))
                {
                    Configuration.PerChannelAnnouncements = perChannel;
                    Configuration.Save();
                }

                Tooltip("Lets each announcement pick its own channel instead of the selected one.");

                var over = Configuration.PersonalModePartyOverride;
                if (ImGui.Checkbox("OVERRIDE: allow personal announcements in /p party chat", ref over))
                {
                    Configuration.PersonalModePartyOverride = over;
                    Configuration.Save();
                }

                Tooltip("Allows the personal callouts into /p party chat. This can flood your party's chat; Party Mode is the intended way to send to party.");
            }

            var showPersonal = Configuration.ShowPersonalMode;
            if (ImGui.Checkbox("Show Personal Mode (advanced)", ref showPersonal))
            {
                Configuration.ShowPersonalMode = showPersonal;
                if (!showPersonal)
                {
                    // Hiding the option reverts to Party Mode behaviour.
                    Configuration.PersonalMode = false;
                }

                Configuration.Save();
            }

            Tooltip("Reveals the Personal Mode option next to Party Mode.");
        }

        /// <summary>
        /// Draws the bulk enable/disable buttons for the selected channel: all announcements (except titles) and the set titles.
        /// </summary>
        private void DrawAnnouncementBulkToggles()
        {
            ImGui.TextUnformatted("Quick toggles (this channel)");

            // Size every button to the widest label so the text never clips and the two rows stay aligned.
            var labels = new[] { "Turn on all announcements", "Turn off all announcements", "Turn on set titles", "Turn off set titles" };
            var width = 0f;
            foreach (var text in labels)
            {
                width = Math.Max(width, ImGui.CalcTextSize(text).X);
            }

            var buttonSize = new Vector2(width + ImGui.GetStyle().FramePadding.X * 2f + 4f, 0f);
            if (ImGui.Button("Turn on all announcements", buttonSize))
            {
                SetAllAnnouncementSlots(enabled: true, titlesOnly: false);
            }

            Tooltip("Turns on every announcement in this channel except the set titles.");

            ImGui.SameLine();
            if (ImGui.Button("Turn off all announcements", buttonSize))
            {
                SetAllAnnouncementSlots(enabled: false, titlesOnly: false);
            }

            Tooltip("Turns off every announcement in this channel except the set titles.");

            if (ImGui.Button("Turn on set titles", buttonSize))
            {
                SetAllAnnouncementSlots(enabled: true, titlesOnly: true);
            }

            Tooltip("Turns on the 1st/2nd set title lines in this channel.");

            ImGui.SameLine();
            if (ImGui.Button("Turn off set titles", buttonSize))
            {
                SetAllAnnouncementSlots(enabled: false, titlesOnly: true);
            }

            Tooltip("Turns off the 1st/2nd set title lines in this channel.");
        }

        /// <summary>
        /// Enables or disables announcement slots across both categories and every set/real-fake leaf for the selected channel.
        /// When <paramref name="titlesOnly"/> is true only the title slots are affected; otherwise every non-title slot is.
        /// </summary>
        /// <param name="enabled">Whether the affected slots turn on.</param>
        /// <param name="titlesOnly">Whether only the Exdeath set titles are affected instead of every non-title slot.</param>
        private void SetAllAnnouncementSlots(bool enabled, bool titlesOnly)
        {
            var announcements = Configuration.GetAnnouncements(Configuration.AnnouncementChannel);
            ApplyAnnouncementToggle(announcements.Exdeath, "exdeath", enabled, titlesOnly);

            // The set-title buttons only affect the Exdeath 1st/2nd set titles, not the Chaos or Kefka titles.
            if (!titlesOnly)
            {
                ApplyAnnouncementToggle(announcements.Chaos, "chaos", enabled, titlesOnly);
                ApplyAnnouncementToggle(announcements.Kefka, "kefka", enabled, titlesOnly);
            }

            Configuration.Save();

            chatStatus = titlesOnly
                ? (enabled ? "Turned on the 1st/2nd set titles." : "Turned off the 1st/2nd set titles.")
                : (enabled ? "Turned on all announcements (except titles)." : "Turned off all announcements (except titles).");
        }

        /// <summary>
        /// Applies an enable/disable to the matching slots of every leaf in one category, ensuring the leaf's slots exist first.
        /// </summary>
        /// <param name="category">The category whose leaves are updated.</param>
        /// <param name="categoryId">The category id, either "exdeath" or "chaos".</param>
        /// <param name="enabled">Whether the affected slots turn on.</param>
        /// <param name="titlesOnly">Whether only the title slots are affected instead of every other slot.</param>
        private static void ApplyAnnouncementToggle(AnnouncementCategory category, string categoryId, bool enabled, bool titlesOnly)
        {
            for (var setIndex = 0; setIndex < 2; setIndex++)
            {
                var isFirst = setIndex == 0;
                var slotIds = AnnouncementData.SlotIdsFor(categoryId, isFirst);

                for (var branchIndex = 0; branchIndex < 2; branchIndex++)
                {
                    var leaf = category.GetLeaf(isFirst, branchIndex == 0);
                    AnnouncementData.EnsureSlots(leaf, slotIds);
                    foreach (var slot in leaf.Slots)
                    {
                        if (titlesOnly != (slot.Id == "title"))
                        {
                            continue;
                        }

                        slot.Enabled = enabled;
                    }
                }
            }
        }

        /// <summary>
        /// Draws a "Copy settings to..." dropdown that clones the current channel's announcements to another channel.
        /// </summary>
        private void DrawCopyToChannel()
        {
            ImGui.SetNextItemWidth(260f);
            using var combo = ImRaii.Combo("Copy settings to...##copychan", "Copy settings to another channel");
            Tooltip("Copies this channel's whole announcement setup to another channel.");
            if (!combo)
            {
                return;
            }

            foreach (var (label, prefix) in ChatChannels)
            {
                if (prefix == Configuration.AnnouncementChannel)
                {
                    continue;
                }

                if (ImGui.Selectable(label))
                {
                    var source = Newtonsoft.Json.JsonConvert.SerializeObject(Configuration.GetAnnouncements(Configuration.AnnouncementChannel));
                    Configuration.Announcements[prefix] = Newtonsoft.Json.JsonConvert.DeserializeObject<ChannelAnnouncements>(source) ?? new ChannelAnnouncements();
                    Configuration.Save();
                    chatStatus = $"Copied announcements to {label}.";
                }
            }
        }

        /// <summary>
        /// Draws one announce category (Exdeath or Chaos): its mode toggle and the first/second set sections.
        /// </summary>
        /// <param name="label">The collapsing header text for the category.</param>
        /// <param name="category">The category being edited.</param>
        /// <param name="categoryId">The category id, either "exdeath" or "chaos".</param>
        private void DrawAnnounceCategory(string label, AnnouncementCategory category, string categoryId)
        {
            if (!ImGui.CollapsingHeader($"{label}##cat_{categoryId}"))
            {
                return;
            }

            ImGui.Indent();

            var ordered = category.Ordered;
            if (ImGui.RadioButton($"Ordered list##mode_{categoryId}", ordered) && !ordered)
            {
                category.Ordered = true;
                Configuration.Save();
            }

            Tooltip("A reorderable list of per-mechanic lines, each with its own toggle and optional custom messages.");

            ImGui.SameLine();
            if (ImGui.RadioButton($"Simple text box##mode_{categoryId}", !ordered) && ordered)
            {
                category.Ordered = false;
                Configuration.Save();
            }

            Tooltip("One growing text box per set and real/fake branch; each non-empty line is sent as its own chat message.");

            if (categoryId == "chaos")
            {
                // Chaos sets are static: Inferno always resolves first, Tsunami always second, so the sections are named by mechanic.
                DrawSetSection("Inferno", category, true, categoryId, AnnouncementData.ChaosFirstSlots);
                DrawSetSection("Tsunami", category, false, categoryId, AnnouncementData.ChaosSecondSlots);
            }
            else if (categoryId == "kefka")
            {
                DrawSetSection("Thunder", category, true, categoryId, AnnouncementData.KefkaFirstSlots);
                DrawSetSection("Blizzard", category, false, categoryId, AnnouncementData.KefkaSecondSlots);
            }
            else
            {
                DrawSetSection("First set", category, true, categoryId, AnnouncementData.ExdeathSlots);
                DrawSetSection("Second set", category, false, categoryId, AnnouncementData.ExdeathSlots);
            }

            ImGui.Unindent();
        }

        /// <summary>
        /// Draws a set section (First/Second) with its real and fake leaves.
        /// </summary>
        /// <param name="label">The collapsing header text for the set.</param>
        /// <param name="category">The category the set belongs to.</param>
        /// <param name="isFirst">Whether the first set is drawn rather than the second.</param>
        /// <param name="categoryId">The category id, either "exdeath" or "chaos".</param>
        /// <param name="slotIds">The canonical slot ids for the set.</param>
        private void DrawSetSection(string label, AnnouncementCategory category, bool isFirst, string categoryId, string[] slotIds)
        {
            if (!ImGui.CollapsingHeader($"{label}##set_{categoryId}_{isFirst}"))
            {
                return;
            }

            ImGui.Indent();
            DrawLeaf("Real", category.GetLeaf(isFirst, true), category.Ordered, categoryId, slotIds, isFirst, true);
            DrawLeaf("Fake", category.GetLeaf(isFirst, false), category.Ordered, categoryId, slotIds, isFirst, false);
            ImGui.Unindent();
        }

        /// <summary>
        /// Draws one leaf (a set and real/fake) in either ordered or simple mode.
        /// </summary>
        /// <param name="label">The collapsing header text for the branch.</param>
        /// <param name="leaf">The leaf being edited.</param>
        /// <param name="ordered">Whether the category uses ordered-list mode rather than simple text.</param>
        /// <param name="categoryId">The category id, either "exdeath" or "chaos".</param>
        /// <param name="slotIds">The canonical slot ids for the set.</param>
        /// <param name="isFirst">Whether the leaf belongs to the first set rather than the second.</param>
        /// <param name="isReal">Whether the leaf belongs to the real branch rather than the fake one.</param>
        private void DrawLeaf(string label, AnnouncementLeaf leaf, bool ordered, string categoryId, string[] slotIds, bool isFirst, bool isReal)
        {
            var key = $"{categoryId}_{isFirst}_{isReal}";
            if (!ImGui.CollapsingHeader($"{label}##leaf_{key}"))
            {
                return;
            }

            ImGui.Indent();
            if (ordered)
            {
                DrawOrderedLeaf(leaf, categoryId, slotIds, isFirst, isReal, key);
            }
            else
            {
                DrawSimpleLeaf(leaf, key);
            }

            ImGui.Unindent();
        }

        /// <summary>
        /// Draws the simple-mode growing text box for a leaf.
        /// </summary>
        /// <param name="leaf">The leaf whose simple text is edited.</param>
        /// <param name="key">The ImGui id suffix keeping the text box unique.</param>
        private void DrawSimpleLeaf(AnnouncementLeaf leaf, string key)
        {
            var text = leaf.SimpleText;
            var rows = Math.Max(5, text.Split('\n').Length + 1);
            var size = new Vector2(360f, rows * ImGui.GetTextLineHeight() + ImGui.GetStyle().FramePadding.Y * 2f);
            if (ImGui.InputTextMultiline($"##simple_{key}", ref text, 2048, size))
            {
                leaf.SimpleText = text;
            }

            Tooltip("One chat message per line; empty lines are ignored.");

            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                Configuration.Save();
            }
        }

        /// <summary>
        /// Draws the ordered-mode reorderable announcement slots for a leaf, each with its custom message list.
        /// </summary>
        /// <param name="leaf">The leaf whose slots are edited.</param>
        /// <param name="categoryId">The category id, either "exdeath" or "chaos".</param>
        /// <param name="slotIds">The canonical slot ids for the set.</param>
        /// <param name="isFirst">Whether the leaf belongs to the first set rather than the second.</param>
        /// <param name="isReal">Whether the leaf belongs to the real branch rather than the fake one.</param>
        /// <param name="key">The ImGui id suffix keeping the controls unique.</param>
        private void DrawOrderedLeaf(AnnouncementLeaf leaf, string categoryId, string[] slotIds, bool isFirst, bool isReal, string key)
        {
            AnnouncementData.EnsureSlots(leaf, slotIds);
            var bothRoles = !Configuration.IsPersonalMode || Configuration.AnnouncementChannel == "/p";
            var spreadLetters = Configuration.SpreadLetters(bothRoles);
            var stackLetters = Configuration.StackLetters(bothRoles);
            var moveFrom = -1;
            var moveTo = -1;
            var removeIndex = -1;

            for (var index = 0; index < leaf.Slots.Count; index++)
            {
                var slot = leaf.Slots[index];

                // Party Mode only shows the party-safe slots (gaze, Inferno, Tsunami); the rest are Personal Mode only.
                if (!Configuration.IsPersonalMode && !AnnouncementData.IsPartySafe(slot.Id))
                {
                    continue;
                }

                using (ImRaii.PushId($"{key}_{slot.Id}"))
                {
                    if (ImGui.ArrowButton("up", ImGuiDir.Up) && index > 0)
                    {
                        moveFrom = index;
                        moveTo = index - 1;
                    }

                    ImGui.SameLine();
                    if (ImGui.ArrowButton("down", ImGuiDir.Down) && index < leaf.Slots.Count - 1)
                    {
                        moveFrom = index;
                        moveTo = index + 1;
                    }

                    ImGui.SameLine();
                    var enabled = slot.Enabled;
                    var label = slot.IsCustom ? "Announce (custom message)" : $"Announce {AnnouncementData.SlotLabel(slot.Id)}";
                    if (ImGui.Checkbox(label, ref enabled))
                    {
                        slot.Enabled = enabled;
                        Configuration.Save();
                    }

                    Tooltip("Sends this line to chat when the matching macro button for this set is pressed. The list order is the send order.");

                    if (slot.IsCustom)
                    {
                        ImGui.SameLine();
                        if (ImGui.SmallButton("Remove"))
                        {
                            removeIndex = index;
                        }

                        Tooltip("Deletes this custom message from the list.");
                    }

                    if (Configuration.IsPersonalMode && Configuration.PerChannelAnnouncements)
                    {
                        DrawSlotChannelCombo(slot);
                    }

                    ImGui.Indent();
                    if (slot.IsCustom)
                    {
                        // Custom slots are always custom text; show their message list directly.
                        DrawMessageList(slot);
                    }
                    else if (ImGui.CollapsingHeader("Message settings##msgset"))
                    {
                        var custom = slot.UseCustomMessage;
                        if (ImGui.Checkbox("Enable custom message", ref custom))
                        {
                            slot.UseCustomMessage = custom;
                            if (custom && slot.Messages.Count == 0)
                            {
                                slot.Messages.Add(AnnouncementData.DefaultMessage(categoryId, slot.Id, isFirst, isReal, Configuration.AnnouncementShowSetNumber, spreadLetters, stackLetters, bothRoles));
                            }

                            Configuration.Save();
                        }

                        Tooltip("Replaces the default message with your own reorderable list; blank boxes are skipped.");

                        if (slot.UseCustomMessage)
                        {
                            DrawMessageList(slot);
                        }
                        else
                        {
                            ImGui.TextDisabled($"Default: {AnnouncementData.DefaultMessage(categoryId, slot.Id, isFirst, isReal, Configuration.AnnouncementShowSetNumber, spreadLetters, stackLetters, bothRoles)}");
                        }
                    }

                    ImGui.Unindent();
                }
            }

            // Custom messages are not party-safe, so they are only offered in Personal Mode.
            if (Configuration.IsPersonalMode)
            {
                if (ImGui.Button($"+ Add custom message##addcustom_{key}"))
                {
                    leaf.Slots.Add(AnnouncementData.NewCustomSlot());
                    Configuration.Save();
                }

                Tooltip("Adds your own extra message into the list; it can be reordered and removed like any other entry.");
            }

            if (moveFrom >= 0)
            {
                var moved = leaf.Slots[moveFrom];
                leaf.Slots.RemoveAt(moveFrom);
                leaf.Slots.Insert(moveTo, moved);
                Configuration.Save();
            }

            if (removeIndex >= 0)
            {
                leaf.Slots.RemoveAt(removeIndex);
                Configuration.Save();
            }
        }

        /// <summary>
        /// Draws the reorderable list of custom message boxes for a slot, with add and remove controls.
        /// </summary>
        /// <param name="slot">The slot whose message list is edited.</param>
        private void DrawMessageList(AnnouncementSlot slot)
        {
            if (slot.Messages.Count == 0)
            {
                slot.Messages.Add(string.Empty);
            }

            var moveFrom = -1;
            var moveTo = -1;
            for (var index = 0; index < slot.Messages.Count; index++)
            {
                using (ImRaii.PushId(index))
                {
                    if (ImGui.ArrowButton("mup", ImGuiDir.Up) && index > 0)
                    {
                        moveFrom = index;
                        moveTo = index - 1;
                    }

                    ImGui.SameLine();
                    if (ImGui.ArrowButton("mdown", ImGuiDir.Down) && index < slot.Messages.Count - 1)
                    {
                        moveFrom = index;
                        moveTo = index + 1;
                    }

                    ImGui.SameLine();
                    var message = slot.Messages[index];
                    ImGui.SetNextItemWidth(280f);
                    if (ImGui.InputText("##msg", ref message, 256))
                    {
                        slot.Messages[index] = message;
                    }

                    if (ImGui.IsItemDeactivatedAfterEdit())
                    {
                        Configuration.Save();
                    }
                }
            }

            if (moveFrom >= 0)
            {
                var moved = slot.Messages[moveFrom];
                slot.Messages.RemoveAt(moveFrom);
                slot.Messages.Insert(moveTo, moved);
                Configuration.Save();
            }

            if (ImGui.Button("+##addmsg"))
            {
                slot.Messages.Add(string.Empty);
                Configuration.Save();
            }

            if (slot.Messages.Count > 1)
            {
                ImGui.SameLine();
                if (ImGui.Button("-##delmsg"))
                {
                    slot.Messages.RemoveAt(slot.Messages.Count - 1);
                    Configuration.Save();
                }
            }
        }

        /// <summary>
        /// The chat channels the announcements can be sent to, paired with their command prefix.
        /// </summary>
        private static readonly (string Label, string Prefix)[] ChatChannels =
        {
            ("Party (/p)", "/p"),
            ("Say (/s)", "/s"),
            ("Yell (/y)", "/y"),
            ("Shout (/sh)", "/sh"),
            ("Alliance (/a)", "/a"),
            ("Tell - current target (/tell <t>)", "/tell <t>"),
            ("Free Company (/fc)", "/fc"),
            ("Linkshell 1 (/l1)", "/l1"),
            ("Linkshell 2 (/l2)", "/l2"),
            ("Linkshell 3 (/l3)", "/l3"),
            ("Linkshell 4 (/l4)", "/l4"),
            ("Linkshell 5 (/l5)", "/l5"),
            ("Linkshell 6 (/l6)", "/l6"),
            ("Linkshell 7 (/l7)", "/l7"),
            ("Linkshell 8 (/l8)", "/l8"),
            ("Cross-world Linkshell 1 (/cwl1)", "/cwl1"),
            ("Cross-world Linkshell 2 (/cwl2)", "/cwl2"),
            ("Cross-world Linkshell 3 (/cwl3)", "/cwl3"),
            ("Cross-world Linkshell 4 (/cwl4)", "/cwl4"),
            ("Cross-world Linkshell 5 (/cwl5)", "/cwl5"),
            ("Cross-world Linkshell 6 (/cwl6)", "/cwl6"),
            ("Cross-world Linkshell 7 (/cwl7)", "/cwl7"),
            ("Cross-world Linkshell 8 (/cwl8)", "/cwl8"),
            ("Echo - only you see it (/echo)", "/echo"),
        };

        /// <summary>
        /// The sides the docked ANNOUNCE button can anchor to, paired with their stored value.
        /// </summary>
        private static readonly (string Label, string Value)[] DockSides =
        {
            ("Top", "top"),
            ("Bottom", "bottom"),
            ("Left", "left"),
            ("Right", "right"),
        };

        /// <summary>
        /// Draws the dropdown that selects which side of the Kefka panel the docked ANNOUNCE button anchors to.
        /// </summary>
        private void DrawDockSideCombo()
        {
            var current = Configuration.LastFakeAnnounceDockSide;
            var preview = current;
            foreach (var (label, value) in DockSides)
            {
                if (value == current)
                {
                    preview = label;
                    break;
                }
            }

            ImGui.SetNextItemWidth(160f);
            using var combo = ImRaii.Combo("Dock side##lastfakedockside", preview);
            Tooltip("Which side of the Kefka text panel the docked ANNOUNCE button sits on.");
            if (!combo)
            {
                return;
            }

            foreach (var (label, value) in DockSides)
            {
                if (ImGui.Selectable(label, current == value))
                {
                    Configuration.LastFakeAnnounceDockSide = value;
                    Configuration.Save();
                }
            }
        }

        /// <summary>
        /// Draws a compact per-announcement channel dropdown, with a "(selected channel)" entry that clears the override.
        /// </summary>
        /// <param name="slot">The slot whose channel override is edited.</param>
        private void DrawSlotChannelCombo(AnnouncementSlot slot)
        {
            var current = slot.Channel;
            var preview = "(selected channel)";
            foreach (var (channelLabel, prefix) in ChatChannels)
            {
                if (prefix == current && !string.IsNullOrEmpty(current))
                {
                    preview = channelLabel;
                    break;
                }
            }

            ImGui.SetNextItemWidth(240f);
            using var combo = ImRaii.Combo("Channel##slotchan", preview);
            Tooltip("Sends this announcement to its own channel; \"(selected channel)\" follows the channel picked at the top of the tab.");
            if (!combo)
            {
                return;
            }

            if (ImGui.Selectable("(selected channel)", string.IsNullOrEmpty(current)))
            {
                slot.Channel = string.Empty;
                Configuration.Save();
            }

            foreach (var (channelLabel, prefix) in ChatChannels)
            {
                if (ImGui.Selectable(channelLabel, current == prefix))
                {
                    slot.Channel = prefix;
                    Configuration.Save();
                }
            }
        }

        /// <summary>
        /// Draws the dropdown that selects which chat channel the announcements are sent to.
        /// </summary>
        private void DrawChannelSelector()
        {
            DrawChannelCombo("Channel##announcechannel", () => Configuration.AnnouncementChannel, value => Configuration.AnnouncementChannel = value);
        }

        /// <summary>
        /// Draws a chat channel dropdown bound to a getter and setter.
        /// </summary>
        /// <param name="label">The label shown next to the dropdown.</param>
        /// <param name="getChannel">Reads the currently selected channel prefix.</param>
        /// <param name="setChannel">Writes the newly selected channel prefix.</param>
        private void DrawChannelCombo(string label, Func<string> getChannel, Action<string> setChannel)
        {
            var current = getChannel();
            var preview = current;
            foreach (var (channelLabel, prefix) in ChatChannels)
            {
                if (prefix == current)
                {
                    preview = channelLabel;
                    break;
                }
            }

            ImGui.SetNextItemWidth(260f);
            using var combo = ImRaii.Combo(label, preview);
            if (!combo)
            {
                return;
            }

            foreach (var (channelLabel, prefix) in ChatChannels)
            {
                if (ImGui.Selectable(channelLabel, current == prefix))
                {
                    setChannel(prefix);
                    Configuration.Save();
                }
            }
        }

        /// <summary>
        /// Draws the layout mode toggles.
        /// </summary>
        private void DrawLayout()
        {
            ImGui.TextUnformatted("General Settings");

            var showToolbar = Configuration.ShowToolbar;
            if (ImGui.Checkbox("Show toolbar (Edit / Detached / Move All / Reset)", ref showToolbar))
            {
                Configuration.ShowToolbar = showToolbar;
                Configuration.Save();
            }

            Tooltip("Shows the quick toolbar with the Hide, Settings, Edit layout, Detached, Move All, Undo and Reset controls.");

            var hideToolbarWhenHidden = Configuration.HideToolbarWhenHidden;
            if (ImGui.Checkbox("Hide Toolbar when UI is hidden", ref hideToolbarWhenHidden))
            {
                Configuration.HideToolbarWhenHidden = hideToolbarWhenHidden;
                Configuration.Save();
            }

            Tooltip("Hides the toolbar completely while the display is hidden, instead of showing it collapsed. Ignored while the Floating Hide button is off, since the toolbar is then the only way to bring the display back.");

            var persistCollapsed = Configuration.PersistToolbarCollapsed;
            if (ImGui.Checkbox("Persistent Toolbar Collapsed State", ref persistCollapsed))
            {
                Configuration.PersistToolbarCollapsed = persistCollapsed;
                Configuration.Save();
            }

            Tooltip("Keeps the toolbar's collapsed state unchanged when Hide/Show is pressed; only the arrows change it.");

            var hideAllLabels = Configuration.HideLabels;
            if (ImGui.Checkbox("Hide all name labels", ref hideAllLabels))
            {
                Configuration.HideLabels = hideAllLabels;
                Configuration.Save();
            }

            Tooltip("Hides every header and name label. While on, the per-section switches in the Layout tab are overridden and disabled.");

            var hideAllTitleBars = Configuration.NoTitleBar;
            if (ImGui.Checkbox("Hide all title bars", ref hideAllTitleBars))
            {
                Configuration.NoTitleBar = hideAllTitleBars;
                Configuration.Save();
            }

            Tooltip("Hides every window title bar. While on, the per-section switches in the Layout tab are overridden and disabled.");

            var detached = Configuration.Detached;
            if (ImGui.Checkbox("Detached windows (each section is its own window)", ref detached))
            {
                Configuration.Detached = detached;
                Configuration.Save();
            }

            Tooltip("Splits the display into one floating window per section instead of one hub window.");

            var editMode = Configuration.EditMode;
            if (ImGui.Checkbox("Edit layout (click a section to drag it)", ref editMode))
            {
                plugin.SetEditMode(editMode);
            }

            Tooltip("Fills every panel with sample text and lets you drag sections to reposition them. The buttons are locked while it is on.");

            if (Configuration.Detached)
            {
                var moveAll = Configuration.MoveAllActive;
                if (ImGui.Checkbox("Move All (drag any window to move them together)", ref moveAll))
                {
                    plugin.SetMoveAll(moveAll);
                }

                Tooltip("Drags every detached window together so the whole layout keeps its shape.");
            }

            var floatingHide = Configuration.FloatingHideButton;
            if (ImGui.Checkbox("Floating Hide button (floats as its own panel, otherwise docks to the toolbar)", ref floatingHide))
            {
                Configuration.FloatingHideButton = floatingHide;
                Configuration.Save();
            }

            Tooltip("Turns the Hide/Show control into its own repositionable panel.");

            var floatingReset = Configuration.FloatingResetButton;
            if (ImGui.Checkbox("Floating Reset button (floats as its own panel, otherwise docks to the toolbar)", ref floatingReset))
            {
                Configuration.FloatingResetButton = floatingReset;
                Configuration.Save();
            }

            Tooltip("Turns the Reset button into its own repositionable panel.");

            var floatingUndo = Configuration.FloatingUndoButton;
            if (ImGui.Checkbox("Floating Undo button (floats as its own panel, otherwise docks to the toolbar)", ref floatingUndo))
            {
                Configuration.FloatingUndoButton = floatingUndo;
                Configuration.Save();
            }

            Tooltip("Turns the Undo button into its own repositionable panel.");

            var hideResolved = Configuration.HideResolvedButtons;
            if (ImGui.Checkbox("Hide Resolved Buttons Until Reset", ref hideResolved))
            {
                Configuration.HideResolvedButtons = hideResolved;
                Configuration.Save();
            }

            Tooltip("Hides a button group once fully entered: Exdeath after both sets resolve, each chaos pair and each Kefka pair once pressed. Everything returns on Reset. Text panels and the Last Fake toggles are never affected.");

            // Only Classic Mode shares a line between the body and the Acceleration; the other modes give every debuff its own line.
            if (Configuration.SolverMode == SolverMode.Classic)
            {
                var accelerationSameLine = Configuration.AccelerationSameLine;
                if (ImGui.Checkbox("Acceleration text on same line as Stack/Spread", ref accelerationSameLine))
                {
                    Configuration.AccelerationSameLine = accelerationSameLine;
                    Configuration.Save();
                }

                Tooltip("Appends the movement word to the spread or stack line, for example \"Spread on X and MOVE\".");
            }

            // Giga Simple Mode has no debuff buttons, so there is nothing to undock there.
            if (Configuration.SolverMode != SolverMode.GigaSimple)
            {
                var splitExdeath = Configuration.SplitExdeathButtons;
                if (ImGui.Checkbox("Undock debuff buttons from Exdeath", ref splitExdeath))
                {
                    Configuration.SplitExdeathButtons = splitExdeath;
                    Configuration.Save();
                }

                Tooltip("Moves the Lightning/Drop/Acceleration buttons into their own repositionable panel instead of sitting under the real/fake Exdeath pair.");

                if (Configuration.SplitExdeathButtons && Configuration.SolverMode == SolverMode.Classic)
                {
                    ImGui.Indent();
                    var splitColumns = Configuration.SplitDebuffColumns;
                    if (ImGui.Checkbox("Split SHORT and LONG columns into separate panels", ref splitColumns))
                    {
                        Configuration.SplitDebuffColumns = splitColumns;
                        Configuration.Save();
                    }

                    Tooltip("Further splits the undocked debuff grid into a SHORT panel and a LONG panel that move independently.");
                    ImGui.Unindent();
                }
            }

            var splitChaos = Configuration.SplitChaosButtons;
            if (ImGui.Checkbox("Split Chaos into Inferno and Tsunami panels", ref splitChaos))
            {
                Configuration.SplitChaosButtons = splitChaos;
                Configuration.Save();
            }

            Tooltip("Gives the Inferno and Tsunami pairs their own repositionable panels instead of one Chaos panel.");

            var combineSets = Configuration.CombineSets;
            if (ImGui.Checkbox("Combine First and Second set into one panel", ref combineSets))
            {
                Configuration.CombineSets = combineSets;
                Configuration.Save();
            }

            Tooltip("Shows both sets in one panel, stacked or side by side, divided by a line.");

            if (Configuration.CombineSets)
            {
                ImGui.Indent();
                var combineHorizontal = Configuration.CombineSetsHorizontal;
                if (ImGui.Checkbox("Horizontal orientation (side by side instead of stacked)", ref combineHorizontal))
                {
                    Configuration.CombineSetsHorizontal = combineHorizontal;
                    Configuration.Save();
                }

                Tooltip("Places the two sets side by side instead of one above the other.");

                if (Configuration.CombineSetsHorizontal)
                {
                    ImGui.Indent();
                    var anchorDivider = Configuration.CombineSetsAnchorDivider;
                    if (ImGui.Checkbox("Pin the divider in place (sets grow outward from it, text stays left-aligned)", ref anchorDivider))
                    {
                        Configuration.CombineSetsAnchorDivider = anchorDivider;
                        Configuration.Save();
                    }

                    Tooltip("Keeps the divider at a fixed position while the sets grow outward. Drag the panel or use the CombinedSets X/Y sliders to move the divider.");
                    ImGui.Unindent();
                }

                var mirror = Configuration.CombineSetsExpandFromCenter;
                if (ImGui.Checkbox("Mirror the sets (right-align the first set against the divider)", ref mirror))
                {
                    Configuration.CombineSetsExpandFromCenter = mirror;
                    Configuration.Save();
                }

                Tooltip("Right-aligns the first set against the divider so the two sets mirror each other.");

                DrawFloatSlider("Divider thickness##combdiv", () => Configuration.CombineDividerThickness,
                                value => Configuration.CombineDividerThickness = value, 0.5f, 6f);
                Tooltip("The thickness of the line between the two sets.");
                DrawColorPicker("Divider colour##combdivcol", () => Configuration.CombineDividerColor,
                                value => Configuration.CombineDividerColor = value);
                Tooltip("The colour of the line between the two sets.");

                ImGui.Unindent();
            }

            if (Configuration.Detached)
            {
                ImGui.Separator();
                if (ImGui.Button("Bring all windows on-screen"))
                {
                    plugin.RecenterDetachedWindows();
                }

                Tooltip("Clamps every detached window back inside the screen.");
            }
        }

        /// <summary>
        /// Draws the global layout settings: the collapsible scaling and opacity groups.
        /// </summary>
        private void DrawAppearance()
        {
            ImGui.TextUnformatted("Global Layout Settings");
            DrawUiScale();
            DrawOpacitySettings();
            DrawPositionSettings();
        }

        /// <summary>
        /// Draws the per-section layout controls for the current mode.
        /// Only the current mode's values are shown, since windowed and detached mode keep separate settings.
        /// </summary>
        private void DrawSections()
        {
            ImGui.TextUnformatted(Configuration.Detached ? "Sections (Detached mode)" : "Sections (Windowed mode)");
            ImGui.TextDisabled("Fine tune layout settings below on an individual section basis.");

            foreach (var section in plugin.Sections)
            {
                if (!plugin.SectionEnabled(section.Id))
                {
                    continue;
                }

                if (!ImGui.CollapsingHeader($"{section.Name}##sec_{section.Id}"))
                {
                    continue;
                }

                var id = section.Id;
                ImGui.Indent();

                if (Configuration.Detached)
                {
                    DrawDetachedPosition(id, section.DefaultOffset);
                }
                else
                {
                    DrawWindowedPosition(id, section.DefaultOffset);
                }

                var sectionScale = Configuration.GetSectionScale(id);
                ImGui.SetNextItemWidth(306f);
                if (ImGui.SliderFloat($"Scale##{id}", ref sectionScale, 0.5f, 3.0f, "%.2fx"))
                {
                    Configuration.SetSectionScale(id, Math.Clamp(sectionScale, 0.5f, 3.0f));
                    Configuration.Save();
                }

                if (OrientationSectionIds.Contains(id))
                {
                    DrawOrientationControls(id);
                }

                DrawAppearanceControls(() => Configuration.GetSectionBackgroundAlpha(id), value => Configuration.SetSectionBackgroundAlpha(id, value),
                                       () => Configuration.GetSectionNoTitleBar(id), value => Configuration.SetSectionNoTitleBar(id, value),
                                       () => Configuration.GetSectionHideLabels(id), value => Configuration.SetSectionHideLabels(id, value),
                                       () => Configuration.GetSectionButtonAlpha(id), value => Configuration.SetSectionButtonAlpha(id, value),
                                       id, section.HasButtons ? "Button opacity" : "Text opacity");

                ImGui.Unindent();
            }
        }

        /// <summary>
        /// The macro button panels that offer the button layout and reverse-order options.
        /// </summary>
        private static readonly HashSet<string> OrientationSectionIds = new()
        {
            "Exdeath",
            "Debuffs",
            "ShortDebuffs",
            "LongDebuffs",
            "FireWaterButtons",
            "Inferno",
            "Tsunami",
            "ThunderButtons",
        };

        /// <summary>
        /// The names shown in the button layout dropdown, indexed by the <see cref="PanelOrientation"/> value.
        /// </summary>
        private static readonly string[] OrientationLabels = { "Default", "Vertical", "Horizontal" };

        /// <summary>
        /// Draws the button layout dropdown and, for panels with button pairs, the reverse-order checkbox.
        /// </summary>
        /// <param name="sectionId">The section the controls belong to.</param>
        private void DrawOrientationControls(string sectionId)
        {
            var orientation = Configuration.GetSectionOrientation(sectionId);
            ImGui.SetNextItemWidth(150f);
            using (var combo = ImRaii.Combo($"Button layout##orient_{sectionId}", OrientationLabels[(int)orientation]))
            {
                Tooltip("Default keeps the panel's usual grid. Vertical stacks every button in one column and Horizontal lays them out in one row; the column headers only show on the default layout.");
                if (combo)
                {
                    for (var optionIndex = 0; optionIndex < OrientationLabels.Length; optionIndex++)
                    {
                        if (ImGui.Selectable(OrientationLabels[optionIndex], (int)orientation == optionIndex))
                        {
                            Configuration.SetSectionOrientation(sectionId, (PanelOrientation)optionIndex);
                            Configuration.Save();
                        }
                    }
                }
            }

            if (!SectionHasPairs(sectionId))
            {
                return;
            }

            var reversedButtons = Configuration.GetSectionReversed(sectionId);
            if (ImGui.Checkbox($"Reverse button order##rev_{sectionId}", ref reversedButtons))
            {
                Configuration.SetSectionReversed(sectionId, reversedButtons);
                Configuration.Save();
            }

            Tooltip("Swaps the two buttons inside every pair, putting Fake before Real and LONG before SHORT.");
        }

        /// <summary>
        /// Determines whether a panel currently holds button pairs the reverse option can swap.
        /// The single-column debuff panels never do, and the Debuffs panel only pairs up in Classic Mode.
        /// </summary>
        /// <param name="sectionId">The section to test.</param>
        /// <returns>True when the panel draws real/fake or short/long pairs.</returns>
        private bool SectionHasPairs(string sectionId)
        {
            if (sectionId is "ShortDebuffs" or "LongDebuffs")
            {
                return false;
            }

            if (sectionId == "Debuffs")
            {
                return Configuration.SolverMode == SolverMode.Classic;
            }

            return true;
        }

        /// <summary>
        /// Draws the detached window position sliders, which force the window position when changed.
        /// </summary>
        /// <param name="sectionId">The section whose position sliders are drawn.</param>
        /// <param name="defaultOffset">The position used when none has been stored.</param>
        private void DrawDetachedPosition(string sectionId, Vector2 defaultOffset)
        {
            var position = Configuration.GetDetachedPosition(sectionId, defaultOffset);
            var positionX = position.X;
            var positionY = position.Y;
            var changed = false;

            ImGui.SetNextItemWidth(150f);
            changed |= ImGui.SliderFloat($"X##dpos{sectionId}", ref positionX, 0f, 3840f, "%.0f");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(150f);
            changed |= ImGui.SliderFloat($"Y##dpos{sectionId}", ref positionY, 0f, 2160f, "%.0f");

            if (changed)
            {
                Configuration.SetDetachedPosition(sectionId, new Vector2(positionX, positionY));
                plugin.DetachedPositionDirty.Add(sectionId);
                Configuration.Save();
            }
        }

        /// <summary>
        /// Draws the windowed offset sliders for a section.
        /// </summary>
        /// <param name="sectionId">The section whose offset sliders are drawn.</param>
        /// <param name="defaultOffset">The offset used when none has been stored.</param>
        private void DrawWindowedPosition(string sectionId, Vector2 defaultOffset)
        {
            var offset = Configuration.GetOffset(sectionId, defaultOffset);
            var offsetX = offset.X;
            var offsetY = offset.Y;
            var changed = false;

            ImGui.SetNextItemWidth(150f);
            changed |= ImGui.SliderFloat($"X##wpos{sectionId}", ref offsetX, -300f, 1400f, "%.0f");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(150f);
            changed |= ImGui.SliderFloat($"Y##wpos{sectionId}", ref offsetY, -100f, 1000f, "%.0f");

            if (changed)
            {
                Configuration.SetOffset(sectionId, new Vector2(offsetX, offsetY));
                Configuration.Save();
            }
        }

        /// <summary>
        /// Draws the reset-layout and restore-all buttons.
        /// </summary>
        private void DrawResetButtons()
        {
            if (ImGui.Button("Reset layout to defaults"))
            {
                Configuration.Offsets.Clear();
                Configuration.DetachedPositions.Clear();
                Configuration.SectionScales.Clear();
                Configuration.Save();
            }

            Tooltip("Resets every section position and scale; all other settings are kept.");

            ImGui.SameLine();
            if (ImGui.Button("Restore ALL settings to defaults"))
            {
                plugin.RestoreAllDefaults();
            }

            Tooltip("Restores every setting in every tab to its default value.");
        }

        /// <summary>
        /// Draws the Last Fake toggle options, which appear only once the toggles have been unlocked.
        /// </summary>
        private void DrawLastFakeSettings()
        {
            if (!Configuration.ShowLastFake)
            {
                return;
            }

            ImGui.Separator();
            ImGui.TextUnformatted("Last Fake toggles");

            var basic = Configuration.UseBasicToggles;
            if (ImGui.Checkbox("Use Basic Toggles (checkboxes)", ref basic))
            {
                Configuration.UseBasicToggles = basic;
                Configuration.Save();
            }

            Tooltip("Shows the Last Fake toggles as plain checkboxes instead of the coloured REAL/FAKE buttons.");

            if (!basic)
            {
                ImGui.Indent();
                DrawCustomToggleText();
                DrawToggleSharedSettings();
                DrawToggleDetachSettings();
                ImGui.Unindent();
            }

            ImGui.Separator();
            DrawLastFakeAnnounceSettings();
        }

        /// <summary>
        /// Draws the ANNOUNCE button options: enable, dock, channel, message and the parseable macros.
        /// </summary>
        private void DrawLastFakeAnnounceSettings()
        {
            ImGui.TextUnformatted("Announce Last Fake");

            var enabled = Configuration.LastFakeAnnounceEnabled;
            if (ImGui.Checkbox("Show the ANNOUNCE button", ref enabled))
            {
                Configuration.LastFakeAnnounceEnabled = enabled;
                Configuration.Save();
            }

            Tooltip("Adds an ANNOUNCE button that posts the current Kefka values to the channel below when you press it. Nothing is sent without your press.");

            if (!Configuration.LastFakeAnnounceEnabled)
            {
                return;
            }

            ImGui.Indent();

            var docked = Configuration.LastFakeAnnounceDocked;
            if (ImGui.Checkbox("Dock to the Kefka text panel (otherwise floats as its own button)", ref docked))
            {
                Configuration.LastFakeAnnounceDocked = docked;
                Configuration.Save();
            }

            Tooltip("Attaches the ANNOUNCE button to a side of the Kefka text panel instead of floating as its own panel.");

            if (Configuration.LastFakeAnnounceDocked)
            {
                ImGui.Indent();
                DrawDockSideCombo();
                ImGui.Unindent();
            }

            DrawChannelCombo("Channel##lastfakeannounce", () => Configuration.LastFakeAnnounceChannel, value => Configuration.LastFakeAnnounceChannel = value);
            Tooltip("The chat channel the ANNOUNCE message is sent to.");

            var message = Configuration.LastFakeAnnounceMessage;
            ImGui.SetNextItemWidth(360f);
            if (ImGui.InputText("Message##lastfakeannouncemsg", ref message, 512))
            {
                Configuration.LastFakeAnnounceMessage = message;
            }

            Tooltip("The message sent when you press ANNOUNCE. {KefkaThunder} and {KefkaBlizzard} are replaced with the current Thunder and Blizzard values; an unpressed mechanic shows as \"?\".");

            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                Configuration.Save();
            }

            var realText = Configuration.LastFakeAnnounceRealText;
            ImGui.SetNextItemWidth(120f);
            if (ImGui.InputText("Text for REAL", ref realText, 64))
            {
                Configuration.LastFakeAnnounceRealText = realText;
            }

            Tooltip("What {KefkaThunder} and {KefkaBlizzard} resolve to when that mechanic is currently real.");

            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                Configuration.Save();
            }

            var fakeText = Configuration.LastFakeAnnounceFakeText;
            ImGui.SetNextItemWidth(120f);
            if (ImGui.InputText("Text for FAKE", ref fakeText, 64))
            {
                Configuration.LastFakeAnnounceFakeText = fakeText;
            }

            Tooltip("What {KefkaThunder} and {KefkaBlizzard} resolve to when that mechanic is currently fake.");

            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                Configuration.Save();
            }

            ImGui.Unindent();
        }

        /// <summary>
        /// Draws the custom toggle text option and its label fields.
        /// </summary>
        private void DrawCustomToggleText()
        {
            var customText = Configuration.UseCustomToggleText;
            if (ImGui.Checkbox("Custom button text", ref customText))
            {
                Configuration.UseCustomToggleText = customText;
                Configuration.Save();
            }

            Tooltip("Replaces the REAL and FAKE button labels with your own text.");

            if (!customText)
            {
                return;
            }

            ImGui.Indent();

            var realText = Configuration.CustomRealText;
            ImGui.SetNextItemWidth(200f);
            if (ImGui.InputText("REAL label##togreal", ref realText, 32))
            {
                Configuration.CustomRealText = realText;
            }

            Tooltip("The label shown while the toggle is in the REAL state. Leave it blank for a square button with no text.");

            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                Configuration.Save();
            }

            var fakeText = Configuration.CustomFakeText;
            ImGui.SetNextItemWidth(200f);
            if (ImGui.InputText("FAKE label##togfake", ref fakeText, 32))
            {
                Configuration.CustomFakeText = fakeText;
            }

            Tooltip("The label shown while the toggle is in the FAKE state. Leave it blank for a square button with no text.");

            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                Configuration.Save();
            }

            ImGui.Unindent();
        }

        /// <summary>
        /// Draws the shared-settings toggle and the dedicated toggle scale and opacity sliders.
        /// </summary>
        private void DrawToggleSharedSettings()
        {
            var shared = Configuration.UseSharedToggleSettings;
            if (ImGui.Checkbox("Use Shared Settings (match the Kefka text panel)", ref shared))
            {
                Configuration.UseSharedToggleSettings = shared;
                Configuration.Save();
            }

            Tooltip("Sizes the toggle buttons from the Kefka text panel's scale and opacity instead of the sliders below.");

            if (shared)
            {
                return;
            }

            ImGui.Indent();
            DrawFloatSlider("Button scale X##togsx", () => Configuration.ToggleButtonScaleX, value => Configuration.ToggleButtonScaleX = value, 0.5f, 4f);
            Tooltip("The toggle buttons' width multiplier.");
            DrawFloatSlider("Button scale Y##togsy", () => Configuration.ToggleButtonScaleY, value => Configuration.ToggleButtonScaleY = value, 0.5f, 4f);
            Tooltip("The toggle buttons' height multiplier.");
            DrawFloatSlider("Text scale##togts", () => Configuration.ToggleTextScale, value => Configuration.ToggleTextScale = value, 0.5f, 3f);
            Tooltip("The toggle button labels' text size multiplier.");
            DrawFloatSlider("Opacity##togop", () => Configuration.ToggleButtonAlpha, value => Configuration.ToggleButtonAlpha = value, 0f, 1f);
            Tooltip("The toggle buttons' opacity.");
            ImGui.Unindent();
        }

        /// <summary>
        /// Draws the detach toggle option and its alignment and split options.
        /// </summary>
        private void DrawToggleDetachSettings()
        {
            var detach = Configuration.DetachToggleButtons;
            if (ImGui.Checkbox("Detach toggles into their own panel", ref detach))
            {
                Configuration.DetachToggleButtons = detach;
                Configuration.Save();
            }

            Tooltip("Pulls the Last Fake toggles out of the Kefka text panel into their own repositionable panel.");

            if (!detach)
            {
                return;
            }

            ImGui.Indent();

            var horizontal = Configuration.ToggleButtonsHorizontal;
            if (ImGui.Checkbox("Horizontal alignment", ref horizontal))
            {
                Configuration.ToggleButtonsHorizontal = horizontal;
                Configuration.Save();
            }

            Tooltip("Places the two toggles side by side instead of stacked.");

            var individual = Configuration.ToggleButtonsIndividualPanels;
            if (ImGui.Checkbox("Separate panel per button", ref individual))
            {
                Configuration.ToggleButtonsIndividualPanels = individual;
                Configuration.Save();
            }

            Tooltip("Splits the Thunder and Blizzard toggles into two panels that move independently.");

            ImGui.Unindent();
        }

        /// <summary>
        /// Draws the collapsed-by-default controller section with the hide-macro-buttons option and the copyable command list.
        /// Only the commands available in the active gameplay mode are listed, and the Last Fake commands only appear when that hidden feature is unlocked.
        /// </summary>
        private void DrawControllerSettings()
        {
            // The hide-macro-buttons toggle is the setting controller players actually want, so it is spaced out and highlighted here after a tester missed it entirely.
            ImGui.TextColored(new Vector4(1f, 0.84f, 0f, 1f),
                              "Controller players: enable this to hide the on-screen macro buttons.");
            ImGui.Spacing();

            var hideMacroButtons = Configuration.HideMacroButtons;
            if (ImGui.Checkbox("Hide macro buttons (keep only the text panels)", ref hideMacroButtons))
            {
                Configuration.HideMacroButtons = hideMacroButtons;
                Configuration.Save();
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            ImGui.TextWrapped("Controller players cannot click the buttons. Put each command below into its own game macro and bind it, then hide the macro buttons above to keep only the resolution text.");
            ImGui.Spacing();
            ImGui.TextDisabled("Commands for the active mode (change modes under General).");
            ImGui.Spacing();

            DrawMacroRow("/snazzyp4 ExDeathReal");
            DrawMacroRow("/snazzyp4 ExDeathFake");

            if (Configuration.SolverMode == SolverMode.Classic)
            {
                ImGui.Spacing();
                DrawMacroRow("/snazzyp4 LightningShort");
                DrawMacroRow("/snazzyp4 LightningLong");
                DrawMacroRow("/snazzyp4 DropShort");
                DrawMacroRow("/snazzyp4 DropLong");
                DrawMacroRow("/snazzyp4 AccelerationShort");
                DrawMacroRow("/snazzyp4 AccelerationLong");
            }
            else if (Configuration.SolverMode == SolverMode.Simple)
            {
                ImGui.Spacing();
                DrawMacroRow("/snazzyp4 Lightning");
                DrawMacroRow("/snazzyp4 Drop");
                DrawMacroRow("/snazzyp4 Acceleration");
            }

            ImGui.Spacing();
            DrawMacroRow("/snazzyp4 InfernoReal");
            DrawMacroRow("/snazzyp4 InfernoFake");
            DrawMacroRow("/snazzyp4 TsunamiReal");
            DrawMacroRow("/snazzyp4 TsunamiFake");
            ImGui.Spacing();
            DrawMacroRow("/snazzyp4 ThunderReal");
            DrawMacroRow("/snazzyp4 ThunderFake");
            DrawMacroRow("/snazzyp4 BlizzardReal");
            DrawMacroRow("/snazzyp4 BlizzardFake");
            ImGui.Spacing();
            DrawMacroRow("/snazzyp4 Reset");
            DrawMacroRow("/snazzyp4 Undo");
            DrawMacroRow("/snazzyp4 Hide");

            if (Configuration.ShowLastFake)
            {
                ImGui.Spacing();
                ImGui.TextDisabled("Last Fake (hidden feature):");
                DrawMacroRow("/snazzyp4 LastThunderReal");
                DrawMacroRow("/snazzyp4 LastThunderFake");
                DrawMacroRow("/snazzyp4 LastBlizzardReal");
                DrawMacroRow("/snazzyp4 LastBlizzardFake");
            }
        }

        /// <summary>
        /// Draws one command row with a copy button that places the command on the clipboard.
        /// </summary>
        /// <param name="command">The slash command shown with its copy button.</param>
        private static void DrawMacroRow(string command)
        {
            if (ImGui.Button($"Copy##{command}"))
            {
                ImGui.SetClipboardText(command);
            }

            ImGui.SameLine();
            ImGui.TextUnformatted(command);
        }

        /// <summary>
        /// Draws the custom text editor, grouping each editable label with a field that falls back to its default when blank.
        /// </summary>
        private void DrawTextSettings()
        {
            ImGui.TextDisabled("Rename any label or callout. Leave a field blank to use the default.");
            if (ImGui.Button("Reset all text to defaults"))
            {
                Configuration.ResetText();
                Configuration.Save();
            }

            ImGui.Separator();

            var group = string.Empty;
            foreach (var (id, defaultValue, label, entryGroup) in TextLabels.Entries)
            {
                if (!TextEntryVisible(id))
                {
                    continue;
                }

                if (entryGroup != group)
                {
                    group = entryGroup;
                    ImGui.Spacing();
                    ImGui.TextUnformatted(group);
                }

                var value = Configuration.GetRawText(id);
                ImGui.SetNextItemWidth(240f);
                if (ImGui.InputTextWithHint($"{label}##txt_{id}", defaultValue, ref value, 128))
                {
                    Configuration.SetText(id, value);
                }

                if (ImGui.IsItemDeactivatedAfterEdit())
                {
                    Configuration.Save();
                }
            }
        }

        /// <summary>
        /// Determines whether a text label applies to the active gameplay mode, hiding entries the mode never renders.
        /// The short/long headers and the Acceleration joiner belong to Classic Mode, the debuff names to the Simple modes, and the Acceleration words split between the personal and party variants.
        /// </summary>
        /// <param name="id">The text label id to test.</param>
        /// <returns>True when that label is editable in the active mode.</returns>
        private bool TextEntryVisible(string id)
        {
            if (Configuration.SolverMode == SolverMode.Classic)
            {
                return id is not (TextLabels.LightningName or TextLabels.DropName or TextLabels.AccelerationName
                                  or TextLabels.AccelerationStillness or TextLabels.AccelerationMotion or TextLabels.DebuffPanelLabel);
            }

            if (id is TextLabels.ShortColumnHeader or TextLabels.LongColumnHeader or TextLabels.AndJoiner)
            {
                return false;
            }

            if (Configuration.SolverMode == SolverMode.Simple)
            {
                return id is not (TextLabels.AccelerationStillness or TextLabels.AccelerationMotion);
            }

            return id is not (TextLabels.StandStill or TextLabels.Move or TextLabels.DebuffsHeader or TextLabels.DebuffPanelLabel);
        }

        /// <summary>
        /// Draws the collapsed-by-default palette editor covering every coloured element.
        /// </summary>
        private void DrawColorSettings()
        {
            ImGui.TextUnformatted("Colour presets");
            if (ImGui.Button("Default"))
            {
                ApplyColorPreset("default");
            }

            ImGui.SameLine();
            if (ImGui.Button("Deuteranopia"))
            {
                ApplyColorPreset("deuteranopia");
            }

            ImGui.SameLine();
            if (ImGui.Button("Protanopia"))
            {
                ApplyColorPreset("protanopia");
            }

            ImGui.SameLine();
            if (ImGui.Button("Tritanopia"))
            {
                ApplyColorPreset("tritanopia");
            }

            ImGui.TextDisabled("Presets are colourblind-friendly starting points; fine-tune below.");
            ImGui.Separator();

            DrawColorPicker("Spread on target (Support)##colSpreadSup", () => Configuration.ColorSpreadSupport, value => Configuration.ColorSpreadSupport = value);
            DrawColorPicker("Stack on target (Support)##colStackSup", () => Configuration.ColorStackSupport, value => Configuration.ColorStackSupport = value);
            DrawColorPicker("Spread on target (DPS)##colSpreadDps", () => Configuration.ColorSpreadDps, value => Configuration.ColorSpreadDps = value);
            DrawColorPicker("Stack on target (DPS)##colStackDps", () => Configuration.ColorStackDps, value => Configuration.ColorStackDps = value);
            DrawColorPicker("Acceleration (Move / Stand)##colAccel", () => Configuration.ColorAcceleration, value => Configuration.ColorAcceleration = value);
            DrawColorPicker("Gaze Real##colGazeReal", () => Configuration.ColorGazeReal, value => Configuration.ColorGazeReal = value);
            DrawColorPicker("Gaze Fake##colGazeFake", () => Configuration.ColorGazeFake, value => Configuration.ColorGazeFake = value);
            DrawColorPicker("Inferno##colFire", () => Configuration.ColorFire, value => Configuration.ColorFire = value);
            DrawColorPicker("Tsunami##colWater", () => Configuration.ColorWater, value => Configuration.ColorWater = value);
            DrawColorPicker("Thunder##colThunder", () => Configuration.ColorThunder, value => Configuration.ColorThunder = value);
            DrawColorPicker("Blizzard##colBlizzard", () => Configuration.ColorBlizzard, value => Configuration.ColorBlizzard = value);
            DrawColorPicker("Last Fake toggle - Real##colTogReal", () => Configuration.ColorToggleReal, value => Configuration.ColorToggleReal = value);
            DrawColorPicker("Last Fake toggle - Fake##colTogFake", () => Configuration.ColorToggleFake, value => Configuration.ColorToggleFake = value);

            if (ImGui.Button("Reset colors to defaults"))
            {
                ApplyColorPreset("default");
            }
        }

        /// <summary>
        /// Applies a colour preset to every configurable colour.
        /// The default preset restores the shipped colours, while the colourblind presets pick hues that stay distinguishable for that deficiency.
        /// </summary>
        /// <param name="preset">The preset name: "default", "deuteranopia", "protanopia" or "tritanopia".</param>
        private void ApplyColorPreset(string preset)
        {
            if (preset == "default")
            {
                var defaults = new Configuration();
                Configuration.ColorSpreadSupport = defaults.ColorSpreadSupport;
                Configuration.ColorStackSupport = defaults.ColorStackSupport;
                Configuration.ColorSpreadDps = defaults.ColorSpreadDps;
                Configuration.ColorStackDps = defaults.ColorStackDps;
                Configuration.ColorAcceleration = defaults.ColorAcceleration;
                Configuration.ColorGazeReal = defaults.ColorGazeReal;
                Configuration.ColorGazeFake = defaults.ColorGazeFake;
                Configuration.ColorFire = defaults.ColorFire;
                Configuration.ColorWater = defaults.ColorWater;
                Configuration.ColorThunder = defaults.ColorThunder;
                Configuration.ColorBlizzard = defaults.ColorBlizzard;
                Configuration.ColorToggleReal = defaults.ColorToggleReal;
                Configuration.ColorToggleFake = defaults.ColorToggleFake;
                Configuration.Save();
                return;
            }

            // Palette entries drawn from colourblind-safe hue sets.
            var skyBlue = new Vector4(0.35f, 0.70f, 0.90f, 1f);
            var blue = new Vector4(0.00f, 0.45f, 0.70f, 1f);
            var orange = new Vector4(0.90f, 0.60f, 0.00f, 1f);
            var vermillion = new Vector4(0.85f, 0.37f, 0.00f, 1f);
            var yellow = new Vector4(0.95f, 0.90f, 0.25f, 1f);
            var purple = new Vector4(0.80f, 0.60f, 0.70f, 1f);
            var green = new Vector4(0.00f, 0.72f, 0.36f, 1f);
            var red = new Vector4(0.88f, 0.16f, 0.18f, 1f);
            var cyan = new Vector4(0.20f, 0.78f, 0.82f, 1f);
            var magenta = new Vector4(0.85f, 0.28f, 0.72f, 1f);

            if (preset == "tritanopia")
            {
                // Blue-yellow deficiency: lean on red, green, cyan and magenta.
                Configuration.ColorSpreadSupport = magenta;
                Configuration.ColorStackSupport = red;
                Configuration.ColorSpreadDps = magenta;
                Configuration.ColorStackDps = cyan;
                Configuration.ColorAcceleration = red;
                Configuration.ColorGazeReal = green;
                Configuration.ColorGazeFake = red;
                Configuration.ColorFire = red;
                Configuration.ColorWater = cyan;
                Configuration.ColorThunder = magenta;
                Configuration.ColorBlizzard = cyan;
                Configuration.ColorToggleReal = green;
                Configuration.ColorToggleFake = red;
            }
            else
            {
                // Red-green deficiency (deuteranopia and protanopia): lean on blue, orange and yellow, keeping the "real" states blue and the "fake" states orange.
                var fake = preset == "protanopia" ? orange : vermillion;
                Configuration.ColorSpreadSupport = purple;
                Configuration.ColorStackSupport = orange;
                Configuration.ColorSpreadDps = yellow;
                Configuration.ColorStackDps = blue;
                Configuration.ColorAcceleration = fake;
                Configuration.ColorGazeReal = skyBlue;
                Configuration.ColorGazeFake = orange;
                Configuration.ColorFire = fake;
                Configuration.ColorWater = blue;
                Configuration.ColorThunder = purple;
                Configuration.ColorBlizzard = skyBlue;
                Configuration.ColorToggleReal = skyBlue;
                Configuration.ColorToggleFake = fake;
            }

            Configuration.Save();
        }

        /// <summary>
        /// Draws a single colour picker and persists the value once editing finishes.
        /// </summary>
        /// <param name="label">The label shown next to the picker.</param>
        /// <param name="getColor">Reads the current colour.</param>
        /// <param name="setColor">Writes the new colour.</param>
        private void DrawColorPicker(string label, Func<Vector4> getColor, Action<Vector4> setColor)
        {
            var color = getColor();
            if (ImGui.ColorEdit4(label, ref color, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaPreviewHalf))
            {
                setColor(color);
            }

            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                Configuration.Save();
            }
        }

        /// <summary>
        /// Draws a clamped float slider bound to a getter and setter.
        /// </summary>
        /// <param name="label">The label shown next to the slider.</param>
        /// <param name="getValue">Reads the current value.</param>
        /// <param name="setValue">Writes the new value.</param>
        /// <param name="minimum">The slider's lower bound.</param>
        /// <param name="maximum">The slider's upper bound.</param>
        private void DrawFloatSlider(string label, Func<float> getValue, Action<float> setValue, float minimum, float maximum)
        {
            var value = getValue();
            ImGui.SetNextItemWidth(200f);
            if (ImGui.SliderFloat(label, ref value, minimum, maximum, "%.2f"))
            {
                setValue(Math.Clamp(value, minimum, maximum));
                Configuration.Save();
            }
        }

        /// <summary>
        /// Draws the hidden unlock field and, once the code is entered, the acceptance flow.
        /// </summary>
        private void DrawHiddenSettings()
        {
            ImGui.TextUnformatted("...");

            ImGui.SetNextItemWidth(220f);
            ImGui.InputText("##hiddencode", ref hiddenInput, 64);
            ImGui.SameLine();
            if (ImGui.Button("Confirm##hiddencode"))
            {
                if (hiddenInput == "i_need_it")
                {
                    showProceedPrompt = true;
                    scrollToPrompt = true;
                }

                hiddenInput = string.Empty;
            }

            if (showProceedPrompt)
            {
                DrawProceedPrompt();
            }

            if (Configuration.ShowLastFake)
            {
                if (ImGui.Button("Disable all hidden settings"))
                {
                    Configuration.ShowLastFake = false;
                    Configuration.Save();
                }
            }
        }

        /// <summary>
        /// Draws the unlock warning with its confirm and reject buttons.
        /// </summary>
        private void DrawProceedPrompt()
        {
            ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + 440f);
            ImGui.TextUnformatted(ProceedText);
            ImGui.PopTextWrapPos();

            if (ImGui.Button("Confirm##proceed"))
            {
                Configuration.ShowLastFake = true;
                Configuration.Save();
                showProceedPrompt = false;
            }

            ImGui.SameLine();
            if (ImGui.Button("Reject##proceed"))
            {
                showProceedPrompt = false;
            }

            // Bring the prompt and its buttons into view once, the frame they first appear.
            if (scrollToPrompt)
            {
                ImGui.SetScrollHereY(1.0f);
                scrollToPrompt = false;
            }
        }

        /// <summary>
        /// Shows a tooltip for the most recently drawn control while it is hovered, including while it is disabled.
        /// The text wraps at a fixed width so long descriptions do not stretch across the screen.
        /// </summary>
        /// <param name="text">The tooltip text.</param>
        private static void Tooltip(string text)
        {
            if (!ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            {
                return;
            }

            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(ImGui.GetFontSize() * 28f);
            ImGui.TextUnformatted(text);
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
        }

        /// <summary>
        /// Applies a clamped value through a scale setter and persists it.
        /// </summary>
        /// <param name="setScale">Writes the clamped scale value.</param>
        /// <param name="value">The requested scale before clamping.</param>
        private void SetScale(Action<float> setScale, float value)
        {
            setScale(Math.Clamp(value, 0.5f, 3.0f));
            Configuration.Save();
        }

        /// <summary>
        /// Draws the background opacity, title bar, label and button opacity controls.
        /// This is reused for the universal block and for each per-section block.
        /// </summary>
        /// <param name="getBackgroundAlpha">Reads the background opacity.</param>
        /// <param name="setBackgroundAlpha">Writes the background opacity.</param>
        /// <param name="getNoTitleBar">Reads the hide-title-bar flag.</param>
        /// <param name="setNoTitleBar">Writes the hide-title-bar flag.</param>
        /// <param name="getHideLabels">Reads the hide-labels flag.</param>
        /// <param name="setHideLabels">Writes the hide-labels flag.</param>
        /// <param name="getButtonAlpha">Reads the button opacity.</param>
        /// <param name="setButtonAlpha">Writes the button opacity.</param>
        /// <param name="idSuffix">The ImGui id suffix keeping the controls unique.</param>
        /// <param name="opacityLabel">The label used for the button or text opacity slider.</param>
        private void DrawAppearanceControls(Func<float> getBackgroundAlpha, Action<float> setBackgroundAlpha,
                                            Func<bool> getNoTitleBar, Action<bool> setNoTitleBar,
                                            Func<bool> getHideLabels, Action<bool> setHideLabels,
                                            Func<float> getButtonAlpha, Action<float> setButtonAlpha,
                                            string idSuffix, string opacityLabel)
        {
            var backgroundAlpha = getBackgroundAlpha();
            ImGui.SetNextItemWidth(200f);
            if (ImGui.SliderFloat($"Background opacity##bg_{idSuffix}", ref backgroundAlpha, 0f, 1f, "%.2f"))
            {
                setBackgroundAlpha(Math.Clamp(backgroundAlpha, 0f, 1f));
                Configuration.Save();
            }

            // The per-section switches are moot while the matching global switch on the General tab hides everything.
            using (ImRaii.Disabled(Configuration.NoTitleBar))
            {
                var noTitleBar = getNoTitleBar();
                if (ImGui.Checkbox($"Hide title bar##nt_{idSuffix}", ref noTitleBar))
                {
                    setNoTitleBar(noTitleBar);
                    Configuration.Save();
                }
            }

            using (ImRaii.Disabled(Configuration.HideLabels))
            {
                var hideLabels = getHideLabels();
                if (ImGui.Checkbox($"Hide label names##hl_{idSuffix}", ref hideLabels))
                {
                    setHideLabels(hideLabels);
                    Configuration.Save();
                }
            }

            var buttonAlpha = getButtonAlpha();
            ImGui.SetNextItemWidth(200f);
            if (ImGui.SliderFloat($"{opacityLabel}##ba_{idSuffix}", ref buttonAlpha, 0f, 1f, "%.2f"))
            {
                setButtonAlpha(Math.Clamp(buttonAlpha, 0f, 1f));
                Configuration.Save();
            }
        }

    }
}
