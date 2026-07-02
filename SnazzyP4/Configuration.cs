using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using Dalamud.Configuration;

namespace SnazzyP4;

/// <summary>
/// The persisted plugin settings.
/// Per-section appearance and scale values are keyed by mode (windowed versus detached) so each layout mode keeps its own look and layout.
/// </summary>
[Serializable]
public class Configuration : IPluginConfiguration
{
    /// <summary>
    /// The configuration schema version used by Dalamud for migrations.
    /// </summary>
    public int Version { get; set; } = 0;

    /// <summary>
    /// The user-facing scale multiplier applied on top of Dalamud's global scale.
    /// </summary>
    public float UiScale { get; set; } = 1.0f;

    /// <summary>
    /// Whether the player is playing a Support role.
    /// Support resolves to the Ignore1/Bind1 markers and the A (stack) and D (spread) target letters, while DPS uses Ignore2/Bind2 and the C and B letters.
    /// </summary>
    public bool IsSupport { get; set; } = true;

    /// <summary>
    /// Whether the plugin should place the spread marker on the player automatically once a spread is determined.
    /// </summary>
    public bool AutoMarker { get; set; } = true;

    /// <summary>
    /// Whether header and label text is hidden universally, while keeping the First Set and Second Set labels.
    /// </summary>
    public bool HideLabels { get; set; }

    /// <summary>
    /// Whether the hidden Last Fake toggles have been unlocked through the settings unlock flow.
    /// </summary>
    public bool ShowLastFake { get; set; }

    /// <summary>
    /// Whether the Last Fake toggles render as plain checkboxes instead of the coloured REAL/FAKE buttons.
    /// </summary>
    public bool UseBasicToggles { get; set; }

    /// <summary>
    /// Whether the Last Fake buttons use the custom text labels instead of the default REAL and FAKE.
    /// </summary>
    public bool UseCustomToggleText { get; set; }

    /// <summary>
    /// The custom label shown while a Last Fake toggle is in the REAL state.
    /// An empty value renders a square button with no text.
    /// </summary>
    public string CustomRealText { get; set; } = string.Empty;

    /// <summary>
    /// The custom label shown while a Last Fake toggle is in the FAKE state.
    /// An empty value renders a square button with no text.
    /// </summary>
    public string CustomFakeText { get; set; } = string.Empty;

    /// <summary>
    /// Whether the Last Fake buttons inherit the Kefka text panel's scale and opacity instead of the dedicated toggle values.
    /// </summary>
    public bool UseSharedToggleSettings { get; set; } = true;

    /// <summary>
    /// The horizontal scale multiplier for the Last Fake buttons when shared settings are off.
    /// </summary>
    public float ToggleButtonScaleX { get; set; } = 1.0f;

    /// <summary>
    /// The vertical scale multiplier for the Last Fake buttons when shared settings are off.
    /// </summary>
    public float ToggleButtonScaleY { get; set; } = 1.0f;

    /// <summary>
    /// The text scale multiplier for the Last Fake button labels when shared settings are off.
    /// </summary>
    public float ToggleTextScale { get; set; } = 1.0f;

    /// <summary>
    /// The opacity for the Last Fake buttons when shared settings are off.
    /// </summary>
    public float ToggleButtonAlpha { get; set; } = 1.0f;

    /// <summary>
    /// Whether the Last Fake toggles are pulled out of the Kefka text panel into their own section.
    /// </summary>
    public bool DetachToggleButtons { get; set; }

    /// <summary>
    /// Whether the detached Last Fake toggles are laid out side by side instead of stacked.
    /// </summary>
    public bool ToggleButtonsHorizontal { get; set; }

    /// <summary>
    /// Whether the detached Last Fake toggles are split into one panel per button.
    /// </summary>
    public bool ToggleButtonsIndividualPanels { get; set; }

    /// <summary>
    /// Whether gaze resolutions are announced in party chat.
    /// </summary>
    public bool PartyGazeEnabled { get; set; }

    /// <summary>
    /// Whether the custom gaze message is sent instead of the default one.
    /// </summary>
    public bool PartyGazeCustom { get; set; }

