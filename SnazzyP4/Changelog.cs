namespace SnazzyP4;

/// <summary>
/// The embedded, detailed changelog shown in the update notice and the changelog window.
/// Entries are newest first and each version lists what changed, where to find it and how to use it.
/// </summary>
public static class Changelog
{
    /// <summary>
    /// A single version's changelog entry.
    /// </summary>
    /// <param name="Version">The release version, matching the assembly version.</param>
    /// <param name="Changes">The detailed change lines for that version.</param>
    public readonly record struct Entry(string Version, string[] Changes);

    /// <summary>
    /// Every version's changelog, newest first.
    /// </summary>
    public static readonly Entry[] Entries =
    {
        new("1.0.0.14", new[]
        {
            "Update notice: the first time you open the plugin after updating, a window pops up telling you which version you came from, which version you are now on, and a detailed list of everything that changed in between. It only appears once per update (not every time you log in), and closing it dismisses it until the next update.",
            "The plugin version is now shown next to \"Snazzy P4\" at the top of the settings window.",
            "A changelog button (the clipboard icon in the settings window's title bar) opens a window with the full, detailed changelog for every version so you can read it any time.",
        }),
        new("1.0.0.13", new[]
        {
            "Customisable auto-markers: under Settings > General > Auto-place marker you can now choose which head marker is placed for each role and set (First/Second set for Support and DPS). Options include Attack 1-8, Bind 1-3, Ignore 1-2, the four shapes, or (none). The marker is always placed on yourself.",
            "Customisable target letters: the A/B/C/D letters shown in the resolutions (\"Spread on B\") can be renamed per role in Settings > Text, under the new \"Target letters\" group.",
        }),
        new("1.0.0.12", new[]
        {
            "New Text tab (Settings > Text): rename almost every piece of text the plugin shows - the panel labels (< First Set >, < Kefka >, Last Fake?), the spread/stack prefixes, gaze, acceleration, chaos and Thunder/Blizzard callouts, the section headers and the RESET/HIDE/SHOW buttons. Leave a field blank to keep its default (shown as a greyed hint).",
        }),
        new("1.0.0.11", new[]
        {
            "The chat announcements moved to their own Chat tab (renamed from \"Party chat messages\" to \"Chat Messages\").",
            "Added more announcement channels: Tell (your current target), Free Company, Linkshells 1-8 and Cross-world Linkshells 1-8.",
        }),
        new("1.0.0.10", new[]
        {
            "Chat channel selector: gaze and chaos announcements can be sent to a channel of your choice (Party, Say, Yell, Shout, Alliance, or Echo so only you see it - handy for testing) instead of always party chat.",
        }),
        new("1.0.0.9", new[]
        {
            "Settings are now organised into tabs (General, Chat, Appearance, Colors, Text, Layout, Controller, Sections, Hidden).",
            "Settings profiles (Settings > General): copy your whole setup to the clipboard and paste it back or share it with others.",
            "Colourblind palette presets (Settings > Colors): one-click Deuteranopia / Protanopia / Tritanopia palettes plus Default.",
            "Bring all windows on-screen (Settings > Layout, detached): clamps every detached window back into view.",
            "The combined-set divider can now be pinned in both orientations (stacked as well as side by side) and its thickness and colour are customisable.",
            "Added an MIT LICENSE and this changelog.",
        }),
        new("1.0.0.8", new[]
        {
            "Combined sets can pin their divider at a fixed position: the two sets grow outward from the divider while the text stays left-aligned. Drag the panel or use the CombinedSets X/Y sliders to place the divider.",
            "The previous right-align behaviour became the separate \"Mirror the sets\" option.",
        }),
        new("1.0.0.7", new[]
        {
            "Added the first expand-from-divider (mirror) option for the combined set panel.",
        }),
        new("1.0.0.6", new[]
        {
            "Acceleration on the same line (Settings > Layout): append MOVE / STAND STILL to the spread/stack line, e.g. \"Spread on X and MOVE\".",
            "The First Set / Second Set labels now respect the Hide Labels setting.",
            "Combine First and Second set into one panel (Settings > Layout): stacked or side by side with a divider.",
        }),
        new("1.0.0.5", new[]
        {
            "Highlighted the Hide macro buttons setting in Controller Settings so it is easier to find.",
        }),
        new("1.0.0.4", new[]
        {
            "Controller support: every button now has a /snazzyp4 slash command that does the same thing, so controller players can bind them to macros.",
            "New Controller Settings section with copyable commands and a \"Hide macro buttons\" option that leaves only the resolution text panels on screen.",
        }),
        new("1.0.0.3", new[]
        {
            "Softened the wording of the hidden Last Fake unlock warning.",
        }),
        new("1.0.0.2", new[]
        {
            "Added kefka / phase search tags for the plugin listing.",
        }),
        new("1.0.0.1", new[]
        {
            "The Floating Hide button is now enabled by default.",
        }),
        new("1.0.0.0", new[]
        {
            "Initial release of Snazzy P4: an on-screen cheat sheet for the Kefka UMAD/DMU Phase 4 mechanics. Press the buttons for what you see and it lays out clean, colour-coded callouts (spread/stack, gaze, chaos, Thunder/Blizzard). Everything is repositionable, rescalable and recolourable, windowed or as detached panels. Open with /snazzyp4, settings with /snazzyp4 config.",
        }),
    };
}
