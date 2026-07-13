using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using Dalamud.Configuration;

namespace SnazzyP4
{
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
        /// The plugin version that last showed its update notice, used to decide whether to show the changelog after an update.
        /// An empty value means the notice has never been shown.
        /// </summary>
        public string LastSeenVersion { get; set; } = string.Empty;

        /// <summary>
        /// When true, the update/changelog notice is never shown automatically after a version change.
        /// The changelog remains available on demand from the title-bar button.
        /// </summary>
        public bool SuppressUpdateNotices { get; set; }

        /// <summary>
        /// The global scale multiplier applied to everything the plugin draws, on top of Dalamud's global scale.
        /// </summary>
        public float UiScale { get; set; } = 0.5f;

        /// <summary>
        /// The scale multiplier applied to the quick-settings toolbar, layered on top of the global scale.
        /// </summary>
        public float ToolbarScale { get; set; } = 1.0f;

        /// <summary>
        /// The scale multiplier applied to the macro buttons and text panels, layered on top of the global scale.
        /// </summary>
        public float MacroUiScale { get; set; } = 1.0f;

        /// <summary>
        /// Whether the player is playing a Support role.
        /// Support resolves to the Ignore1/Bind1 markers and the A (stack) and D (spread) target letters, while DPS uses Ignore2/Bind2 and the C and B letters.
        /// </summary>
        public bool IsSupport { get; set; }

        /// <summary>
        /// Whether the plugin should place the spread marker on the player automatically once a spread is determined.
        /// </summary>
        public bool AutoMarker { get; set; } = true;

        /// <summary>
        /// The head marker placed for the first (short) set spread while playing a Support role.
        /// An empty value places no marker.
        /// </summary>
        public string MarkerFirstSetSupport { get; set; } = "ignore1";

        /// <summary>
        /// The head marker placed for the first (short) set spread while playing a DPS role.
        /// An empty value places no marker.
        /// </summary>
        public string MarkerFirstSetDps { get; set; } = "ignore2";

        /// <summary>
        /// The head marker placed for the second (long) set spread while playing a Support role.
        /// An empty value places no marker.
        /// </summary>
        public string MarkerSecondSetSupport { get; set; } = "bind1";

        /// <summary>
        /// The head marker placed for the second (long) set spread while playing a DPS role.
        /// An empty value places no marker.
        /// </summary>
        public string MarkerSecondSetDps { get; set; } = "bind2";

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
        /// Whether the Last Fake ANNOUNCE button is shown, which announces the current Kefka text to a chat channel.
        /// </summary>
        public bool LastFakeAnnounceEnabled { get; set; }

        /// <summary>
        /// Whether the ANNOUNCE button is docked inside the Kefka text panel rather than floating as its own button.
        /// </summary>
        public bool LastFakeAnnounceDocked { get; set; }

        /// <summary>
        /// Which side of the Kefka text panel the docked ANNOUNCE button anchors to: "top", "bottom", "left" or "right".
        /// </summary>
        public string LastFakeAnnounceDockSide { get; set; } = "top";

        /// <summary>
        /// The chat channel the Last Fake ANNOUNCE button sends to.
        /// </summary>
        public string LastFakeAnnounceChannel { get; set; } = "/p";

        /// <summary>
        /// The message sent by the ANNOUNCE button, where {KefkaThunder} and {KefkaBlizzard} are replaced with the current values.
        /// </summary>
        public string LastFakeAnnounceMessage { get; set; } = "Thunder: {KefkaThunder}  Blizzard: {KefkaBlizzard}";

        /// <summary>
        /// The text {KefkaThunder} and {KefkaBlizzard} resolve to when that mechanic is currently real.
        /// </summary>
        public string LastFakeAnnounceRealText { get; set; } = "REAL";

        /// <summary>
        /// The text {KefkaThunder} and {KefkaBlizzard} resolve to when that mechanic is currently fake.
        /// </summary>
        public string LastFakeAnnounceFakeText { get; set; } = "FAKE";

        /// <summary>
        /// Whether the plugin window auto-opens on entering, and auto-closes on leaving, the captured duty (see <see cref="AutoDutyTerritoryId"/>). Off by default.
        /// </summary>
        public bool AutoOpenCloseOnDuty { get; set; }

