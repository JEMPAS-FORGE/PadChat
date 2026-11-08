# Forever compatibility - PadChat 0.2.2 Preview 8

Checked 8 November 2026: Forever source **1.60.1.70862 / Interface 16001**.
Blizzard US/EU/KR/TW download endpoints still report **1.60.1.70532**. Source
is ahead of regional downloads; this does not confirm installed-client rollout.

**Keep Preview 8. No new download is required solely for this source update.**
No required compatibility fix was identified. Voice users still need the matching
Windows companion for keyboard or controller voice typing.

Complete source trees show nine changed files since 70790. All eight changed UI
files were fetched and checked against immutable Git blob hashes. Blizzard adds
a controller focus lock for certain native top-level screens, including the
barber shop, and keeps the navigation cursor attached to alternate UI parents.
PadChat reads the retained focus flag and uses a normal native bound action to
return control. It must respect native modal locks. Chat sending, saved binding
and invitation contracts remain unchanged; no bypass was added.

All **15 exact published-source regression suites pass**, covering opening/
closing/options, bindings/modifiers, chat destinations, invitations, focus/draft
cleanup, direct sending, voice recovery, clipboard/microphone request ownership
and installer transactions. Actual native Escape branches pass. Four cases of
the updated native focus-release handler pass with mocked frames, including
preserving a locked top-level parent's focus. These are not engine security or
physical controller tests.

All 11 release assets retain matching hashes/sizes. Fresh addon/source code
agree, bundled and standalone installers match, and fresh CurseForge file
9191123 bytes match the tested addon. Prior full backend/installer/startup/
repair/microphone tests apply to unchanged packages; those heavyweight checks
were not repeated.

Source audits and automated tests do not certify live gameplay. Physical
controllers, combat/repeated sessions, interaction with native locked modals,
popup handling, monitors/fullscreen, a fresh PC and the reporter's audio setup
still need live acceptance. Close native modal screens normally before returning
to gameplay. No running game was controlled or restarted and no microphone or
settings were changed. Earlier intermittent PTT/Enter causes remain unconfirmed.

Forever only; experimental multi-client support is not promoted. No larger model
or paid service is required.

Source pin: `9dc84f0d3bbae1768ac3405b3716000bd93eda06`.
Previous pin: `eac30af597e5aaa43a60f6675ef5fdcea100375f`.

- [Blizzard development notes](https://us.forums.blizzard.com/en/wow/t/2360696/10)
- [Pinned source](https://github.com/Gethe/wow-ui-source/tree/9dc84f0d3bbae1768ac3405b3716000bd93eda06)
- [Installer and downloads](https://github.com/JEMPAS-FORGE/PadChat/releases/tag/v0.2.2-preview.8)
- [Installation guide](README.md)
