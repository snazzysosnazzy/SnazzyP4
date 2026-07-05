using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

namespace SnazzyP4.Windows;

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
    /// Creates the settings window bound to the plugin.
    /// </summary>
    public ConfigWindow(Plugin plugin)
        : base("Snazzy P4 Settings###SnazzyP4Config",
               ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse)
    {
        this.plugin = plugin;
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

        using var tabs = ImRaii.TabBar("##snazzyp4_settings_tabs");
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

        using (var tab = ImRaii.TabItem("Appearance"))
        {
            if (tab)
            {
                DrawAppearance();
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

        using (var tab = ImRaii.TabItem("Layout"))
        {
            if (tab)
            {
                DrawLayout();
            }
        }

        using (var tab = ImRaii.TabItem("Controller"))
        {
            if (tab)
            {
                DrawControllerSettings();
            }
        }

        using (var tab = ImRaii.TabItem("Sections"))
        {
            if (tab)
            {
                DrawSections();
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
    /// Draws the General tab with scale, role, party chat, settings profiles and the reset buttons.
    /// </summary>
    private void DrawGeneralTab()
    {
        DrawUiScale();
        ImGui.Separator();
        DrawRole();
        ImGui.Separator();
        DrawProfileImportExport();
        ImGui.Separator();
        DrawResetButtons();
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

            Configuration.CopyFrom(imported);
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
        var credit = "made by snazz";
        ImGui.SameLine();
        ImGui.SetCursorPosX(ImGui.GetContentRegionMax().X - ImGui.CalcTextSize(credit).X);
        ImGui.TextColored(new Vector4(1f, 0.84f, 0f, 1f), credit);
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Separator();
    }

    /// <summary>
    /// Draws the UI scale slider and the quick preset buttons.
    /// </summary>
    private void DrawUiScale()
    {
        ImGui.TextUnformatted("UI Scale");
        var scale = Configuration.UiScale;
        ImGui.SetNextItemWidth(220f);
        if (ImGui.SliderFloat("##uiscale", ref scale, 0.5f, 3.0f, "%.2fx"))
        {
            SetScale(scale);
        }

        if (ImGui.Button("50%"))
        {
            SetScale(0.5f);
        }

        ImGui.SameLine();
        if (ImGui.Button("100%"))
        {
            SetScale(1.0f);
        }

        ImGui.SameLine();
        if (ImGui.Button("150%"))
        {
            SetScale(1.5f);
        }

        ImGui.SameLine();
        if (ImGui.Button("200%"))
        {
            SetScale(2.0f);
        }
    }

    /// <summary>
    /// Draws the role selection and the auto-marker option.
    /// </summary>
    private void DrawRole()
    {
        ImGui.TextUnformatted("Role");
        var role = Configuration.IsSupport ? 0 : 1;
        var roleChanged = false;
        roleChanged |= ImGui.RadioButton("Support (Ignore1 / Bind1)", ref role, 0);
        roleChanged |= ImGui.RadioButton("DPS (Ignore2 / Bind2)", ref role, 1);
        if (roleChanged)
        {
            Configuration.IsSupport = role == 0;
            Configuration.Save();
        }

        var autoMarker = Configuration.AutoMarker;
        if (ImGui.Checkbox("Auto-place marker", ref autoMarker))
        {
            Configuration.AutoMarker = autoMarker;
            Configuration.Save();
        }

        ImGui.TextDisabled("Sends the /mk command to the game for you when a spread\nis determined. This issues input on your behalf - use at your\nown risk. Turn off to only Copy/Place manually.");
    }

    /// <summary>
    /// Draws the Chat Messages tab with the channel selector and the gaze and chaos announcement options.
    /// </summary>
    private void DrawChatMessages()
    {
        ImGui.TextUnformatted("Chat Messages");
        ImGui.TextDisabled("Sends a chat message on your behalf in the selected channel when a\nmechanic is determined. Issues input on your behalf - use at your own risk.");

        DrawChannelSelector();

        DrawPartyMechanic(
            "Announce Gaze", "gaze",
            () => Configuration.PartyGazeEnabled, value => Configuration.PartyGazeEnabled = value,
            () => Configuration.PartyGazeCustom, value => Configuration.PartyGazeCustom = value,
            () => Configuration.PartyGazeCustomText, value => Configuration.PartyGazeCustomText = value);

        DrawPartyMechanic(
            "Announce Chaos (Inferno/Tsunami)", "chaos",
            () => Configuration.PartyChaosEnabled, value => Configuration.PartyChaosEnabled = value,
            () => Configuration.PartyChaosCustom, value => Configuration.PartyChaosCustom = value,
            () => Configuration.PartyChaosCustomText, value => Configuration.PartyChaosCustomText = value);
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
    /// Draws the dropdown that selects which chat channel the gaze and chaos announcements are sent to.
    /// </summary>
    private void DrawChannelSelector()
    {
        var preview = Configuration.PartyChatChannel;
        foreach (var (label, prefix) in ChatChannels)
        {
            if (prefix == Configuration.PartyChatChannel)
            {
                preview = label;
                break;
            }
        }

        ImGui.SetNextItemWidth(260f);
        using var combo = ImRaii.Combo("Channel##partychannel", preview);
        if (!combo)
        {
            return;
        }

        foreach (var (label, prefix) in ChatChannels)
        {
            if (ImGui.Selectable(label, Configuration.PartyChatChannel == prefix))
            {
                Configuration.PartyChatChannel = prefix;
                Configuration.Save();
            }
        }
    }

    /// <summary>
    /// Draws the layout mode toggles.
    /// </summary>
    private void DrawLayout()
    {
        ImGui.TextUnformatted("Layout");

        var showToolbar = Configuration.ShowToolbar;
        if (ImGui.Checkbox("Show toolbar (Edit / Detached / Move All / Reset)", ref showToolbar))
        {
            Configuration.ShowToolbar = showToolbar;
            Configuration.Save();
        }

        var detached = Configuration.Detached;
        if (ImGui.Checkbox("Detached windows (each section is its own window)", ref detached))
        {
            Configuration.Detached = detached;
            Configuration.Save();
        }

        var editMode = Configuration.EditMode;
        if (ImGui.Checkbox("Edit layout (click a section to drag it)", ref editMode))
        {
            plugin.SetEditMode(editMode);
        }

        if (Configuration.Detached)
        {
            var moveAll = Configuration.MoveAllActive;
            if (ImGui.Checkbox("Move All (drag any window to move them together)", ref moveAll))
            {
                plugin.SetMoveAll(moveAll);
            }
        }

        var floatingHide = Configuration.FloatingHideButton;
        if (ImGui.Checkbox("Floating Hide button (floats as its own panel, otherwise docks to the toolbar)", ref floatingHide))
        {
            Configuration.FloatingHideButton = floatingHide;
            Configuration.Save();
        }

        var floatingReset = Configuration.FloatingResetButton;
        if (ImGui.Checkbox("Floating Reset button (floats as its own panel, otherwise docks to the toolbar)", ref floatingReset))
        {
            Configuration.FloatingResetButton = floatingReset;
            Configuration.Save();
        }

        var accelerationSameLine = Configuration.AccelerationSameLine;
        if (ImGui.Checkbox("Acceleration text on same line as Stack/Spread", ref accelerationSameLine))
        {
            Configuration.AccelerationSameLine = accelerationSameLine;
            Configuration.Save();
        }

        var combineSets = Configuration.CombineSets;
        if (ImGui.Checkbox("Combine First and Second set into one panel", ref combineSets))
        {
            Configuration.CombineSets = combineSets;
            Configuration.Save();
        }

        if (Configuration.CombineSets)
        {
            ImGui.Indent();
            var combineHorizontal = Configuration.CombineSetsHorizontal;
            if (ImGui.Checkbox("Horizontal orientation (side by side instead of stacked)", ref combineHorizontal))
            {
                Configuration.CombineSetsHorizontal = combineHorizontal;
                Configuration.Save();
            }

            if (Configuration.CombineSetsHorizontal)
            {
                ImGui.Indent();
                var anchorDivider = Configuration.CombineSetsAnchorDivider;
                if (ImGui.Checkbox("Pin the divider in place (sets grow outward from it, text stays left-aligned)", ref anchorDivider))
                {
                    Configuration.CombineSetsAnchorDivider = anchorDivider;
                    Configuration.Save();
                }

                ImGui.TextDisabled("Drag the panel or use the CombinedSets X/Y sliders to move the divider.");
                ImGui.Unindent();
            }

            var mirror = Configuration.CombineSetsExpandFromCenter;
            if (ImGui.Checkbox("Mirror the sets (right-align the first set against the divider)", ref mirror))
            {
                Configuration.CombineSetsExpandFromCenter = mirror;
                Configuration.Save();
            }

            DrawFloatSlider("Divider thickness##combdiv", () => Configuration.CombineDividerThickness,
                value => Configuration.CombineDividerThickness = value, 0.5f, 6f);
            DrawColorPicker("Divider colour##combdivcol", () => Configuration.CombineDividerColor,
                value => Configuration.CombineDividerColor = value);

            ImGui.Unindent();
        }

        if (Configuration.Detached)
        {
            ImGui.Separator();
            if (ImGui.Button("Bring all windows on-screen"))
            {
                plugin.RecenterDetachedWindows();
            }

            ImGui.TextDisabled("Clamps every detached window back inside the screen.");
        }
    }

    /// <summary>
    /// Draws the universal appearance controls or a hint that they are set per section.
    /// </summary>
    private void DrawAppearance()
    {
        ImGui.TextUnformatted("Appearance");

        var universal = Configuration.UseUniversalSettings;
        if (ImGui.Checkbox("Use Universal Settings", ref universal))
        {
            Configuration.UseUniversalSettings = universal;
            Configuration.Save();
        }

        if (universal)
        {
            ImGui.Indent();
            DrawAppearanceControls(
                () => Configuration.BackgroundAlpha, value => Configuration.BackgroundAlpha = value,
                () => Configuration.NoTitleBar, value => Configuration.NoTitleBar = value,
                () => Configuration.HideLabels, value => Configuration.HideLabels = value,
                () => Configuration.ButtonAlpha, value => Configuration.ButtonAlpha = value,
                "univ", "Button / Text opacity");
            ImGui.Unindent();
        }
        else
        {
            ImGui.TextDisabled("Background / title bar / labels / button opacity are set\nper section below.");
        }

        var clickThrough = Configuration.ClickThrough;
        if (ImGui.Checkbox("Click-through (display only)", ref clickThrough))
        {
            Configuration.ClickThrough = clickThrough;
            Configuration.Save();
        }

        if (Configuration.ClickThrough)
        {
            ImGui.TextDisabled("Buttons are disabled while click-through is on.\nReopen this window with  /snazzyp4 config  to turn it off.");
        }
    }

    /// <summary>
    /// Draws the per-section layout controls for the current mode.
    /// Only the current mode's values are shown, since windowed and detached mode keep separate settings.
    /// </summary>
    private void DrawSections()
    {
        ImGui.TextUnformatted(Configuration.Detached ? "Sections (Detached mode)" : "Sections (Windowed mode)");
        ImGui.TextDisabled(Configuration.Detached
            ? "X/Y set each detached window's screen position."
            : "X/Y set each section's offset within the main window.");

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

            if (!Configuration.UseUniversalSettings)
            {
                DrawAppearanceControls(
                    () => Configuration.GetSectionBackgroundAlpha(id), value => Configuration.SetSectionBackgroundAlpha(id, value),
                    () => Configuration.GetSectionNoTitleBar(id), value => Configuration.SetSectionNoTitleBar(id, value),
                    () => Configuration.GetSectionHideLabels(id), value => Configuration.SetSectionHideLabels(id, value),
                    () => Configuration.GetSectionButtonAlpha(id), value => Configuration.SetSectionButtonAlpha(id, value),
                    id, section.HasButtons ? "Button opacity" : "Text opacity");
            }

            ImGui.Unindent();
        }
    }

    /// <summary>
    /// Draws the detached window position sliders, which force the window position when changed.
    /// </summary>
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

        ImGui.SameLine();
        if (ImGui.Button("Restore ALL settings to defaults"))
        {
            plugin.RestoreAllDefaults();
        }
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

        if (basic)
        {
            return;
        }

        ImGui.Indent();
        DrawCustomToggleText();
        DrawToggleSharedSettings();
        DrawToggleDetachSettings();
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

        if (ImGui.IsItemDeactivatedAfterEdit())
        {
            Configuration.Save();
        }

        ImGui.TextDisabled("Leave a label blank for a square button with no text.");
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

        if (shared)
        {
            return;
        }

        ImGui.Indent();
        DrawFloatSlider("Button scale X##togsx", () => Configuration.ToggleButtonScaleX, value => Configuration.ToggleButtonScaleX = value, 0.5f, 4f);
        DrawFloatSlider("Button scale Y##togsy", () => Configuration.ToggleButtonScaleY, value => Configuration.ToggleButtonScaleY = value, 0.5f, 4f);
        DrawFloatSlider("Text scale##togts", () => Configuration.ToggleTextScale, value => Configuration.ToggleTextScale = value, 0.5f, 3f);
        DrawFloatSlider("Opacity##togop", () => Configuration.ToggleButtonAlpha, value => Configuration.ToggleButtonAlpha = value, 0f, 1f);
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

        var individual = Configuration.ToggleButtonsIndividualPanels;
        if (ImGui.Checkbox("Separate panel per button", ref individual))
        {
            Configuration.ToggleButtonsIndividualPanels = individual;
            Configuration.Save();
        }

        ImGui.Unindent();
    }

    /// <summary>
    /// Draws the collapsed-by-default controller section with the hide-macro-buttons option and the copyable command list.
    /// The Last Fake commands are only listed when that hidden feature is unlocked.
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

        DrawMacroRow("/snazzyp4 ExDeathReal");
        DrawMacroRow("/snazzyp4 ExDeathFake");
        ImGui.Spacing();
        DrawMacroRow("/snazzyp4 LightningShort");
        DrawMacroRow("/snazzyp4 LightningLong");
        DrawMacroRow("/snazzyp4 DropShort");
        DrawMacroRow("/snazzyp4 DropLong");
        DrawMacroRow("/snazzyp4 AccelerationShort");
        DrawMacroRow("/snazzyp4 AccelerationLong");
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
        DrawColorPicker("Fire (Inferno)##colFire", () => Configuration.ColorFire, value => Configuration.ColorFire = value);
        DrawColorPicker("Water (Tsunami)##colWater", () => Configuration.ColorWater, value => Configuration.ColorWater = value);
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
    /// Sets and persists the clamped UI scale.
    /// </summary>
    private void SetScale(float value)
    {
        Configuration.UiScale = Math.Clamp(value, 0.5f, 3.0f);
        Configuration.Save();
    }

    /// <summary>
    /// Draws the background opacity, title bar, label and button opacity controls.
    /// This is reused for the universal block and for each per-section block.
    /// </summary>
    private void DrawAppearanceControls(
        Func<float> getBackgroundAlpha, Action<float> setBackgroundAlpha,
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

        var noTitleBar = getNoTitleBar();
        if (ImGui.Checkbox($"Hide title bar##nt_{idSuffix}", ref noTitleBar))
        {
            setNoTitleBar(noTitleBar);
            Configuration.Save();
        }

        var hideLabels = getHideLabels();
        if (ImGui.Checkbox($"Hide label names##hl_{idSuffix}", ref hideLabels))
        {
            setHideLabels(hideLabels);
            Configuration.Save();
        }

        var buttonAlpha = getButtonAlpha();
        ImGui.SetNextItemWidth(200f);
        if (ImGui.SliderFloat($"{opacityLabel}##ba_{idSuffix}", ref buttonAlpha, 0f, 1f, "%.2f"))
        {
            setButtonAlpha(Math.Clamp(buttonAlpha, 0f, 1f));
            Configuration.Save();
        }
    }

    /// <summary>
    /// Draws the enable, custom-message and text-field controls for a party-chat mechanic.
    /// The text field is greyed out unless custom is on, and the whole block is greyed out unless the mechanic is enabled.
    /// </summary>
    private void DrawPartyMechanic(string label, string id,
        Func<bool> getEnabled, Action<bool> setEnabled,
        Func<bool> getCustom, Action<bool> setCustom,
        Func<string> getText, Action<string> setText)
    {
        var enabled = getEnabled();
        if (ImGui.Checkbox($"{label}##{id}en", ref enabled))
        {
            setEnabled(enabled);
            Configuration.Save();
        }

        using (ImRaii.Disabled(!enabled))
        {
            ImGui.Indent();

            var custom = getCustom();
            if (ImGui.Checkbox($"Enable custom message##{id}cust", ref custom))
            {
                setCustom(custom);
                Configuration.Save();
            }

            using (ImRaii.Disabled(!custom))
            {
                var text = getText();
                ImGui.SetNextItemWidth(320f);
                if (ImGui.InputText($"##{id}text", ref text, 256))
                {
                    setText(text);
                }

                if (ImGui.IsItemDeactivatedAfterEdit())
                {
                    Configuration.Save();
                }
            }

            ImGui.Unindent();
        }
    }
}