        /// <summary>
        /// The territory id captured for <see cref="AutoOpenCloseOnDuty"/>. Zero means none captured yet.
        /// </summary>
        public uint AutoDutyTerritoryId { get; set; }

        /// <summary>
        /// Whether pressing the Hide button also runs Reset. Off by default.
        /// </summary>
        public bool ResetOnHide { get; set; }

        /// <summary>
        /// Whether a party wipe runs Reset. Off by default.
        /// </summary>
        public bool ResetOnWipe { get; set; }

        /// <summary>
        /// Whether a party wipe hides the display. Off by default.
        /// </summary>
        public bool HideOnWipe { get; set; }

        /// <summary>
        /// The master switch for the Exdeath and Chaos chat announcements. When false, no announcement fires on any button press.
        /// Disabled by default so nothing is sent to chat until the user opts in.
        /// </summary>
        public bool AnnouncementsEnabled { get; set; }

        /// <summary>
        /// Whether the advanced Personal Mode option is revealed in the Chat tab. Hidden by default so only Party Mode shows.
        /// </summary>
        public bool ShowPersonalMode { get; set; }

        /// <summary>
        /// Whether Personal Mode is selected. Personal Mode allows every announcement (debuffs, gaze, chaos) but blocks the
        /// non-party-safe ones from party (/p) chat unless the override is on. Party Mode (the default) sends only gaze and
        /// Inferno/Tsunami. Only takes effect while <see cref="ShowPersonalMode"/> is true; see <see cref="IsPersonalMode"/>.
        /// </summary>
        public bool PersonalMode { get; set; }

        /// <summary>
        /// The dangerous override that lets Personal Mode send its non-party-safe announcements (debuffs, titles, custom) to
        /// party (/p) chat. Off by default; enabling it can spam your party, so Party Mode is the intended way to talk to party.
        /// </summary>
        public bool PersonalModePartyOverride { get; set; }

        /// <summary>
        /// Personal Mode only: when true, each ordered announcement can be routed to its own channel instead of the single selected channel.
        /// </summary>
        public bool PerChannelAnnouncements { get; set; }

        /// <summary>
        /// The effective mode: Personal only when it is both revealed and selected; otherwise Party Mode.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public bool IsPersonalMode => ShowPersonalMode && PersonalMode;

        /// <summary>
        /// When true, per-press announcements are suppressed and instead a single chronological list of every
        /// enabled announcement is sent to the selected channel once the full sequence and both chaos presses are complete.
        /// The list order is: first-set debuffs, 1st gaze, Inferno, second-set debuffs, 2nd gaze, Tsunami.
        /// Uses the currently selected channel's configured announcement messages.
        /// </summary>
        public bool AnnouncementChronological { get; set; }

        /// <summary>
        /// Whether the generated default Exdeath announcement messages include the "[1st]"/"[2nd]" set prefix.
        /// When false, a default reads "Lightning - Spread" instead of "[1st] Lightning - Spread". Enabled by default.
        /// </summary>
        public bool AnnouncementShowSetNumber { get; set; } = true;

        /// <summary>
        /// The chat channel currently selected in the Chat tab, whose announcement configuration is edited and fired.
        /// </summary>
        public string AnnouncementChannel { get; set; } = "/p";

        /// <summary>
        /// The per-channel Exdeath and Chaos announcement configuration, keyed by chat command prefix such as "/p".
        /// </summary>
        public Dictionary<string, ChannelAnnouncements> Announcements { get; set; } = new();

        /// <summary>
        /// Whether each section is drawn as its own floating window instead of inside the single hub window.
        /// This is enabled by default.
        /// </summary>
        public bool Detached { get; set; } = true;

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
        /// This is enabled by default.
        /// </summary>
        public bool FloatingHideButton { get; set; } = true;

        /// <summary>
        /// Whether the Reset control is a separate floating section rather than a toolbar button.
        /// When this is off, the Reset control docks to the toolbar instead of floating.
        /// This is enabled by default.
        /// </summary>
        public bool FloatingResetButton { get; set; } = true;

        /// <summary>
        /// Whether the Undo control is a separate floating section rather than a toolbar button.
        /// When this is off, the Undo control docks to the toolbar instead of floating.
        /// This is enabled by default.
        /// </summary>
        public bool FloatingUndoButton { get; set; } = true;

