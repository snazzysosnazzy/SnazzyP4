using System.Collections.Generic;
using System.IO;
using System.Numerics;
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
    /// Whether the layout is being edited, which is true when Edit Layout or Move All is on.
    /// </summary>
    public bool LayoutEditActive => Configuration.EditMode || Configuration.MoveAllActive;

    /// <summary>
    /// Loads the configuration, builds the sections and windows, and registers the plugin hooks.
    /// </summary>
    public Plugin()
    {
        IconDir = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "Icons");

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
            new("FireWaterButtons", "Chaos", new Vector2(340, 52), Solver.DrawFireWaterButtons, HasButtons: true),
            new("ThunderButtons", "Kefka", new Vector2(560, 52), Solver.DrawThunderButtons, HasButtons: true),
            new("ThunderText", "Kefka (text)", new Vector2(560, 240), Solver.DrawThunderText),
            new("LastFakeToggles", "Last Fake toggles", new Vector2(560, 360), Solver.DrawLastFakeToggles, HasButtons: true),
            new("LastFakeThunder", "Last Fake (Thunder)", new Vector2(560, 360), Solver.DrawLastFakeThunderToggle, HasButtons: true),
            new("LastFakeBlizzard", "Last Fake (Blizzard)", new Vector2(700, 360), Solver.DrawLastFakeBlizzardToggle, HasButtons: true),
            new("Reset", "Reset button", new Vector2(560, 430), Solver.DrawReset, HasButtons: true),
            new("Hide", "Hide / Show", new Vector2(460, 430), Solver.DrawHideToggle, HasButtons: true),
        };

        MainWindow = new MainWindow(this);
        ConfigWindow = new ConfigWindow(this);
        WindowSystem.AddWindow(MainWindow);
        WindowSystem.AddWindow(ConfigWindow);

        foreach (var section in Sections)
        {
            var sectionWindow = new SectionWindow(this, section);
            sectionWindows.Add(sectionWindow);
            WindowSystem.AddWindow(sectionWindow);
        }

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Toggle the Snazzy P4 window. Use '/snazzyp4 config' to open settings."
        });

        // UpdateWindows runs before the window system draws so the detached windows track the hub state.
        PluginInterface.UiBuilder.Draw += UpdateWindows;
        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;

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

        WindowSystem.RemoveAllWindows();
        MainWindow.Dispose();
        ConfigWindow.Dispose();
        foreach (var sectionWindow in sectionWindows)
        {
            sectionWindow.Dispose();
        }

        CommandManager.RemoveHandler(CommandName);
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

        // The Hide control stays visible even when hidden and only when it is the floating variant, while every other section hides when the display is hidden or empty.
        var baseShow = Configuration.Detached && MainWindow.IsOpen;
        foreach (var sectionWindow in sectionWindows)
        {
            var show = sectionWindow.Id == "Hide"
                ? baseShow && Configuration.FloatingHideButton
                : baseShow && !Configuration.Hidden;

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
    /// Handles the slash command, opening settings for the config argument and toggling the main window otherwise.
    /// </summary>
    private void OnCommand(string command, string arguments)
    {
        var normalized = arguments.Trim().ToLowerInvariant();
        if (normalized is "config" or "settings" or "cfg")
        {
            ConfigWindow.Toggle();
        }
        else
        {
            MainWindow.Toggle();
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
    /// The detached Last Fake toggle sections only exist when their settings enable them.
    /// </summary>
    public bool SectionEnabled(string id)
    {
        return id switch
        {
            "LastFakeToggles" => Configuration.ShowLastFake && Configuration.DetachToggleButtons
                                 && !Configuration.ToggleButtonsIndividualPanels,
            "LastFakeThunder" or "LastFakeBlizzard" => Configuration.ShowLastFake && Configuration.DetachToggleButtons
                                 && Configuration.ToggleButtonsIndividualPanels,
            _ => true,
        };
    }
}
