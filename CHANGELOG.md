# Changelog

All notable changes to Snazzy P4 are listed here. Versions match the `vX.Y.Z.W`
release tags.

## v1.0.8.0 — 2026-07-12

- **Global label and title-bar switches** under General Settings, so they're easy to find:
  **Hide all name labels** (off by default) and **Hide all title bars** (on by default).
- While a global switch is on it hides everything of that kind, and the matching per-section
  switches in the Layout tab are **disabled** since they no longer matter. Turn the global
  switch off to pick per section again.

## v1.0.7.1 — 2026-07-12

- While the **Floating Hide button is off**, the settings toolbar now **always shows**
  (collapsible as normal) and is never hidden — it is the only way to bring the display back in
  that setup, so **Hide Settings toolbar when UI is hidden** and **Show toolbar** off are
  overridden to prevent a confusing lock-out.

## v1.0.7.0 — 2026-07-12

- **Hiding the UI no longer leaves an empty black square.** The toolbar now **collapses when
  you press Hide and expands when you press Show**, and the expand/collapse arrows keep working
  while the UI is hidden — a collapsed toolbar stays collapsed, and you can expand it any time.
- **Hide Settings toolbar when UI is hidden** (General Settings, off by default) — the toolbar
  disappears completely while the UI is hidden instead of showing collapsed; the hub window
  goes fully invisible and click-through.
- **Persistent Settings Collapsed State** (General Settings, off by default) — the toolbar's
  collapsed state ignores Hide/Show presses and only changes via the arrows. The hide-entirely
  option still wins while the UI is hidden.

## v1.0.6.1 — 2026-07-12

- **Thunder and Blizzard buttons lock after being pressed**, matching the chaos buttons: press
  a Thunder (real or fake) and both Thunder buttons disable until Reset; same for Blizzard. Use
  **Undo** to take back a mispress. The **Last Fake toggles are unaffected** and stay usable.

## v1.0.6.0 — 2026-07-12

- **UI Scale is renamed Global UI Scale** and still multiplies everything the plugin draws.
- **Two new scale sliders** sit right below it, each with 50/100/150/200% presets and both
  defaulting to 1.00×:
  - **Settings Toolbar Scale** — multiplies just the quick-settings toolbar.
  - **Macro UI Scale** — multiplies the macro buttons and text panels (on top of the global
    scale and the per-section scales).

## v1.0.5.1 — 2026-07-10

- **Hide Resolved Buttons no longer shifts the remaining buttons** — a hidden chaos or Kefka
  pair keeps its exact spot, so pressing Inferno no longer moves the Tsunami buttons up (and
  likewise for Thunder/Blizzard).
- **Fixed misaligned text in windowed (non-detached) mode** — the Real/Fake and Short/Long
  column headers and the Last Fake column were pushed right by the panel's position. They now
  line up the same as in detached mode.

## v1.0.5.0 — 2026-07-10