        /// <summary>
        /// When true, a button group hides once its inputs are fully entered, until the next reset:
        /// the Exdeath section once both sets are resolved, and each chaos or Kefka pair once it is pressed.
        /// Text panels and the Last Fake toggles are never affected. Off by default.
        /// </summary>
        public bool HideResolvedButtons { get; set; }

        /// <summary>
        /// When true, the quick-settings toolbar disappears entirely while the display is hidden,
        /// instead of showing in its collapsed state. Off by default.
        /// </summary>
        public bool HideToolbarWhenHidden { get; set; }

        /// <summary>
        /// When true, the toolbar's collapsed state is left untouched by the Hide and Show buttons,
        /// instead of collapsing on hide and expanding on show. Off by default.
        /// </summary>
        public bool PersistToolbarCollapsed { get; set; }

        /// <summary>
        /// Whether the macro button sections are hidden so only the resolution text panels remain.
        /// This is for controller players who drive the plugin through the slash commands instead of clicking.
        /// </summary>
        public bool HideMacroButtons { get; set; }

        /// <summary>
        /// Whether the Acceleration callout is appended to the spread or stack line instead of sitting on its own line.
        /// When on, a set reads like "Spread on X and MOVE" with the "and" in the normal text colour and the movement word in the Acceleration colour.
        /// This is enabled by default.
        /// </summary>
        public bool AccelerationSameLine { get; set; } = true;

        /// <summary>
        /// Whether the First Set and Second Set are drawn together in one combined panel with a divider between them.
        /// </summary>
        public bool CombineSets { get; set; }

        /// <summary>
        /// Whether the combined First and Second set panel lays its two sets out side by side instead of stacked.
        /// This only applies when the sets are combined.
        /// </summary>
        public bool CombineSetsHorizontal { get; set; }

        /// <summary>
        /// Whether the combined set panel right-aligns the first set against the divider so the two sets mirror each other.
        /// Side by side, this right-aligns the first set; stacked, it centres every line.
        /// This only applies when the sets are combined.
        /// </summary>
        public bool CombineSetsExpandFromCenter { get; set; }

        /// <summary>
        /// Whether the combined panel keeps its divider pinned at a fixed position, growing each set outward from it while the text stays left-aligned.
        /// The divider position is the section's detached position, so it can be set by dragging or the position sliders.
        /// This only applies when the sets are combined.
        /// </summary>
        public bool CombineSetsAnchorDivider { get; set; }

        /// <summary>
        /// The thickness in pixels of the combined panel's divider line.
        /// </summary>
        public float CombineDividerThickness { get; set; } = 1.5f;

        /// <summary>
        /// The colour of the combined panel's divider line.
        /// </summary>
        public Vector4 CombineDividerColor { get; set; } = new(0.6f, 0.6f, 0.6f, 1f);

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
        public bool UseUniversalSettings { get; set; }

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
        /// The custom text overrides for panel labels, headers, callouts and buttons, keyed by the ids in <see cref="TextLabels"/>.
        /// An id that is absent or empty uses the default text.
        /// </summary>
        public Dictionary<string, string> CustomText { get; set; } = new();

        /// <summary>
        /// The per-section, per-mode background opacity overrides.
        /// The defaults make the detached button panels transparent and the text panels semi-opaque.
        /// </summary>
        public Dictionary<string, float> SectionBackgroundAlpha { get; set; } = new()
        {
            ["d:Exdeath"] = 0.0f,
            ["d:FireWaterButtons"] = 0.0f,
            ["d:ThunderButtons"] = 0.0f,
            ["d:FirstSet"] = 0.7f,
            ["d:SecondSet"] = 0.7f,
            ["d:CombinedSets"] = 0.7f,
            ["d:ThunderText"] = 0.7f,
            ["d:LastFakeToggles"] = 1.0f,
            ["d:LastFakeThunder"] = 1.0f,
            ["d:Reset"] = 0.0f,
            ["d:Hide"] = 0.0f,
        };

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
        /// The defaults lay the sections out in the shipped arrangement.
        /// </summary>
        public Dictionary<string, Vector2> DetachedPositions { get; set; } = new()
        {
            ["Exdeath"] = new Vector2(196f, 126f),
            ["FireWaterButtons"] = new Vector2(52f, 148f),
            ["ThunderButtons"] = new Vector2(49f, 303f),
            ["FirstSet"] = new Vector2(356f, 151f),
            ["SecondSet"] = new Vector2(352f, 270f),
            ["CombinedSets"] = new Vector2(356f, 151f),
            ["ThunderText"] = new Vector2(355f, 393f),
            ["Reset"] = new Vector2(58f, 463f),
            ["Hide"] = new Vector2(167f, 462f),
        };

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
        /// Gets the effective display text for a text id, falling back to its registered default.
        /// </summary>
        /// <param name="id">The text label id from <see cref="TextLabels"/>.</param>
        /// <returns>The custom text when one is set, otherwise the default.</returns>
        public string GetText(string id)
        {
            return CustomText.TryGetValue(id, out var value) && !string.IsNullOrEmpty(value) ? value : TextLabels.Default(id);
        }