    /// <summary>
    /// The custom party message sent when a gaze is determined.
    /// </summary>
    public string PartyGazeCustomText { get; set; } = string.Empty;

    /// <summary>
    /// Whether chaos resolutions are announced in party chat.
    /// </summary>
    public bool PartyChaosEnabled { get; set; }

    /// <summary>
    /// Whether the custom chaos message is sent instead of the default one.
    /// </summary>
    public bool PartyChaosCustom { get; set; }

    /// <summary>
    /// The custom party message sent when a chaos twister is determined.
    /// </summary>
    public string PartyChaosCustomText { get; set; } = string.Empty;

    /// <summary>
    /// Whether each section is drawn as its own floating window instead of inside the single hub window.
    /// </summary>
    public bool Detached { get; set; }

    /// <summary>
    /// Whether the layout is being edited, allowing sections to be dragged.
    /// This is a transient interaction and is never carried across sessions.
    /// </summary>
    public bool EditMode { get; set; }

    /// <summary>
    /// Whether all sections are hidden behind a single Show control, keeping their positions and scales.
    /// </summary>
    public bool Hidden { get; set; }

    /// <summary>
    /// Whether the Hide/Show control is a separate floating section rather than a toolbar button.
    /// </summary>
    public bool FloatingHideButton { get; set; }

    /// <summary>
    /// Whether dragging any detached window moves them all together.
    /// This is a transient interaction and is never carried across sessions.
    /// </summary>
    public bool MoveAllActive { get; set; }

    /// <summary>
    /// Whether the toolbar with the Edit, Detached, Move All and Reset controls is shown.
    /// </summary>
    public bool ShowToolbar { get; set; } = true;

    /// <summary>
    /// Whether the toolbar is collapsed to a single expand button.
    /// </summary>
    public bool ToolbarCollapsed { get; set; }

    /// <summary>
    /// Whether the universal appearance values apply to everything.
    /// When this is off, the per-section overrides are used instead.
    /// </summary>
    public bool UseUniversalSettings { get; set; } = true;

    /// <summary>
    /// The universal window background opacity from zero to one.
    /// </summary>
    public float BackgroundAlpha { get; set; } = 1.0f;

    /// <summary>
    /// Whether title bars are hidden universally.
    /// </summary>
    public bool NoTitleBar { get; set; } = true;

    /// <summary>
    /// The universal icon-button opacity from zero to one.
    /// </summary>
    public float ButtonAlpha { get; set; } = 1.0f;

    /// <summary>
    /// Whether the windows pass the mouse through to the game, which also disables the buttons.
    /// This value always applies universally.
    /// </summary>
    public bool ClickThrough { get; set; }

    /// <summary>
    /// The per-section, per-mode background opacity overrides.
    /// </summary>
    public Dictionary<string, float> SectionBackgroundAlpha { get; set; } = new();

    /// <summary>
    /// The per-section, per-mode hide-title-bar overrides.
    /// </summary>
    public Dictionary<string, bool> SectionNoTitleBar { get; set; } = new();

    /// <summary>
    /// The per-section, per-mode hide-labels overrides.
    /// </summary>
    public Dictionary<string, bool> SectionHideLabels { get; set; } = new();

    /// <summary>
    /// The per-section, per-mode icon-button opacity overrides.
    /// </summary>
    public Dictionary<string, float> SectionButtonAlpha { get; set; } = new();

    /// <summary>
    /// The per-section, per-mode scale multipliers applied on top of the global scale.
    /// </summary>
    public Dictionary<string, float> SectionScales { get; set; } = new();

    /// <summary>
    /// The per-section cursor offsets used to position sections inside the windowed hub.
    /// </summary>
    public Dictionary<string, Vector2> Offsets { get; set; } = new();

    /// <summary>
    /// The per-section screen positions used to place the detached windows.
    /// </summary>
    public Dictionary<string, Vector2> DetachedPositions { get; set; } = new();

