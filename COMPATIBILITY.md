# Forever compatibility - PadChat 0.2.2 Preview 8

Checked on 23 October 2026: WoW Forever **1.60.1.70532 / Interface 16001**.
Blizzard's US, EU, KR and TW patch endpoints report this build.

## Result

No compatibility code fix was identified. **Keep Preview 8; no new download is
required solely for this game build.** Voice users still need its matching Windows
companion. These source and automated checks do not certify live gameplay.

## What changed and what was checked

The complete Gethe Forever trees show 19 changed files since 70450. The review
checked all six relevant changed controller/popup/guild-invite files. Two API/UI
files plus version.txt changed among the existing 50 pinned files.

Blizzard removes the native tooltip-toggle function/right-stick prompt and
moves native popup extra actions from right stick to D-pad Down. Guild invitation
windows gain native controller focus and accept/decline bindings. PadChat does
not call the removed tooltip function or these native popup internals. Its own
bindings and controller choices remain supported by the source checks. Default
Bindings.xml files and core chat-send, mapped controller input, binding,
party/friend/voice and restricted-frame contracts remain unchanged (18 core files
checked). Executing the actual old/new popup mapping confirms the four face
buttons are unchanged and only the extra-button mapping moved. Real simultaneous
popup/controller focus interaction remains unverified.

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

Source pin: `ca8368f2f2380cbe696854f7f2646387c278a61b`.
Previous pin: `172a387e3ab85b26e6640d3058d5ec1e6ccd4c63`.

- [Blizzard development notes](https://us.forums.blizzard.com/en/wow/t/2360696/6)
- [Pinned Forever source](https://github.com/Gethe/wow-ui-source/tree/ca8368f2f2380cbe696854f7f2646387c278a61b)
- [Installer and downloads](https://github.com/JEMPAS-FORGE/PadChat/releases/tag/v0.2.2-preview.8)
- [Installation guide](README.md)