        /// <summary>
        /// Gets the raw custom override for a text id, or an empty string when there is none.
        /// </summary>
        /// <param name="id">The text label id from <see cref="TextLabels"/>.</param>
        /// <returns>The stored custom text, or an empty string when unset.</returns>
        public string GetRawText(string id)
        {
            return CustomText.TryGetValue(id, out var value) ? value : string.Empty;
        }

        /// <summary>
        /// Stores a custom text override, or removes it when the value is empty so the default is used again.
        /// </summary>
        /// <param name="id">The text label id from <see cref="TextLabels"/>.</param>
        /// <param name="value">The custom text; an empty value reverts to the default.</param>
        public void SetText(string id, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                CustomText.Remove(id);
            }
            else
            {
                CustomText[id] = value;
            }
        }

        /// <summary>
        /// Clears every custom text override so all labels return to their defaults.
        /// </summary>
        public void ResetText()
        {
            CustomText.Clear();
        }

        /// <summary>
        /// Gets the announcement configuration for a chat channel, creating an empty one on first use.
        /// </summary>
        /// <param name="channel">The chat command prefix the configuration belongs to, such as "/p".</param>
        /// <returns>That channel's announcement configuration with its slots populated.</returns>
        public ChannelAnnouncements GetAnnouncements(string channel)
        {
            if (!Announcements.TryGetValue(channel, out var channelAnnouncements))
            {
                channelAnnouncements = new ChannelAnnouncements();
                Announcements[channel] = channelAnnouncements;
            }

            // Populate each leaf's slots (with their default enabled state) on every access, so a fresh
            // channel or an un-opened set announces its defaults without the user first opening the dropdown.
            EnsureCategorySlots(channelAnnouncements.Exdeath, "exdeath");
            EnsureCategorySlots(channelAnnouncements.Chaos, "chaos");
            return channelAnnouncements;
        }

        /// <summary>
        /// Ensures all four leaves (first/second set, real/fake) of a category have their slots.
        /// </summary>
        /// <param name="category">The category whose leaves are populated.</param>
        /// <param name="categoryId">The category id, either "exdeath" or "chaos".</param>
        private static void EnsureCategorySlots(AnnouncementCategory category, string categoryId)
        {
            AnnouncementData.EnsureSlots(category.GetLeaf(true, true), AnnouncementData.SlotIdsFor(categoryId, true));
            AnnouncementData.EnsureSlots(category.GetLeaf(true, false), AnnouncementData.SlotIdsFor(categoryId, true));
            AnnouncementData.EnsureSlots(category.GetLeaf(false, true), AnnouncementData.SlotIdsFor(categoryId, false));
            AnnouncementData.EnsureSlots(category.GetLeaf(false, false), AnnouncementData.SlotIdsFor(categoryId, false));
        }

        /// <summary>
        /// Builds the dictionary key for a per-section value in the current layout mode.
        /// </summary>
        /// <param name="sectionId">The section the key is built for.</param>
        /// <returns>The section id prefixed with the current layout mode.</returns>
        private string ModeKey(string sectionId)
        {
            return (Detached ? "d:" : "w:") + sectionId;
        }

        /// <summary>
        /// Gets the windowed offset for a section, falling back to the supplied default.
        /// </summary>
        /// <param name="sectionId">The section whose offset is looked up.</param>
        /// <param name="fallback">The offset used when none has been stored.</param>
        /// <returns>The stored offset, or the fallback.</returns>
        public Vector2 GetOffset(string sectionId, Vector2 fallback)
        {
            return Offsets.TryGetValue(sectionId, out var offset) ? offset : fallback;
        }

        /// <summary>
        /// Stores the windowed offset for a section.
        /// </summary>
        /// <param name="sectionId">The section whose offset is stored.</param>
        /// <param name="offset">The new windowed offset.</param>
        public void SetOffset(string sectionId, Vector2 offset)
        {
            Offsets[sectionId] = offset;
        }

        /// <summary>
        /// Gets the detached window position for a section, falling back to the supplied default.
        /// </summary>
        /// <param name="sectionId">The section whose position is looked up.</param>
        /// <param name="fallback">The position used when none has been stored.</param>
        /// <returns>The stored screen position, or the fallback.</returns>
        public Vector2 GetDetachedPosition(string sectionId, Vector2 fallback)
        {
            return DetachedPositions.TryGetValue(sectionId, out var position) ? position : fallback;
        }

        /// <summary>
        /// Stores the detached window position for a section.
        /// </summary>
        /// <param name="sectionId">The section whose position is stored.</param>
        /// <param name="position">The new detached screen position.</param>
        public void SetDetachedPosition(string sectionId, Vector2 position)
        {
            DetachedPositions[sectionId] = position;
        }

        /// <summary>
        /// Gets the scale multiplier for a section in the current mode.
        /// </summary>
        /// <param name="sectionId">The section whose scale is looked up.</param>
        /// <returns>The stored scale for the current mode, or 1.0 when unset.</returns>
        public float GetSectionScale(string sectionId)
        {
            return SectionScales.TryGetValue(ModeKey(sectionId), out var scale) ? scale : 1.0f;
        }

        /// <summary>
        /// Stores the scale multiplier for a section in the current mode.
        /// </summary>
        /// <param name="sectionId">The section whose scale is stored.</param>
        /// <param name="scale">The new per-section scale multiplier.</param>
        public void SetSectionScale(string sectionId, float scale)
        {
            SectionScales[ModeKey(sectionId)] = scale;
        }

        /// <summary>
        /// Resolves the effective background opacity for a section, using the universal value or the per-section override.
        /// </summary>
        /// <param name="sectionId">The section being drawn.</param>
        /// <returns>The universal background opacity, or the section's own when universal settings are off.</returns>
        public float EffectiveBackgroundAlpha(string sectionId)
        {
            return UseUniversalSettings ? BackgroundAlpha : GetSectionBackgroundAlpha(sectionId);
        }

        /// <summary>
        /// Resolves the effective hide-title-bar flag for a section, using the universal value or the per-section override.
        /// </summary>
        /// <param name="sectionId">The section being drawn.</param>
        /// <returns>Whether that section's title bar is hidden under the active settings.</returns>
        public bool EffectiveNoTitleBar(string sectionId)
        {
            return UseUniversalSettings ? NoTitleBar : GetSectionNoTitleBar(sectionId);
        }

        /// <summary>
        /// Resolves the effective hide-labels flag for a section, using the universal value or the per-section override.
        /// </summary>
        /// <param name="sectionId">The section being drawn.</param>
        /// <returns>Whether that section's labels are hidden under the active settings.</returns>
        public bool EffectiveHideLabels(string sectionId)
        {
            return UseUniversalSettings ? HideLabels : GetSectionHideLabels(sectionId);
        }

        /// <summary>
        /// Resolves the effective button opacity for a section, using the universal value or the per-section override.
        /// </summary>
        /// <param name="sectionId">The section being drawn.</param>
        /// <returns>The universal button opacity, or the section's own when universal settings are off.</returns>
        public float EffectiveButtonAlpha(string sectionId)
        {
            return UseUniversalSettings ? ButtonAlpha : GetSectionButtonAlpha(sectionId);
        }

        /// <summary>
        /// Gets the per-section background opacity for the current mode, defaulting to fully opaque.
        /// </summary>
        /// <param name="sectionId">The section whose value is looked up.</param>
        /// <returns>The stored background opacity for the current mode, or fully opaque when unset.</returns>
        public float GetSectionBackgroundAlpha(string sectionId)
        {
            return SectionBackgroundAlpha.TryGetValue(ModeKey(sectionId), out var alpha) ? alpha : 1.0f;
        }

        /// <summary>
        /// Stores the per-section background opacity for the current mode.
        /// </summary>
        /// <param name="sectionId">The section whose value is stored.</param>
        /// <param name="alpha">The new background opacity.</param>
        public void SetSectionBackgroundAlpha(string sectionId, float alpha)
        {
            SectionBackgroundAlpha[ModeKey(sectionId)] = alpha;
        }

        /// <summary>
        /// Gets the per-section hide-title-bar flag for the current mode, defaulting to hidden.
        /// </summary>
        /// <param name="sectionId">The section whose value is looked up.</param>
        /// <returns>Whether the title bar is hidden for the current mode; hidden by default.</returns>
        public bool GetSectionNoTitleBar(string sectionId)
        {
            return SectionNoTitleBar.TryGetValue(ModeKey(sectionId), out var hidden) ? hidden : true;
        }

        /// <summary>
        /// Stores the per-section hide-title-bar flag for the current mode.
        /// </summary>
        /// <param name="sectionId">The section whose value is stored.</param>
        /// <param name="hidden">Whether the title bar is hidden.</param>
        public void SetSectionNoTitleBar(string sectionId, bool hidden)
        {
            SectionNoTitleBar[ModeKey(sectionId)] = hidden;
        }

        /// <summary>
        /// Gets the per-section hide-labels flag for the current mode, defaulting to shown.
        /// </summary>
        /// <param name="sectionId">The section whose value is looked up.</param>
        /// <returns>Whether the labels are hidden for the current mode.</returns>
        public bool GetSectionHideLabels(string sectionId)
        {
            return SectionHideLabels.TryGetValue(ModeKey(sectionId), out var hidden) && hidden;
        }

        /// <summary>
        /// Stores the per-section hide-labels flag for the current mode.
        /// </summary>
        /// <param name="sectionId">The section whose value is stored.</param>
        /// <param name="hidden">Whether the labels are hidden.</param>
        public void SetSectionHideLabels(string sectionId, bool hidden)
        {
            SectionHideLabels[ModeKey(sectionId)] = hidden;
        }

        /// <summary>
        /// Gets the per-section button opacity for the current mode, defaulting to fully opaque.
        /// </summary>
        /// <param name="sectionId">The section whose value is looked up.</param>
        /// <returns>The stored button opacity for the current mode, or fully opaque when unset.</returns>
        public float GetSectionButtonAlpha(string sectionId)
        {
            return SectionButtonAlpha.TryGetValue(ModeKey(sectionId), out var alpha) ? alpha : 1.0f;
        }

        /// <summary>
        /// Stores the per-section button opacity for the current mode.
        /// </summary>
        /// <param name="sectionId">The section whose value is stored.</param>
        /// <param name="alpha">The new button opacity.</param>
        public void SetSectionButtonAlpha(string sectionId, float alpha)
        {
            SectionButtonAlpha[ModeKey(sectionId)] = alpha;
        }

        /// <summary>
        /// Resets every setting to its default value, except the schema version, and persists the result.
        /// The reset is done by copying from a fresh instance so new settings are covered automatically.
        /// </summary>
        public void RestoreDefaults()
        {
            CopyFrom(new Configuration());
            Save();
        }

        /// <summary>
        /// Copies every writable setting from another configuration, except the schema version and the transient edit-interaction flags.
        /// This is used both by the restore-defaults flow and by importing a shared settings profile.
        /// </summary>
        /// <param name="source">The configuration whose values are copied over this one.</param>
        public void CopyFrom(Configuration source)
        {
            foreach (var property in typeof(Configuration).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!property.CanRead || !property.CanWrite
                    || property.Name is nameof(Version) or nameof(LastSeenVersion) or nameof(EditMode) or nameof(MoveAllActive))
                {
                    continue;
                }

                property.SetValue(this, property.GetValue(source));
            }
        }

        /// <summary>
        /// Persists the configuration to disk.
        /// </summary>
        public void Save()
        {
            Plugin.PluginInterface.SavePluginConfig(this);
        }
    }
}
