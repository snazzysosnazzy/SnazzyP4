## Planned Updates

1. Alternative mode: Combination macros. Cuts number of macros in half by doing "combination" button presses. Further cuts down buttons
2. Alternative mode; Simple macros. Cuts numbers of macros down the simplest number needed.

# Snazzy P4

A robust custom macro system for the Kefka UMAD/DMU Phase 4 mechanics. You tap the
buttons for what you see in the fight, and the plugin turns them into short, readable
callouts (spread/stack targets, gaze, chaos and Kefka) so you can cut the macro bloat.

![Snazzy P4 in action](docs/preview.png)

## Preface

I made this plugin ultimately to cut down on macro bloat and make it easier to read/parse macro text. As well as creating
a solution for the community that would persuade them to avoid ever adopting Auto-Markers for this phase.

What Snazzy P4 does ***NOT*** do:
1. Does not read the game in any way or parse combat data in anyway.
2. Does not resolve the mechanic for you.

I have tried my best to stay true to this.

This means you must still:
1. Read your debuffs.
2. Press macros appropriately.
3. Know whether you are the Gaze shooter.
4. Resolve Anti-Light by looking at debuffs.
5. Resolve the Thunder/Blizzard Fake/Real Math.
6. Know how the mechanic works, when things occur, and where to go.

I did not make this plugin to do everything for you, which is why it does not include the things I've listed above.
If you desire those things - use cactbot. I will not be updating this plugin to do any of those things.

## Overview

You hate macro slop - so do I. 

Snazzy P4 is designed to be a replacement for the in-game macros, reducing macro bloat
and making macro resolution text more readable and easier to parse.

You press a few buttons as the mechanics happen and it lays out clean, colour-coded text telling you
what to do. Everything is repositionable, rescalable and recolourable, and it can run
as one window or as separate floating panels.

- **Exdeath** — Real (`!`) or Fake (`?`) macros for Exdeath tell, 
                Short/Long debuff (Lightning / Drop / Acceleration) macros, 
				and respective Resolution text.
- **Chaos** — Inferno / Tsunami macros and resolution text.
- **Kefka** — Thunder / Blizzard buttons macros and resolution text.
- **Organized Sets** — Separate First/Second set text windows that organize the resolution text in chronological order
                       in which they will occur (or optionally combined into one panel, stacked or side by side).
					   This includes the following (In this exact order):
					       1. Spread / Stack
                           2. Move / Stand Still
                           3. Gaze
                           4. Twister / Donut
- **Chat Callouts** — Optional announcements sent to a chat channel of your choice (party,
                      linkshell, free company, echo for testing, and more). A party-safe
                      **Party Mode** (gaze + Inferno/Tsunami only) is the default; an advanced
                      **Personal Mode** adds your debuff/title/custom callouts.
- **Self Marked Spreads** — Optional Self marking for spread markers based on role.

## Installation

Snazzy P4 is distributed through my personal Dalamud plugin repository.

1. In-game, open the Dalamud settings with `/xlsettings`.
2. Go to the **Experimental** tab.
3. Under **Custom Plugin Repositories**, paste this URL into an empty row and press the **+** button:

   ```
   https://raw.githubusercontent.com/snazzysosnazzy/SnazzyP4/main/pluginmaster.json
   ```

4. Press **Save and Close**.
5. Open the plugin installer with `/xlplugins`, search for **Snazzy P4**, and install it.
6. Open the plugin with `/snazzyp4`.

## How to Use

1. Open by typing the command `/snazzyp4`
2. Access Settings using either the **Settings** button on the Toolbar, or typing the command `/snazzyp4 config`.
3. Select your **Role** in **Settings** — Support or DPS.
4. As the fight plays out, press the buttons for what happens:
   - Press **Real (`!`)** or **Fake (`?`)** for the **1st Exdeath** tell, then the **Short/Long**
     button matching your debuff for that set. Repeat for the **2nd Exdeath**.
   - Press the **Chaos** button (Inferno/Tsunami) that goes off.
   - Press the **Kefka** button (Thunder/Blizzard) that goes off.
