# Forever compatibility - PadChat 0.2.2 Preview 8

Checked 4 November 2026: Forever source **1.60.1.70755 / Interface 16001**.
Blizzard US/EU/KR/TW download endpoints still report **1.60.1.70532** at this
check. The source is ahead of those endpoints; this does not mean every
installed game has updated.

No necessary PadChat compatibility fix was found. **Keep Preview 8; no new
download is required solely for this source update.** Voice typing needs its
matching Windows companion, for keyboard and controller players.

## What was checked

Complete old/new UI source trees show 173 changed files. We checked 47 changed
API, chat, friends, controller, menu, popup and keybinding files against immutable
Git blob hashes. Eleven of the 50 pinned files change; eighteen core
chat/input/party/friend/voice/restricted-frame contracts and default bindings
remain unchanged.

Blizzard introduces navigation taint barriers, secure callbacks, popup return
owners and protection against stuck dismissal loops. PadChat does not use the
removed navigation/footer methods or mutate native navigation internals. It
reads the retained UI-focus state; its companion uses the native bound input
when returning control to the game. Other changes add account icon metadata,
refresh native invitation eligibility and improve controller action bars and
targeting. No changed PadChat dependency requires a code adjustment.

All **15 exact published-source regression suites pass**: opening/closing,
options and bindings/modifiers, channels/whispers, invitations, draft/focus
cleanup, direct sending, helper recovery, clipboard handling, microphone request
ownership and installer transactions. The actual native chat Escape handler
passes its four branches. Three cases of the updated native radial dismissal
loop pass with mocked frames; this does not test the engine's taint enforcement.

All 11 release assets retain matching sizes/digests. Fresh addon/source files
agree, the Windows ZIP installer matches the standalone installer, and fresh
CurseForge file 9191123 bytes match the tested addon. Prior full installer,
startup/repair, microphone and bundled speech backend checks apply to these
unchanged assets; those heavyweight tests were not repeated.

## Limits

These are source audits and automated checks, not live gameplay certification.
Physical controllers, combat/repeated sessions, new native popup interactions,
mixed-DPI/fullscreen/monitor removal, a second fresh PC and the reporter's audio
environment still need live acceptance. No running game was controlled or
restarted and no settings or microphone were used. Earlier intermittent input
and chat symptoms do not have a confirmed root cause. Engine restrictions can
change independently of UI source.

Forever only; experimental multi-client support is not promoted. No paid service
or larger speech model is required.

Source pin: `17ed2c3e8471cdc3806ab0d26a082be7c67d3d7c`.
Previous pin: `ca8368f2f2380cbe696854f7f2646387c278a61b`.

- [Blizzard development notes](https://us.forums.blizzard.com/en/wow/t/2360696/9)
- [Pinned Forever source](https://github.com/Gethe/wow-ui-source/tree/17ed2c3e8471cdc3806ab0d26a082be7c67d3d7c)
- [Installer and downloads](https://github.com/JEMPAS-FORGE/PadChat/releases/tag/v0.2.2-preview.8)
- [Installation guide](README.md)
