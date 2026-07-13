namespace SnazzyP4
{
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
        /// <param name="Date">The release date in ISO format (yyyy-MM-dd).</param>
        /// <param name="Changes">The detailed change lines for that version.</param>
        public readonly record struct Entry(string Version, string Date, string[] Changes);

        /// <summary>
        /// Every version's changelog, newest first.
        /// </summary>
        public static readonly Entry[] Entries =
        {
            new("1.0.9.0", "2026-07-12", new[]
            {
                "The General tab now groups its sliders into two collapsible sections: Scaling (Global UI Scale, Toolbar Scale, Macro UI Scale) and a new Opacity group (Background, Toolbar, Button and Text opacity).",
                "Opacity now works like the scales: the General values are global multipliers and each section's own opacity in the Layout tab multiplies on top, instead of the old Use Universal Settings switch that overrode and hid the per-section controls. That switch is gone, the universal block is removed from the Layout tab, and the per-section controls always show.",
                "The single universal Button / Text opacity is split into separate Button Opacity and Text Opacity multipliers, and the toolbar gets its own opacity slider.",
                "Renamed for clarity: \"Hide Toolbar when UI is hidden\", \"Persistent Toolbar Collapsed State\" and \"Toolbar Scale\".",
            }),
            new("1.0.8.0", "2026-07-12", new[]
            {
                "Two global switches now live under General Settings so they are easy to find: \"Hide all name labels\" (off by default) and \"Hide all title bars\" (on by default).",
                "While a global switch is on it hides everything of that kind, and the matching per-section switches in the Layout tab are disabled since they no longer matter. Turn the global switch off to pick per section again.",
            }),
            new("1.0.7.1", "2026-07-12", new[]
            {
                "While the Floating Hide button is off, the settings toolbar now always shows (collapsible as normal) and is never hidden - it is the only way to bring the display back in that setup, so \"Hide Settings toolbar when UI is hidden\" and \"Show toolbar\" off are overridden to prevent a confusing lock-out.",
            }),
            new("1.0.7.0", "2026-07-12", new[]
            {
                "Hiding the UI no longer reduces the toolbar to an empty black square. The toolbar now collapses when you press Hide and expands when you press Show, and its expand/collapse arrows keep working while the UI is hidden - so a collapsed toolbar stays collapsed, and you can expand it any time.",
                "New General Settings option \"Hide Settings toolbar when UI is hidden\" (off by default): the toolbar disappears completely while the UI is hidden instead of showing collapsed, and the hub window goes fully invisible and click-through.",
                "New General Settings option \"Persistent Settings Collapsed State\" (off by default): the toolbar's collapsed state ignores Hide/Show presses and only changes when you use the arrows. The hide-entirely option above still wins while the UI is hidden.",
            }),
            new("1.0.6.1", "2026-07-12", new[]
            {
                "The Thunder and Blizzard buttons now lock after being pressed, matching the chaos buttons: press a Thunder (real or fake) and both Thunder buttons disable until Reset; same for Blizzard. Use Undo to take back a mispress. The Last Fake toggles are unaffected and stay usable.",
            }),
            new("1.0.6.0", "2026-07-12", new[]
            {
                "UI Scale is renamed Global UI Scale and still multiplies everything the plugin draws.",
                "Two new scale sliders sit right below it, each with 50/100/150/200% presets and both defaulting to 1.00x. Settings Toolbar Scale multiplies just the quick-settings toolbar, and Macro UI Scale multiplies the macro buttons and text panels (on top of the global scale and the per-section scales).",
            }),
            new("1.0.5.1", "2026-07-10", new[]
            {
                "Hide Resolved Buttons no longer shifts the remaining buttons: a hidden chaos or Kefka pair keeps its exact spot, so pressing Inferno no longer moves the Tsunami buttons up (and likewise for Thunder/Blizzard).",
                "Fixed misaligned text in windowed (non-detached) mode: the Real/Fake and Short/Long column headers and the Last Fake column were pushed right by the panel's position. They now line up the same as in detached mode.",
            }),
            new("1.0.5.0", "2026-07-10", new[]
            {
                "Picking Lightning or Drop now locks ALL Lightning and Drop buttons, not just the picked one (issue #1 follow-up). The fight only ever assigns one of the two body debuffs per pull, so after a body pick only Acceleration remains selectable.",
                "New General Settings option \"Hide Resolved Buttons Until Reset\" (off by default). The Exdeath buttons hide once both sets are fully entered, each chaos pair hides once pressed, and each Kefka pair hides once pressed. Everything returns on Reset. Text panels and the Last Fake toggles are never affected.",
            }),
            new("1.0.4.4", "2026-07-10", new[]
            {
                "Fixed a brief lag spike when the window first opened (most noticeable with the auto-open on entering the duty). All button icons are now loaded once when the plugin starts and kept in memory (well under 1 MB), instead of being reloaded from disk and uploaded to the GPU in one burst on the zone-in frame.",
            }),
            new("1.0.4.3", "2026-07-06", new[]
            {
                "Implemented issue #1: a debuff kind can no longer be picked twice in a pull. Once you press a Lightning (spread), Drop (stack) or Acceleration, both its Short and Long buttons grey out until Reset, since the fight never gives you the same one twice. Previously only duplicates within a single set were blocked.",
            }),
            new("1.0.4.2", "2026-07-06", new[]
            {
                "Fixed a bug (issue #2) where an announcement only started working after you opened its dropdown in the Chat tab. Each set's announcement slots now get their default enabled state as soon as the channel is used, so every enabled callout fires without you having to open each set first.",
            }),
            new("1.0.4.1", "2026-07-06", new[]
            {
                "Moved the Automation section on the General tab to sit above the General Settings section (just below Role).",
            }),
            new("1.0.4.0", "2026-07-06", new[]
            {
                "New Automation settings on the General tab (all off by default). \"Auto Open/Close SnazzyP4 upon Enter/Exit of Duty\" opens the overlay when you enter a captured instance and closes it when you leave - click \"Use current instance\" while standing in Dancing Mad (Ultimate) to set the trigger.",
                "\"Reset on Hide Button Press\" runs Reset whenever you press the Hide button.",
                "\"Reset on Wipe\" and \"Hide on Wipe\" run Reset / Hide when the party wipes, using the game's duty-state wipe detection.",
                "Note: the wipe and duty automation are the only game state the plugin ever reads, and it is non-combat (which duty you are in, and whether the party wiped) - nothing about the fight itself is read or resolved.",
            }),
            new("1.0.3.3", "2026-07-06", new[]
            {
                "Minor internal cleanup and changelog wording tidy-up.",
            }),
            new("1.0.3.2", "2026-07-06", new[]
            {
                "Fixed the Layout tab appearing in the wrong place (after Text) instead of between Chat and Colors - ImGui was remembering the old Layout tab's saved position. It now sits in the intended order: General, Chat, Layout, Colors, Text, Controller, Hidden.",
            }),
            new("1.0.3.1", "2026-07-06", new[]
            {
                "The changelog now shows each version's release date, both in the update popup and the full changelog window.",
                "Renamed the in-app \"Layout\" heading on the General tab to \"General Settings\" so it no longer clashes with the Layout tab.",
            }),
            new("1.0.3.0", "2026-07-06", new[]
            {
                "Settings tabs reorganised. The old Layout tab's options (toolbar, detached windows, edit layout, Move All, floating Hide/Reset/Undo, Acceleration on the same line, combine sets, bring windows on-screen) now live on the General tab, just below the marker settings.",
                "The Appearance tab has absorbed the old Sections tab and is renamed Layout. The separate Appearance and Sections tabs are gone. Tabs are now: General, Chat, Layout, Colors, Text, Controller, Hidden.",
            }),
            new("1.0.2.1", "2026-07-06", new[]
            {
                "The Edit Layout preview now shows INFERNO TWISTER in the first set (and Tsunami in the second), matching the static chaos sets, instead of Tsunami in both.",
            }),
            new("1.0.2.0", "2026-07-06", new[]
            {
                "Announcements now have two mutually-exclusive modes. PARTY MODE (the new default) sends only the party-safe callouts - gaze and Inferno/Tsunami - so it is safe to broadcast to your party. By default it has everything enabled except the titles.",
                "PERSONAL MODE is the old full behaviour with every announcement (debuffs, titles, custom). It is hidden by default; tick \"Show Personal Mode (advanced)\" to reveal it. It blocks its non-party-safe callouts from /p party chat and suggests using Party Mode - there is a heavily-warned override that WILL send them to party anyway if you insist.",
                "Personal Mode also adds \"Per-channel announcements\", which lets each announcement be routed to its own channel instead of the single selected channel.",
                "On a fresh setup, built-in announcements now start enabled (except the titles) so Party Mode works out of the box once you turn the master switch on.",
            }),
            new("1.0.1.1", "2026-07-06", new[]
            {
                "Fixed the Quick toggle buttons clipping their text - they now size to fit their labels.",
                "The set-title Quick toggles now only affect the 1st/2nd set (Exdeath) titles and leave the Inferno/Tsunami (Chaos) titles alone.",
                "New Chat-tab setting \"Include [1st] / [2nd] prefix in default messages\" (on by default). Turn it off to drop the set number, so a default reads \"Lightning - Spread\" instead of \"[1st] Lightning - Spread\".",
            }),
            new("1.0.1.0", "2026-07-06", new[]
            {
                "Inferno and Tsunami no longer use the words \"Fire\" or \"Water\" anywhere. The resolution text panel now reads INFERNO TWISTER / INFERNO DONUT / TSUNAMI DONUT / TSUNAMI TWISTER, and the two colour pickers are simply labelled \"Inferno\" and \"Tsunami\".",
                "Chaos announcement defaults now spell out the movement: Donut (STAY) and Twister (MOVE) - for example \"Inferno - Twister (MOVE)\" and \"Tsunami - Donut (STAY)\". The STAY/MOVE hint is only on the announcements; the resolution text panel still just shows the twister/donut shape.",
                "First/Second set announcement titles now include the real/fake state, for example \"---------- 2nd Set : FAKE ----------\".",
            }),
            new("1.0.0.22", "2026-07-06", new[]
            {
                "Chat tab: new Quick toggles. \"Turn on all announcements\" / \"Turn off all announcements\" flip every announcement except the titles, and \"Turn on set titles\" / \"Turn off set titles\" flip just the titles. They apply across the whole selected channel (both Exdeath and Chaos, every set and real/fake).",
                "Announcement default messages are reformatted to \"[set] Debuff - Resolvement\", for example \"[1st] Lightning - Spread\" or \"[2nd] Acceleration - Move\". This only affects the generated defaults; any messages you have customised are left as they are.",
            }),
            new("1.0.0.21", "2026-07-06", new[]
            {
                "The Chronological summary now sends to whichever channel is selected in the Chat tab (using that channel's configured messages), instead of always going to Party (/p).",
            }),
            new("1.0.0.20", "2026-07-05", new[]
            {
                "New \"Chronological party-chat summary\" option on the Chat tab. When on, the per-press announcements are held back and the whole announcement list is instead sent to Party (/p) as one ordered list, only once both Exdeaths, both debuff picks and both chaos have been pressed.",
                "The list is sent in resolution order: 1st-set Exdeath debuffs, then the 1st gaze, then Inferno, then the 2nd-set debuffs, then the 2nd gaze, then Tsunami. It uses the Party (/p) channel's configured announcement messages, sends exactly once per pull, and re-arms after a Reset or Undo.",
            }),
            new("1.0.0.19", "2026-07-05", new[]
            {
                "Chaos resolutions are now static: Inferno always resolves in the First Set and Tsunami always in the Second Set, no matter which you press first.",
                "The Inferno and Tsunami buttons each disable after you press them, until the next Reset - so you can no longer double-enter the same mechanic.",
                "Removed the 1st/2nd labelling from Inferno and Tsunami, since their sets are fixed. Default Chaos announcement messages no longer carry a set number, and the Chaos announcement sections are now named \"Inferno\" and \"Tsunami\" instead of \"First set\" / \"Second set\".",
            }),
            new("1.0.0.18", "2026-07-05", new[]
            {
                "The Undo button can now float as its own panel, just like Hide and Reset. A new \"Floating Undo button\" setting sits next to the Floating Hide/Reset options, and all three float by default (turn one off to dock that button back onto the toolbar).",
                "Acceleration text now shares the same line as the Stack/Spread callout by default (for example \"Spread on X and MOVE\"), so the setting \"Acceleration text on same line as Stack/Spread\" is now on out of the box - turn it off to put Acceleration back on its own line.",
            }),
            new("1.0.0.17", "2026-07-05", new[]
            {
                "Chat tab: a master \"Enable chat announcements\" toggle sits at the top and is OFF by default. While it is off, nothing is sent to chat no matter which per-mechanic announcements are turned on - turn it on once you have your announcements set up.",
                "New UNDO button on the toolbar (next to Reset) steps back the last button press and its resolution text, one press at a time - handy for a misclick without resetting the whole pull. It is also available as the slash command /snazzyp4 undo. Note: chat announcements and markers already sent for that press cannot be recalled.",
            }),
            new("1.0.0.16", "2026-07-05", new[]
            {
                "Announce Last Fake: the docked ANNOUNCE button can now anchor to any side of the Kefka panel - a new \"Dock side\" dropdown (Top / Bottom / Left / Right) appears when docking is on.",
                "Chat announcements (ordered mode): every set now has an \"Announce Title\" line, defaulting to \"---------- 1st Set ----------\" / \"---------- 2nd Set ----------\" for Exdeath and \"---------- 1st Chaos ----------\" / \"---------- 2nd Chaos ----------\" for Chaos. Like any slot it is off until you enable it and can be reordered or customised.",
                "Chat announcements (ordered mode): a \"+ Add custom message\" button lets you add your own extra messages into the list alongside the mechanics, each reorderable and removable. The title and custom messages always fire when enabled (Chaos still only fires the mechanic you pressed).",
                "General tab: a gold \"Never show version update messages\" toggle stops the changelog popup from appearing after each update, and the update popup itself now has a matching button. The full changelog is always still available from the title-bar button.",
            }),
            new("1.0.0.15", "2026-07-05", new[]
            {
                "Extended chat announcements (Chat tab): the old \"Announce Gaze / Chaos\" toggles are replaced with a full announcement system. \"Announce Exdeath\" fires the moment you press an Exdeath Real/Fake button; \"Announce Chaos\" fires when you press a chaos button. Each is organised into First/Second set and then Real/Fake.",
                "Two modes per category. Ordered list: a reorderable list of per-mechanic toggles (Gaze, Spread, Water Drop, Acceleration for Exdeath; Inferno, Tsunami for Chaos). Enable the ones you want; the order is the send order. Expand a toggle's Message settings for \"Enable custom message\" and a reorderable list of message boxes you can grow with + and shrink with - (blank boxes are skipped). Exdeath sends every enabled toggle; Chaos sends only the mechanic you pressed. Simple text box: one growing box per set/real-fake, one chat message per non-empty line.",
                "Announcements are stored per channel. Pick the active channel at the top of the Chat tab; only that channel is sent to. \"Copy settings to...\" clones the current channel's setup to another channel. Sensible default messages are generated for every announcement.",
                "Last Fake ANNOUNCE (Hidden tab, once Last Fake is unlocked): \"Announce Last Fake\" adds an ANNOUNCE button (floating by default, or dockable to the Kefka text panel) that posts the current Kefka values to a channel of your choice. Put {KefkaThunder} and {KefkaBlizzard} in your message and they become the current value (REAL/FAKE by default, both customisable; \"?\" if that mechanic has not been pressed).",
            }),
            new("1.0.0.14", "2026-07-05", new[]
            {
                "Update notice: the first time you open the plugin after updating, a window pops up telling you which version you came from, which version you are now on, and a detailed list of everything that changed in between. It only appears once per update (not every time you log in), and closing it dismisses it until the next update.",
                "The plugin version is now shown next to \"Snazzy P4\" at the top of the settings window.",
                "A changelog button (the clipboard icon in the settings window's title bar) opens a window with the full, detailed changelog for every version so you can read it any time.",
            }),
            new("1.0.0.13", "2026-07-05", new[]
            {
                "Customisable auto-markers: under Settings > General > Auto-place marker you can now choose which head marker is placed for each role and set (First/Second set for Support and DPS). Options include Attack 1-8, Bind 1-3, Ignore 1-2, the four shapes, or (none). The marker is always placed on yourself.",
                "Customisable target letters: the A/B/C/D letters shown in the resolutions (\"Spread on B\") can be renamed per role in Settings > Text, under the new \"Target letters\" group.",
            }),
            new("1.0.0.12", "2026-07-05", new[]
            {
                "New Text tab (Settings > Text): rename almost every piece of text the plugin shows - the panel labels (< First Set >, < Kefka >, Last Fake?), the spread/stack prefixes, gaze, acceleration, chaos and Thunder/Blizzard callouts, the section headers and the RESET/HIDE/SHOW buttons. Leave a field blank to keep its default (shown as a greyed hint).",
            }),
            new("1.0.0.11", "2026-07-04", new[]
            {
                "The chat announcements moved to their own Chat tab (renamed from \"Party chat messages\" to \"Chat Messages\").",
                "Added more announcement channels: Tell (your current target), Free Company, Linkshells 1-8 and Cross-world Linkshells 1-8.",
            }),
            new("1.0.0.10", "2026-07-03", new[]
            {
                "Chat channel selector: gaze and chaos announcements can be sent to a channel of your choice (Party, Say, Yell, Shout, Alliance, or Echo so only you see it - handy for testing) instead of always party chat.",
            }),
            new("1.0.0.9", "2026-07-03", new[]
            {
                "Settings are now organised into tabs (General, Chat, Appearance, Colors, Text, Layout, Controller, Sections, Hidden).",
                "Settings profiles (Settings > General): copy your whole setup to the clipboard and paste it back or share it with others.",
                "Colourblind palette presets (Settings > Colors): one-click Deuteranopia / Protanopia / Tritanopia palettes plus Default.",
                "Bring all windows on-screen (Settings > Layout, detached): clamps every detached window back into view.",
                "The combined-set divider can now be pinned in both orientations (stacked as well as side by side) and its thickness and colour are customisable.",
                "Added this changelog.",
            }),
            new("1.0.0.8", "2026-07-03", new[]
            {
                "Combined sets can pin their divider at a fixed position: the two sets grow outward from the divider while the text stays left-aligned. Drag the panel or use the CombinedSets X/Y sliders to place the divider.",
                "The previous right-align behaviour became the separate \"Mirror the sets\" option.",
            }),
            new("1.0.0.7", "2026-07-03", new[]
            {
                "Added the first expand-from-divider (mirror) option for the combined set panel.",
            }),
            new("1.0.0.6", "2026-07-03", new[]
            {
                "Acceleration on the same line (Settings > Layout): append MOVE / STAND STILL to the spread/stack line, e.g. \"Spread on X and MOVE\".",
                "The First Set / Second Set labels now respect the Hide Labels setting.",
                "Combine First and Second set into one panel (Settings > Layout): stacked or side by side with a divider.",
            }),
            new("1.0.0.5", "2026-07-03", new[]
            {
                "Highlighted the Hide macro buttons setting in Controller Settings so it is easier to find.",
            }),
            new("1.0.0.4", "2026-07-03", new[]
            {
                "Controller support: every button now has a /snazzyp4 slash command that does the same thing, so controller players can bind them to macros.",
                "New Controller Settings section with copyable commands and a \"Hide macro buttons\" option that leaves only the resolution text panels on screen.",
            }),
            new("1.0.0.3", "2026-07-03", new[]
            {
                "Softened the wording of the hidden Last Fake unlock warning.",
            }),
            new("1.0.0.2", "2026-07-03", new[]
            {
                "Added kefka / phase search tags for the plugin listing.",
            }),
            new("1.0.0.1", "2026-07-03", new[]
            {
                "The Floating Hide button is now enabled by default.",
            }),
            new("1.0.0.0", "2026-07-03", new[]
            {
                "Initial release of Snazzy P4: an on-screen cheat sheet for the Kefka UMAD/DMU Phase 4 mechanics. Press the buttons for what you see and it lays out clean, colour-coded callouts (spread/stack, gaze, chaos, Thunder/Blizzard). Everything is repositionable, rescalable and recolourable, windowed or as detached panels. Open with /snazzyp4, settings with /snazzyp4 config.",
            }),
        };
    }
}