    /// <summary>
    /// The colour of the Spread callout letter for the Support role.
    /// </summary>
    public Vector4 ColorSpreadSupport { get; set; } = new(0.72f, 0.42f, 0.98f, 1f);

    /// <summary>
    /// The colour of the Stack callout letter for the Support role.
    /// </summary>
    public Vector4 ColorStackSupport { get; set; } = new(0.95f, 0.22f, 0.20f, 1f);

    /// <summary>
    /// The colour of the Spread callout letter for the DPS role.
    /// </summary>
    public Vector4 ColorSpreadDps { get; set; } = new(1.00f, 0.85f, 0.20f, 1f);

    /// <summary>
    /// The colour of the Stack callout letter for the DPS role.
    /// </summary>
    public Vector4 ColorStackDps { get; set; } = new(0.30f, 0.55f, 1.00f, 1f);

    /// <summary>
    /// The colour of the Acceleration movement callout.
    /// </summary>
    public Vector4 ColorAcceleration { get; set; } = new(1.00f, 0.12f, 0.12f, 1f);

    /// <summary>
    /// The colour of the real gaze callout.
    /// </summary>
    public Vector4 ColorGazeReal { get; set; } = new(0.45f, 0.90f, 0.45f, 1f);

    /// <summary>
    /// The colour of the fake gaze callout.
    /// </summary>
    public Vector4 ColorGazeFake { get; set; } = new(1.00f, 0.65f, 0.25f, 1f);

    /// <summary>
    /// The colour of the fire (Inferno) chaos callout.
    /// </summary>
    public Vector4 ColorFire { get; set; } = new(0.95f, 0.22f, 0.20f, 1f);

    /// <summary>
    /// The colour of the water (Tsunami) chaos callout.
    /// </summary>
    public Vector4 ColorWater { get; set; } = new(0.32f, 0.52f, 1.00f, 1f);

    /// <summary>
    /// The colour of the Thunder callout.
    /// </summary>
    public Vector4 ColorThunder { get; set; } = new(0.78f, 0.42f, 0.95f, 1f);

    /// <summary>
    /// The colour of the Blizzard callout.
    /// </summary>
    public Vector4 ColorBlizzard { get; set; } = new(0.36f, 0.80f, 1.00f, 1f);

    /// <summary>
    /// The colour of a Last Fake toggle in the REAL state.
    /// </summary>
    public Vector4 ColorToggleReal { get; set; } = new(0.20f, 0.65f, 0.25f, 1f);

    /// <summary>
    /// The colour of a Last Fake toggle in the FAKE state.
    /// </summary>
    public Vector4 ColorToggleFake { get; set; } = new(0.80f, 0.22f, 0.22f, 1f);

    /// <summary>
    /// Builds the dictionary key for a per-section value in the current layout mode.
    /// </summary>
    private string ModeKey(string sectionId) => (Detached ? "d:" : "w:") + sectionId;

    /// <summary>
    /// Gets the windowed offset for a section, falling back to the supplied default.
    /// </summary>
    public Vector2 GetOffset(string sectionId, Vector2 fallback)
        => Offsets.TryGetValue(sectionId, out var offset) ? offset : fallback;

    /// <summary>
    /// Stores the windowed offset for a section.
    /// </summary>
    public void SetOffset(string sectionId, Vector2 offset) => Offsets[sectionId] = offset;

    /// <summary>
    /// Gets the detached window position for a section, falling back to the supplied default.
    /// </summary>
    public Vector2 GetDetachedPosition(string sectionId, Vector2 fallback)
        => DetachedPositions.TryGetValue(sectionId, out var position) ? position : fallback;

    /// <summary>
    /// Stores the detached window position for a section.
    /// </summary>
    public void SetDetachedPosition(string sectionId, Vector2 position) => DetachedPositions[sectionId] = position;

    /// <summary>
    /// Gets the scale multiplier for a section in the current mode.
    /// </summary>
    public float GetSectionScale(string sectionId)
        => SectionScales.TryGetValue(ModeKey(sectionId), out var scale) ? scale : 1.0f;

