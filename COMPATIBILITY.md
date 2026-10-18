# Forever compatibility - PadChat 0.2.2 Preview 8

Checked on 18 October 2026: WoW Forever **1.60.1.70450 / Interface 16001**.
Blizzard's US, EU, KR and TW patch endpoints report this build.

## Result

No compatibility code fix was identified. **Keep Preview 8; no new download is
required solely for this game build.** Voice users still need its matching Windows
companion. These source and automated checks do not certify live gameplay.

## What changed and what was checked

The complete Gethe Forever trees show 357 changed files since 70374. The review
checked all 54 relevant changed chat/channel/controller/binding/friend/API files,
including files beyond GitHub's normal 300-file comparison limit. Fifteen API/UI
files plus version.txt changed among the existing 50 pinned files.

Blizzard's controller Chat Channels menu now has native voice-channel
transcription and new focus/navigation behavior. Its transcription footer uses
the right shoulder button while that menu is focused. This is separate from
PadChat's Windows dictation. Default binding files are unchanged. PadChat does
not call the removed native chat-menu/reticle methods. Core chat-send, mapped
controller input, binding, party/friend/voice and restricted-frame contracts
remain unchanged in the published source (18 contracts checked).

All 15 exact published-source suites pass: opening/closing, keyboard and
controller options, saved bindings/modifiers, chat/whisper/channel destinations,
focus/draft cleanup, invitations, one-shot direct sending, speech recovery,
clipboard ownership, stale microphone requests and installer transactions.
The actual updated native Escape handler was checked for empty gamepad chat,
non-empty drafts, keyboard use and autocomplete priority. The release's secure
frame fixture matches the new source pin after line-ending normalization.

All 11 current GitHub assets retain their verified sizes/digests. Fresh addon
and source downloads match; the bundled and standalone installers match; fresh
CurseForge file 9191123 bytes match the tested addon. Prior exact-shipping frozen
speech backend, full installer startup/repair and microphone package tests apply
to these unchanged assets; those heavyweight tests were not repeated.

## Limits

No running game was controlled or restarted, no microphone captured and no
settings changed. Physical controllers, combat/repeated sessions, interaction
with the new native menus, mixed-DPI/fullscreen/monitor removal, a second fresh
PC and the reporter's audio-routing setup still need live acceptance. Earlier
intermittent input/chat symptoms do not have a confirmed root cause. Native
engine restrictions can change independently of the published UI source.

Forever only. Experimental multi-client support is not promoted. No larger model
or paid service is required.

Source pin: `172a387e3ab85b26e6640d3058d5ec1e6ccd4c63`.
Previous pin: `735496de78ebd30ce816a0637982c556987f258c`.

- [Blizzard development notes](https://us.forums.blizzard.com/en/wow/t/2360696/6)
- [Pinned Forever source](https://github.com/Gethe/wow-ui-source/tree/172a387e3ab85b26e6640d3058d5ec1e6ccd4c63)
- [Installer and downloads](https://github.com/JEMPAS-FORGE/PadChat/releases/tag/v0.2.2-preview.8)
- [Installation guide](README.md)
