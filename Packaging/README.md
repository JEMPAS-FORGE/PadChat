# PadChat - installation and voice setup

PadChat adds a controller chat keyboard to WoW Forever, with optional free,
offline voice typing for keyboard, mouse and controller players.
**Current release: 0.2.2-preview.7. Windows voice: Windows 10/11 x64, English.**

## Start here: choose the right download

- **Want voice typing?** [Download PadChat-Setup.exe](https://github.com/JEMPAS-FORGE/PadChat/releases/download/v0.2.2-preview.7/PadChat-Setup.exe). It installs **both the addon and Windows voice companion**, including the speech models. You do not need to install Python, buy a speech account or enter an API key.
- **Want a PDF tutorial?** [Read or download the installation guide](https://github.com/JEMPAS-FORGE/PadChat/blob/main/docs/PadChat-Installation-Guide.pdf).
- **Want the complete ZIP?** [Open the Preview 7 release](https://github.com/JEMPAS-FORGE/PadChat/releases/tag/v0.2.2-preview.7), download `PadChat-Windows-Voice-0.2.2-preview.7.zip`, choose **Extract All**, then run the extracted `PadChat-Setup.exe`.
- **Want the keyboard without speech?** [CurseForge](https://www.curseforge.com/wow/addons/padchat) provides the **addon only**. It does not install or update the voice companion.

Use the installer from the same release as the addon. `PadChat-source.zip` is
for source code; it is not the installer. [All releases and future updates](https://github.com/JEMPAS-FORGE/PadChat/releases).

## 1. Install both parts

1. **Close WoW before installing, updating or repairing.**
2. Run the downloaded `PadChat-Setup.exe`.
3. Use **Browse** to select the **Forever client folder containing `WowB.exe`**.
   A typical path is `C:\Program Files (x86)\World of Warcraft\_classic_beta_`
   or `E:\World of Warcraft\_classic_beta_`.
4. Keep **Install voice typing** checked if you want speech.
5. Choose the startup preference. Enabling startup launches the companion when
   you sign in to Windows. If you leave it off, open **PadChat Voice** from the
   Windows Start menu before using voice.
6. Click **Install**. If Windows requests permission to write the game folder,
   approve it only if you intend to install there.
7. In the microphone window, select the microphone or headset you will use and
   save it. Close the companion's microphone and binding windows before testing
   PTT: voice is paused while those configuration windows are open.

**Folder check:** select `_classic_beta_`, not `Interface\AddOns`, the parent
`World of Warcraft` folder, or a new folder such as `E:\Padchat`. Shared `Data`
may stay in the parent folder; do not move it. Preview 7 supports Forever
1.60.x, not Retail or other Classic clients.

For an addon-only manual install, extract `PadChat-0.2.2-preview.7.zip` so the
file ends up at `_classic_beta_\Interface\AddOns\PadChat\PadChat.toc`, with
no extra nested `PadChat` folder. Voice still requires the Windows installer.

## 2. Enable PadChat and choose your controls

1. Start WoW. At character selection, open **AddOns** and enable **PadChat**.
   Disable older `ControllerKeyboard` / `ControllerKeyboardTouchpad` addons
   and do not run an older voice helper alongside PadChat Voice.
2. Enter the world. Click PadChat's **controller/chat-bubble minimap button**
   for Options. Keyboard users can type `/padchat options` or `/padchat voice`.
3. To open the on-screen keyboard, native PlayStation controllers use
   **touchpad click**. Other controller identities get a first-use Options
   panel; choose an opening button and optional held trigger/shoulder, then
   save. **F9 is an optional keyboard shortcut**, not a required first step.
4. Navigate Options with the D-pad, activate with X/A, and go back or close
   with Circle/B. Avoid controls already used for combat or voice.

## 3. Configure voice in the game

Open **Voice settings > Guided setup**, or type `/padchat voice`.

1. Set **Voice: On** and choose your microphone. It must be available to Windows
   on the PC running the companion. **Refresh** updates the available list;
   **Keep current** preserves the existing companion choice.
2. Click **Bind push-to-talk**, then press the key or middle/side mouse button
   you want. Ctrl/Alt/Shift modifiers are supported. The default is **F8**.
3. For controller voice, choose the controller button and an optional held
   trigger/shoulder. Default: **Share/Create/Back**, **Hold first: None**.
4. Check binding-conflict notices. Choose another control, or explicitly accept
   the conflict if the override is what you want.
5. When safe, choose **Save & reload UI**. Reopen Voice settings afterwards.
   Pending selections are not active until saved and applied by the companion.

## 4. Test without sending anything

Keep the in-game **Voice settings panel open**. Close normal chat/search boxes
and finish any binding capture. Select **Test microphone / check companion**.
Hold your **saved PTT**, speak a short phrase, then release.

The test shows connection/loading feedback, microphone level during recording
and recognised words. **It sends no chat or invitations.** Test your controller
PTT separately from your keyboard/mouse PTT. A failed connection check happens
before microphone recording and does not prove that your microphone is broken.

## 5. Use voice or the chat keyboard

Close Voice settings before sending a real message. With WoW selected:

- **Tap PTT** to cycle chat destinations.
- **Hold PTT** to speak, then **release to send**. Start with Say so the first
  message has a clear destination.
- **Escape** cancels dictation. Optional **review, then press PTT again** lets
  you inspect recognised words before sending; Escape or Circle/B cancels.
- **WoW words / names** accepts optional recognition hints. They are not
  guaranteed corrections or automatic substitutions.
- Open the PadChat keyboard with your saved opening control. D-pad/stick
  chooses a key; X/A selects. Its **Send**, **Options** and **Close** keys are
  selectable. Escape or Circle/B closes and preserves the draft.

Supported destinations include Say, Yell, General, Party, Raid, Guild and
available whisper reply targets. Speech recognition can mishear words or names.

## If voice stops responding

1. Check **Voice: On**, the saved PTT and microphone in Voice settings. Return
   focus to WoW and close Windows companion configuration windows.
2. If there is no response, open Windows' hidden tray icons beside the clock,
   right-click PadChat's icon, and choose **Exit PadChat voice**. Then open
   **PadChat Voice** from Windows Start. **WoW can stay running for this restart.**
3. Release PTT and press it again. If the headset was disconnected, reconnect
   the same saved microphone, or select the intended input and save when safe.
4. If it still fails, **close WoW** and rerun the matching installer to repair.
   Keep voice enabled. Updating only through CurseForge does not repair the
   Windows companion. Existing microphone/PTT preferences and other addons
   are preserved by the installer.

There is currently no in-game Restart voice button. The connection status is
the **last acknowledged check**, not a continuous guarantee. A recognition
timeout discards the affected recording; release PTT before trying again.

## Updates and help

- [Matching Preview 7 installer and downloads](https://github.com/JEMPAS-FORGE/PadChat/releases/tag/v0.2.2-preview.7)
- [Detailed setup reference](https://github.com/JEMPAS-FORGE/PadChat/blob/main/START-HERE.txt)
- [Report a problem or attach a screenshot/video](https://github.com/JEMPAS-FORGE/PadChat/issues)
- [CurseForge comments](https://www.curseforge.com/wow/addons/padchat/comments)

For a report, include the addon/companion versions, controller model and
USB/Bluetooth connection, saved PTT, microphone and exact error or failed step.
Hide character names and private chats in screenshots and videos.

The installer is an **unsigned preview**. Tested shipping installer/addon checks
pass; fresh-PC, broader physical-controller and protected live-game acceptance
remain incomplete. Original code is MIT licensed; bundled dependencies have
their own notices. Recognition is local and needs no paid speech service.
[Build instructions](BUILD.md), [validation](VALIDATION.md),
[changes](CHANGELOG.md) and [license](LICENSE) are included in the repository.
