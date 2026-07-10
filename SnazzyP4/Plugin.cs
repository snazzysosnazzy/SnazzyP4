using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using SnazzyP4.Windows;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace SnazzyP4;

/// <summary>
/// The plugin entry point.
/// It owns the configuration, solver and windows, and coordinates the Edit Layout and Move All interactions.
/// </summary>
public sealed class Plugin : IDalamudPlugin
{
    /// <summary>
    /// The Dalamud plugin interface service.
    /// </summary>
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;

    /// <summary>
    /// The Dalamud texture service used to load the icon images.
    /// </summary>
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;

    /// <summary>
    /// The Dalamud command service used to register the slash command.
    /// </summary>
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;

    /// <summary>
    /// The Dalamud framework service used to run game commands on the game thread.
    /// </summary>
    [PluginService] internal static IFramework Framework { get; private set; } = null!;

    /// <summary>
    /// The Dalamud logging service.
    /// </summary>
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    /// <summary>
    /// The Dalamud client-state service, used for the current territory and territory-change notifications.
    /// </summary>
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;

    /// <summary>
    /// The Dalamud duty-state service, used to detect a party wipe.
    /// </summary>
    [PluginService] internal static IDutyState DutyState { get; private set; } = null!;

    /// <summary>
    /// The slash command that opens the plugin.
    /// </summary>
    private const string CommandName = "/snazzyp4";

    /// <summary>
    /// The section windows created for detached mode, one per section.
    /// </summary>
    private readonly List<SectionWindow> sectionWindows = new();

    /// <summary>
    /// The move delta accumulated during the current frame's draws for the group move.
    /// </summary>
    private Vector2 pendingGroupMove;

    /// <summary>
    /// Whether the layout was being edited on the previous transition, used to run the enter and exit behaviour once.
    /// </summary>
    private bool wasLayoutEditActive;

    /// <summary>
    /// The hidden state to restore when Edit Layout or Move All is turned off.
    /// </summary>
    private bool hiddenBeforeLayoutEdit;

    /// <summary>
    /// The parsed plugin configuration.
    /// </summary>
    public Configuration Configuration { get; init; }

    /// <summary>
    /// The fight solver that holds the input state and draws the sections.
    /// </summary>
    public Solver Solver { get; init; }

    /// <summary>
    /// The section definitions in draw order.
    /// </summary>
    public IReadOnlyList<SectionDef> Sections { get; init; }

    /// <summary>
    /// The Dalamud window system that draws the plugin windows.
    /// </summary>
    public readonly WindowSystem WindowSystem = new("SnazzyP4");

    /// <summary>
    /// The unified hub window.
    /// </summary>
    private MainWindow MainWindow { get; init; }

    /// <summary>
    /// The settings window.
    /// </summary>
    private ConfigWindow ConfigWindow { get; init; }

    /// <summary>
    /// The changelog window opened from the settings title bar.
    /// </summary>
    private ChangelogWindow ChangelogWindow { get; init; }

    /// <summary>
    /// The one-time update notice window.
    /// </summary>
    private UpdateWindow UpdateWindow { get; init; }

    /// <summary>
    /// The plugin's assembly version, such as "1.0.0.13".
    /// </summary>
    public static string Version { get; } = typeof(Plugin).Assembly.GetName().Version?.ToString() ?? "0.0.0.0";

    /// <summary>
    /// The version the user was on before this update, captured when the update notice is first shown.
    /// </summary>
    public string UpdateFromVersion { get; private set; } = string.Empty;

    /// <summary>
    /// Whether the update-notice check has already run this session.
    /// </summary>
    private bool updateChecked;

    /// <summary>
    /// The last known territory id, used to detect leaving the captured auto-open/close duty.
    /// </summary>
    private uint lastTerritory;

    /// <summary>
    /// The group move delta published for the current frame.
    /// It is applied one frame after being queued so the window draw order stays simple.
    /// </summary>
    public Vector2 GroupMove { get; private set; }

    /// <summary>
    /// Whether a group drag started over a detached window's background this drag.
    /// </summary>
    public bool GroupDragActive { get; set; }