- **One body debuff per pull** ([#1](https://github.com/snazzysosnazzy/SnazzyP4/issues/1)
  follow-up) — picking Lightning **or** Drop now locks **all** Lightning and Drop buttons, since
  the fight only ever assigns one of the two. After a body pick, only Acceleration remains
  selectable.
- **Hide Resolved Buttons Until Reset** — new General Settings option (off by default). The
  Exdeath buttons hide once both sets are fully entered, each chaos pair hides once pressed, and
  each Kefka pair hides once pressed. Everything returns on Reset. Text panels and the Last Fake
  toggles are never affected.

## v1.0.4.4 — 2026-07-10

- Fixed a **brief lag spike when the window first opened** (most noticeable with the auto-open
  on entering the duty). All button icons are now **preloaded once at plugin start** and kept in
  memory (well under 1 MB), instead of being reloaded from disk and uploaded to the GPU in one
  burst on the zone-in frame.

## v1.0.4.3 — 2026-07-06

- Implemented [#1](https://github.com/snazzysosnazzy/SnazzyP4/issues/1): a debuff kind can no
  longer be picked twice in a pull. Once you press a Lightning (spread), Drop (stack) or
  Acceleration, **both its Short and Long buttons grey out** until Reset — the fight never gives
  you the same one twice. Previously only duplicates within a single set were blocked.

## v1.0.4.2 — 2026-07-06

- Fixed [#2](https://github.com/snazzysosnazzy/SnazzyP4/issues/2): an announcement only started
  working after you opened its dropdown in the Chat tab. Each set's announcement slots now get
  their default enabled state as soon as the channel is used, so every enabled callout fires
  without having to open each set first.

## v1.0.4.1 — 2026-07-06

- Moved the **Automation** section on the General tab to sit **above General Settings** (just
  below Role).

## v1.0.4.0 — 2026-07-06

- **Automation settings** (General tab, all **off by default**):
  - **Auto Open/Close SnazzyP4 upon Enter/Exit of Duty** — opens the overlay when you enter a
    captured instance and closes it when you leave. Click **Use current instance** while inside
    Dancing Mad (Ultimate) to set the trigger zone.
  - **Reset on Hide Button Press** — runs Reset whenever you press the Hide button.
  - **Reset on Wipe** / **Hide on Wipe** — run Reset / Hide when the party wipes, via the game's
    duty-state wipe detection.
- These are the only game state the plugin reads, and it's non-combat (which duty you're in, and
  whether the party wiped) — nothing about the fight itself is read or resolved.

## v1.0.3.3 — 2026-07-06

- Minor internal cleanup and changelog wording tidy-up.

## v1.0.3.2 — 2026-07-06

- Fixed the **Layout** tab showing in the wrong place (after Text) instead of between **Chat**
  and **Colors** — ImGui was remembering the old Layout tab's saved position. Tab order is now
  General, Chat, Layout, Colors, Text, Controller, Hidden as intended.

## v1.0.3.1 — 2026-07-06

- The changelog now shows each version's **release date**, both in the update popup and the
  full changelog window.
- Renamed the in-app **Layout** heading on the General tab to **General Settings** so it no
  longer clashes with the Layout tab.

## v1.0.3.0 — 2026-07-06

- **Settings tabs reorganised.** The old **Layout** tab's options (toolbar, detached windows,
  edit layout, Move All, floating Hide/Reset/Undo, Acceleration on the same line, combine sets,
  bring windows on-screen) now live on the **General** tab, just below the marker settings.
- The **Appearance** tab has absorbed the old **Sections** tab and is renamed **Layout**. The
  separate Appearance and Sections tabs are gone. Tabs are now: General, Chat, Layout, Colors,
  Text, Controller, Hidden.

## v1.0.2.1 — 2026-07-06

- The **Edit Layout** preview now shows **INFERNO TWISTER** in the first set (and Tsunami in the
  second), matching the static chaos sets, instead of Tsunami in both.

## v1.0.2.0 — 2026-07-06

- **Party Mode / Personal Mode** — announcements now have two mutually-exclusive modes.
  - **Party Mode** (new default) sends only the party-safe callouts — **gaze and
    Inferno/Tsunami** — so it is safe to broadcast to your party. By default everything is
    enabled except the titles.
  - **Personal Mode** is the old full behaviour (all debuffs, titles, custom callouts). It is
    **hidden by default** — tick **Show Personal Mode (advanced)** to reveal it. It blocks its
    non-party-safe callouts from `/p` party chat and suggests Party Mode, with a heavily-warned
    **override** if you insist on sending them to party anyway.
- **Per-channel announcements** (Personal Mode) — route each announcement to its own channel
  instead of the single selected channel.
- Built-in announcements now start **enabled (except titles)** on a fresh setup, so Party Mode
  works out of the box once the master switch is on.

## v1.0.1.1 — 2026-07-06

- **Quick toggle buttons** now size to fit their labels, so the text no longer clips.
- The **set-title** Quick toggles only affect the 1st/2nd set (Exdeath) titles now, leaving
  the Inferno/Tsunami (Chaos) titles alone.
- New Chat-tab setting **Include [1st] / [2nd] prefix in default messages** (on by default).
  Turn it off and a default reads `Lightning - Spread` instead of `[1st] Lightning - Spread`.

## v1.0.1.0 — 2026-07-06

- **Inferno/Tsunami terminology** — the words "Fire" and "Water" are gone. The resolution text
  panel reads **INFERNO TWISTER / INFERNO DONUT / TSUNAMI DONUT / TSUNAMI TWISTER**, and the
  colour pickers are labelled **Inferno** and **Tsunami**.
- **STAY / MOVE on chaos announcements** — chaos announcement defaults now spell out the
  movement: **Donut (STAY)** and **Twister (MOVE)**, e.g. `Inferno - Twister (MOVE)`,
  `Tsunami - Donut (STAY)`. The STAY/MOVE hint is only on announcements; the resolution text
  panel still shows just the twister/donut shape.
- **Real/Fake in set titles** — First/Second set announcement titles now include the state,
  e.g. `---------- 2nd Set : FAKE ----------`.

## v1.0.0.22 — 2026-07-06

- **Quick toggles** (Chat tab) — **Turn on / off all announcements** (everything except titles)
  and **Turn on / off set titles**, each applied across the whole selected channel (both
  Exdeath and Chaos, every set and real/fake).
- **New default message format** — `[set] Debuff - Resolvement`, e.g. `[1st] Lightning - Spread`
  or `[2nd] Acceleration - Move`. Only the generated defaults change; customised messages are
  left alone.

## v1.0.0.21 — 2026-07-06

- **Chronological summary follows the selected channel** — it now sends to whichever channel
  is chosen in the Chat tab (using that channel's configured messages), instead of always
  going to Party (/p).

## v1.0.0.20 — 2026-07-05

- **Chronological party-chat summary** — a new Chat-tab option. When on, the per-press
  announcements are held back and the whole announcement list is instead sent to **Party
  (/p)** as one ordered list, **only once both Exdeaths, both debuff picks and both chaos have
  been pressed**. Order: 1st-set debuffs → 1st gaze → Inferno → 2nd-set debuffs → 2nd gaze →
  Tsunami. It uses the Party (/p) channel's configured messages, sends once per pull, and
  re-arms after a Reset or Undo.

## v1.0.0.19 — 2026-07-05

- **Static Chaos sets** — Inferno always resolves in the **First Set** and Tsunami always in
  the **Second Set**, regardless of press order.
- **One press each** — the Inferno and Tsunami buttons each **disable after being pressed**
  until the next Reset, so the same mechanic can't be entered twice.
- **No more 1st/2nd on Chaos** — the set-number labelling is dropped from Inferno/Tsunami.
  Default Chaos announcement messages no longer carry a set number, and the Chaos
  announcement sections are now named **Inferno** and **Tsunami** instead of First/Second set.

## v1.0.0.18 — 2026-07-05

- **Floating Undo button** — the Undo button can now float as its own panel like Hide and
  Reset. A new **Floating Undo button** setting sits next to the Floating Hide/Reset options,
  and all three **float by default** (turn one off to dock that button onto the toolbar).
- **Acceleration on the same line by default** — the **Acceleration text on same line as
  Stack/Spread** setting is now **on out of the box** (e.g. "Spread on X and MOVE"). Turn it
  off to put Acceleration back on its own line.

## v1.0.0.17 — 2026-07-05

- **Master announcements toggle** — an **Enable chat announcements** checkbox at the top of the
  **Chat** tab, **off by default**. While off, nothing is sent no matter which per-mechanic
  announcements are enabled.
- **Undo button** — a new **Undo** button on the toolbar (next to Reset) steps back the last
  button press and its resolution text, one press at a time. Also available as
  `/snazzyp4 undo`. Chat announcements and markers already sent for that press are not recalled.

## v1.0.0.16 — 2026-07-05

- **Dock ANNOUNCE to any side** — when the Last Fake ANNOUNCE button is docked, a new
  **Dock side** dropdown anchors it to the **Top**, **Bottom**, **Left** or **Right** of the
  Kefka panel.
- **Announce Title** — each announcement set now has a title line, defaulting to
  `---------- 1st Set ----------` / `---------- 2nd Set ----------` (Exdeath) and
  `---------- 1st Chaos ----------` / `---------- 2nd Chaos ----------` (Chaos). Off until
  enabled, reorderable and customisable like any slot.
- **Custom message entries** — a **+ Add custom message** button adds your own extra
  messages into the ordered list alongside the mechanics, each reorderable and removable.
  Titles and custom messages always fire when enabled (Chaos still fires only the pressed mechanic).
- **Never show version update messages** — a gold toggle at the top of the **General** tab
  (and a matching button on the update popup) stops the changelog appearing after each
  release. The full changelog stays available from the title-bar button.

## v1.0.0.15 — 2026-07-05

- **Extended chat announcements** — the Chat tab replaces the old Gaze/Chaos toggles
  with a full system. **Announce Exdeath** fires on an Exdeath Real/Fake press,
  **Announce Chaos** on a chaos press, each organised by First/Second set and Real/Fake.
  Two modes: **ordered list** (reorderable per-mechanic toggles, each with an
  "Enable custom message" and a reorderable `+`/`-` list of messages) and **simple text
  box** (one message per line). Settings are **per channel**; a **Copy settings to...**
  clones one channel's setup to another. Default messages are generated.
- **Last Fake ANNOUNCE** (Hidden tab) — an **ANNOUNCE** button (floating or dockable to the
  Kefka panel) posts the current Kefka values to a channel. Use `{KefkaThunder}` /
  `{KefkaBlizzard}` in the message (current value, REAL/FAKE customisable, `?` if unpressed).

## v1.0.0.14 — 2026-07-05

- **Update notice** — the first time you open the plugin after an update, a window
  shows which version you came from, which you're on now, and a detailed changelog
  of everything in between. Once per update only; closing dismisses it.
- **Version in settings** — shown next to "Snazzy P4" at the top of the settings window.
- **Changelog button** — the clipboard icon in the settings title bar opens the full,
  detailed changelog for every version, browsable any time.

## v1.0.0.13 — 2026-07-05

- **Customisable auto-markers** — choose which head marker is placed for each
  role and set (attack 1-8, bind 1-3, ignore 1-2, the shapes, or none). Shown
  under Auto-place marker in the General tab. The marker is always self-placed.
- **Customisable target letters** — the A/B/C/D spread/stack letters shown in the
  resolutions can now be renamed in the Text tab (per role).

## v1.0.0.12 — 2026-07-05

- **Custom text** — a new **Text** settings tab lets you rename every panel label,
  section header, resolution callout (spread/stack, gaze, acceleration, chaos,
  Thunder/Blizzard) and button to whatever text you want. Leave a field blank to
  keep the default.

## v1.0.0.11 — 2026-07-04

- The chat announcements moved to their own **Chat** settings tab (renamed from
  "Party chat messages" to **Chat Messages**).
- Added more channels: **Tell** (current target), **Free Company**, **Linkshells
  1-8** and **Cross-world Linkshells 1-8**.

## v1.0.0.10 — 2026-07-03

- **Chat channel selector** — the gaze and chaos announcements can now be sent to
  a channel of your choice (Party, Say, Yell, Shout, Alliance, or **Echo** so only
  you see it, which is handy for testing).

## v1.0.0.9 — 2026-07-03

- **Tabbed settings** — the settings window is now organised into General,
  Appearance, Colors, Layout, Controller, Sections and Hidden tabs.
- **Settings profiles** — copy your whole setup to the clipboard and paste it
  back (or share it with others) from the General tab.
- **Colourblind palette presets** — one-click Deuteranopia / Protanopia /
  Tritanopia palettes (plus Default) in the Colors tab.
- **Bring all windows on-screen** — a Layout button that clamps every detached
  window back into view.
- **Pinned divider, both orientations** — the combined-set divider can now be
  pinned when stacked (grows up/down) as well as side by side.
- **Customisable divider** — thickness and colour options for the combined-set
  divider.
- Added this changelog.

## v1.0.0.8 — 2026-07-03

- Added the pinned-divider mode for the combined set panel: the divider holds a
  fixed position while the sets grow outward and the text stays left-aligned.
- Renamed the previous right-align behaviour to the "Mirror the sets" option.

## v1.0.0.7 — 2026-07-03

- Added the expand-from-divider (mirror) option for combined sets.

## v1.0.0.6 — 2026-07-03

- **Acceleration on the same line** — option to append MOVE / STAND STILL to the
  Spread/Stack line ("Spread on X and MOVE").
- The First Set / Second Set labels now respect the Hide Labels setting.
- **Combined set panel** — option to draw the First and Second sets in one panel,
  stacked or side by side with a divider.

## v1.0.0.5 — 2026-07-03

- Highlighted the Hide macro buttons setting in Controller Settings so it is
  easier to find.

## v1.0.0.4 — 2026-07-03

- **Controller support** — every button now has a `/snazzyp4 <button>` slash
  command that performs the same action.
- Added the Controller Settings section with copyable commands and a Hide macro
  buttons option that keeps only the text panels.

## v1.0.0.3 — 2026-07-03

- Softened the wording of the hidden Last Fake unlock warning.

## v1.0.0.2 — 2026-07-03

- Added kefka / phase search tags.

## v1.0.0.1 — 2026-07-03

- The Floating Hide button is now enabled by default.

## v1.0.0.0 — 2026-07-03

- Initial release of Snazzy P4 as a Dalamud plugin distributed through a custom
  repository, with a tag-triggered release workflow and Ko-fi support link.
