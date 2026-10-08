# PadChat — keyboard, controller and local voice chat

0.2.2 Preview 1 for WoW Forever. Install the addon for a controller chat keyboard,
word suggestions, channel selection and friend invites. Keyboard players can use
the same optional Windows voice companion. Recognition runs locally without API
keys or speech-token charges.

## Install

Download [PadChat-Setup.exe](https://github.com/JEMPAS-FORGE/PadChat/releases/tag/v0.2.2-preview.1)
for both parts. Close WoW, run Setup, select the client folder containing WowB.exe
and Data, keep voice enabled, and choose your microphone and startup preference.
The CurseForge ZIP installs only the addon. Voice needs the matching Windows
installer. Read [START-HERE.txt](START-HERE.txt) for the complete setup and repair guide.

## Controls and options

- Click PadChat's controller/chat-bubble minimap icon for Options and Voice settings.
- F9 or /padchat opens/closes the keyboard; Escape, Close and B/Circle dismiss it.
- Native PlayStation touchpad opens the keyboard. Other controllers receive a
  first-use setup panel; choose an opening button and optional held trigger.
- In Voice settings, click Bind push-to-talk, press your key or middle/side mouse
  button, select a Windows microphone, and Save & reload somewhere safe.
- Default voice: tap F8 or Share/Create/Back/View to cycle channels; hold to dictate,
  release to send. Both keyboard and controller PTT can be changed in the game.
- Test microphone while Voice settings stays open: hold your saved PTT, speak and
  release. The acknowledged test reports level/words and never sends chat/invites.
- Optional review mode waits for another PTT press before sending. Escape or
  B/Circle cancels. Automatic release-to-send remains the default.

Malformed installer discovery entries are skipped with a Browse fallback. Existing
preferences are retained. Panels now fit smaller logical screen sizes. Status
shows the last acknowledged check, not continuous companion availability.

## Preview limits

Forever 1.60.1.70291 / interface 16001 only; Windows voice is English, x64.
Automated and isolated shipping checks pass. Physical inputs, protected chat,
visual layout and a second clean PC still need acceptance. See [VALIDATION.md](VALIDATION.md)
and [COMPATIBILITY.md](COMPATIBILITY.md). The installer is unsigned.
Report problems at [GitHub Issues](https://github.com/JEMPAS-FORGE/PadChat/issues),
including version, client build, controller type and what happened. Avoid posting
recordings, character names or private logs without checking them first.

## Build and test

Source package includes the exact addon/helper/installer sources and regression
tests. Read [BUILD.md](BUILD.md). MIT project license; bundled dependencies retain
their own licenses and model notices.
