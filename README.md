# Snazzy P4

A robust custom macro system for the Kefka UMAD/DMU Phase 4 mechanics. You tap the
buttons for what you see in the fight, and the plugin turns them into short, readable
callouts (spread/stack targets, gaze, chaos and Kefka) so you can cut the macro bloat.

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
                       in which they will occur. This includes the following (In this exact order):
					       1. Spread / Stack
                           2. Move / Stand Still
                           3. Gaze
                           4. Twister / Donut
- **Party Callouts** — Optional sending of messages in party chat for Gazes and Chaos.
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
6. Press **RESET** between pulls to clear everything.

That's it — the buttons grey out when they no longer apply, so you can't mis-enter.

### Moving things around

- **Edit Layout** (toolbar or settings): the buttons lock and every panel fills with
  sample text; click-and-drag any panel to reposition it. Turn it off when done.
- **Detached** mode makes each panel its own window. **Move All** (detached only) drags
  every window together.

## Configuration Settings

Open with the **Settings** button or `/snazzyp4 config`.

- **Role** — Support or DPS. Changes the spread/stack target letters and colours.
- **UI Scale** — global size; each section also has its own scale.
- **Auto-place marker** — optionally place `/mk` markers on yourself for spreads.
- **Party chat messages** — optionally announce gaze/chaos in party chat.
- **Layout** — Show toolbar, Detached windows, Edit layout, Move All, Floating Hide button.
- **Appearance** — Use Universal Settings (one look for everything) or per-section
  background opacity / hide title bar / hide labels / button opacity. Values are kept
  **separately for windowed and detached mode**, and only the current mode's controls
  are shown.
- **Color Accessibility Settings** — recolour every element (collapsed by default).
- **Sections** — per-section position, scale and (when not universal) appearance.
- **Reset layout to defaults** / **Restore ALL settings to defaults**.

## Hidden Settings

I previously mentioned that this plugin does not Resolve the Thunder/Blizzard Fake/Real Math.

However, after a lot of requests, I have added this feature as a hidden setting that you must unlock for it to be available.
I would much prefer no one to use this setting and for you to resolve it manually as intended, as this feature does not align with
the original goals of this plugin. This plugin was designed to ultimately reduce macro bloat and macro resolvement text to be more readable.
I never wanted this plugin to resolve anything automatically for you and I have tried my best to stay true to that.

In other words: I'd rather you didn't. It's there if you truly need it, but the plugin
is meant to *reduce* what you have to think about, not resolve the fight for you.

But I get it, this phase is hard. So if you REALLY require this due to laziness or just being bad, then by all means do what you have to do; I am not going to tell you how to play the game.
I hope you can decide to respect my wishes and to not enable this setting. I do not believe anyone actually needs it, and I think you are doing a disservice
to yourself as a raider by choosing to use it. If you still do not care about any of this - go ahead and access the setting.

This hidden option, is unlocked through the `...` field at the bottom of settings, that turns the Kefka **Last Fake?** resolution buttons on. 
Type "i_need_it" inside of the text box and hit `Confirm`. A final message will appear to double confirm that you want to use the feature.
Hit `Confirm` at the bottom of the message if you still want it to be enabled.

## Support

If Snazzy P4 makes your P4 pulls a little less painful and you want to support development, you can buy me a coffee:

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/snazzysosnazzy)

---

*made by snazz*
