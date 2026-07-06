# Changelog

All notable changes to Snazzy P4 are listed here. Versions match the `vX.Y.Z.W`
release tags.

## v1.0.0.22

- **Quick toggles** (Chat tab) — **Turn on / off all announcements** (everything except titles)
  and **Turn on / off set titles**, each applied across the whole selected channel (both
  Exdeath and Chaos, every set and real/fake).
- **New default message format** — `[set] Debuff - Resolvement`, e.g. `[1st] Lightning - Spread`
  or `[2nd] Acceleration - Move`. Only the generated defaults change; customised messages are
  left alone.

## v1.0.0.21

- **Chronological summary follows the selected channel** — it now sends to whichever channel
  is chosen in the Chat tab (using that channel's configured messages), instead of always
  going to Party (/p).

## v1.0.0.20

- **Chronological party-chat summary** — a new Chat-tab option. When on, the per-press
  announcements are held back and the whole announcement list is instead sent to **Party
  (/p)** as one ordered list, **only once both Exdeaths, both debuff picks and both chaos have
  been pressed**. Order: 1st-set debuffs → 1st gaze → Inferno → 2nd-set debuffs → 2nd gaze →
  Tsunami. It uses the Party (/p) channel's configured messages, sends once per pull, and
  re-arms after a Reset or Undo.

## v1.0.0.19

- **Static Chaos sets** — Inferno always resolves in the **First Set** and Tsunami always in
  the **Second Set**, regardless of press order.
- **One press each** — the Inferno and Tsunami buttons each **disable after being pressed**
  until the next Reset, so the same mechanic can't be entered twice.
- **No more 1st/2nd on Chaos** — the set-number labelling is dropped from Inferno/Tsunami.
  Default Chaos announcement messages no longer carry a set number, and the Chaos
  announcement sections are now named **Inferno** and **Tsunami** instead of First/Second set.

## v1.0.0.18

- **Floating Undo button** — the Undo button can now float as its own panel like Hide and
  Reset. A new **Floating Undo button** setting sits next to the Floating Hide/Reset options,
  and all three **float by default** (turn one off to dock that button onto the toolbar).
- **Acceleration on the same line by default** — the **Acceleration text on same line as
  Stack/Spread** setting is now **on out of the box** (e.g. "Spread on X and MOVE"). Turn it
  off to put Acceleration back on its own line.

## v1.0.0.17

- **Master announcements toggle** — an **Enable chat announcements** checkbox at the top of the
  **Chat** tab, **off by default**. While off, nothing is sent no matter which per-mechanic
  announcements are enabled.
- **Undo button** — a new **Undo** button on the toolbar (next to Reset) steps back the last
  button press and its resolution text, one press at a time. Also available as
  `/snazzyp4 undo`. Chat announcements and markers already sent for that press are not recalled.

## v1.0.0.16

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

## v1.0.0.15

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

## v1.0.0.14

- **Update notice** — the first time you open the plugin after an update, a window
  shows which version you came from, which you're on now, and a detailed changelog
  of everything in between. Once per update only; closing dismisses it.
- **Version in settings** — shown next to "Snazzy P4" at the top of the settings window.
- **Changelog button** — the clipboard icon in the settings title bar opens the full,
  detailed changelog for every version, browsable any time.

## v1.0.0.13

- **Customisable auto-markers** — choose which head marker is placed for each
  role and set (attack 1-8, bind 1-3, ignore 1-2, the shapes, or none). Shown
  under Auto-place marker in the General tab. The marker is always self-placed.
- **Customisable target letters** — the A/B/C/D spread/stack letters shown in the
  resolutions can now be renamed in the Text tab (per role).

## v1.0.0.12

- **Custom text** — a new **Text** settings tab lets you rename every panel label,
  section header, resolution callout (spread/stack, gaze, acceleration, chaos,
  Thunder/Blizzard) and button to whatever text you want. Leave a field blank to
  keep the default.

## v1.0.0.11

- The chat announcements moved to their own **Chat** settings tab (renamed from
  "Party chat messages" to **Chat Messages**).
- Added more channels: **Tell** (current target), **Free Company**, **Linkshells
  1-8** and **Cross-world Linkshells 1-8**.

## v1.0.0.10

- **Chat channel selector** — the gaze and chaos announcements can now be sent to
  a channel of your choice (Party, Say, Yell, Shout, Alliance, or **Echo** so only
  you see it, which is handy for testing).

## v1.0.0.9

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
- Added an MIT `LICENSE` and this changelog.

## v1.0.0.8

- Added the pinned-divider mode for the combined set panel: the divider holds a
  fixed position while the sets grow outward and the text stays left-aligned.
- Renamed the previous right-align behaviour to the "Mirror the sets" option.

## v1.0.0.7

- Added the expand-from-divider (mirror) option for combined sets.

## v1.0.0.6

- **Acceleration on the same line** — option to append MOVE / STAND STILL to the
  Spread/Stack line ("Spread on X and MOVE").
- The First Set / Second Set labels now respect the Hide Labels setting.
- **Combined set panel** — option to draw the First and Second sets in one panel,
  stacked or side by side with a divider.

## v1.0.0.5

- Highlighted the Hide macro buttons setting in Controller Settings so it is
  easier to find.

## v1.0.0.4

- **Controller support** — every button now has a `/snazzyp4 <button>` slash
  command that performs the same action.
- Added the Controller Settings section with copyable commands and a Hide macro
  buttons option that keeps only the text panels.

## v1.0.0.3

- Softened the wording of the hidden Last Fake unlock warning.

## v1.0.0.2

- Added kefka / phase search tags.

## v1.0.0.1

- The Floating Hide button is now enabled by default.

## v1.0.0.0

- Initial release of Snazzy P4 as a Dalamud plugin distributed through a custom
  repository, with a tag-triggered release workflow and Ko-fi support link.