    /// <summary>
    /// The section ids whose detached position slider changed and must be forced onto the window on the next frame.
    /// </summary>
    public HashSet<string> DetachedPositionDirty { get; } = new();

    /// <summary>
    /// The directory next to the plugin assembly that holds the icon images.
    /// </summary>
    public static string IconDir { get; private set; } = string.Empty;

    /// <summary>
    /// The icon textures preloaded at plugin load and held for the plugin's lifetime, keyed by file name.
    /// Holding rented wraps keeps them resident so a window opening (for example the auto-open on entering the duty)
    /// never has to reload every icon from disk and upload them to the GPU in one burst on the zone-in frame.
    /// </summary>
    private static readonly Dictionary<string, IDalamudTextureWrap> PreloadedIcons = new();

    /// <summary>
    /// Whether the plugin has been disposed, used to release any icon rent that completes after unload.
    /// </summary>
    private static bool iconsDisposed;

    /// <summary>
    /// Whether the layout is being edited, which is true when Edit Layout or Move All is on.
    /// </summary>
    public bool LayoutEditActive => Configuration.EditMode || Configuration.MoveAllActive;

    /// <summary>
    /// Loads the configuration, builds the sections and windows, and registers the plugin hooks.
    /// </summary>
    public Plugin()
    {
        IconDir = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "Icons");
        PreloadIconTextures();

        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        // The Edit Layout and Move All modes are transient interactions and must not carry across sessions.
        Configuration.EditMode = false;
        Configuration.MoveAllActive = false;

        Solver = new Solver(Configuration);

        // The buttons and their text outputs are separate sections so every text element can be positioned independently.
        Sections = new List<SectionDef>
        {
            new("Exdeath", "Exdeath", new Vector2(8, 52), Solver.DrawExdeath, HasButtons: true),
            new("FirstSet", "First Set (text)", new Vector2(8, 330), Solver.DrawFirstSet),
            new("SecondSet", "Second Set (text)", new Vector2(210, 330), Solver.DrawSecondSet),
            new("CombinedSets", "First + Second Set (combined text)", new Vector2(8, 330), Solver.DrawCombinedSets),
            new("FireWaterButtons", "Chaos", new Vector2(340, 52), Solver.DrawFireWaterButtons, HasButtons: true),
            new("ThunderButtons", "Kefka", new Vector2(560, 52), Solver.DrawThunderButtons, HasButtons: true),
            new("ThunderText", "Kefka (text)", new Vector2(560, 240), Solver.DrawThunderText),
            new("LastFakeToggles", "Last Fake toggles", new Vector2(560, 360), Solver.DrawLastFakeToggles, HasButtons: true),
            new("LastFakeThunder", "Last Fake (Thunder)", new Vector2(560, 360), Solver.DrawLastFakeThunderToggle, HasButtons: true),
            new("LastFakeBlizzard", "Last Fake (Blizzard)", new Vector2(700, 360), Solver.DrawLastFakeBlizzardToggle, HasButtons: true),
            new("AnnounceLastFake", "Announce Last Fake button", new Vector2(700, 300), Solver.DrawAnnounceLastFakeButton, HasButtons: true),
            new("Reset", "Reset button", new Vector2(560, 430), Solver.DrawReset, HasButtons: true),
            new("Undo", "Undo button", new Vector2(360, 430), Solver.DrawUndo, HasButtons: true),
            new("Hide", "Hide / Show", new Vector2(460, 430), Solver.DrawHideToggle, HasButtons: true),
        };

        MainWindow = new MainWindow(this);
        ConfigWindow = new ConfigWindow(this);
        ChangelogWindow = new ChangelogWindow();
        UpdateWindow = new UpdateWindow(this);
        WindowSystem.AddWindow(MainWindow);
        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(ChangelogWindow);
        WindowSystem.AddWindow(UpdateWindow);