    /// <summary>
    /// Stores the scale multiplier for a section in the current mode.
    /// </summary>
    public void SetSectionScale(string sectionId, float scale) => SectionScales[ModeKey(sectionId)] = scale;

    /// <summary>
    /// Resolves the effective background opacity for a section, using the universal value or the per-section override.
    /// </summary>
    public float EffectiveBackgroundAlpha(string sectionId)
        => UseUniversalSettings ? BackgroundAlpha : GetSectionBackgroundAlpha(sectionId);

    /// <summary>
    /// Resolves the effective hide-title-bar flag for a section, using the universal value or the per-section override.
    /// </summary>
    public bool EffectiveNoTitleBar(string sectionId)
        => UseUniversalSettings ? NoTitleBar : GetSectionNoTitleBar(sectionId);

    /// <summary>
    /// Resolves the effective hide-labels flag for a section, using the universal value or the per-section override.
    /// </summary>
    public bool EffectiveHideLabels(string sectionId)
        => UseUniversalSettings ? HideLabels : GetSectionHideLabels(sectionId);

    /// <summary>
    /// Resolves the effective button opacity for a section, using the universal value or the per-section override.
    /// </summary>
    public float EffectiveButtonAlpha(string sectionId)
        => UseUniversalSettings ? ButtonAlpha : GetSectionButtonAlpha(sectionId);

    /// <summary>
    /// Gets the per-section background opacity for the current mode, defaulting to fully opaque.
    /// </summary>
    public float GetSectionBackgroundAlpha(string sectionId)
        => SectionBackgroundAlpha.TryGetValue(ModeKey(sectionId), out var alpha) ? alpha : 1.0f;

    /// <summary>
    /// Stores the per-section background opacity for the current mode.
    /// </summary>
    public void SetSectionBackgroundAlpha(string sectionId, float alpha) => SectionBackgroundAlpha[ModeKey(sectionId)] = alpha;

    /// <summary>
    /// Gets the per-section hide-title-bar flag for the current mode, defaulting to hidden.
    /// </summary>
    public bool GetSectionNoTitleBar(string sectionId)
        => SectionNoTitleBar.TryGetValue(ModeKey(sectionId), out var hidden) ? hidden : true;

    /// <summary>
    /// Stores the per-section hide-title-bar flag for the current mode.
    /// </summary>
    public void SetSectionNoTitleBar(string sectionId, bool hidden) => SectionNoTitleBar[ModeKey(sectionId)] = hidden;

    /// <summary>
    /// Gets the per-section hide-labels flag for the current mode, defaulting to shown.
    /// </summary>
    public bool GetSectionHideLabels(string sectionId)
        => SectionHideLabels.TryGetValue(ModeKey(sectionId), out var hidden) && hidden;

    /// <summary>
    /// Stores the per-section hide-labels flag for the current mode.
    /// </summary>
    public void SetSectionHideLabels(string sectionId, bool hidden) => SectionHideLabels[ModeKey(sectionId)] = hidden;

    /// <summary>
    /// Gets the per-section button opacity for the current mode, defaulting to fully opaque.
    /// </summary>
    public float GetSectionButtonAlpha(string sectionId)
        => SectionButtonAlpha.TryGetValue(ModeKey(sectionId), out var alpha) ? alpha : 1.0f;

    /// <summary>
    /// Stores the per-section button opacity for the current mode.
    /// </summary>
    public void SetSectionButtonAlpha(string sectionId, float alpha) => SectionButtonAlpha[ModeKey(sectionId)] = alpha;

    /// <summary>
    /// Resets every setting to its default value, except the schema version, and persists the result.
    /// The reset is done by copying from a fresh instance so new settings are covered automatically.
    /// </summary>
    public void RestoreDefaults()
    {
        var defaults = new Configuration();
        foreach (var property in typeof(Configuration).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanRead || !property.CanWrite || property.Name == nameof(Version))
            {
                continue;
            }

            property.SetValue(this, property.GetValue(defaults));
        }

        Save();
    }

    /// <summary>
    /// Persists the configuration to disk.
    /// </summary>
    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);
}
