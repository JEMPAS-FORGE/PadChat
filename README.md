# PadChat - installation and voice setup

PadChat is a controller chat keyboard for WoW Forever with optional free, offline voice typing for keyboard, mouse and controller players.

**Current release: 0.2.2-preview.13. Forever 1.60.x; Windows 10/11 x64; English voice.**

## Choose your download

- **For voice:** [Download PadChat-Setup.exe](https://github.com/JEMPAS-FORGE/PadChat/releases/download/v0.2.2-preview.13/PadChat-Setup.exe). It includes the addon, Windows companion and speech models. No Python install, API key, ChatGPT subscription or paid speech service is needed.
- **Complete ZIP:** [Preview 13 downloads](https://github.com/JEMPAS-FORGE/PadChat/releases/tag/v0.2.2-preview.13). Extract All, then run PadChat-Setup.exe.
- **Addon only:** [CurseForge](https://www.curseforge.com/wow/addons/padchat). This does not install or update the Windows voice companion.
- **Printable tutorial:** [Installation guide](https://github.com/JEMPAS-FORGE/PadChat/blob/main/docs/PadChat-Installation-Guide.pdf).
- [Future releases](https://github.com/JEMPAS-FORGE/PadChat/releases) and [bug reports](https://github.com/JEMPAS-FORGE/PadChat/issues).

## Install or update

1. Close WoW before installing, updating or repairing.
2. Run PadChat-Setup.exe. If using the ZIP, extract it first.
3. Select the Forever client folder containing **Wow.exe**, usually `C:\Program Files (x86)\World of Warcraft\_forever_` or `E:\World of Warcraft\_forever_`.
4. Keep **Install voice typing** checked for speech. Choose whether the companion should start when you sign in to Windows, then install.
5. Choose your microphone in the companion window, save, and close that window. PTT pauses while Windows microphone/binding configuration windows are open.
6. Start WoW and enable PadChat at character selection. Disable older ControllerKeyboard addons and use one voice helper.

Do not select Interface\AddOns or a new folder named Padchat. Shared Data may remain in the parent World of Warcraft folder. Version-checked legacy Forever folders containing WowB.exe remain supported; setup prefers the launch folder when migrating an old beta installation. Retail and other Classic clients are not supported.

For manual addon-only installation, the result must be `_forever_\Interface\AddOns\PadChat\PadChat.toc`, without another nested PadChat folder. For voice, install both parts from the same release. Updating through CurseForge alone does not update the Windows companion. Updates preserve saved settings and other addons.

## Set up inside WoW

Open PadChat's controller/chat-bubble minimap button. Keyboard users can also use `/padchat voice`.

- **Controls:** choose the on-screen keyboard opening key, controller button and optional held modifier. Native PlayStation touchpad click can open it; F9 is optional. Other controller identities receive an opening-control setup panel.
- **Voice:** choose Voice On, microphone, saved PTT, controller button/modifier, startup preference and send-on-release or review-first mode. Click Bind push-to-talk, then press a keyboard key or middle/side mouse button. New users prefer F10 or another unused key; use the key actually shown in Options. Existing F8/custom bindings are preserved.
- **Appearance:** text size, top/centre/bottom position, mic meter and optional ready sound. The sound defaults to Off. These changes preserve recognition settings and do not restart the model when voice settings are unchanged.
- **Help:** safe microphone/PTT test, restart voice, failed-message recovery and optional WoW word/name hints. Guided setup is on this page.

D-pad directions follow the visible button layout. X/A selects and Circle/B goes back. Choice lists show the current value with `*`; up/down chooses, left/right pages, X/A accepts and Circle/B cancels. Mouse and controller share the same selection.

Page switches preserve pending edits. Save & reload UI applies them when safe. Closing with edits offers Save, Discard or Stay. A fresh companion waits for a saved keyboard/mouse PTT choice. Binding-conflict notices help avoid occupying a game action. Finish dictation before saving or reloading. Controls also includes Reload UI.

## Test without sending

1. Keep Options open, select Help > Test saved PTT / microphone, and close other chat/search boxes.
2. Hold your saved PTT, wait for green **READY - SPEAK NOW**, speak briefly, then release.
3. The test displays recognized text without sending chat or invitations. Test controller and keyboard/mouse inputs separately.

The ready cue means microphone samples have arrived, including silence. The meter shows signal level, not recognition accuracy. A connection error before recording does not establish a microphone fault.

## Dictate normally

Close Options and keep WoW selected. Tap PTT to cycle channel. Hold PTT, wait for the green cue, speak, then release to send. Default controller voice is Share / Create / Back; a held modifier is optional. The overlay shows the destination, including Trade when available. Check the destination before speaking. Losing focus or changing destination during dictation cancels sending.

Review-first mode waits for another PTT press after recognition before sending. Escape or Circle/B cancels. WoW word/name hints can help but do not guarantee recognition. Spoken invite commands remain subject to destination and name checks.

## If voice or sending fails

- Check Voice On, saved PTT and microphone. Close the Windows configuration windows and return focus to WoW.
- When safe, Help > Restart voice + reload UI restarts the companion. The Windows tray also has Restart voice. If it is closed, open PadChat Voice from the Start menu.
- Release PTT fully before retrying. Reconnect the saved headset; selecting a different microphone requires saving the new choice.
- Help > Copy last failed message asks for one PTT hold to retrieve it without recording. The companion copies it to the Windows clipboard; Use as draft opens it for editing without sending. The tray also has a copy action. Recovery lasts only in this companion session. An uncertain send may already be in chat: check first. There is no automatic resend or transcript file.
- If needed, close WoW and rerun the matching installer with voice enabled to repair.

## Privacy, compatibility and testing

Speech is processed locally. No audio recordings or transcript history are written by normal operation; temporary failed-message recovery is memory-only. Diagnostics may record technical status. No personal streaming setup is required.

The published CPU models/defaults are retained; this release does not switch everyone to a smaller model or enable GPU speech. Speed/accuracy depend on the PC, microphone and speech.

Forever interface 16001 and source build 1.60.1.70862 were reviewed. This is an unsigned preview. The new menu and D-pad were user-confirmed in game; matching automated source, helper and installer checks pass. New appearance settings, destination display and failed-message recovery still need broader live testing. A clean second PC was unavailable. Do not treat automated checks as proof of every controller or protected gameplay scenario.

For support, include both versions, controller model/USB or Bluetooth, saved PTT, microphone and exact failed step. Hide character names and private chat in screenshots. [Report an issue](https://github.com/JEMPAS-FORGE/PadChat/issues).