        foreach (var section in Sections)
        {
            var sectionWindow = new SectionWindow(this, section);
            sectionWindows.Add(sectionWindow);
            WindowSystem.AddWindow(sectionWindow);
        }

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Toggle the Snazzy P4 window. '/snazzyp4 config' opens settings; per-button commands for controllers are listed under Settings, Controller Settings."
        });

        // UpdateWindows runs before the window system draws so the detached windows track the hub state.
        PluginInterface.UiBuilder.Draw += UpdateWindows;
        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;

        // Optional game-state automation: auto open/close on the captured duty, and reset/hide on a party wipe.
        lastTerritory = ClientState.TerritoryType;
        ClientState.TerritoryChanged += OnTerritoryChanged;
        DutyState.DutyWiped += OnDutyWiped;

        Log.Information("Snazzy P4 loaded. Use /snazzyp4 to open.");
    }

    /// <summary>
    /// Unregisters the hooks, disposes the windows and removes the command handler.
    /// </summary>
    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= UpdateWindows;
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        ClientState.TerritoryChanged -= OnTerritoryChanged;
        DutyState.DutyWiped -= OnDutyWiped;

        WindowSystem.RemoveAllWindows();
        MainWindow.Dispose();
        ConfigWindow.Dispose();
        foreach (var sectionWindow in sectionWindows)
        {
            sectionWindow.Dispose();
        }

        CommandManager.RemoveHandler(CommandName);

        // Release the preloaded icon textures last, after every draw hook is unregistered.
        lock (PreloadedIcons)
        {
            iconsDisposed = true;
            foreach (var wrap in PreloadedIcons.Values)
            {
                wrap.Dispose();
            }

            PreloadedIcons.Clear();
        }
    }

    /// <summary>
    /// Starts loading every button icon into a rented texture wrap held for the plugin's lifetime.
    /// This runs once at plugin load (login or enable - a calm moment), so the icons are already resident
    /// by the time any window opens and can never be evicted from the cache while the windows are closed.
    /// </summary>
    private static void PreloadIconTextures()
    {
        iconsDisposed = false;
        string[] files;
        try
        {
            files = Directory.GetFiles(IconDir, "*.png");
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "Could not enumerate the icon directory; icons will load on demand instead.");
            return;
        }

        foreach (var path in files)
        {
            var name = Path.GetFileName(path);
            TextureProvider.GetFromFile(path).RentAsync().ContinueWith(task =>
            {
                if (!task.IsCompletedSuccessfully)
                {
                    // Reading task.Exception also marks a faulted task as observed.
                    Log.Warning(task.Exception, $"Could not preload icon {name}; it will load on demand instead.");
                    return;
                }

                lock (PreloadedIcons)
                {
                    // A rent that lands after unload (or a duplicate) is released immediately so nothing leaks.
                    if (iconsDisposed || !PreloadedIcons.TryAdd(name, task.Result))
                    {
                        task.Result.Dispose();
                    }
                }
            });
        }
    }

    /// <summary>
    /// Returns a preloaded icon texture by file name, or null while it is still loading (or failed to load),
    /// in which case the caller falls back to the on-demand shared texture.
    /// </summary>
    public static IDalamudTextureWrap? PreloadedIcon(string file)
    {
        lock (PreloadedIcons)
        {
            return PreloadedIcons.TryGetValue(file, out var wrap) ? wrap : null;
        }
    }

    /// <summary>
    /// Opens the plugin window when entering, and closes it when leaving, the captured auto-open/close duty.
    /// </summary>
    private void OnTerritoryChanged(uint territory)
    {
        if (Configuration.AutoOpenCloseOnDuty && Configuration.AutoDutyTerritoryId != 0)
        {
            if (territory == Configuration.AutoDutyTerritoryId)
            {
                MainWindow.IsOpen = true;
            }
            else if (lastTerritory == Configuration.AutoDutyTerritoryId)
            {
                MainWindow.IsOpen = false;
            }
        }

        lastTerritory = territory;
    }

    /// <summary>
    /// Runs the optional Reset and Hide actions when the party wipes.
    /// </summary>
    private void OnDutyWiped(Dalamud.Game.DutyState.IDutyStateEventArgs args)
    {
        if (Configuration.ResetOnWipe)
        {
            Solver.ResetAll();
        }

        if (Configuration.HideOnWipe)
        {
            Solver.SetHidden(true);
        }
    }

    /// <summary>
    /// Queues a move delta to be applied to the detached windows on the next frame.
    /// </summary>
    public void QueueGroupMove(Vector2 delta) => pendingGroupMove += delta;

    /// <summary>
    /// Runs the per-frame housekeeping before the window system draws.
    /// It publishes the group move, updates the group drag, fires auto-markers and sets each detached window's visibility.
    /// </summary>
    private void UpdateWindows()
    {
        GroupMove = pendingGroupMove;
        pendingGroupMove = Vector2.Zero;

        // A group drag ends when the mouse is released; while it is active and Move All is on, feed the mouse delta into the group move.
        if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            GroupDragActive = false;
        }

        if (GroupDragActive && Configuration.MoveAllActive)
        {
            QueueGroupMove(ImGui.GetIO().MouseDelta);
        }

        // The detached window positions are synced live in memory each frame, so they only need to be persisted once a drag finishes.
        if (ImGui.IsMouseReleased(ImGuiMouseButton.Left)
            && Configuration.Detached
            && (Configuration.EditMode || Configuration.MoveAllActive))
        {
            Configuration.Save();
        }

        Solver.UpdateAutoMarkers();
        Solver.MaybeSendChronological();

        // The Hide control stays visible even when hidden and only when it is the floating variant, the Reset control only shows while floating, and every other section hides when the display is hidden or empty.
        var baseShow = Configuration.Detached && MainWindow.IsOpen;
        foreach (var sectionWindow in sectionWindows)
        {
            bool show;
            if (sectionWindow.Id == "Hide")
            {
                show = baseShow && Configuration.FloatingHideButton;
            }
            else if (sectionWindow.Id == "Reset")
            {
                show = baseShow && !Configuration.Hidden && Configuration.FloatingResetButton;
            }
            else if (sectionWindow.Id == "Undo")
            {
                show = baseShow && !Configuration.Hidden && Configuration.FloatingUndoButton;
            }
            else
            {
                show = baseShow && !Configuration.Hidden;
            }

            if (show && !SectionEnabled(sectionWindow.Id))
            {
                show = false;
            }

            if (show && !sectionWindow.HasButtons && !Solver.SectionHasContent(sectionWindow.Id))
            {
                show = false;
            }

            sectionWindow.IsOpen = show;
        }
    }

    /// <summary>
    /// Executes a game command exactly as if the player typed it, dispatched onto the framework thread.
    /// This sends input to the game on the player's behalf, so it is only used by the opt-in marker and party features.
    /// </summary>
    public static void ExecuteGameCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return;
        }

        Framework.RunOnFrameworkThread(() => SendCommandUnsafe(command));
    }

    /// <summary>
    /// Sends a command string through the game's shell module.
    /// </summary>
    private static unsafe void SendCommandUnsafe(string command)
    {
        var uiModule = UIModule.Instance();
        if (uiModule == null)
        {
            return;
        }

        var shellModule = uiModule->GetRaptureShellModule();
        if (shellModule == null)
        {
            return;
        }

        var commandString = Utf8String.FromString(command);
        if (commandString == null)
        {
            return;
        }

        try
        {
            shellModule->ExecuteCommandInner(commandString, uiModule);
        }
        finally
        {
            commandString->Dtor(true);
        }
    }

    /// <summary>
    /// Handles the slash command.
    /// With no argument it toggles the main window, "config" opens the settings, and every button argument performs the same action as clicking that button so controller players can drive the plugin from macros.
    /// </summary>
    private void OnCommand(string command, string arguments)
    {
        var normalized = arguments.Trim().ToLowerInvariant();
        switch (normalized)
        {
            case "config":
            case "settings":
            case "cfg":
                ConfigWindow.Toggle();
                break;
            case "exdeathreal":
                Solver.CommandExdeath(true);
                break;
            case "exdeathfake":
                Solver.CommandExdeath(false);
                break;
            case "lightningshort":
                Solver.CommandLightning(true);
                break;
            case "lightninglong":
                Solver.CommandLightning(false);
                break;
            case "dropshort":
                Solver.CommandDrop(true);
                break;
            case "droplong":
                Solver.CommandDrop(false);
                break;
            case "accelerationshort":
                Solver.CommandAcceleration(true);
                break;
            case "accelerationlong":
                Solver.CommandAcceleration(false);
                break;
            case "infernoreal":
                Solver.CommandInferno(true);
                break;
            case "infernofake":
                Solver.CommandInferno(false);
                break;
            case "tsunamireal":
                Solver.CommandTsunami(true);
                break;
            case "tsunamifake":
                Solver.CommandTsunami(false);
                break;
            case "thunderreal":
                Solver.CommandThunder(true);
                break;
            case "thunderfake":
                Solver.CommandThunder(false);
                break;
            case "blizzardreal":
                Solver.CommandBlizzard(true);
                break;
            case "blizzardfake":
                Solver.CommandBlizzard(false);
                break;
            case "reset":
                Solver.ResetAll();
                break;
            case "undo":
                Solver.Undo();
                break;
            case "hide":
                Solver.SetHidden(!Configuration.Hidden);
                break;
            case "lastthunderreal" when Configuration.ShowLastFake:
                Solver.CommandLastThunder(false);
                break;
            case "lastthunderfake" when Configuration.ShowLastFake:
                Solver.CommandLastThunder(true);
                break;
            case "lastblizzardreal" when Configuration.ShowLastFake:
                Solver.CommandLastBlizzard(false);
                break;
            case "lastblizzardfake" when Configuration.ShowLastFake:
                Solver.CommandLastBlizzard(true);
                break;
            default:
                MainWindow.Toggle();
                break;
        }
    }

    /// <summary>
    /// Toggles the main hub window.
    /// </summary>
    private void ToggleMainUi() => MainWindow.Toggle();

    /// <summary>
    /// Toggles the settings window.
    /// </summary>
    public void ToggleConfigUi() => ConfigWindow.Toggle();

    /// <summary>
    /// Toggles the changelog window.
    /// </summary>
    public void ToggleChangelog() => ChangelogWindow.Toggle();

    /// <summary>
    /// Returns the changelog entries newer than the given version, or every entry when the version is empty.
    /// </summary>
    public Changelog.Entry[] ChangesSince(string from)
    {
        if (string.IsNullOrEmpty(from) || !System.Version.TryParse(from, out var fromVersion))
        {
            return Changelog.Entries;
        }

        var newer = new List<Changelog.Entry>();
        foreach (var entry in Changelog.Entries)
        {
            if (System.Version.TryParse(entry.Version, out var entryVersion) && entryVersion > fromVersion)
            {
                newer.Add(entry);
            }
        }

        return newer.ToArray();
    }

    /// <summary>
    /// Shows the one-time update notice the first time the plugin is opened after updating, then records the version so it does not show again until the next update.
    /// </summary>
    public void MaybeShowUpdateNotice()
    {
        if (updateChecked)
        {
            return;
        }

        updateChecked = true;
        if (Configuration.LastSeenVersion == Version)
        {
            return;
        }

        UpdateFromVersion = Configuration.LastSeenVersion;
        Configuration.LastSeenVersion = Version;
        Configuration.Save();

        // Advance the seen version even when suppressed, so the notice does not queue up for a later boot.
        if (Configuration.SuppressUpdateNotices)
        {
            return;
        }

        UpdateWindow.IsOpen = true;
    }

    /// <summary>
    /// Toggles Edit Layout, keeping it mutually exclusive with Move All and running the shared enter and exit behaviour.
    /// </summary>
    public void SetEditMode(bool on)
    {
        if (Configuration.EditMode == on)
        {
            return;
        }

        Configuration.EditMode = on;
        if (on)
        {
            Configuration.MoveAllActive = false;
        }

        ApplyLayoutEditTransition();
        Configuration.Save();
    }

    /// <summary>
    /// Toggles Move All, keeping it mutually exclusive with Edit Layout and running the shared enter and exit behaviour.
    /// </summary>
    public void SetMoveAll(bool on)
    {
        if (Configuration.MoveAllActive == on)
        {
            return;
        }

        Configuration.MoveAllActive = on;
        if (on)
        {
            Configuration.EditMode = false;
        }

        ApplyLayoutEditTransition();
        Configuration.Save();
    }

    /// <summary>
    /// Restores every setting to defaults and clears the transient layout-edit state.
    /// </summary>
    public void RestoreAllDefaults()
    {
        Configuration.RestoreDefaults();
        wasLayoutEditActive = false;
        DetachedPositionDirty.Clear();
    }

    /// <summary>
    /// Runs the shared behaviour when the layout-edit state changes.
    /// Entering clears the board and forces the UI visible while remembering the prior hidden state, and leaving restores that hidden state.
    /// </summary>
    private void ApplyLayoutEditTransition()
    {
        var active = LayoutEditActive;
        if (active && !wasLayoutEditActive)
        {
            hiddenBeforeLayoutEdit = Configuration.Hidden;
            Configuration.Hidden = false;
            Solver.ResetAll();
        }
        else if (!active && wasLayoutEditActive)
        {
            Configuration.Hidden = hiddenBeforeLayoutEdit;
        }

        wasLayoutEditActive = active;
    }

    /// <summary>
    /// Builds the full path to an icon image inside the plugin's icon directory.
    /// </summary>
    public static string Icon(string file) => Path.Combine(IconDir, file);

    /// <summary>
    /// Determines whether a section participates in the current configuration.
    /// The detached Last Fake toggle sections only exist when their settings enable them, and every button section is hidden while the controller macro-button option is on.
    /// </summary>
    public bool SectionEnabled(string id)
    {
        if (Configuration.HideMacroButtons && IsButtonSection(id))
        {
            return false;
        }

        return id switch
        {
            "LastFakeToggles" => Configuration.ShowLastFake && Configuration.DetachToggleButtons
                                 && !Configuration.ToggleButtonsIndividualPanels,
            "LastFakeThunder" or "LastFakeBlizzard" => Configuration.ShowLastFake && Configuration.DetachToggleButtons
                                 && Configuration.ToggleButtonsIndividualPanels,
            "AnnounceLastFake" => Configuration.ShowLastFake && Configuration.LastFakeAnnounceEnabled
                                 && !Configuration.LastFakeAnnounceDocked,
            "FirstSet" or "SecondSet" => !Configuration.CombineSets,
            "CombinedSets" => Configuration.CombineSets,
            _ => true,
        };
    }

    /// <summary>
    /// Determines whether a section is a button section rather than a text panel.
    /// </summary>
    private bool IsButtonSection(string id)
    {
        foreach (var section in Sections)
        {
            if (section.Id == id)
            {
                return section.HasButtons;
            }
        }

        return false;
    }

    /// <summary>
    /// Clamps every detached window position back inside the main viewport so windows dragged or imported off-screen come back into view.
    /// </summary>
    public void RecenterDetachedWindows()
    {
        var viewport = ImGui.GetMainViewport();
        var min = viewport.WorkPos;
        var max = Vector2.Max(min, viewport.WorkPos + viewport.WorkSize - new Vector2(80f, 40f));
        foreach (var section in Sections)
        {
            var position = Configuration.GetDetachedPosition(section.Id, section.DefaultOffset);
            Configuration.SetDetachedPosition(section.Id, Vector2.Clamp(position, min, max));
            DetachedPositionDirty.Add(section.Id);
        }

        Configuration.Save();
    }

    /// <summary>
    /// Marks every detached window position dirty so each window re-reads its saved position on the next frame.
    /// This is used after importing a settings profile so the windows jump to the imported positions.
    /// </summary>
    public void MarkAllPositionsDirty()
    {
        foreach (var section in Sections)
        {
            DetachedPositionDirty.Add(section.Id);
        }
    }
}