5. Read the **First Set** / **Second Set** / **Kefka** panels for the resolution.
6. Made a misclick? Press **Undo** to step back the last button press (one at a time).
7. Press **RESET** between pulls to clear everything.

That's it — the buttons grey out when they no longer apply, so you can't mis-enter.

### Moving things around

- **Edit Layout** (toolbar or settings): the buttons lock and every panel fills with
  sample text; click-and-drag any panel to reposition it. Turn it off when done.
- **Detached** mode makes each panel its own window. **Move All** (detached only) drags
  every window together.

## Controller Players

Controller players can't click the panel buttons, so every button also has a slash command
that does the exact same thing. Put each one into its own in-game macro and bind it.

**Recommended:** since you won't be clicking anything, turn on **Hide macro buttons** in
**Settings → Controller Settings**. It removes the Exdeath / Chaos / Kefka / Reset / Hide
button panels from the normal UI and keeps only the **First Set**, **Second Set** and
**Kefka** resolution text on screen. That same section also has a **Copy** button for every
command below.

```
/snazzyp4 ExDeathReal        /snazzyp4 ExDeathFake
/snazzyp4 LightningShort     /snazzyp4 LightningLong
/snazzyp4 DropShort          /snazzyp4 DropLong
/snazzyp4 AccelerationShort  /snazzyp4 AccelerationLong
/snazzyp4 InfernoReal        /snazzyp4 InfernoFake
/snazzyp4 TsunamiReal        /snazzyp4 TsunamiFake
/snazzyp4 ThunderReal        /snazzyp4 ThunderFake
/snazzyp4 BlizzardReal       /snazzyp4 BlizzardFake
/snazzyp4 Reset              /snazzyp4 Hide
/snazzyp4 Undo
```

The commands respect the same rules as the buttons — a pick that isn't valid yet (for example
a short/long before its Exdeath) is simply ignored, exactly like a greyed-out button.

## Configuration Settings

Open with the **Settings** button or `/snazzyp4 config`. Settings are grouped into tabs:

- **General** — a gold **Never show version update messages** toggle (stops the changelog
  popup after each update), Role (Support / DPS), UI scale, auto-place `/mk` markers with a
  **choice of head marker per role and set** (attack / bind / ignore / shapes, always placed
  on yourself), then a **General Settings** group — show toolbar, detached windows, edit layout,
  Move All, floating **Hide** / **Reset** / **Undo** buttons, **Acceleration on the same line**
  as Spread/Stack (`Spread on X and MOVE`), **combine the First and Second sets** into one panel
  (stacked or side by side, with a pinnable divider), and **bring all windows on-screen** —
  followed by **settings profiles** (copy your whole setup to the clipboard or paste one in) and
  the reset buttons (**Reset layout to defaults** / **Restore ALL settings to defaults**).
- **Chat** — a full announcement system, gated behind a master **Enable chat announcements**
  toggle (off by default). It has two mutually-exclusive modes: **Party Mode** (default) sends
  only the party-safe callouts — gaze and Inferno/Tsunami — so it is safe to broadcast to your
  party, while **Personal Mode** (advanced, hidden until you reveal it) adds your debuff, title
  and custom callouts but blocks those from `/p` party chat unless you flip a heavily-warned
  override. **Announce Exdeath** fires when you press an Exdeath Real/Fake button and **Announce
  Chaos** when you press a chaos button, each split by First/Second set and Real/Fake. Choose
  **ordered-list** mode (reorderable per-mechanic toggles with custom, reorderable message lists,
  an optional **Announce Title** line, and a **+ Add custom message** button) or **simple text
  box** mode (one chat line per message). Settings are kept **per channel** (Party, Say,
  Linkshells, Echo for testing, and more) with a **Copy settings to...** option, and Personal
  Mode can even route each announcement to its own channel.
