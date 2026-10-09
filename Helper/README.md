# PadChat — keyboard, controller and local voice chat

Four improvements for WoW Forever: better vocabulary hints, PTT conflict warnings,

guided microphone/setup testing and safer microphone/controller reconnect handling.

**Forever 1.60.1.70291 / Interface 16001. Windows voice: 10/11 x64, English.**

## Install and set up voice

1. Download **PadChat-Setup.exe** from [the matching release](https://github.com/JEMPAS-FORGE/PadChat/releases/tag/v0.2.2-preview.3). Close WoW and run it; choose the folder

   containing **WowB.exe (the shared Data folder may be in its parent)**, keep voice enabled and select startup preference.

2. Choose a Windows microphone when prompted, then start WoW and enable PadChat.

3. Click PadChat's controller/chat-bubble minimap icon > **Voice settings > Guided setup**.

4. Select your microphone, click **Bind push-to-talk** and press your key or middle/

   side mouse button. Controller primary and held trigger/shoulder are choices too.

5. Resolve any binding conflict or explicitly accept it, then **Save & reload UI**

   somewhere safe. Hold your saved PTT with Voice settings open, speak and release.

   This test shows recognised words and sends no chat or invitations.

The **CurseForge ZIP is addon-only**. Speech requires the matching Windows companion;

voice users must update both parts. Existing preferences are preserved. Recognition

is local, without API keys or paid speech tokens. Use one voice helper at a time.

## Everyday controls

Outside Voice settings, tap PTT to cycle channels; hold to speak; release to send.

Defaults: F8 for keyboard, Share/Create or Back/View for controllers. Change them

in Voice settings. Optional review waits for another PTT press; Escape/B/Circle

cancels. F9/native PlayStation touchpad opens the chat keyboard; other controllers

can choose an opening button/modifier in Options. D-pad selects; A/X activates.

**WoW words / names** accepts optional comma-separated recognition hints. Put the most important names first; only a short bounded hint list is used. They

are not guaranteed substitutions or invite targets. Save & reload applies them.

Interrupted capture/source changes discard affected speech; reconnect the same

saved input, release PTT and press again. No automatic mic switching or stale send.

If a device stays missing, use Refresh/select in Voice settings. If the companion

does not respond, start PadChat Voice from Windows Start and check mic permissions.

## Verification and limits

Exact source, frozen speech engine, compiled companion and shipping installer

automated checks pass. Installation/startup operations use isolated simulated

fixtures, and speech tests use synthetic audio. **A separate fresh PC was unavailable**;

physical devices/reconnects, protected live chat, visuals and real Windows startup

still need acceptance. This remains an **unsigned preview**, Forever only.

Read START-HERE.txt, VALIDATION.md and CHANGELOG.md below; checksums match assets.

Report problems at [GitHub Issues](https://github.com/JEMPAS-FORGE/PadChat/issues).

For source builds and tests see BUILD.md. MIT license; dependencies retain their own notices.

