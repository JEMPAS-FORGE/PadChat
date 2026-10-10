PadChat 0.2.2 Preview 2 — WoW Forever 1.60.1 / Interface 16001
Windows voice companion: Windows 10/11 x64, English, local recognition.

INSTALL BOTH PARTS
1. Close WoW. Run PadChat-Setup.exe. Choose the Forever client folder containing
   WowB.exe (the shared Data folder may be in its parent) (usually World of Warcraft\_classic_beta_).
   An arbitrary E:\Padchat folder is not a WoW game folder.
2. Keep Install voice typing enabled if you want speech. Choose whether the
   companion starts when you sign in to Windows, then Install.
3. Select a microphone Windows detects on the gaming PC. Start WoW and enable
   PadChat. Disable older ControllerKeyboard addons and old voice helpers.
4. Click PadChat's controller/chat-bubble minimap icon to open Options.

ADDON ONLY
The CurseForge ZIP installs the keyboard and in-game Options, not the Windows
speech engine. Download the matching PadChat-Setup.exe from:
https://github.com/JEMPAS-FORGE/PadChat/releases/tag/v0.2.2-preview.6

KEYBOARD AND CONTROLLER
F9 or /padchat opens/closes the keyboard. Native PlayStation touchpad click
opens it too. Finger sliding is not supported. Other controller identities get
a first-use Options panel you can navigate with D-pad and A/X. Choose an opening
button (for example R3) and an optional held trigger. Hold the modifier first.
Save opening controls. Share/Create/Back is reserved for voice by default.
Choose a combination that does not overlap your combat actions.
Inside Options, D-pad changes/selects, A/X activates, B/Circle closes.
The keyboard has selectable Options and Close keys; Escape also keeps the draft.

VOICE SETTINGS IN THE GAME
Choose Guided setup for the four steps: select a microphone, bind PTT, save
and reload, then speak a safe test phrase with the Voice panel still open.
Setup resumes after reload. Nothing is sent during the test.
Click the minimap icon > Voice settings, or type /padchat voice.
Click Bind push-to-talk, then press your preferred key or middle/side mouse
button. Ctrl/Alt/Shift modifiers are supported. Controller voice button and held
trigger/shoulder are separate choices. Select Microphone to cycle Windows inputs
reported by WoW; Refresh lists hotplug changes. "Keep current" preserves the
companion's existing input. A missing/ambiguous input is refused.
Save & reload UI applies voice choices through WoW's SavedVariables. Do this
somewhere safe. Pending edits are not the active shortcut until saved and applied.

BINDING WARNINGS AND WOW WORDS
A binding already used by WoW (such as F8 for a bag) shows the existing action.
Choose a different shortcut, or explicitly accept the conflict before saving.
Controller primary buttons are reserved even with a held trigger. Saved WoW
bindings are not rewritten. Closing Options discards pending edits/acceptance.
WoW words / names lets you type optional comma-separated recognition hints.
Use up to 32 terms, 600 UTF-8 bytes total / 60 bytes per term. Put the most important names first: the decoder uses a short bounded hint list.
These are local hints, not automatic substitutions or invite commands; accuracy can still vary.
Save & reload applies hints. Clear them to use only the built-in WoW vocabulary.

TEST VOICE WITHOUT SENDING
Open Voice settings, click Test microphone / check companion, then hold your
CURRENT saved PTT shortcut while this panel stays open. Speak and release.
The companion reports ready/loading, shows the level/recognised words, and does
not submit chat or invitations. Closing the panel during a test still prevents
that test from being sent. If nothing responds, start PadChat Voice from Windows
Start, check the saved shortcut and microphone permissions, then try again.
The in-game status reflects the last acknowledged check, not a permanent promise
that the companion is still running. Tests are foreground-only.

SEND A MESSAGE
Outside Voice settings, tap your voice shortcut to cycle channels; hold to speak
and release to send. Default: F8 or PlayStation Create/Share, Xbox Back/View.
Select Say for your first real message. Say/Yell/General/Party/Raid/Guild and
available whisper reply targets are supported. Offline/ambiguous invites are
guarded; use the friend list if recognition mishears a name.

OPTIONAL REVIEW
In Voice settings change Send to "review, then press PTT again", Save & reload.
After dictating, inspect the preview, then press PTT again to send.
Escape or controller B/Circle cancels. Focus loss/disconnect also cancels.
Automatic release-to-send remains the default and existing choice is preserved.

RECONNECT A MICROPHONE OR CONTROLLER
Interrupted audio or controller identity changes cancel affected speech; stale
messages are discarded. Microphone errors refresh the speech helper; reconnect
the same saved microphone, release PTT and press again. Missing/ambiguous inputs
never silently switch to another microphone. If still missing, Refresh and select
the input again in Voice settings. Keyboard PTT remains independent of controller
reconnection. Only a fresh press begins another message.

REPAIR / PRIVACY / LIMITS
Close WoW and rerun Setup for update/repair. Microphone, voice preferences,
learned words and other addons are preserved. Malformed discovery metadata is
skipped rather than closing Setup; use Browse if automatic discovery is empty.
Recognition runs locally: no API key, model download or paid speech account.
No recordings or recognition history are written by the microphone test. The
companion uses/restores the clipboard only for bounded addon-owned handshakes
and normal native chat delivery. Game sound is independent of PadChat.
This is an unsigned preview. Automated checks pass; live 70291 acceptance and a
separate clean PC still need verification. No other WoW client support claimed.