- **Layout** — **Appearance** (Use Universal Settings for one look, or per-section background
  opacity / hide title bar / hide labels / button opacity, plus **click-through (display-only)**
  mode; values are kept **separately for windowed and detached mode**) and **per-section
  position, scale and appearance** for the current mode.
- **Colors** — recolour every element, with one-click **colourblind palette presets**
  (Deuteranopia / Protanopia / Tritanopia) as starting points you can fine-tune.
- **Text** — rename any panel label, section header, resolution callout (spread/stack, gaze,
  acceleration, chaos, Thunder/Blizzard), the **A/B/C/D target letters** or any button to your
  own custom text; blank keeps the default.
- **Controller** — hide the macro buttons and copy the per-button slash commands
  (see [Controller Players](#controller-players)).
- **Hidden** — the unlock field for the optional Last Fake resolver (see below).

By default Snazzy P4 runs in **detached** mode — each panel is its own floating window — at a
compact scale. Use **Edit Layout** (drag any panel) or the **Sections** tab to arrange them,
and **Bring all windows on-screen** in the Layout tab if anything ends up off-screen.

## Hidden Settings

I previously mentioned that this plugin does not Resolve the Thunder/Blizzard Fake/Real Math.

However, after a lot of requests, I have added this feature as a hidden setting that you must unlock for it to be available.
I would much prefer no one to use this setting and for you to resolve it manually as intended, as this feature does not align with
the original goals of this plugin. This plugin was designed to ultimately reduce macro bloat and macro resolvement text to be more readable.
I never wanted this plugin to resolve anything automatically for you and I have tried my best to stay true to that.

In other words: I'd rather you didn't. It's there if you truly need it, but the plugin
is meant to *reduce* what you have to think about, not resolve the fight for you.

But I get it, this phase is hard. So if you REALLY require this then by all means do what you have to do; I am not going to tell you how to play the game.
I hope you can decide to respect my wishes and to not enable this setting. I do not believe anyone actually needs it, and I think you are doing a disservice
to yourself as a raider by choosing to use it. If you still do not care about any of this - go ahead and access the setting.

This hidden option, is unlocked through the `...` field at the bottom of settings, that turns the Kefka **Last Fake?** resolution buttons on. 
Type "i_need_it" inside of the text box and hit `Confirm`. A final message will appear to double confirm that you want to use the feature.
Hit `Confirm` at the bottom of the message if you still want it to be enabled.

**How it works:** this *does* resolve the Thunder/Blizzard fake/real for you — but only
from your button input, never by reading combat data. It adds a small **Last Fake?** toggle
button next to each of the Thunder and Blizzard resolution lines in the Kefka panel, for the
last set. You press the button to mark whether that line is the fake, and the resolution
text flips between **REAL** and **FAKE** accordingly. Nothing happens on its own — it only
updates when *you* press a button, so it still requires your input; it just does the flip
for you instead of you doing it in your head.

**Appearance:** once unlocked, the Hidden tab gains a **Last Fake toggles** section where you
can show the toggles as plain checkboxes or as custom REAL/FAKE buttons (with your own text,
size and opacity), and optionally pull them out into their own detached panel(s).

**Announce Last Fake:** the same section can add an **ANNOUNCE** button (floating, or dockable
to any side of the Kefka panel — top, bottom, left or right) that posts the current Kefka
values to a chat channel. Put `{KefkaThunder}`
and `{KefkaBlizzard}` in your message and they are replaced with the current value (REAL/FAKE,
both customisable; `?` if that mechanic hasn't been pressed).

**Controller commands:** once this feature is unlocked, the Last Fake toggles also get slash
commands (with Copy buttons in Settings → Controller Settings), so controller players can
flip them from macros just like the other buttons:

```
/snazzyp4 LastThunderReal    /snazzyp4 LastThunderFake
/snazzyp4 LastBlizzardReal   /snazzyp4 LastBlizzardFake
```

## Support

If Snazzy P4 makes your P4 pulls a little less painful and you want to support development, you can buy me a coffee:

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/snazzysosnazzy)

---

*made by snazz*
