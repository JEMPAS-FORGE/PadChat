# Forever compatibility - PadChat 0.2.2 Preview 8

Checked 5 November 2026: Forever source **1.60.1.70790 / Interface 16001**.
Blizzard US/EU/KR/TW download endpoints still report **1.60.1.70532**. Source
is ahead of regional downloads; this does not establish installed-client rollout.

**Keep Preview 8. No new download is required solely for this source update.**
No necessary compatibility fix was identified. Voice users need its matching
Windows companion, whether using keyboard or controller.

The complete source trees show five changed files since 70755. Four changed
API/chat/whisper-menu files were downloaded and checked against immutable Git
blob hashes. Blizzard now routes whispers to title-friends through character
whispers, while ordinary Battle.net friends retain their existing route.
PadChat already uses explicit validated character whispers; it does not call
the changed smart-whisper helpers. Unsupported Battle.net reply destinations
remain unsupported. Core sending/input/invitation contracts and default
bindings are unchanged.

All **15 exact published-source regression suites pass**, including opening/
closing, options and saved bindings/modifiers, channels/whispers, invitations,
focus/draft cleanup, direct sending, speech recovery, clipboard/microphone
request ownership and installer transactions. The actual native Escape handler
passes its four branches. Five cases of Blizzard's new title-friend and ordinary
Battle.net whisper helpers pass with mocked API/frame responses.

All 11 release assets retain matching sizes/digests. Fresh addon/source code
agree, the bundled installer matches standalone, and fresh CurseForge file
9191123 bytes match the tested addon. Existing full backend/installer/startup/
repair/microphone package evidence applies to unchanged binaries; heavyweight
tests were not repeated.

These are source and automated checks, not live gameplay certification. Physical
controllers, combat/repeated sessions, real title-friend messaging, native popup
interactions, monitor/fullscreen changes, a fresh second PC and the reporter's
audio setup remain unverified. No game was controlled or restarted and no
settings or microphone were used. Earlier intermittent PTT/normal Enter causes
remain unconfirmed. Engine restrictions may change independently of UI source.

Forever only. Experimental multi-client support is not promoted. No larger
speech model or paid service is required.

Source pin: `eac30af597e5aaa43a60f6675ef5fdcea100375f`.
Previous pin: `17ed2c3e8471cdc3806ab0d26a082be7c67d3d7c`.

- [Blizzard development notes](https://us.forums.blizzard.com/en/wow/t/2360696/10)
- [Pinned source](https://github.com/Gethe/wow-ui-source/tree/eac30af597e5aaa43a60f6675ef5fdcea100375f)
- [Installer and downloads](https://github.com/JEMPAS-FORGE/PadChat/releases/tag/v0.2.2-preview.8)
- [Installation guide](README.md)
