# PadChat

Controller chat keyboard with optional local voice typing for both keyboard
and controller players. Original code, MIT licence.

0.3.0 preview targets current Retail, Era/Hardcore, Anniversary, progression
Classic and Forever. See COMPATIBILITY.md for exact audited builds and test
limits. New client families are experimental until live gameplay checks pass.

Install in Interface/AddOns/PadChat. For English voice typing on Windows 10/11
x64, use the complete Windows installer, once for each client folder. One shared
voice helper and microphone choice. Close all WoW clients before installation.
Disable older ControllerKeyboard addons/helpers to avoid conflicts.

- F9 or /padchat: open/close outside combat.
- Tap F8: channel. Hold F8: dictate. Release: send.
- PlayStation Create/Share or Xbox Back/View: the same voice action.
- Native PlayStation touchpad click: open keyboard; sliding is not supported.
- D-pad/stick: choose; X/A: select; Square/X: delete; Triangle/Y: space.
- Circle/B, Escape or Close: close and preserve the draft.
- L1/LB: channel; R1/RB: words; L2/LT: shift; R2/RT: letters/numbers.
- Options/Start or Send: send the draft.

Select Options on the bottom row or use /padchat options. D-pad up/down selects
a setting, left/right changes it, X/A activates, Circle/B closes. Choose a
keyboard shortcut, controller button and optional held shoulder/trigger.
Share conflicts are shown. These options do not change combat or voice binds.

Voice uses a microphone recognised by Windows and bundled English models.
No API keys or paid tokens. The addon alone cannot record speech; voice needs
the optional Windows companion. Game audio is independent. Learned words remain
in each client's SavedVariables. Repeat Setup for each folder when updating;
voice/startup settings are shared. Uninstall removes all recorded PadChat folders.
