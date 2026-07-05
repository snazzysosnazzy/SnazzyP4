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
        new("1.0.0.19", new[]
        {
            "Chaos resolutions are now static: Inferno always resolves in the First Set and Tsunami always in the Second Set, no matter which you press first.",
            "The Inferno and Tsunami buttons each disable after you press them, until the next Reset - so you can no longer double-enter the same mechanic.",
            "Removed the 1st/2nd labelling from Inferno and Tsunami, since their sets are fixed. Default Chaos announcement messages no longer carry a set number, and the Chaos announcement sections are now named \"Inferno\" and \"Tsunami\" instead of \"First set\" / \"Second set\".",
        }),
        new("1.0.0.18", new[]
        {
            "The Undo button can now float as its own panel, just like Hide and Reset. A new \"Floating Undo button\" setting sits next to the Floating Hide/Reset options, and all three float by default (turn one off to dock that button back onto the toolbar).",
            "Acceleration text now shares the same line as the Stack/Spread callout by default (for example \"Spread on X and MOVE\"), so the setting \"Acceleration text on same line as Stack/Spread\" is now on out of the box - turn it off to put Acceleration back on its own line.",
        }),
        new("1.0.0.17", new[]
        {
            "Chat tab: a master \"Enable chat announcements\" toggle sits at the top and is OFF by default. While it is off, nothing is sent to chat no matter which per-mechanic announcements are turned on - turn it on once you have your announcements set up.",
            "New UNDO button on the toolbar (next to Reset) steps back the last button press and its resolution text, one press at a time - handy for a misclick without resetting the whole pull. It is also available as the slash command /snazzyp4 undo. Note: chat announcements and markers already sent for that press cannot be recalled.",
        }),
        new("1.0.0.16", new[]
        {
            "Announce Last Fake: the docked ANNOUNCE button can now anchor to any side of the Kefka panel - a new \"Dock side\" dropdown (Top / Bottom / Left / Right) appears when docking is on.",
            "Chat announcements (ordered mode): every set now has an \"Announce Title\" line, defaulting to \"---------- 1st Set ----------\" / \"---------- 2nd Set ----------\" for Exdeath and \"---------- 1st Chaos ----------\" / \"---------- 2nd Chaos ----------\" for Chaos. Like any slot it is off until you enable it and can be reordered or customised.",
            "Chat announcements (ordered mode): a \"+ Add custom message\" button lets you add your own extra messages into the list alongside the mechanics, each reorderable and removable. The title and custom messages always fire when enabled (Chaos still only fires the mechanic you pressed).",
            "General tab: a gold \"Never show version update messages\" toggle stops the changelog popup from appearing after each update, and the update popup itself now has a matching button. The full changelog is always still available from the title-bar button.",
        }),
        new("1.0.0.15", new[]
        {
            "Extended chat announcements (Chat tab): the old \"Announce Gaze / Chaos\" toggles are replaced with a full announcement system. \"Announce Exdeath\" fires the moment you press an Exdeath Real/Fake button; \"Announce Chaos\" fires when you press a chaos button. Each is organised into First/Second set and then Real/Fake.",
            "Two modes per category. Ordered list: a reorderable list of per-mechanic toggles (Gaze, Spread, Water Drop, Acceleration for Exdeath; Inferno, Tsunami for Chaos). Enable the ones you want; the order is the send order. Expand a toggle's Message settings for \"Enable custom message\" and a reorderable list of message boxes you can grow with + and shrink with - (blank boxes are skipped). Exdeath sends every enabled toggle; Chaos sends only the mechanic you pressed. Simple text box: one growing box per set/real-fake, one chat message per non-empty line.",
            "Announcements are stored per channel. Pick the active channel at the top of the Chat tab; only that channel is sent to. \"Copy settings to...\" clones the current channel's setup to another channel. Sensible default messages are generated for every announcement.",
            "Last Fake ANNOUNCE (Hidden tab, once Last Fake is unlocked): \"Announce Last Fake\" adds an ANNOUNCE button (floating by default, or dockable to the Kefka text panel) that posts the current Kefka values to a channel of your choice. Put {KefkaThunder} and {KefkaBlizzard} in your message and they become the current value (REAL/FAKE by default, both customisable; \"?\" if that mechanic has not been pressed).",
        }),
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
